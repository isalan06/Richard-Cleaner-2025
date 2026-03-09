using CleanerControlApp.Hardwares.Shuttle.Interfaces;
using CleanerControlApp.Modules.DeltaMS300.Interfaces;
using CleanerControlApp.Modules.DeltaMS300.Services;
using CleanerControlApp.Modules.MitsubishiPLC.Interfaces;
using CleanerControlApp.Modules.Motor.Interfaces;
using CleanerControlApp.Modules.TempatureController.Interfaces;
using CleanerControlApp.Utilities;
using CleanerControlApp.Utilities.Alarm;
using CleanerControlApp.Utilities.Log;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private bool _autoStopFlag = false;

        private bool _sim_pass_motor = false;

        private static int _clamperTimeoutThreshold_Value = 30;

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
            AlarmManager.AttachFlagGetter("ALM110", () => _motorXAlarm);
            AlarmManager.AttachFlagGetter("ALM111", () => _motorXAlarmLimitN);
            AlarmManager.AttachFlagGetter("ALM112", () => _motorXAlarmLimitP);
            AlarmManager.AttachFlagGetter("ALM113", () => _motorXAlarmHomeTimeout);
            AlarmManager.AttachFlagGetter("ALM114", () => _motorXAlarmMoveTimeout);

            StartLoop();

            Start();
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
        public void Start() { _running = true; }
        public void Stop() { _running = false; }

        public ISingleAxisMotor? ShuttleXMotor => _motorXAxis;
        public ISingleAxisMotor? ShuttleZMotor => _motorZAxis;

        public bool Sensor_ClamperFrontOpen => _plcService != null && _plcService.ShuttleZFClamperOpen;
        public bool Sensor_ClamperFrontClose => _plcService != null && _plcService.ShuttleZFClamperClose;
        public bool Sensor_ClamperBackOpen => _plcService != null && _plcService.ShuttleZBClamperOpen;
        public bool Sensor_ClamperBackClose => _plcService != null && _plcService.ShuttleZBClamperClose;
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
        public bool Moving => _moving;
        public bool Cassette => _cassette;
        public bool Initialized => _initialized && (_sim_pass_motor || MotorHome);
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
        public bool HasAlarm => MotorAlarm || _motorXAlarmLimitN || _motorXAlarmLimitP || _motorZAlarmLimitN || _motorZAlarmLimitP;
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
        }


        public bool MotorServoOn => _motorXAxis != null && _motorXAxis.MotorServoOn && _motorZAxis != null && _motorZAxis.MotorServoOn;
        public bool MotorIdle => _motorXAxis != null && _motorXAxis.MotorIdle && _motorZAxis != null && _motorZAxis.MotorIdle;
        public bool MotorBusy => (_motorXAxis != null && _motorXAxis.MotorBusy) || (_motorZAxis != null && _motorZAxis.MotorBusy);
        public bool MotorAlarm => (_motorXAxis != null && _motorXAxis.MotorAlarm) || (_motorZAxis != null && _motorZAxis.MotorAlarm);
        public bool MotorHoming => (_motorXAxis != null && _motorXAxis.MotorHoming) || (_motorZAxis != null && _motorZAxis.MotorHoming);
        public bool MotorMoving => (_motorXAxis != null && _motorXAxis.MotorMoving) || (_motorZAxis != null && _motorZAxis.MotorMoving);
        public bool MotorHome => _motorXAxis != null && _motorXAxis.MotorHome && _motorZAxis != null && _motorZAxis.MotorHome;

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


            AutoProcedure();

            await Task.Yield();
        }

        #endregion

        #region Auto Procedure


        private async void Initialize()
        {
            if (_motorXAxis != null) _motorXAxis.ServoOn(true);
            if (_motorZAxis != null) _motorZAxis.ServoOn(true);

            ActStartStatus();

            _auto = false;
            _cassette = false;
            _pausing = false;
            _autoStopFlag = false;

            ResetTimeoutFlag();
            
            await Home();

            if(!MotorAlarm && MotorHome)
                _initialized = true;
        }

        private async Task Home()
        {
            if (_motorXAxis != null && _motorZAxis != null)
            { 
                _motorZAxis.Home();

                while (!_motorZAxis.MotorHome && !_motorZAxis.ErrorHomeTimeout && !_motorZAxis.MotorAlarm)
                { 
                    await Task.Delay(500);
                }

                if (!_motorZAxis.ErrorHomeTimeout && !_motorZAxis.MotorAlarm)
                { 
                    _motorXAxis.Home();
                    while (!_motorXAxis.MotorHome && !_motorXAxis.ErrorHomeTimeout && !_motorXAxis.MotorAlarm)
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

        }

        private void MotorStop()
        {
            if (_motorXAxis != null) _motorXAxis.MotorStop();
            if (_motorZAxis != null) _motorZAxis.MotorStop();
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

            }
            catch
            {
                // swallow any immediate exceptions
            }
        }

        private bool _motorXAlarm => _motorXAxis != null && _motorXAxis.MotorAlarm;
        private bool _motorXAlarmLimitN => _motorXAxis != null && _motorXAxis.ErrorLimitN;
        private bool _motorXAlarmLimitP => _motorXAxis != null && _motorXAxis.ErrorLimitP;
        private bool _motorXAlarmHomeTimeout => _motorXAxis != null && _motorXAxis.ErrorHomeTimeout;
        private bool _motorXAlarmMoveTimeout => _motorXAxis != null && _motorXAxis.ErrorCommandTimeout;

        private bool _motorZAlarm => _motorZAxis != null && _motorZAxis.MotorAlarm;
        private bool _motorZAlarmLimitN => _motorZAxis != null && _motorZAxis.ErrorLimitN;
        private bool _motorZAlarmLimitP => _motorZAxis != null && _motorZAxis.ErrorLimitP;
        private bool _motorZAlarmHomeTimeout => _motorZAxis != null && _motorZAxis.ErrorHomeTimeout;
        private bool _motorZAlarmMoveTimeout => _motorZAxis != null && _motorZAxis.ErrorCommandTimeout;


        #endregion
    }
}
