using CleanerControlApp.Hardwares.Sink.Interfaces;
using CleanerControlApp.Modules.DeltaMS300.Interfaces;
using CleanerControlApp.Modules.MitsubishiPLC.Interfaces;
using CleanerControlApp.Modules.MitsubishiPLC.Services;
using CleanerControlApp.Modules.Modbus.Interfaces;
using CleanerControlApp.Modules.TempatureController.Interfaces;
using CleanerControlApp.Modules.TempatureController.Services;
using CleanerControlApp.Utilities;
using CleanerControlApp.Utilities.Alarm;
using CleanerControlApp.Utilities.Log;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Hardwares.Sink.Services
{
    public class Sink: ISink, IDisposable
    {
        #region Constant

        public static readonly int TC_Index = 3; // TC-4
        public static readonly int Motor_Index = 2; // Axis-3
        public static readonly int MS300_Index = 0; // MS300-1

        #endregion

        #region attribute

        private readonly ILogger<Sink>? _logger;

        // background loop
        private CancellationTokenSource? _cts;
        private Task? _loopTask;
        private readonly TimeSpan _loopInterval = TimeSpan.FromMilliseconds(10);

        //private IModbusRTUService? _modbusService;
        private IDeltaMS300? _deltaMS300;

        private bool _running;

        private readonly UnitSettings _unitSettings;
        private readonly ModuleSettings _moduleSettings;

        private readonly IPLCOperator? _plcService;

        private readonly ISingleTemperatureController? _temperatureController;
        private readonly ITemperatureControllers? _temperatureControllers;

        private bool _auto = false;
        private bool _pausing = false;
        private bool _pressure = false;
        private bool _cassette = false;
        private bool _initialized = false;
        private bool _autoStopFlag = false;

        private int PV_Check_High => ((_moduleSettings.Sink != null ? _moduleSettings.Sink.SV_High : 0)) -
            (_unitSettings.Sink != null ? _unitSettings.Sink.SV_CheckOffet : 0);
        private int PV_Check_Low => ((_moduleSettings.Sink != null ? _moduleSettings.Sink.SV_Low : 0)) +
            (_unitSettings.Sink != null ? _unitSettings.Sink.SV_CheckOffet : 0);

        private static int _pvLowTimeoutThreshold_Value = 30;
        private static int _pvHighTimeoutThreshold_Value = 30;
        private static int _coverOpenTimeoutThreshold_Value = 30;
        private static int _coverCloseTimeoutThreshold_Value = 30;

        private bool _sim_pv = false;
        private bool _sim_pass_motor = false;

        private bool _motor_commanding = false;
        private int _motor_air_retry_count = 0;
        private bool _motor_air_up_flag = false;
        private bool RetryAirFinished => _moduleSettings.Sink != null && _moduleSettings.Sink.AirKnifeRetryCount == _motor_air_retry_count;

        #endregion

        #region constructor

        public Sink(ILogger<Sink>? logger, IPLCOperator ? plcService, ITemperatureControllers? temperatureControllers, UnitSettings unitSettings, ModuleSettings moduleSettings, IDeltaMS300 deltaMS300)
        {
            _unitSettings = unitSettings;
            _moduleSettings = moduleSettings;

            _logger = logger;

            _plcService = plcService;
            _deltaMS300 = deltaMS300;

            _temperatureController = temperatureControllers?[TC_Index]; // 這裡是用在做壓力控制
            _temperatureControllers = temperatureControllers;

            

            RefreshTimeoutValue();

            // Alarm
            AlarmManager.AttachFlagGetter("ALM201", () => _PV_Low_Timeout);
            AlarmManager.AttachFlagGetter("ALM202", () => _PV_High_Timeout);
            AlarmManager.AttachFlagGetter("ALM203", () => _Cover_Open_Timeout);
            AlarmManager.AttachFlagGetter("ALM204", () => _Cover_Close_Timeout);
            AlarmManager.AttachFlagGetter("ALM205", () => _motorAlarm);
            AlarmManager.AttachFlagGetter("ALM206", () => _motorAlarmLimitN);
            AlarmManager.AttachFlagGetter("ALM207", () => _motorAlarmLimitP);
            AlarmManager.AttachFlagGetter("ALM208", () => _motorAlarmHomeTimeout);
            AlarmManager.AttachFlagGetter("ALM209", () => _motorAlarmMoveTimeout);
            AlarmManager.AttachFlagGetter("ALM210", () => _invErrorAlarm);
            AlarmManager.AttachFlagGetter("ALM211", () => _invWarningAlarm);

            StartLoop();

            Start();
        }

        #endregion

        #region destructor and dispose pattern

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
        ~Sink()
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

        #region ISink implementation


        public bool IsRunning => _running;
        public void Start() { _running = true; }
        public void Stop() { _running = false; }

        public bool Sensor_CoverOpen
        {
            get => (_plcService != null) && _plcService.CleanerCoverFIn;
        }
        public bool Sensor_CoverClose
        {
            get => (_plcService != null) && _plcService.CleanerCoverBIn;

        }

        public bool Command_CleanerCoverClose
        {
            get => (_plcService != null) && _plcService.Command_CleanerCoverOpen;
            set
            {
                if (_plcService != null)
                    _plcService.Command_CleanerCoverOpen = value;
            }
        }
        public bool Command_CleanerAirOpen
        {
            get => (_plcService != null) && _plcService.Command_CleanerAirKnifeOpen;

            set
            {
                if (_plcService != null)
                    _plcService.Command_CleanerAirKnifeOpen = value;
            }
        }

        public int PV
        {
            get => (_temperatureController != null) ? _temperatureController.PV : 0;
        }
        public int SV
        {
            get => (_temperatureController != null) ? _temperatureController.SV : 0;
        }
        public float PV_Value
        {
            get => (_temperatureController != null && _unitSettings.Sink != null) ? (float)(_temperatureController.PV * _unitSettings.Sink.UnitTransfer) : 0f;
        }
        public float SV_Value
        {
            get => (_temperatureController != null && _unitSettings.Sink != null) ? (float)(_temperatureController.SV * _unitSettings.Sink.UnitTransfer) : 0f;
        }
        public void SetSV(int value)
        {
            if (_temperatureControllers != null)
            {
                _temperatureControllers.SetSV(TC_Index, value);
            }
        }
        public void SetSV(float value)
        {
            float setValue = 0f;

            setValue = value / ((_unitSettings.Sink != null) ? _unitSettings.Sink.UnitTransfer : 1f);

            SetSV((int)(setValue));
        }

        public bool Auto => _auto;
        public bool Pausing => _pausing;
        public bool Pressure => _pressure;
        public bool Cassette => _cassette;
        public bool Initialized => _initialized && (_sim_pass_motor || MotorHome);
        public bool Idle => Sensor_CoverOpen && !_pressure && !_cassette && _initialized && IsNormalStatus && (_sim_pass_motor || (MotorServoOn && MotorIdle && MotorHome));

        public bool HighPressure => (PV > PV_Check_High) || _sim_pv;
        public bool LowPressure => PV < PV_Check_Low;
        public bool PressureOP(bool pressure)
        {
            bool result = false;


            if (_moduleSettings.Sink != null)
            {
                if (HS_WaterSystemError && pressure) pressure = false;

                SetSV(pressure ? _moduleSettings.Sink.SV_High : _moduleSettings.Sink.SV_Low);
                _pressure = pressure;
                result = true;
            }

            return result;
        }
        public bool ManualPressureOP(bool pressure)
        {
            bool result = false;

            if (!_auto)
            {
                result = PressureOP(pressure);
                OperateLog.Log($"沖水槽 手動噴水 " + (pressure ? "開" : "關"), $"沖水槽 手動噴水 " + (pressure ? "開" : "關"));
            }

            return false;
        }
        public bool AirOP(bool air)
        {
            bool result = true;
            if (_plcService != null)
            {
                Command_CleanerAirOpen = air;
            }
            else
                result = false;

            return result;
        }
        public bool ManualAirOP(bool air)
        {
            bool result = false;

            if (_plcService != null)
            {
                result = AirOP(air);
                OperateLog.Log($"沖水槽 手動氣閥 " + (air ? "開" : "關"), $"沖水槽 手動氣閥 " + (air ? "開" : "關"));
            }

            return result;
        }
        
        public bool CoverClose(bool close)
        {
            bool result = true;
            if (_plcService != null)
            {
                Command_CleanerCoverClose = close;
            }
            else
                result = false;

            return result;
        }
        public bool ManualCoverClose(bool close)
        {
            bool result = false;

            if (_plcService != null)
            {
                result = CoverClose(close);
                OperateLog.Log($"沖水槽 手動蓋子 " + (close ? "關" : "開"), $"沖水槽 手動蓋子 " + (close ? "關" : "開"));
            }

            return result;
        }

        public bool HS_ClamperMoving { get; set; }
        public bool HS_ClamperPickFinished { get; set; }
        public bool HS_ClamperPlaceFinished { get; set; }
        public bool HS_WaterSystemError { get; set; }
        public bool HS_InputPermit => Idle && !_pausing && !HS_ClamperMoving && _auto && InPos1;
        public bool HS_ActFinished => _cassette && Sensor_CoverOpen && !HS_ClamperMoving && !Pressure && _actFinished && InPos1 && RetryAirFinished;

        public int ElpasedPressureTime_Seconds => (int)(_elapsedTime != null ? _elapsedTime.Value.TotalSeconds : 0);
        public int RemainingPressureTime_Seconds => (_moduleSettings.Sink != null) ? _moduleSettings.Sink.ActTime_Second - ElpasedPressureTime_Seconds : 0;

        public bool ModulePass { get; set; }
        public bool HasWarning => _PV_Low_Timeout || _PV_High_Timeout || _Cover_Open_Timeout || _Cover_Close_Timeout || _motorAlarmHomeTimeout || _motorAlarmMoveTimeout || _invErrorAlarm;
        public bool HasAlarm => _motorAlarm || _motorAlarmLimitN || _motorAlarmLimitP;
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
        }
        public void AlarmStop()
        {
            EMOStop();
        }
        public void AutoStart()
        {
            _autoStopFlag = false;
            _pausing = false;
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

        public void SimHiPressure()
        {
            _sim_pv = !_sim_pv;
        }
        public void SimMotorPass()
        {
            _sim_pass_motor = !_sim_pass_motor;
        }

        public bool MotorServoOn => _plcService != null && _plcService.CleanerZServoServoOn;
        public bool MotorUpLimit => _plcService != null && _plcService.CleanerZLimitN;
        public bool MotorDownLimit => _plcService != null && _plcService.CleanerZLimitP;
        public bool MotorIdle => _plcService != null && _plcService.CleanerZIdle && !_plcService.Axis3CommandDriving && !_plcService.Axis3CommandProcedure && !_plcService.Axis3HomeProcedure;
        public bool MotorBusy => !MotorIdle && _plcService != null && MotorServoOn;
        public bool MotorAlarm => _plcService != null && _plcService.CleanerZAlarm;
        public bool MotorHoming => _plcService != null && _plcService.Axis3HomeProcedure;
        public bool MotorMoving => _plcService != null && _plcService.Axis3CommandProcedure;
        public bool MotorHome => _plcService != null && _plcService.Axis3HomeComplete;
        public int Posiition => _plcService != null ? _plcService.Axis3Pos : 0;
        public float Position_Value => (_plcService != null && _unitSettings.Sink != null) ? ((float)_plcService.Axis3Pos * _unitSettings.Sink.MotorUnitTransfer) : 1f;

        public void ServoOn(bool servo)
        {
            if (_plcService != null)
            {
                _plcService.Command_Axis3ServoOn = servo;
            }
        }
        public void Jog(bool jog, int dir, int speed) // dir: 0: Down(+), 1: Up(-); speed: 0: low, 1: medium, 2:high
        { 
            if(_plcService != null && !MotorAlarm && MotorServoOn && !_auto && !_plcService.Axis3CommandProcedure && !_plcService.Axis3HomeProcedure)
            {
                if (speed == 2)
                    _plcService.Command_Axis3JogSpeedH = true;
                else if (speed == 1)
                {
                    _plcService.Command_Axis3JogSpeedH = false;
                    _plcService.Command_Axis3JogSpeedM = true;
                }
                else
                { 
                    _plcService.Command_Axis3JogSpeedH = false;
                    _plcService.Command_Axis3JogSpeedM = false;
                }
                
                if (jog)
                {
                    if (dir == 0)
                        _plcService.Command_Axis3JogP = true;
                    else
                        _plcService.Command_Axis3JogN = true;
                }
                else
                {
                    _plcService.Command_Axis3JogP = false;
                    _plcService.Command_Axis3JogN = false;
                }

            }
        }
        public void Home()
        {
            if (_plcService != null && !MotorAlarm && MotorServoOn && Idle)
            {
                try
                {
                    _plcService.Command_Axis3Home = true;

                    // fire-and-forget task to reset the command after a delay
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                            if (_plcService != null)
                                _plcService.Command_Axis3Home = false;
                        }
                        catch
                        {
                            // swallow exceptions from the background task
                        }
                    });
                }
                catch { }
            }
        }
        public void MoveToPosition(int position, int speed)
        {
            if (_plcService != null && !MotorAlarm && MotorServoOn && Idle)
            {
                try
                {
                    int setPos = _moduleSettings.Sink != null ? _moduleSettings.Sink.MotorPosition_01 : 0; // default to position 1
                    if(position == 2)
                        setPos = _moduleSettings.Sink != null ? _moduleSettings.Sink.MotorPosition_02 : 0;
                    else if(position == 3)
                        setPos = _moduleSettings.Sink != null ? _moduleSettings.Sink.MotorPosition_03 : 0;

                    int setVel = _moduleSettings.Sink != null ? _moduleSettings.Sink.MotorVelocity_01 : 0; // default to velocity 1
                    if(speed == 1)
                        setVel = _moduleSettings.Sink != null ? _moduleSettings.Sink.MotorVelocity_02 : 0;

                    _plcService.Command_Axis3Pos = setPos;
                    _plcService.Command_Axis3Speed = setVel;
                    _plcService.Command_Axis3Command = true;
                    // fire-and-forget task to reset the command after a delay

                    _motor_commanding = true;

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                            if (_plcService != null)
                                _plcService.Command_Axis3Command = false;

                            _motor_commanding = false;
                        }
                        catch
                        {
                            // swallow exceptions from the background task
                        }
                    });
                }
                catch { }
            }
        }
        public void MotorStop()
        {
            if (_plcService != null)
            {
                try
                {
                    _plcService.Command_Axis3Stop = true;

                    // fire-and-forget task to reset the command after a delay
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                            if (_plcService != null)
                                _plcService.Command_Axis3Stop = false;
                        }
                        catch
                        {
                            // swallow exceptions from the background task
                        }
                    });
                }
                catch { }

            }
        }

        public bool InPos1 => ((_plcService != null) && (Posiition == (_moduleSettings.Sink != null ? _moduleSettings.Sink.MotorPosition_01 : 0))) || _sim_pass_motor;
        public bool InPos2 => ((_plcService != null) && (Posiition == (_moduleSettings.Sink != null ? _moduleSettings.Sink.MotorPosition_02 : 0))) || _sim_pass_motor;
        public bool InPos3 => ((_plcService != null) && (Posiition == (_moduleSettings.Sink != null ? _moduleSettings.Sink.MotorPosition_03 : 0))) || _sim_pass_motor;

        public void Teach(int position)
        {
            if (_plcService != null && _moduleSettings.Sink != null)
            {
                if (position == 0)
                    _moduleSettings.Sink.MotorPosition_01 = Posiition;
                else if (position == 1)
                    _moduleSettings.Sink.MotorPosition_02 = Posiition;
                else if (position == 2)
                    _moduleSettings.Sink.MotorPosition_03 = Posiition;

                if(position >= 0 && position < 3)
                    ConfigLoader.SetModuleSettings(_moduleSettings);
            }
        }

        public int InvErrorCode => _deltaMS300 != null ? _deltaMS300.ErrorCode : 0;
        public int InvWarningCode => _deltaMS300 != null ? _deltaMS300.WarningCode : 0;
        public float InvCommandFrequency => _deltaMS300 != null ? _deltaMS300.Frquency_Command : 0f;
        public float InvActualFrequency => _deltaMS300 != null ? _deltaMS300.Frquency_Output : 0f;

        public string Hint()
        {
            var sb = new StringBuilder();

            // High level status
            sb.AppendLine(" - 沖水槽狀態概覽:");
            sb.AppendLine($" 已初始化: {_initialized}");
            sb.AppendLine($" 自動模式: {_auto}");
            sb.AppendLine($" 暫停中: {_pausing}");
            sb.AppendLine($" 是否有卡匣: {_cassette}");
            sb.AppendLine($" 蓋子開啟感測: {Sensor_CoverOpen}");
            sb.AppendLine($" 蓋子關閉感測: {Sensor_CoverClose}");
            sb.AppendLine($"目前壓力: {Pressure}");
            sb.AppendLine($" PV: {PV} SV: {SV}");
            sb.AppendLine($" 馬達狀態: ServoOn={MotorServoOn}, Home={MotorHome}, Idle={MotorIdle}, Alarm={MotorAlarm}");

            // Alarms / warnings
            if (HasAlarm)
            {
                sb.AppendLine(" - 模組發生錯誤(Alarm)。請先排除錯誤。可呼叫 AlarmReset()以嘗試重置馬達警報，或針對硬體排除問題。");
            }
            if (HasWarning)
            {
                sb.AppendLine(" - 模組發生警告(Warning)。建議檢查 PV/蓋子狀態或其他警告來源，並視情況呼叫 WarningStop()。");
            }

            // Initialization guidance
            if (!_initialized)
            {
                sb.AppendLine(" - 尚未初始化：請呼叫 ModuleReset() 或 Initialize()以啟動模組初始化流程（會啟動 Servo、Home 等）。");
                return sb.ToString(); // many other suggestions require initialization
            }

            // Auto / manual guidance
            if (!_auto)
            {
                if (IsNormalStatus)
                {
                    sb.AppendLine(" - 模組已初始化且狀態正常：可呼叫 AutoStart() 開始自動流程。");
                }
                else
                {
                    sb.AppendLine(" - 模組狀態不完全正常，請先解除警告/錯誤後再啟動自動。");
                }

                sb.AppendLine(" - 若需手動操作，建議步驟：");
                if (!MotorServoOn)
                    sb.AppendLine(" * 呼叫 ServoOn(true) 開啟馬達伺服。 ");
                if (!MotorHome && MotorServoOn && MotorIdle)
                    sb.AppendLine(" * 呼叫 Home()進行回原點。 ");
                if (MotorServoOn && MotorIdle)
                    sb.AppendLine(" * 可使用 MoveToPosition(position, speed) 或 Jog(...) 做手動定位/換位。 ");

                if (!_cassette && Sensor_CoverOpen)
                    sb.AppendLine(" * 蓋子已開，請放入卡匣後由夾爪完成放置，或手動控制馬達至上方位置確保放置位置正確。 ");
                if (_cassette && !Sensor_CoverClose)
                    sb.AppendLine(" * 已偵測卡匣但蓋子尚未關閉：可呼叫 CoverClose(true) 關蓋。 ");

                sb.AppendLine(" - 手動噴水/氣刀控制：");
                if (!Pressure)
                    sb.AppendLine(" * 可呼叫 ManualPressureOP(true) 開啟沖水（若無自動）。");
                else
                    sb.AppendLine(" * 若要提前停止沖水可呼叫 ManualPressureOP(false)。");

                sb.AppendLine(" * 可用 ManualAirOP(true/false) 控制氣刀。 ");

                return sb.ToString();
            }
            else // auto mode
            {
                sb.AppendLine(" - 模組處於自動模式：");

                if (_autoStopFlag)
                    sb.AppendLine(" * 自動流程已被要求停止，系統會在空閒時停止。可觀察 Idle 狀態。 ");

                if (_pausing)
                    sb.AppendLine(" * 自動流程暫停中：呼叫 AutoStart() 可恢復，或 AutoStop(force=true) 強制停止。 ");

                // 未完成烘乾前 (act not finished)
                if (!_actFinished)
                {
                    sb.AppendLine(" * 未完成處理階段（尚在沖水/加熱流程）：");

                    if (!_cassette)
                    {
                        if (Command_CleanerCoverClose)
                            sb.AppendLine(" - 蓋子目前要求關閉：若未放入卡匣，呼叫 CoverClose(false) 可打開蓋子以放入卡匣。");
                        if (Sensor_CoverOpen && MotorIdle && !InPos1)
                            sb.AppendLine(" - 蓋子開啟且馬達空閒，但馬達不在上方：可呼叫 MoveToPosition(0,0) 將馬達移至上方以便放卡匣。");
                    }
                    else // 有卡匣
                    {
                        if (!Command_CleanerCoverClose)
                            sb.AppendLine(" - 已放入卡匣且需要關蓋：系統會自動移動馬達至放置位置並關蓋。若延遲太久請檢查馬達狀態。");

                        if (Sensor_CoverClose && !Pressure && !_pausing)
                            sb.AppendLine(" - 卡匣已關蓋且未在沖水：系統會開始沖水流程（SetSV/PressureOP）。");

                        if (Pressure)
                            sb.AppendLine(" - 正在沖水中：等待計時完成或系統暫停/停止。");
                    }
                }
                else // act finished
                {
                    sb.AppendLine(" * 已完成處理階段（烘乾/沖水完成）：");
                    if (_cassette && Command_CleanerCoverClose && !_pressure)
                        sb.AppendLine(" - 蓋子會打開以便取出卡匣，系統也會啟動氣刀吹乾。若需要立刻取卡可暫停自動或關閉氣刀。 ");

                    if (_cassette && Sensor_CoverOpen && !InPos1)
                        sb.AppendLine(" - 蓋子開啟且馬達不在上方：系統會嘗試將馬達移至上方(位置1)。");

                    if (_cassette && InPos1 && MotorIdle && !_motor_commanding && !RetryAirFinished)
                        sb.AppendLine(" - 系統正在進行風刀反覆吹氣，直到 RetryAirFinished 條件滿足。");

                    if (!RetryAirFinished && !_motor_air_up_flag)
                        sb.AppendLine(" - 若想手動結束吹氣可呼叫 ManualAirOP(false)。");
                }

                sb.AppendLine(" - 自動模式下若發生 Alarm/Warning，系統會暫停/停止。請先處理警報再繼續。 ");

                return sb.ToString();
            }
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
            _loopTask = Task.Run(() => LoopAsync(token), token);
        }

        private async Task LoopAsync(CancellationToken token)
        {
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

            if (HS_WaterSystemError)
            {
                if (Pressure) PressureOP(false);
            }

            AutoProcedure();

            await Task.Yield();
        }

        #endregion

        #region Auto Procedure

        private bool _actFinished = false;

        // Heating duration tracking
        private static int _heatingDuration_Minutes = 10; // default minutes, change later as needed
        private readonly TimeSpan _heatingDurationThreshold = TimeSpan.FromMinutes(_heatingDuration_Minutes);
        private DateTime? _heatingStartTime = null;

        private TimeSpan? _elapsedTime = new TimeSpan();



        private void Initialize()
        {
            ServoOn(true);
            ActStartStatus();

            _auto = false;
            _cassette = false;
            _pausing = false;
            _autoStopFlag = false;
            _motor_commanding = false;
            _motor_air_retry_count = 0;

            ResetTimeoutFlag();
            _initialized = true;

            HS_ClamperMoving = false;
            _elapsedTime = new TimeSpan();

            Home();
        }

        private void ActStartStatus()
        {
            PressureOP(false);
            AirOP(false);
            CoverClose(false);
        }

        private void EMOStop()
        {
            MotorStop();
            PressureOP(false);
            AirOP(false);

            _auto = false;
            _initialized = false;

            HS_ClamperMoving = false;
            _elapsedTime = new TimeSpan();
        }

        private void AutoProcedure()
        {
            if (_auto)
            {
                if (Idle && _autoStopFlag)
                {
                    _autoStopFlag = false;
                    _auto = false;
                }


                // 未烘乾完成前程序
                if (!_actFinished)
                {
                    // 蓋子打開等待卡匣放入
                    if (!_cassette && Command_CleanerCoverClose)
                    {
                        CoverClose(false);
                    }

                    // 卡匣放入前確認蓋子打開且馬達在上方位置，若不在原點位置則移動到上方位置
                    if (!_cassette && Sensor_CoverOpen && MotorIdle && !_pausing && !_motor_commanding && !InPos1)
                    {
                        MoveToPosition(0, 0);
                    }

                    //卡匣放入後蓋子關閉
                    if (_cassette && !Command_CleanerCoverClose)
                    {
                        if(!InPos3 && !_motor_commanding && !_pausing)
                            MoveToPosition(2, 0);

                        if (InPos3)
                            CoverClose(true);
                    }



                    // 有卡匣且蓋子關閉後開始加熱
                    if (_cassette && Sensor_CoverClose)
                    {
                        if (!Pressure && !_pausing) // 開始沖水
                        {
                            PressureOP(true);
                        }

                        if (Pressure) // 沖水過程
                        {
                            if (_pausing) // 沖水過程中暫停
                            {
                                PressureOP(false);
                            }

                        }
                    }

                    // 沖水時間計算
                    if (Pressure && !_pausing && Sensor_CoverClose)
                    {
                        // start timer when condition becomes true
                        if (_heatingStartTime == null)
                        {
                            _heatingStartTime = DateTime.UtcNow;
                        }
                        else
                        {
                            // 馬達往復搖擺流程
                            if(InPos3 && MotorIdle && !_motor_commanding && !_pausing)
                            {
                                MoveToPosition(2, 0);
                            }
                            else if(InPos2 && MotorIdle && !_motor_commanding && !_pausing)
                            {
                                MoveToPosition(1, 0);
                            }


                            // 計算時間
                            _elapsedTime += DateTime.UtcNow - _heatingStartTime.Value;
                            _heatingStartTime = DateTime.UtcNow;
                            if (_elapsedTime >= TimeSpan.FromSeconds((double)(_moduleSettings.Sink != null ? _moduleSettings.Sink.ActTime_Second : 60.0)))
                            {
                                PressureOP(false);
                                _actFinished = true;
                                MotorStop();
                            }
                        }
                    }
                    else
                    {
                        // reset timer when condition no longer holds
                        _heatingStartTime = null;
                        MotorStop();
                    }


                    // Clamper放置完成後確認並設定有卡匣
                    if (HS_ClamperPlaceFinished)
                    {
                        HS_ClamperPlaceFinished = false;
                        _cassette = true;
                    }
                }
                else // 烘乾完成程序 
                {
                    // 蓋子打開等待卡匣取出
                    if (_cassette && Command_CleanerCoverClose && !_pressure)
                    {
                        _motor_air_retry_count = 0;
                        _motor_air_up_flag = false;
                        CoverClose(false);
                        AirOP(true);
                    }

                    // 卡匣取出前確認蓋子打開且馬達在上方位置，若不在原點位置則移動到上方位置
                    if (_cassette && Sensor_CoverOpen && !InPos1 && !_motor_commanding && MotorIdle)
                    { 
                        MoveToPosition(0, 0);
                    }

                    // 風刀反覆吹氣流程
                    if (_cassette && InPos1 && MotorIdle && !_motor_commanding)
                    {
                        if (!RetryAirFinished)
                        {
                            MoveToPosition(2, 0);
                            _motor_air_retry_count++;
                        }
                    }

                    // 卡匣取出後流程結束
                    if (HS_ClamperPickFinished)
                    {
                        HS_ClamperPickFinished = false;
                        _cassette = false;
                        _actFinished = false;
                        _elapsedTime = new TimeSpan();
                        AirOP(false);
                    }
                }
            }
        }

        #endregion

        #region Timeout

        private bool _PV_Low_Timeout = false;
        private bool _PV_High_Timeout = false;
        private bool _Cover_Open_Timeout = false;
        private bool _Cover_Close_Timeout = false;

        private void RefreshTimeoutValue()
        {
            if (_unitSettings.Sink != null)
            {
                _pvLowTimeoutThreshold_Value = _unitSettings.Sink.PV_Low_Timeout_Second;
                _pvHighTimeoutThreshold_Value = _unitSettings.Sink.PV_High_Timeout_Second;
                _coverOpenTimeoutThreshold_Value = _unitSettings.Sink.Cover_Close_Timeout_Second;
                _coverCloseTimeoutThreshold_Value = _unitSettings.Sink.Cover_Open_Timeout_Second;

                _pvLowTimeoutThreshold = TimeSpan.FromSeconds(_pvLowTimeoutThreshold_Value);
                _pvHighTimeoutThreshold = TimeSpan.FromSeconds(_pvHighTimeoutThreshold_Value);
                _coverOpenTimeoutThreshold = TimeSpan.FromSeconds(_coverOpenTimeoutThreshold_Value);
                _coverCloseTimeoutThreshold = TimeSpan.FromSeconds(_coverCloseTimeoutThreshold_Value);
            }
        }

        // Timer thresholds (can be adjusted). If module settings provide timeout values they can be used here instead.
        private TimeSpan _pvLowTimeoutThreshold = TimeSpan.FromSeconds(_pvLowTimeoutThreshold_Value);
        private TimeSpan _pvHighTimeoutThreshold = TimeSpan.FromSeconds(_pvHighTimeoutThreshold_Value);
        private TimeSpan _coverOpenTimeoutThreshold = TimeSpan.FromSeconds(_coverOpenTimeoutThreshold_Value);
        private TimeSpan _coverCloseTimeoutThreshold = TimeSpan.FromSeconds(_coverCloseTimeoutThreshold_Value);

        // Start times for each monitored condition (null when condition not active)
        private DateTime? _pvLowStart;
        private DateTime? _pvHighStart;
        private DateTime? _coverOpenStart;
        private DateTime? _coverCloseStart;

        private void ResetTimeoutFlag()
        {
            _PV_Low_Timeout = false;
            _PV_High_Timeout = false;
            _Cover_Open_Timeout = false;
            _Cover_Close_Timeout = false;

            _pvLowStart = null;
            _pvHighStart = null;
            _coverOpenStart = null;
            _coverCloseStart = null;
        }

        private void CheckTimeout()
        {
            // Conditions to monitor (per request):
            bool pvLowCondition = !Pressure && !LowPressure && false; // 因為低壓目的是在將加壓關掉，在本專案溫度控制器並無法降壓，所以暫時不啟用此條件
            bool pvHighCondition = Pressure && !HighPressure;
            bool coverOpenCondition = !Command_CleanerCoverClose && !Sensor_CoverOpen;
            bool coverCloseCondition = Command_CleanerCoverClose && !Sensor_CoverClose;

            var now = DateTime.UtcNow;

            // PV Low
            if (pvLowCondition)
            {
                if (_pvLowStart == null)
                    _pvLowStart = now;
                else if (!_PV_Low_Timeout && now - _pvLowStart >= _pvLowTimeoutThreshold)
                    _PV_Low_Timeout = true;
            }
            else
            {
                _pvLowStart = null;
                _PV_Low_Timeout = false;
            }

            // PV High
            if (pvHighCondition)
            {
                if (_pvHighStart == null)
                    _pvHighStart = now;
                else if (!_PV_High_Timeout && now - _pvHighStart >= _pvHighTimeoutThreshold)
                    _PV_High_Timeout = true;
            }
            else
            {
                _pvHighStart = null;
                _PV_High_Timeout = false;
            }

            // Cover Open
            if (coverOpenCondition)
            {
                if (_coverOpenStart == null)
                    _coverOpenStart = now;
                else if (!_Cover_Open_Timeout && now - _coverOpenStart >= _coverOpenTimeoutThreshold)
                    _Cover_Open_Timeout = true;
            }
            else
            {
                _coverOpenStart = null;
                _Cover_Open_Timeout = false;
            }

            // Cover Close
            if (coverCloseCondition)
            {
                if (_coverCloseStart == null)
                    _coverCloseStart = now;
                else if (!_Cover_Close_Timeout && now - _coverCloseStart >= _coverCloseTimeoutThreshold)
                    _Cover_Close_Timeout = true;
            }
            else
            {
                _coverCloseStart = null;
                _Cover_Close_Timeout = false;
            }
        }

        #endregion

        #region Alarm

        private void ResetMotorAlarm()
        {
            if (_plcService != null)
            {
                try
                {
                    // set the alarm reset command true and clear it after ~3 seconds
                    _plcService.Command_Axis3AlarmReset = true;

                    // fire-and-forget task to reset the command after a delay
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                            if (_plcService != null)
                                _plcService.Command_Axis3AlarmReset = false;
                        }
                        catch
                        {
                            // swallow exceptions from the background task
                        }
                    });
                }
                catch
                {
                    // swallow any immediate exceptions
                }
            }
        }

        private bool _motorAlarm => (_plcService != null) && _plcService.Axis3ErrorAlarm;
        private bool _motorAlarmLimitN => (_plcService != null) && _plcService.Axis3ErrorLimitN;
        private bool _motorAlarmLimitP => (_plcService != null) && _plcService.Axis3ErrorLimitP;
        private bool _motorAlarmHomeTimeout => (_plcService != null) && _plcService.Axis3ErrorHomeTimeout;
        private bool _motorAlarmMoveTimeout => (_plcService != null) && _plcService.Axis3ErrorCommandTimeout;

        private bool _invErrorAlarm => (_deltaMS300 != null) && (_deltaMS300.ErrorCode != 0);
        private bool _invWarningAlarm => (_deltaMS300 != null) && (_deltaMS300.WarningCode != 0);

        #endregion
    }
}
