using CleanerControlApp.Hardwares.Shuttle.Interfaces;
using CleanerControlApp.Hardwares.Shuttle.Models;
using CleanerControlApp.Modules.DeltaMS300.Interfaces;
using CleanerControlApp.Modules.DeltaMS300.Services;
using CleanerControlApp.Modules.MitsubishiPLC.Interfaces;
using CleanerControlApp.Modules.Motor.Interfaces;
using CleanerControlApp.Modules.TempatureController.Interfaces;
using CleanerControlApp.Utilities;
using CleanerControlApp.Utilities.Alarm;
using CleanerControlApp.Utilities.Log;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Hardwares.Shuttle.Services
{
    public class Shuttle : IShuttle, IDisposable
    {

        #region attribute

        private readonly ILogger<Shuttle>? _logger;

        // background loop
        private CancellationTokenSource? _cts;
        private Task? _loopTask;
        private readonly TimeSpan _loopInterval = TimeSpan.FromMilliseconds(10);


        private bool _running;

        private readonly UnitSettings _unitSettings;
        private readonly ModuleSettings _moduleSettings;

        private readonly IPLCOperator? _plcService;
        private readonly ISingleAxisMotor? _motorXAxis;
        private readonly ISingleAxisMotor? _motorZAxis;

        private bool _auto = false;
        private bool _pausing = false;
        private bool _moving = false;
        private bool _cassette = false;
        private bool _initialized = false;
        private bool _initializing = false;
        private bool _autoStopFlag = false;

        private bool _sim_pass_motor = false;
        private bool _sim_pass_clamper = false;

        private static int _clamperTimeoutThreshold_Value = 30;

        private bool _pickTrigger = false;
        private bool _placeTrigger = false;
        private bool _pickProcedureError = false;
        private bool _placeProcedureError = false; 
        private int _pickCase = 0;
        private int _placeCase = 0;
        private int _actPositionX = 0;
        private int _actVelocityX = 0;
        private int _actPositionZ = 0;
        private int _actVelocityZ = 0;

        private bool _checkCassetteTrigger = false;
        private bool _checkCassetteProcedureError = false;
        private int _checkCassetteCase = 0;

        private bool _dryRun = false;
        private bool _semiRun = false;

        // Timeout handling for pick/place/check triggers
        private DateTime? _triggerStartTime = null;
        private readonly TimeSpan _triggerTimeout = TimeSpan.FromMinutes(5);

        // Flags raised when timeout occurs; caller will handle these alarms later
        private bool _triggerTimeoutAutoAlarm = false;
        private bool _triggerTimeoutDrySemiAlarm = false;

        // Track OP mode transitions: do not execute the manual-entry actions on the very first read after startup.
        private bool _prevOpModeManual = false;
        private bool _opModeFirstRead = true;

        // Delay CheckOPMode execution for 10 seconds after startup
        private DateTime? _loopStartTime = null;
        private readonly TimeSpan _opModeCheckDelay = TimeSpan.FromSeconds(10);

        // Move-end delay tracking timestamps (non-blocking)
        private DateTime? _moveEndDelayStart;
        private DateTime? _clampEndDelayStart;

        private string _messageForPickPlace = string.Empty;
        private bool _passClamperCheckCassette = false;
        private DateTime? _alarmCheckStartTime = DateTime.UtcNow;

        #endregion

        #region constructor

        public Shuttle(
            ILogger<Shuttle>? logger,
            UnitSettings unitSettings,
            ModuleSettings moduleSettings,
            IPLCOperator? plcService,
            ISingleAxisMotor? motorXAxis,
            ISingleAxisMotor? motorZAxis
            )
        {
            _logger = logger;
            try
            {
                _unitSettings = unitSettings;
                _moduleSettings = moduleSettings;
                _plcService = plcService;
                _motorXAxis = motorXAxis;
                _motorZAxis = motorZAxis;

                RefreshTimeoutValue();

                // Alarm
                AlarmManager.AttachFlagGetter("ALM101", () => _ClamperF_Open_Timeout);
                AlarmManager.AttachFlagGetter("ALM102", () => _ClamperF_Close_Timeout);
                AlarmManager.AttachFlagGetter("ALM103", () => _ClamperB_Open_Timeout);
                AlarmManager.AttachFlagGetter("ALM104", () => _ClamperB_Close_Timeout);
                AlarmManager.AttachFlagGetter("ALM105", () => _motorXAlarm);
                AlarmManager.AttachFlagGetter("ALM106", () => _motorXAlarmLimitN);
                AlarmManager.AttachFlagGetter("ALM107", () => _motorXAlarmLimitP);
                AlarmManager.AttachFlagGetter("ALM108", () => _motorXAlarmHomeTimeout);
                AlarmManager.AttachFlagGetter("ALM109", () => _motorXAlarmMoveTimeout);
                AlarmManager.AttachFlagGetter("ALM110", () => _motorZAlarm);
                AlarmManager.AttachFlagGetter("ALM111", () => _motorZAlarmLimitN);
                AlarmManager.AttachFlagGetter("ALM112", () => _motorZAlarmLimitP);
                AlarmManager.AttachFlagGetter("ALM113", () => _motorZAlarmHomeTimeout);
                AlarmManager.AttachFlagGetter("ALM114", () => _motorZAlarmMoveTimeout);
                AlarmManager.AttachFlagGetter("ALM115", () => _pickProcedureError);
                AlarmManager.AttachFlagGetter("ALM116", () => _placeProcedureError);
                AlarmManager.AttachFlagGetter("ALM117", () => _checkCassetteProcedureError);
                // Trigger timeout alarms
                AlarmManager.AttachFlagGetter("ALM118", () => _triggerTimeoutAutoAlarm);
                AlarmManager.AttachFlagGetter("ALM119", () => _triggerTimeoutDrySemiAlarm);

                StartLoop();

                Start();
            }
            catch (Exception ex)
            {
                try { _logger?.LogError(ex, "Shuttle constructor failed"); } catch { }
                try { System.Windows.MessageBox.Show($"Shuttle ctor exception: {ex}", "Startup Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error); } catch { }
                throw;
            }
        }

        #endregion

        #region Destructor and Dispose pattern

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // stop background loop
                    try
                    {
                        _cts?.Cancel();
                        if (_loopTask != null)
                        {
                            _loopTask.Wait(500);
                        }
                    }
                    catch (AggregateException) { }
                    catch (Exception) { }
                    finally
                    {
                        _cts?.Dispose();
                        _cts = null;
                        _loopTask = null;
                    }

                    // TODO: 處置受控狀態 (受控物件)
                }

                // TODO: 釋出非受控資源 (非受控物件) 並覆寫完成項
                // TODO: 將大型欄位設為 Null
                disposedValue = true;
            }
        }

        // TODO: 僅有當 'Dispose(bool disposing)' 具有會釋出非受控資源的程式碼時，才覆寫完成項
        ~Shuttle()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region IShuttle

        public bool IsRunning => _running;
        public void Start()
        {
            _running = true;
        }
        public void Stop()
        {
            _running = false;
        }

        public ISingleAxisMotor? ShuttleXMotor => _motorXAxis;
        public ISingleAxisMotor? ShuttleZMotor => _motorZAxis;

        public bool Sensor_ClamperFrontOpen => _plcService != null && _plcService.ShuttleZFClamperOpen;
        public bool Sensor_ClamperFrontClose => _plcService != null && _plcService.ShuttleZFClamperClose;
        public bool Sensor_ClamperBackOpen => _plcService != null && _plcService.ShuttleZBClamperOpen;
        public bool Sensor_ClamperBackClose => _plcService != null && _plcService.ShuttleZBClamperClose;
        public bool Sensor_ClamperOpen => _plcService != null && _plcService.ShuttleZClamperOpenSign;
        public bool Sensor_ClamperClose => _plcService != null && _plcService.ShuttleZClamperCloseSign;
        public bool Sensor_OpMode_Auto => Sensor_ClamperOpen;
        public bool Sensor_OpMode_Manual => Sensor_ClamperClose;

        public bool Check_ClamperOpen => Sensor_ClamperFrontOpen && Sensor_ClamperBackOpen;
        public bool Check_ClamperClose => Sensor_ClamperFrontClose && Sensor_ClamperBackClose;

        public bool Sensor_CassetteExist1 => _plcService != null && _plcService.ShuttleZClamperExist1;
        public bool Sensor_CassetteExist2 => _plcService != null && _plcService.ShuttleZClamperExist2;
        public bool Check_ClamperCassetteExist => Sensor_CassetteExist1;
        public bool Check_TankCassetteExist => Sensor_CassetteExist2;

        public bool Command_ClamperClose
        { 
            get => _plcService != null && _plcService.Command_ShuttleZClampClose;
            set { if (_plcService != null) _plcService.Command_ShuttleZClampClose = value; }
        }
        public bool Command_ClamperOpen
        { 
            get => _plcService != null && _plcService.Command_ShuttleZClampOpen;
            set { if (_plcService != null) _plcService.Command_ShuttleZClampOpen = value; }
        }

        public bool Auto => _auto;
        public bool Pausing => _pausing;
        public bool Moving => _moving || _pickTrigger || _placeTrigger || _checkCassetteTrigger;
        public bool Cassette => _cassette;
        public bool Initialized => _initialized && (_sim_pass_motor || MotorHome);
        public bool Initializing => _initializing;
        public bool Idle => !_moving && !_cassette && _initialized && IsNormalStatus && (_sim_pass_motor || (MotorServoOn && MotorIdle && MotorHome));

        public bool ClamperCloseOP(bool close)
        {
            bool result = false;

            if(_plcService != null)
            {
                Command_ClamperClose = close;
                if (close && Command_ClamperOpen) Command_ClamperOpen = false;
                result = true;
            }

            return result;
        }
        public bool ManualClamperCloseOP(bool close)
        {
            bool result = false;

            if(_plcService != null)
            {
                result = ClamperCloseOP(close);
                OperateLog.Log("手動 關閉夾爪 " + (close ? "ON" : "OFF"), result ? "Success" : "Failed");
            }

            return result;
        }
        public bool ClamperOpenOP(bool open)
        {
            bool result = false;
            if(_plcService != null)
            {
                Command_ClamperOpen = open;
                if (open && Command_ClamperClose) Command_ClamperClose = false;
                result = true;
            }
            return result;
        }
        public bool ManualClamperOpenOP(bool open)
        {
            bool result = false;
            if(_plcService != null)
            {
                result = ClamperOpenOP(open);
                OperateLog.Log("手動 開啟夾爪 " + (open ? "ON" : "OFF"), result ? "Success" : "Failed");
            }
            return result;
        }

        public bool HasWarning => _ClamperB_Close_Timeout || _ClamperF_Close_Timeout || _ClamperB_Open_Timeout || _ClamperB_Open_Timeout || _motorXAlarmMoveTimeout || _motorZAlarmMoveTimeout || _motorXAlarmHomeTimeout || _motorZAlarmHomeTimeout;
        public bool HasAlarm => MotorAlarm || _motorXAlarmLimitN || _motorXAlarmLimitP || _motorZAlarmLimitN || _motorZAlarmLimitP || _checkCassetteProcedureError || _pickProcedureError || _placeProcedureError;
        
        public bool IsNormalStatus => !HasWarning && !HasAlarm;
        public void AutoStop()
        {
            if (_auto)
            {
                _autoStopFlag = true;
            }
        }
        public void WarningStop()
        {
            _pausing = true;
            MotorStop();
        }
        public void AlarmStop()
        {
            EMOStop();
        }
        public void AutoStart()
        {
            _autoStopFlag = false;
            _pausing = false;
            _dryRun = false;
            if (!_auto && _initialized && IsNormalStatus)
            {
                _auto = true;
                ActStartStatus();
            }
        }
        public void AutoPause()
        {
            if (_auto && !_pausing)
            {
                _pausing = true;
                MotorStop();
            }
        }
        public void AlarmReset()
        {
            ResetTimeoutFlag();
            ResetMotorAlarm();
        }
        public void ModuleReset()
        {
            Initialize();
        }

        public void SimMotorPass()
        {
            _sim_pass_motor = !_sim_pass_motor;
            _motorXAxis?.SimMotorPass(_sim_pass_motor);
            _motorZAxis?.SimMotorPass(_sim_pass_motor);

        }
        public void SimClamperPass()
        { 
            _sim_pass_clamper = !_sim_pass_clamper;
        }

        public void AllMotorStop()
        {
            MotorStop();
        }


        public bool MotorServoOn => _motorXAxis != null && _motorXAxis.MotorServoOn && _motorZAxis != null && _motorZAxis.MotorServoOn;
        public bool MotorIdle => _motorXAxis != null && _motorXAxis.MotorIdle && _motorZAxis != null && _motorZAxis.MotorIdle;
        public bool MotorBusy => (_motorXAxis != null && _motorXAxis.MotorBusy) || (_motorZAxis != null && _motorZAxis.MotorBusy);
        public bool MotorAlarm => _motorXAlarm || _motorZAlarm;
        public bool MotorHoming => (_motorXAxis != null && _motorXAxis.MotorHoming) || (_motorZAxis != null && _motorZAxis.MotorHoming);
        public bool MotorMoving => (_motorXAxis != null && _motorXAxis.MotorMoving) || (_motorZAxis != null && _motorZAxis.MotorMoving);
        public bool MotorHome => (_motorXAxis != null && _motorXAxis.MotorHome && _motorZAxis != null && _motorZAxis.MotorHome) || _sim_pass_motor;

        public bool ZInIdlePosition => _motorZAxis != null && _motorZAxis.GetInPos(0);

        public bool HasCassette => Check_ClamperClose && Check_ClamperCassetteExist;
        public bool IsEmpty => Check_ClamperOpen && !Check_ClamperCassetteExist;

        // position 1~14 for pick/place position, 0 for original position
        public bool PickCassette(int position, bool dryRun = false, bool semiRun = false)
        {
            bool result = false;
            _messageForPickPlace = string.Empty;

            if ((IsEmpty || _sim_pass_clamper || dryRun || (semiRun && !Cassette) || (_passClamperCheckCassette && _auto)) && !Moving && !MotorMoving && IsNormalStatus && MotorHome && ZInIdlePosition && Check_ClamperOpen)
            {
                if (position > 0 && position < 15)
                {
                    GetActParameters(position, out _actPositionX, out _actVelocityX, out _actPositionZ, out _actVelocityZ);
                    _pickTrigger = true;
                    _dryRun = dryRun;
                    _semiRun = semiRun;
                    result = true;
                }
                else
                    _messageForPickPlace = $"Pick Procedure Start Error: 無效的位置參數: {position}。請提供1~14的值。";
            }
            else
                _messageForPickPlace = $"Pick Procedure Start Error: 條件不符。IsEmpty(true)={IsEmpty}, Moving(false)={Moving}, MotorMoving(false)={MotorMoving}, IsNormalStatus(true)={IsNormalStatus}, MotorHome(true)={MotorHome}, ZInIdlePosition(true)={ZInIdlePosition}, Check_ClamperOpen(true)={Check_ClamperOpen}.";

            return result;
        }
        public bool PlaceCassette(int position, bool dryRun = false, bool semiRun = false)
        {
            bool result = false;
            _messageForPickPlace = string.Empty;

            if ((HasCassette || _sim_pass_clamper || dryRun || (semiRun && Cassette) || (_passClamperCheckCassette && _auto)) && !Moving && !MotorMoving && IsNormalStatus && MotorHome && ZInIdlePosition && Check_ClamperClose)
            {
                if (position > 0 && position < 15)
                {
                    GetActParameters(position, out _actPositionX, out _actVelocityX, out _actPositionZ, out _actVelocityZ);
                    _placeTrigger = true;
                    _dryRun = dryRun;
                    _semiRun = semiRun;
                    result = true;
                }
                else
                    _messageForPickPlace = $"Place Procedure Start Error: 無效的位置參數: {position}。請提供1~14的值。";
            }
            else
                _messageForPickPlace = $"Place Procedure Start Error: 條件不符。HasCassette(true)={HasCassette}, Moving(false)={Moving}, MotorMoving(false)={MotorMoving}, IsNormalStatus(true)={IsNormalStatus}, MotorHome(true)={MotorHome}, ZInIdlePosition(true)={ZInIdlePosition}, Check_ClamperClose(true)={Check_ClamperClose}.";

            return result;
        }
        public bool BackToOriginalPosition()
        {
            bool result = false;
            if (!Moving && !MotorMoving && IsNormalStatus && MotorHome && _motorXAxis != null)
            {
                _motorXAxis.MoveToPosition(0, 1);
                result = true;
            }
            return result;
        }


        public bool CheckTankCassetteExist()
        {
            bool result = false;

            if (!Moving && !MotorMoving && IsNormalStatus && MotorHome && ZInIdlePosition)
            {
                HS_Check_Cassette_Finished = false;

                _checkCassetteTrigger = true;
                result = true;
            }

            return result;
        }

        public bool HS_Check_SinkCassetteExist { get; set; }
        public bool HS_Check_SoakingTankCassetteExist { get; set; }
        public bool HS_Check_DryingTank1CassetteExist { get; set; }
        public bool HS_Check_DryingTank2CassetteExist { get; set; }
        public bool HS_Check_Cassette_Finished { get; set; }

        public string Hint()
        {
            var sb = new StringBuilder();

            // Basic status
            sb.AppendLine(" - 移載組狀態概覽:");
            sb.AppendLine($" 已初始化: {_initialized}");
            sb.AppendLine($" 自動模式: {_auto}");
            sb.AppendLine($" 暫停中: {_pausing}");
            sb.AppendLine($" 正在執行動作(Moving): {_moving || _pickTrigger || _placeTrigger || _checkCassetteTrigger}");
            sb.AppendLine($" 是否有卡匣: {_cassette}");
            sb.AppendLine($" 馬達狀態: ServoOn={MotorServoOn}, Home={MotorHome}, Idle={MotorIdle}, Busy={MotorBusy}, Alarm={MotorAlarm}");
            sb.AppendLine($"夾爪感測: FrontOpen={Sensor_ClamperFrontOpen}, FrontClose={Sensor_ClamperFrontClose}, BackOpen={Sensor_ClamperBackOpen}, BackClose={Sensor_ClamperBackClose}");

            // Alarms / warnings
            if (HasAlarm)
            {
                sb.AppendLine(" - 模組發生錯誤(Alarm)。請先排除硬體或馬達問題，再長按 [錯誤重置]1秒 清除並重置。");
            }
            if (HasWarning)
            {
                sb.AppendLine(" - 模組發生警告(Warning)。請檢查夾爪/馬達狀態或回原點後再繼續。。 ");
            }

            // Initialization guidance
            if (!_initialized)
            {
                sb.AppendLine(" - 尚未初始化：請按下 [初始化] 以初始化模組 進行初始化（會啟動伺服並回原點）。");
                return sb.ToString();
            }

            // When not in auto
            if (!_auto)
            {
                if (IsNormalStatus)
                    sb.AppendLine(" - 模組已初始化且狀態正常：可按下 [啟動] 開始自動流程。");
                else
                    sb.AppendLine(" - 模組狀態不完全正常，請先解除警告/錯誤後再啟動自動。");

                sb.AppendLine(" - 手動操作建議：");
                if (!MotorServoOn) sb.AppendLine(" * 開啟馬達伺服 (ServoOn)。");
                if (!MotorHome && MotorServoOn && MotorIdle) sb.AppendLine(" * 呼叫 [Home] 讓馬達回原點。 ");
                if (MotorServoOn && MotorIdle) sb.AppendLine(" * 可呼叫 [Move]進行手動定位。 ");
                sb.AppendLine(" * 手動控制夾爪開關。");
                sb.AppendLine(" * 半自動取/放程序來測試取放序列 (位置編號1~14)。");
                sb.AppendLine(" * 初始化會檢查各槽的卡匣存在狀態。");

                return sb.ToString();
            }

            // Auto mode guidance
            sb.AppendLine(" - 模組處於自動模式：");
            if (_autoStopFlag) sb.AppendLine(" * 自動流程已被要求停止，系統會在空閒時停止，請觀察所有模組狀態確認是否已停止。 ");
            if (_pausing) sb.AppendLine(" * 自動流程暫停中：按下[啟動] 恢復，或 長按 [停止]1秒 強制停止。 ");

            // Pick/place in-progress guidance
            if (_pickTrigger)
            {
                sb.AppendLine(" * 正在執行取卡流程：");
                sb.AppendLine($" -目前 pickCase: {_pickCase}");
                sb.AppendLine(" - 若流程停滯請檢查馬達是否 Busy/Alarm，或是否有夾爪動作失敗。 ");
            }
            if (_placeTrigger)
            {
                sb.AppendLine(" * 正在執行放卡流程：");
                sb.AppendLine($" -目前 placeCase: {_placeCase}");
                sb.AppendLine(" - 若流程停滯請檢查馬達/夾爪狀態或重置 Alarm。 ");
            }
            if (_checkCassetteTrigger)
            {
                sb.AppendLine(" * 正在執行檢查卡匣流程：");
                sb.AppendLine($" -目前 checkCase: {_checkCassetteCase}");
            }

            // Post-action hints
            if (!_moving && !_pickTrigger && !_placeTrigger && !_checkCassetteTrigger)
            {
                if (!_cassette)
                    sb.AppendLine(" - 機台空閒且無卡匣：若要放入卡匣請開啟蓋子/將卡匣放到 Loader/Unloader 或使用半自動Pick程序進行取卡。");
                else
                    sb.AppendLine(" - 機台空閒且有卡匣：可使用半自動Place程序將卡匣放至目標位置，或讓自動流程繼續。 ");
            }

            sb.AppendLine(" - 若出現警告/錯誤，請先處理後再續自動流程。 ");

            return sb.ToString();
        }

        public bool GetInSemiPosition(int semiPosIndex)
        {
            int xPos = -1, zPos = -1;

            ShuttleSemiPositionList.GetSemiPositionTransferToRealPoint(semiPosIndex, out xPos, out zPos);

            if(xPos < 0 || zPos < 0)
            {
                return false;
            }

            return _motorXAxis != null && _motorXAxis.GetInPos(xPos) && _motorZAxis != null && _motorZAxis.GetInPos(zPos); 
        }
        public void TeachSemiPosition(int semiPosIndex)
        {
            int xPos = -1, zPos = -1;

            ShuttleSemiPositionList.GetSemiPositionTransferToRealPoint(semiPosIndex, out xPos, out zPos);

            if (xPos < 0 || zPos < 0)
            {
                if (_motorXAxis != null && _motorZAxis != null)
                { 
                    _motorXAxis.Teach(xPos);
                    _motorZAxis.Teach(zPos);
                }
            }
        }

        public bool SemiPickCassette(int semiPosIndex)
        { 
            bool result = false;
            _messageForPickPlace = string.Empty;

            if (_motorXAxis != null && _motorZAxis != null)
            {
                int xPos = -1, zPos = -1;
                ShuttleSemiPositionList.GetSemiPositionTransferToRealPoint(semiPosIndex, out xPos, out zPos);
                if (xPos >= 0 && zPos >= 0)
                {
                    if (MotorIdle && !_auto && MotorHome && MotorServoOn && !_semiRun && !_dryRun)
                    {
                        result = this.PickCassette(xPos, semiRun: true);
                    }
                    else
                        _messageForPickPlace = $"Pick 啟動條件不符。Idle(true)={MotorIdle}, Auto(false)={_auto}, Home(true)={MotorHome}, ServoOn(true)={MotorServoOn}, SemiRun(false)={_semiRun}, DryRun(false)={_dryRun}.";
                }
                else
                    _messageForPickPlace = $"Pick 啟動條件不符: 無效的半自動位置參數: {semiPosIndex}。請提供有效的半自動位置索引。";
            }

            return result;
        }

        public bool SemiPlaceCassette(int semiPosIndex)
        {
            bool result = false;
            _messageForPickPlace = string.Empty;

            if (_motorXAxis != null && _motorZAxis != null)
            {
                int xPos = -1, zPos = -1;
                ShuttleSemiPositionList.GetSemiPositionTransferToRealPoint(semiPosIndex, out xPos, out zPos);
                if (xPos >= 0 && zPos >= 0)
                {
                    if (MotorIdle && !_auto && MotorHome && MotorServoOn && !_semiRun && !_dryRun)
                    {
                        result = this.PlaceCassette(xPos, semiRun: true);
                    }
                    else
                        _messageForPickPlace = $"Place 啟動條件不符。Idle(true)={MotorIdle}, Auto(false)={_auto}, Home(true)={MotorHome}, ServoOn(true)={MotorServoOn}, SemiRun(false)={_semiRun}, DryRun(false)={_dryRun}.";
                }
                else
                    _messageForPickPlace = $"Place 啟動條件不符: 無效的半自動位置參數: {semiPosIndex}。請提供有效的半自動位置索引。";
            }

            return result;
        }

        public string MessageForPickPlace => _messageForPickPlace;

        public bool PassClamperCheckCassette
        {
            get => _passClamperCheckCassette;
            set => _passClamperCheckCassette = value;
        }

        public void ModuleClose()
        {
            AlarmStop();
            AllMotorStop();
        }

        #endregion

        #region Function

        private void StartLoop()
        {
            // ensure previous canceled
            _cts?.Cancel();
            _cts?.Dispose();

            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            // Use LongRunning to create a dedicated thread for continuous shuttle control
            _loopTask = Task.Factory.StartNew(
                () => LoopAsync(token).GetAwaiter().GetResult(),
                token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        private async Task LoopAsync(CancellationToken token)
        {
            _loopStartTime = DateTime.UtcNow;

            while (!token.IsCancellationRequested)
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    // Poll modbus if available
                    if (_running)
                    {
                        await PollFunctionAsync(token).ConfigureAwait(false);
                    }

                    await Task.Delay(_loopInterval, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    // swallow or consider logging
                }
                finally
                {
                    sw.Stop();
                }
            }
        }

        private async Task PollFunctionAsync(CancellationToken token)
        {
            RefreshTimeoutValue();
            CheckTimeout();

            // Wait 10 seconds after loop start before executing CheckOPMode
            if (_loopStartTime != null && (DateTime.UtcNow - _loopStartTime.Value) >= _opModeCheckDelay)
            {
                CheckOPMode();
            }

            AutoProcedure();

            await Task.Yield();
        }

        #endregion

        #region Auto Procedure

        // Non-blocking move end delay checker using ModuleSettings value.
        // Returns true when the condition has been continuously true for the configured ms.
        private bool MoveEndDelayPassed(ref DateTime? startTimestamp, bool condition)
        {
            try
            {
                int ms = _moduleSettings?.MS_Shuttle?.Shuttle_Procedure_MoveEndDelayTime_ms ?? 0;
                if (ms <= 0) return condition; // no delay configured

                if (!condition)
                {
                    startTimestamp = null;
                    return false;
                }

                if (startTimestamp == null)
                    startTimestamp = DateTime.UtcNow;

                return (DateTime.UtcNow - startTimestamp.Value) >= TimeSpan.FromMilliseconds(ms);
            }
            catch
            {
                // on error, behave as no delay
                startTimestamp = null;
                return condition;
            }
        }
        private bool ClamperEndDelayPassed(ref DateTime? startTimestamp, bool condition)
        {
            try
            {
                int ms = _moduleSettings?.MS_Shuttle?.Shuttle_Procedure_ClamperActDelayTime_ms ?? 0;
                if (ms <= 0) return condition; // no delay configured

                if (!condition)
                {
                    startTimestamp = null;
                    return false;
                }

                if (startTimestamp == null)
                    startTimestamp = DateTime.UtcNow;

                return (DateTime.UtcNow - startTimestamp.Value) >= TimeSpan.FromMilliseconds(ms);
            }
            catch
            {
                // on error, behave as no delay
                startTimestamp = null;
                return condition;
            }
        }

        private void GetActParameters(int position, out int posX, out int velX, out int posZ, out int velZ)
        {
            posX = position;
            velX = 1;  
            velZ = 1;
            posZ = 1;

            if (position == 6) posZ = 2;
            else if (position == 7) posZ = 3;
            else if (position == 8) posZ = 4;
            else if (position == 9) posZ = 5;
            else if (position >= 10 && position <= 14) posZ = 6;

        }
        private async void Initialize()
        {
            _initializing = true;
            if (_motorXAxis != null) _motorXAxis.ServoOn(true);
            if (_motorZAxis != null) _motorZAxis.ServoOn(true);

            ActStartStatus();

            _auto = false;
            _cassette = false;
            _pausing = false;
            _autoStopFlag = false;

            _moving = false;

            _pickTrigger = false;
            _placeTrigger = false;

            _dryRun = false;

            ResetTimeoutFlag();

            ClamperOpenOP(true);

            await Home();

            if (!MotorAlarm && MotorHome)
            {
                _initialized = true;
            }

            _initializing = false;
        }

        private async Task Home()
        {
            if (_motorXAxis != null && _motorZAxis != null)
            { 
                while(!_motorZAxis.MotorHoming && !_motorZAxis.ErrorHomeTimeout && !_motorZAxis.MotorAlarm && !_sim_pass_motor)
                {
                    _motorZAxis.Home();
                    await Task.Delay(1000);
                }
                

                //await Task.Delay(1000);

                while (!_motorZAxis.MotorHome && !_motorZAxis.ErrorHomeTimeout && !_motorZAxis.MotorAlarm && !_sim_pass_motor)
                { 
                    await Task.Delay(500);
                }

                if (!_motorZAxis.ErrorHomeTimeout && !_motorZAxis.MotorAlarm && !_motorZAxis.ErrorCommandTimeout)
                {
                    while (!_motorXAxis.MotorHoming && !_motorXAxis.ErrorHomeTimeout && !_motorXAxis.MotorAlarm && !_sim_pass_motor)
                    {
                        _motorXAxis.Home();
                        await Task.Delay(1000);

                    }

                    //await Task.Delay(1000);

                    while (!_motorXAxis.MotorHome && !_motorXAxis.ErrorHomeTimeout && !_motorXAxis.MotorAlarm && !_sim_pass_motor)
                    {
                        await Task.Delay(500);
                    }

                    _motorZAxis.MoveToPosition(0, 0);

                    while (!_motorZAxis.GetInPos(0) && !_motorXAxis.ErrorHomeTimeout && !_motorXAxis.MotorAlarm && !_sim_pass_motor)
                    {
                        await Task.Delay(500);
                    }

                }
            } 
        }

        private void ActStartStatus()
        {
           
        }

        private void EMOStop()
        {
            MotorStop();

            _auto = false;
            _initialized = false;
            _initializing = false;

            _moving = false;

            _pickTrigger = false;
            _placeTrigger = false;

            _dryRun = false;

        }

        private void MotorStop()
        {
            if (_motorXAxis != null) _motorXAxis.MotorStop();
            if (_motorZAxis != null) _motorZAxis.MotorStop();
        }   

        private void AutoProcedure()
        {
            // Start/stop timeout timer for triggers
            bool anyTrigger = _pickTrigger || _placeTrigger || _checkCassetteTrigger;

            if (anyTrigger)
            {
                if (_triggerStartTime == null)
                    _triggerStartTime = DateTime.UtcNow;
                else
                {
                    var elapsed = DateTime.UtcNow - _triggerStartTime.Value;
                    if (elapsed >= _triggerTimeout)
                    {
                        // Timeout occurred - set appropriate alarm flags and clear triggers
                        if (_auto)
                        {
                            // Log timeout for Auto mode
                            OperateLog.Log("超時: 移載組在自動模式觸發超時，已清除觸發並停止自動流程", "Alarm");
                            _logger?.LogWarning("Shuttle trigger timeout (auto mode): cleared triggers and stopped auto.");

                            // Auto-mode timeout alarm: clear triggers and stop
                            _pickTrigger = false;
                            _placeTrigger = false;
                            _checkCassetteTrigger = false;
                            _auto = false;
                            _triggerTimeoutAutoAlarm = true;
                        }
                        else if (_dryRun || _semiRun)
                        {
                            // Log timeout for dry/semi run
                            OperateLog.Log("超時: 移載組在 Dry/SemiRun 模式觸發超時，已清除觸發並停用 dryRun/semiRun", "Alarm");
                            _logger?.LogWarning("Shuttle trigger timeout (dry/semi run): cleared triggers and disabled dry/semi run.");

                            // Dry/semi run timeout alarm: clear triggers and disable dry/semi run
                            _pickTrigger = false;
                            _placeTrigger = false;
                            _checkCassetteTrigger = false;
                            _dryRun = false;
                            _semiRun = false;
                            _triggerTimeoutDrySemiAlarm = true;
                        }

                        // reset timer after handling
                        _triggerStartTime = null;
                    }
                }
            }
            else
            {
                // no active triggers -> reset timer
                _triggerStartTime = null;
            }
            
            if (!_pickTrigger && _pickCase !=0) _pickCase =0;
            if (!_placeTrigger && _placeCase !=0) _placeCase =0;
            if (!_checkCassetteTrigger && _checkCassetteCase !=0) _checkCassetteCase =0;

            if (_auto && !_dryRun && !_semiRun)
            {
                if (Idle && _autoStopFlag && !_pickTrigger && !_placeTrigger && !_moving)
                {
                    _autoStopFlag = false;
                    _auto = false;
                }

                // Pick Procedure
                if (_pickTrigger && !_pausing && _motorXAxis != null && _motorZAxis != null)
                {
                    switch (_pickCase)
                    {
                        case 0: // X Axis Move To Pick Position
                            if (_motorXAxis.GetInPos(_actPositionX))
                            {
                                _pickCase = 10;
                            }
                            else if (!MotorMoving)
                            {
                                _motorXAxis.MoveToPosition(_actPositionX, _actVelocityX);
                                //_pickCase = 1; // wait state for X axis
                            }
                            break;

                        case 1: // X Axis Move End Delay
                            if (MoveEndDelayPassed(ref _moveEndDelayStart, _motorXAxis.GetInPos(_actPositionX) && MotorIdle))
                            {
                                RecordCurrentPosition();
                                _pickCase = 10;
                            }
                            break;

                        case 10: // Z Axis Move To Pick Position
                            if (_motorZAxis.GetInPos(_actPositionZ) && MotorIdle)
                            {
                                _pickCase = 20;
                            }
                            else if (!MotorMoving)
                            {
                                _motorZAxis.MoveToPosition(_actPositionZ, _actVelocityZ);
                                //_pickCase = 11; // wait state for Z axis
                            }
                            break;

                        case 11: // Z Axis Move End Delay
                            if (MoveEndDelayPassed(ref _moveEndDelayStart, _motorZAxis.GetInPos(_actPositionZ) && MotorIdle))
                            {
                                RecordCurrentPosition();
                                _pickCase = 20;
                            }
                            break;

                        case 20: // Clamper Close to Pick Cassette
                            if (Check_ClamperClose || _sim_pass_clamper)
                            {
                                _pickCase = 30;
                            }
                            else if (!Command_ClamperClose)
                            {
                                ClamperCloseOP(true);
                                _pickCase = 21; // clamper action wait
                            }
                            break;

                        case 21: // Clamper Close Delay
                            if (ClamperEndDelayPassed(ref _clampEndDelayStart, Check_ClamperClose || _sim_pass_clamper))
                            {
                                _pickCase = 30;
                            }
                            break;

                        case 30: // Z Axis Move To Original Position (P1)
                            if (_motorZAxis.GetInPos(0) && MotorIdle)
                            {
                                _pickCase = 40;
                            }
                            else if (!MotorMoving)
                            {
                                _motorZAxis.MoveToPosition(0, _actVelocityZ);
                                //_pickCase = 31; // wait for Z return
                            }
                            break;

                        case 31: // Z Axis Return Move End Delay
                            if (MoveEndDelayPassed(ref _moveEndDelayStart, _motorZAxis.GetInPos(0) && MotorIdle))
                            {
                                RecordCurrentPosition();
                                _pickCase = 40;
                            }
                            break;

                        case 40: // Check Cassette Exist
                            if (HasCassette || _sim_pass_clamper || _passClamperCheckCassette)
                            {
                                _cassette = true;
                                _pickTrigger = false;
                                _pickCase = 0;
                            }
                            else
                            {
                                _cassette = false;
                                _pickCase = -99;
                            }
                            break;

                        default:
                        case -99:
                            _pickProcedureError = true;
                            _pickTrigger = false;
                            break;

                    }
                }

                // Place Procedure
                if (_placeTrigger && !_pausing && _motorXAxis != null && _motorZAxis != null)
                {
                    switch (_placeCase)
                    {
                        case 0: // X Axis Move To Place Position
                            if (_motorXAxis.GetInPos(_actPositionX))
                            {
                                _placeCase = 10;
                            }
                            else if (!MotorMoving)
                            {
                                _motorXAxis.MoveToPosition(_actPositionX, _actVelocityX);
                                //_placeCase = 1; // wait for X
                            }
                            break;

                        case 1: // X Axis Move End Delay
                            if (MoveEndDelayPassed(ref _moveEndDelayStart, _motorXAxis.GetInPos(_actPositionX) && MotorIdle))
                            {
                                RecordCurrentPosition();
                                _placeCase = 10;
                            }
                            break;

                        case 10: // Z Axis Move To Place Position
                            if (_motorZAxis.GetInPos(_actPositionZ) && MotorIdle)
                            {
                                _placeCase = 20;
                            }
                            else if (!MotorMoving)
                            {
                                _motorZAxis.MoveToPosition(_actPositionZ, _actVelocityZ);
                                //_placeCase = 11; // wait for Z
                            }
                            break;

                        case 11: // Z Axis Move End Delay
                            if (MoveEndDelayPassed(ref _moveEndDelayStart, _motorZAxis.GetInPos(_actPositionZ) && MotorIdle))
                            {
                                RecordCurrentPosition();
                                _placeCase = 20;
                            }
                            break;

                        case 20: // Clamper Open to Place Cassette
                            if (Check_ClamperOpen || _sim_pass_clamper)
                            {
                                _placeCase = 30;
                            }
                            else if (!Command_ClamperOpen)
                            {
                                ClamperOpenOP(true);
                                _placeCase = 21; // clamper open wait
                            }

                            break;

                        case 21: // Clamper Open Delay
                            if (ClamperEndDelayPassed(ref _clampEndDelayStart, Check_ClamperOpen || _sim_pass_clamper))
                            {
                                _placeCase = 30;
                            }
                            break;

                        case 30: // Z Axis Move To Original Position (P1)
                            if (_motorZAxis.GetInPos(0) && MotorIdle)
                            {
                                _placeCase = 40;
                            }
                            else if (!MotorMoving)
                            {
                                _motorZAxis.MoveToPosition(0, _actVelocityZ);
                                //_placeCase = 31; // wait for Z return
                            }

                            break;

                        case 31: // Z Axis Return Move End Delay
                            if (MoveEndDelayPassed(ref _moveEndDelayStart, _motorZAxis.GetInPos(0) && MotorIdle))
                            {
                                RecordCurrentPosition();
                                _placeCase = 40;
                            }
                            break;

                        case 40: // Check Cassette Exist
                            if (IsEmpty || _sim_pass_clamper || _passClamperCheckCassette)
                            {
                                _cassette = false;
                                _placeTrigger = false;
                                _placeCase = 0;
                            }
                            else
                            {
                                _placeCase = -99;
                            }
                            break;

                        default:
                        case -99:
                            _placeProcedureError = true;
                            _placeTrigger = false;
                            break;

                    }
                }

                
            }

            if (!_auto && (_dryRun || _semiRun))
            {
                // Pick Procedure (non-auto dry/semi)
                if (_pickTrigger && !_pausing && _motorXAxis != null && _motorZAxis != null)
                {
                    switch (_pickCase)
                    {
                        case 0: // X Axis Move To Pick Position
                            if (_motorXAxis.GetInPos(_actPositionX))
                            {
                                _pickCase = 10;
                            }
                            else if (!MotorMoving)
                            {
                                _motorXAxis.MoveToPosition(_actPositionX, _actVelocityX);
                                _pickCase = 1; // wait state for X
                            }
                            break;

                        case 1: // X Axis Move End Delay
                            if (MoveEndDelayPassed(ref _moveEndDelayStart, _motorXAxis.GetInPos(_actPositionX) && MotorIdle))
                            {
                                RecordCurrentPosition();
                                _pickCase = 10;
                            }
                            break;

                        case 10: // Z Axis Move To Pick Position
                            if (_motorZAxis.GetInPos(_actPositionZ) && MotorIdle)
                            {
                                _pickCase = 20;
                            }
                            else if (!MotorMoving)
                            {
                                _motorZAxis.MoveToPosition(_actPositionZ, _actVelocityZ);
                                _pickCase = 11; // wait state for Z
                            }
                            break;

                        case 11: // Z Axis Move End Delay
                            if (MoveEndDelayPassed(ref _moveEndDelayStart, _motorZAxis.GetInPos(_actPositionZ) && MotorIdle))
                            {
                                RecordCurrentPosition();
                                _pickCase = 20;
                            }
                            break;

                        case 20: // Clamper Close to Pick Cassette
                            if (Check_ClamperClose || _sim_pass_clamper)
                            {
                                _pickCase = 30;
                            }
                            else if (!Command_ClamperClose)
                            {
                                ClamperCloseOP(true);
                                _pickCase = 21; // clamper action wait
                            }

                            break;

                        case 21: // Clamper Close Delay
                            if (ClamperEndDelayPassed(ref _clampEndDelayStart, Check_ClamperClose || _sim_pass_clamper))
                            {
                                _pickCase = 30;
                            }
                            break;

                        case 30: // Z Axis Move To Original Position (P1)
                            if (_motorZAxis.GetInPos(0) && MotorIdle)
                            {
                                _pickCase = 40;
                            }
                            else if (!MotorMoving)
                            {
                                _motorZAxis.MoveToPosition(0, _actVelocityZ);
                                _pickCase = 31; // wait for Z return
                            }
                            break;

                        case 31: // Z Axis Return Move End Delay
                            if (MoveEndDelayPassed(ref _moveEndDelayStart, _motorZAxis.GetInPos(0) && MotorIdle))
                            {
                                RecordCurrentPosition();
                                _pickCase = 40;
                            }
                            break;

                        case 40: // Check Cassette Exist
                            if (HasCassette || _sim_pass_clamper || _dryRun || _semiRun)
                            {
                                _cassette = true;
                                _pickTrigger = false;
                                _dryRun = false;
                                _semiRun = false;
                                _pickCase = 0;
                            }
                            else
                            {
                                _cassette = false;
                                _pickCase = -99;
                            }
                            break;

                        default:
                        case -99:
                            _pickProcedureError = true;
                            _pickTrigger = false;
                            _dryRun = false;
                            _semiRun = false;
                            break;

                    }
                }

                // Place Procedure (non-auto dry/semi)
                if (_placeTrigger && !_pausing && _motorXAxis != null && _motorZAxis != null)
                {
                    switch (_placeCase)
                    {
                        case 0: // X Axis Move To Place Position
                            if (_motorXAxis.GetInPos(_actPositionX))
                            {
                                _placeCase = 10;
                            }
                            else if (!MotorMoving)
                            {
                                _motorXAxis.MoveToPosition(_actPositionX, _actVelocityX);
                                _placeCase = 1; // wait for X
                            }
                            break;

                        case 1: // X Axis Move End Delay
                            if (MoveEndDelayPassed(ref _moveEndDelayStart, _motorXAxis.GetInPos(_actPositionX) && MotorIdle))
                            {
                                RecordCurrentPosition();
                                _placeCase = 10;
                            }
                            break;

                        case 10: // Z Axis Move To Place Position
                            if (_motorZAxis.GetInPos(_actPositionZ) && MotorIdle)
                            {
                                _placeCase = 20;
                            }
                            else if (!MotorMoving)
                            {
                                _motorZAxis.MoveToPosition(_actPositionZ, _actVelocityZ);
                                _placeCase = 11; // wait for Z
                            }
                            break;

                        case 11: // Z Axis Move End Delay
                            if (MoveEndDelayPassed(ref _moveEndDelayStart, _motorZAxis.GetInPos(_actPositionZ) && MotorIdle))
                            {
                                RecordCurrentPosition();
                                _placeCase = 20;
                            }
                            break;

                        case 20: // Clamper Open to Place Cassette
                            if (Check_ClamperOpen || _sim_pass_clamper)
                            {
                                _placeCase = 30;
                            }
                            else if (!Command_ClamperOpen)
                            {
                                ClamperOpenOP(true);
                                _placeCase = 21; // clamper open wait
                            }
                            break;

                        case 21: // Clamper Open Delay
                            if (ClamperEndDelayPassed(ref _clampEndDelayStart, Check_ClamperOpen || _sim_pass_clamper))
                            {
                                _placeCase = 30;
                            }
                            break;

                        case 30: // Z Axis Move To Original Position (P1)
                            if (_motorZAxis.GetInPos(0) && MotorIdle)
                            {
                                _placeCase = 40;
                            }
                            else if (!MotorMoving)
                            {
                                _motorZAxis.MoveToPosition(0, _actVelocityZ);
                                _placeCase = 31; // wait for Z return
                            }
                            break;

                        case 31: // Z Axis Return Move End Delay
                            if (MoveEndDelayPassed(ref _moveEndDelayStart, _motorZAxis.GetInPos(0) && MotorIdle))
                            {
                                RecordCurrentPosition();
                                _placeCase = 40;
                            }
                            break;

                        case 40: // Check Cassette Exist
                            if (IsEmpty || _sim_pass_clamper || _dryRun || _semiRun)
                            {
                                _cassette = false;
                                _placeTrigger = false;
                                _dryRun = false;
                                _semiRun = false;
                                _placeCase = 0;
                            }
                            else
                            {
                                _placeCase = -99;
                            }
                            break;

                        default:
                        case -99:
                            _placeProcedureError = true;
                            _dryRun = false;
                            _semiRun = false;
                            _placeTrigger = false;
                            break;

                    }
                }
            }

            // Check Cassette of Tank Procedure
            if (_checkCassetteTrigger && !_pausing && _motorXAxis != null && _motorZAxis != null)
            {
                if (_sim_pass_motor)
                {
                    HS_Check_Cassette_Finished = true;
                    _checkCassetteTrigger = false;
                }
                else
                {
                    switch (_checkCassetteCase)
                    {
                        case 0: // Axis Z Back To Original
                            if (_motorZAxis.GetInPos(0))
                            {
                                _checkCassetteCase = 10;
                            }
                            else if (!MotorMoving)
                            {
                                _motorZAxis.MoveToPosition(0,1);
                                //_checkCassetteCase = 1; // wait for Z
                            }
                            break;

                        case 1: // wait for Z original
                            if (MoveEndDelayPassed(ref _moveEndDelayStart, _motorZAxis.GetInPos(0) && MotorIdle))
                            {
                                RecordCurrentPosition();
                                _checkCassetteCase = 10;
                            }
                            break;

                        case 10: // Axis X Move To Sink (P17)
                            if (_motorXAxis.GetInPos(16) && MotorIdle)
                            {
                                _checkCassetteCase = 20;
                            }
                            else if (!MotorMoving)
                            {
                                _motorXAxis.MoveToPosition(16,1);
                                //_checkCassetteCase = 11; // wait for X
                            }
                            break;

                        case 11: // wait for X
                            if (MoveEndDelayPassed(ref _moveEndDelayStart, _motorXAxis.GetInPos(16) && MotorIdle))
                            {
                                RecordCurrentPosition();
                                _checkCassetteCase = 20;
                            }
                            break;

                        case 20: // Axis Z Move To Sink (P8)
                            if ( _motorZAxis.GetInPos(7) && MotorIdle)
                            {
                                _checkCassetteCase = 30;
                            }
                            else if (!MotorMoving)
                            {
                                _motorZAxis.MoveToPosition(7,1);
                                //_checkCassetteCase = 21; // wait for Z
                            }
                            break;

                        case 21: // wait for Z
                            if (MoveEndDelayPassed(ref _moveEndDelayStart, _motorZAxis.GetInPos(7) && MotorIdle))
                            {
                                RecordCurrentPosition();
                                _checkCassetteCase = 30;
                            }
                            break;

                        case 30: // Check Cassette
                            HS_Check_SinkCassetteExist = Check_TankCassetteExist;
                            _checkCassetteCase = 100;
                            break;

                        case 100: // Axis Z Back To Original
                            if ( _motorZAxis.GetInPos(0))
                            {
                                _checkCassetteCase = 110;
                            }
                            else if (!MotorMoving)
                            {
                                _motorZAxis.MoveToPosition(0,1);
                                //_checkCassetteCase = 101; // wait
                            }
                            break;

                        case 101:
                            if (MoveEndDelayPassed(ref _moveEndDelayStart, _motorZAxis.GetInPos(0) && MotorIdle))
                            {
                                RecordCurrentPosition();
                                _checkCassetteCase = 110;
                            }
                            break;

                        case 110: // Axis X Move To Soaking Tank (P18)
                            if (_motorXAxis.GetInPos(17) && MotorIdle)
                            {
                                _checkCassetteCase = 120;
                            }
                            else if (!MotorMoving)
                            {
                                _motorXAxis.MoveToPosition(17,1);
                                //_checkCassetteCase = 111; // wait
                            }
                            break;

                        case 111:
                            if (MoveEndDelayPassed(ref _moveEndDelayStart, _motorXAxis.GetInPos(17) && MotorIdle))
                            {
                                RecordCurrentPosition();
                                _checkCassetteCase = 120;
                            }
                            break;

                        case 120: // Axis Z Move To Sink (P9)
                            if (_motorZAxis.GetInPos(8) && MotorIdle)
                            {
                                _checkCassetteCase = 130;
                            }
                            else if (!MotorMoving)
                            {
                                _motorZAxis.MoveToPosition(8,1);
                                //_checkCassetteCase = 121;
                            }
                            break;

                        case 121:
                            if (MoveEndDelayPassed(ref _moveEndDelayStart, _motorZAxis.GetInPos(8) && MotorIdle))
                            {
                                RecordCurrentPosition();
                                _checkCassetteCase = 130;
                            }
                            break;

                        case 130: // Check Cassette
                            HS_Check_SoakingTankCassetteExist = Check_TankCassetteExist;
                            _checkCassetteCase = 200;
                            break;

                        case 200: // Axis Z Back To Original
                            if (_motorZAxis.GetInPos(0))
                            {
                                _checkCassetteCase = 210;
                            }
                            else if (!MotorMoving)
                            {
                                _motorZAxis.MoveToPosition(0,1);
                                //_checkCassetteCase = 201;
                            }
                            break;

                        case 201:
                            if (MoveEndDelayPassed(ref _moveEndDelayStart, _motorZAxis.GetInPos(0) && MotorIdle))
                            {
                                RecordCurrentPosition();
                                _checkCassetteCase = 210;
                            }
                            break;

                        case 210: // Axis X Move To Drying Tank1 (P19)
                            if (_motorXAxis.GetInPos(18) && MotorIdle)
                            {
                                _checkCassetteCase = 220;
                            }
                            else if (!MotorMoving)
                            {
                                _motorXAxis.MoveToPosition(18,1);
                                //_checkCassetteCase = 211;
                            }
                            break;

                        case 211:
                            if (MoveEndDelayPassed(ref _moveEndDelayStart, _motorXAxis.GetInPos(18) && MotorIdle))
                            {
                                RecordCurrentPosition();
                                _checkCassetteCase = 220;
                            }
                            break;

                        case 220: // Axis Z Move To Sink (P10)
                            if (_motorZAxis.GetInPos(9) && MotorIdle)
                            {
                                _checkCassetteCase = 230;
                            }
                            else if (!MotorMoving)
                            {
                                _motorZAxis.MoveToPosition(9,1);
                                //_checkCassetteCase = 221;
                            }
                            break;

                        case 221:
                            if (MoveEndDelayPassed(ref _moveEndDelayStart, _motorZAxis.GetInPos(9) && MotorIdle))
                            {
                                RecordCurrentPosition();
                                _checkCassetteCase = 230;
                            }
                            break;

                        case 230: // Check Cassette
                            HS_Check_DryingTank1CassetteExist = Check_TankCassetteExist;
                            _checkCassetteCase = 300;
                            break;

                        case 300: // Axis Z Back To Original
                            if (_motorZAxis.GetInPos(0))
                            {
                                _checkCassetteCase = 310;
                            }
                            else if (!MotorMoving)
                            {
                                _motorZAxis.MoveToPosition(0,1);
                                //_checkCassetteCase = 301;
                            }
                            break;

                        case 301:
                            if (MoveEndDelayPassed(ref _moveEndDelayStart, _motorZAxis.GetInPos(0) && MotorIdle))
                            {
                                RecordCurrentPosition();
                                _checkCassetteCase = 310;
                            }
                            break;

                        case 310: // Axis X Move To Drying Tank2 (P20)
                            if (_motorXAxis.GetInPos(19) && MotorIdle)
                            {
                                _checkCassetteCase = 320;
                            }
                            else if (!MotorMoving)
                            {
                                _motorXAxis.MoveToPosition(19,1);
                                //_checkCassetteCase = 311;
                            }
                            break;

                        case 311:
                            if (MoveEndDelayPassed(ref _moveEndDelayStart, _motorXAxis.GetInPos(19) && MotorIdle))
                            {
                                RecordCurrentPosition();
                                _checkCassetteCase = 320;
                            }
                            break;

                        case 320: // Axis Z Move To Sink (P11)
                            if (_motorZAxis.GetInPos(10))
                            {
                                _checkCassetteCase = 330;
                            }
                            else if (!MotorMoving)
                            {
                                _motorZAxis.MoveToPosition(10,1);
                                //_checkCassetteCase = 321;
                            }
                            break;

                        case 321:
                            if (MoveEndDelayPassed(ref _moveEndDelayStart, _motorZAxis.GetInPos(10) && MotorIdle))
                            {
                                RecordCurrentPosition();
                                _checkCassetteCase = 330;
                            }
                            break;

                        case 330: // Check Cassette
                            HS_Check_DryingTank2CassetteExist = Check_TankCassetteExist;
                            _checkCassetteCase = 400;
                            break;

                        case 400: // Axis Z Back To Original
                            if (_motorZAxis.GetInPos(0))
                            {
                                _checkCassetteCase = 410;
                            }
                            else if (!MotorMoving)
                            {
                                _motorZAxis.MoveToPosition(0,1);
                                //_checkCassetteCase = 401;
                            }
                            break;

                        case 401:
                            if (MoveEndDelayPassed(ref _moveEndDelayStart, _motorZAxis.GetInPos(0) && MotorIdle))
                            {
                                RecordCurrentPosition();
                                _checkCassetteCase = 410;
                            }
                            break;

                        case 410: // Axis X Back To Original
                            if (_motorXAxis.GetInPos(0))
                            {
                                _checkCassetteCase = 420;
                            }
                            else if (!MotorMoving)
                            {
                                _motorXAxis.MoveToPosition(0,1);
                                //_checkCassetteCase = 411;
                            }
                            break;

                        case 411:
                            if (MoveEndDelayPassed(ref _moveEndDelayStart, _motorXAxis.GetInPos(0) && MotorIdle))
                            {
                                RecordCurrentPosition();
                                _checkCassetteCase = 420;
                            }
                            break;

                        case 420: // Finish
                            _checkCassetteTrigger = false;
                            _checkCassetteCase = 0;
                            HS_Check_Cassette_Finished = true;
                            break;


                        default:
                        case -99:
                            _checkCassetteProcedureError = true;
                            _checkCassetteTrigger = false;
                            break;
                    }
                }
            }
        }

        private void CheckOPMode()
        { 
            // Read current manual OP mode state
            bool currentManual = Sensor_OpMode_Manual;

            // On first read after startup, just record state and don't trigger actions
            if (_opModeFirstRead)
            {
                _prevOpModeManual = currentManual;
                _opModeFirstRead = false;
                return;
            }

            // Trigger action only when transitioning from false -> true
            if (!_prevOpModeManual && currentManual)
            {

                // When entering manual mode (rising edge), ensure cassette state cleared and clamper opened
                _cassette = false;
                ClamperOpenOP(true);
            }

            // Update previous state
            _prevOpModeManual = currentManual;
        }

        private void RecordCurrentPosition()
        {
            if (_plcService != null)
            {
                int _xCommandPos = _plcService.Axis1Pos;
                int _xEncoderPos = _plcService.Axis1EncoderPos;
                int _zCommandPos = _plcService.Axis2Pos;
                int _zEncoderPos = _plcService.Axis2EncoderPos;
                float _xRatio = (float)_xEncoderPos / (float)(_xCommandPos == 0 ? 1 : _xCommandPos);
                float _zRatio = (float)_zEncoderPos / (float)(_zCommandPos == 0 ? 1 : _zCommandPos);

                OperateLog.Log("Shuttle Position", $"Command X: {_xCommandPos}, Encoder X: {_xEncoderPos}, Command Z: {_zCommandPos}, Encoder Z: {_zEncoderPos}, X Ratio: {_xRatio}, Z Ratio: {_zRatio}");
            }

        }

        #endregion

        #region Timeout

        private bool _ClamperF_Open_Timeout = false;
        private bool _ClamperF_Close_Timeout = false;
        private bool _ClamperB_Open_Timeout = false;
        private bool _ClamperB_Close_Timeout = false;

        private void RefreshTimeoutValue()
        {
            if (_unitSettings.Shuttle != null)
            {
                _clamperTimeoutThreshold_Value = _unitSettings.Shuttle.Clamper_Timeout_Second;

                _clamperTimeoutThreshold = TimeSpan.FromSeconds(_clamperTimeoutThreshold_Value);
            }
        }

        // Timer thresholds (can be adjusted). If module settings provide timeout values they can be used here instead.
        private TimeSpan _clamperTimeoutThreshold = TimeSpan.FromSeconds(_clamperTimeoutThreshold_Value);

        // Start times for each monitored condition (null when condition not active)
        private DateTime? _clamperFOpenStart;
        private DateTime? _clamperFCloseStart;
        private DateTime? _clamperBOpenStart;
        private DateTime? _clamperBCloseStart;

        private void ResetTimeoutFlag()
        {
            _ClamperF_Open_Timeout = false;
            _ClamperF_Close_Timeout = false;
            _ClamperB_Open_Timeout = false;
            _ClamperB_Close_Timeout = false;

            _clamperFOpenStart = null;
            _clamperFCloseStart = null;
            _clamperBOpenStart = null;
            _clamperBCloseStart = null;
        }

        private void CheckTimeout()
        {
            // Conditions to monitor (per request):

            bool clamperFOpenCondition = Command_ClamperOpen && !Sensor_ClamperFrontOpen;
            bool clamperFCloseCondition = Command_ClamperClose && !Sensor_ClamperFrontClose;
            bool clamperBOpenCondition = Command_ClamperOpen && !Sensor_ClamperBackOpen;
            bool clamperBCloseCondition = Command_ClamperClose && !Sensor_ClamperBackClose;

            var now = DateTime.UtcNow;

            // PV Low
            if (clamperFOpenCondition)
            {
                if (_clamperFOpenStart == null)
                    _clamperFOpenStart = now;
                else if (!_ClamperF_Open_Timeout && now - _clamperFOpenStart >= _clamperTimeoutThreshold)
                    _ClamperF_Open_Timeout = true;
            }
            else
            {
                _clamperFOpenStart = null;
                _ClamperF_Open_Timeout = false;
            }

            // PV High
            if (clamperFCloseCondition)
            {
                if (_clamperFCloseStart == null)
                    _clamperFCloseStart = now;
                else if (!_ClamperF_Close_Timeout && now - _clamperFCloseStart >= _clamperTimeoutThreshold)
                    _ClamperF_Close_Timeout = true;
            }
            else
            {
                _clamperFCloseStart = null;
                _ClamperF_Close_Timeout = false;
            }

            // Cover Open
            if (clamperBOpenCondition)
            {
                if (_clamperBOpenStart == null)
                    _clamperBOpenStart = now;
                else if (!_ClamperB_Open_Timeout && now - _clamperBOpenStart >= _clamperTimeoutThreshold)
                    _ClamperB_Open_Timeout = true;
            }
            else
            {
                _clamperBOpenStart = null;
                _ClamperB_Open_Timeout = false;
            }

            // Cover Close
            if (clamperBCloseCondition)
            {
                if (_clamperBCloseStart == null)
                    _clamperBCloseStart = now;
                else if (!_ClamperB_Close_Timeout && now - _clamperBCloseStart >= _clamperTimeoutThreshold)
                    _ClamperB_Close_Timeout = true;
            }
            else
            {
                _clamperBCloseStart = null;
                _ClamperB_Close_Timeout = false;
            }
        }

        #endregion

        #region Alarm

        private void ResetMotorAlarm()
        {
            try
            {
                if (_motorXAxis != null) _motorXAxis.Command_AlarmReset();
                if (_motorZAxis != null) _motorZAxis.Command_AlarmReset();

                _pickProcedureError = false;
                _placeProcedureError = false;
                _checkCassetteProcedureError = false;

            }
            catch
            {
                // swallow any immediate exceptions
            }
        }



        private bool _motorXAlarm
        {
            get
            {
                if (_alarmCheckStartTime != null && DateTime.UtcNow - _alarmCheckStartTime.Value < TimeSpan.FromSeconds(10))
                    return false;
                return _motorXAxis != null && _motorXAxis.MotorAlarm;
            }
        }
        private bool _motorXAlarmLimitN
        {
            get
            {
                if (_alarmCheckStartTime != null && DateTime.UtcNow - _alarmCheckStartTime.Value < TimeSpan.FromSeconds(10))
                    return false;
                return _motorXAxis != null && _motorXAxis.ErrorLimitN;
            }
        }
        private bool _motorXAlarmLimitP
        {
            get
            {
                if (_alarmCheckStartTime != null && DateTime.UtcNow - _alarmCheckStartTime.Value < TimeSpan.FromSeconds(10))
                    return false;
                return _motorXAxis != null && _motorXAxis.ErrorLimitP;
            }
        }
        private bool _motorXAlarmHomeTimeout
        {
            get
            {
                if (_alarmCheckStartTime != null && DateTime.UtcNow - _alarmCheckStartTime.Value < TimeSpan.FromSeconds(10))
                    return false;
                return _motorXAxis != null && _motorXAxis.ErrorHomeTimeout;
            }
        }
        private bool _motorXAlarmMoveTimeout
        {
            get
            {
                if (_alarmCheckStartTime != null && DateTime.UtcNow - _alarmCheckStartTime.Value < TimeSpan.FromSeconds(10))
                    return false;
                return _motorXAxis != null && _motorXAxis.ErrorCommandTimeout;
            }
        }

        private bool _motorZAlarm
        {
            get
            {
                if (_alarmCheckStartTime != null && DateTime.UtcNow - _alarmCheckStartTime.Value < TimeSpan.FromSeconds(10))
                    return false;
                return _motorZAxis != null && _motorZAxis.MotorAlarm;
            }
        }
        private bool _motorZAlarmLimitN
        {
            get
            {
                if (_alarmCheckStartTime != null && DateTime.UtcNow - _alarmCheckStartTime.Value < TimeSpan.FromSeconds(10))
                    return false;
                return _motorZAxis != null && _motorZAxis.ErrorLimitN;
            }
        }
        private bool _motorZAlarmLimitP
        {
            get
            {
                if (_alarmCheckStartTime != null && DateTime.UtcNow - _alarmCheckStartTime.Value < TimeSpan.FromSeconds(10))
                    return false;
                return _motorZAxis != null && _motorZAxis.ErrorLimitP;
            }
        }
        private bool _motorZAlarmHomeTimeout
        {
            get
            {
                if (_alarmCheckStartTime != null && DateTime.UtcNow - _alarmCheckStartTime.Value < TimeSpan.FromSeconds(10))
                    return false;
                return _motorZAxis != null && _motorZAxis.ErrorHomeTimeout;
            }
        }
        private bool _motorZAlarmMoveTimeout
        {
            get
            {
                if (_alarmCheckStartTime != null && DateTime.UtcNow - _alarmCheckStartTime.Value < TimeSpan.FromSeconds(10))
                    return false;
                return _motorZAxis != null && _motorZAxis.ErrorCommandTimeout;
            }
        }


        #endregion
    }
}
