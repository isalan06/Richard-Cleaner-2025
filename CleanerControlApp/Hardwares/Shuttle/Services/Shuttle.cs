using CleanerControlApp.Hardwares.Shuttle.Interfaces;
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
        private bool _autoStopFlag = false;

        private bool _sim_pass_motor = false;

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
            AlarmManager.AttachFlagGetter("ALM115", () => _pickProcedureError);
            AlarmManager.AttachFlagGetter("ALM116", () => _placeProcedureError);
            AlarmManager.AttachFlagGetter("ALM117", () => _checkCassetteProcedureError);

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
        public bool Sensor_ClamperOpen => _plcService != null && _plcService.ShuttleZClampOpen;
        public bool Sensor_ClamperClose => _plcService != null && _plcService.ShuttleZClampClose;

        public bool Check_ClamperOpen => Sensor_ClamperFrontOpen && Sensor_ClamperBackOpen && Sensor_ClamperOpen;
        public bool Check_ClamperClose => Sensor_ClamperFrontClose && Sensor_ClamperBackClose && Sensor_ClamperClose;

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
        public bool MotorAlarm => _motorXAlarm || _motorZAlarm;
        public bool MotorHoming => (_motorXAxis != null && _motorXAxis.MotorHoming) || (_motorZAxis != null && _motorZAxis.MotorHoming);
        public bool MotorMoving => (_motorXAxis != null && _motorXAxis.MotorMoving) || (_motorZAxis != null && _motorZAxis.MotorMoving);
        public bool MotorHome => _motorXAxis != null && _motorXAxis.MotorHome && _motorZAxis != null && _motorZAxis.MotorHome;

        public bool ZInIdlePosition => _motorZAxis != null && _motorZAxis.GetInPos(0);

        public bool HasCassette => _cassette && Check_ClamperClose && Check_ClamperCassetteExist;
        public bool IsEmpty => !_cassette && Check_ClamperOpen && !Check_ClamperCassetteExist;

        // position 1~14 for pick/place position, 0 for original position
        public bool PickCassette(int position)
        {
            bool result = false;

            if (IsEmpty && !Moving && !MotorMoving && IsNormalStatus && MotorHome && ZInIdlePosition)
            {
                if (position > 0 && position < 15)
                {
                    GetActParameters(position, out _actPositionX, out _actVelocityX, out _actPositionZ, out _actVelocityZ);
                    _pickTrigger = true;
                    result = true;
                }
            }

            return result;
        }
        public bool PlaceCassette(int position)
        {
            bool result = false;

            if (HasCassette && !Moving && !MotorMoving && IsNormalStatus && MotorHome && ZInIdlePosition)
            {
                if(position > 0 && position < 15)
                {
                    GetActParameters(position, out _actPositionX, out _actVelocityX, out _actPositionZ, out _actVelocityZ);
                    _placeTrigger = true;
                    result = true;
                }
                
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

                if (!_motorZAxis.ErrorHomeTimeout && !_motorZAxis.MotorAlarm && !_motorZAxis.ErrorCommandTimeout)
                { 
                    _motorXAxis.Home();
                    while (!_motorXAxis.MotorHome && !_motorXAxis.ErrorHomeTimeout && !_motorXAxis.MotorAlarm)
                    {
                        await Task.Delay(500);
                    }

                    _motorZAxis.MoveToPosition(0, 0);

                    while (!_motorZAxis.GetInPos(0))
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

            _moving = false;

            _pickTrigger = false;
            _placeTrigger = false;

        }

        private void MotorStop()
        {
            if (_motorXAxis != null) _motorXAxis.MotorStop();
            if (_motorZAxis != null) _motorZAxis.MotorStop();
        }   

        private void AutoProcedure()
        {
            if (!_pickTrigger && _pickCase != 0) _pickCase = 0;
            if (!_placeTrigger && _placeCase != 0) _placeCase = 0;
            if (!_checkCassetteTrigger && _checkCassetteCase != 0) _checkCassetteCase = 0;

            if (_auto)
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
                                _motorXAxis.MoveToPosition(_actPositionX, _actVelocityX);
                            break;

                        case 10: // Z Axis Move To Pick Position
                            if (_motorZAxis.GetInPos(_actPositionZ))
                            {
                                _pickCase = 20;
                            }
                            else if (!MotorMoving)
                                _motorZAxis.MoveToPosition(_actPositionZ, _actVelocityZ);
                            break;

                        case 20: // Clamper Close to Pick Cassette
                            if (Check_ClamperClose)
                            {
                                _pickCase = 30;
                            }
                            else if (!Command_ClamperClose)
                                ClamperCloseOP(true);

                            break;

                        case 30: // Z Axis Move To Original Position (P1)
                            if (_motorZAxis.GetInPos(0))
                            {
                                _pickCase = 40;
                            }
                            else if (!MotorMoving)
                                _motorZAxis.MoveToPosition(0, _actVelocityZ);

                            break;

                        case 40: // Check Cassette Exist
                            if (HasCassette)
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
                                _motorXAxis.MoveToPosition(_actPositionX, _actVelocityX);
                            break;

                        case 10: // Z Axis Move To Place Position
                            if (_motorZAxis.GetInPos(_actPositionZ))
                            {
                                _placeCase = 20;
                            }
                            else if (!MotorMoving)
                                _motorZAxis.MoveToPosition(_actPositionZ, _actVelocityZ);
                            break;

                        case 20: // Clamper Open to Place Cassette
                            if (Check_ClamperOpen)
                            {
                                _placeCase = 30;
                            }
                            else if (!Command_ClamperOpen)
                                ClamperOpenOP(true);

                            break;

                        case 30: // Z Axis Move To Original Position (P1)
                            if (_motorZAxis.GetInPos(0))
                            {
                                _placeCase = 40;
                            }
                            else if (!MotorMoving)
                                _motorZAxis.MoveToPosition(0, _actVelocityZ);

                            break;

                        case 40: // Check Cassette Exist
                            if (IsEmpty)
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

                // Check Cassette of Tank Procedure
                if (_checkCassetteTrigger && !_pausing && _motorXAxis != null && _motorZAxis != null)
                {
                    switch (_checkCassetteCase)
                    {
                        case 0: // Axis Z Back To Original
                            if (_motorZAxis.GetInPos(0))
                            {
                                _checkCassetteCase = 10;
                            }
                            else if (!MotorMoving)
                                _motorZAxis.MoveToPosition(0, 1);
                            break;

                        case 10: // Axis X Move To Sink (P17)
                            if (_motorXAxis.GetInPos(16))
                            {
                                _checkCassetteCase = 20;
                            }
                            else if (!MotorMoving)
                                _motorXAxis.MoveToPosition(16, 1);
                            break;

                        case 20: // Axis Z Move To Sink (P8)
                            if (_motorZAxis.GetInPos(7))
                            {
                                _checkCassetteCase = 30;
                            }
                            else if (!MotorMoving)
                                _motorZAxis.MoveToPosition(7, 1);
                            break;

                        case 30: // Check Cassette
                            HS_Check_SinkCassetteExist = Check_TankCassetteExist;
                            _checkCassetteCase = 100;
                            break;

                        case 100: // Axis Z Back To Original
                            if (_motorZAxis.GetInPos(0))
                            {
                                _checkCassetteCase = 110;
                            }
                            else if (!MotorMoving)
                                _motorZAxis.MoveToPosition(0, 1);
                            break;

                        case 110: // Axis X Move To Soaking Tank (P18)
                            if (_motorXAxis.GetInPos(17))
                            {
                                _checkCassetteCase = 120;
                            }
                            else if (!MotorMoving)
                                _motorXAxis.MoveToPosition(17, 1);
                            break;

                        case 120: // Axis Z Move To Sink (P9)
                            if (_motorZAxis.GetInPos(8))
                            {
                                _checkCassetteCase = 130;
                            }
                            else if (!MotorMoving)
                                _motorZAxis.MoveToPosition(8, 1);
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
                                _motorZAxis.MoveToPosition(0, 1);
                            break;

                        case 210: // Axis X Move To Drying Tank 1 (P19)
                            if (_motorXAxis.GetInPos(18))
                            {
                                _checkCassetteCase = 220;
                            }
                            else if (!MotorMoving)
                                _motorXAxis.MoveToPosition(18, 1);
                            break;

                        case 220: // Axis Z Move To Sink (P10)
                            if (_motorZAxis.GetInPos(9))
                            {
                                _checkCassetteCase = 230;
                            }
                            else if (!MotorMoving)
                                _motorZAxis.MoveToPosition(9, 1);
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
                                _motorZAxis.MoveToPosition(0, 1);
                            break;

                        case 310: // Axis X Move To Drying Tank 2 (P20)
                            if (_motorXAxis.GetInPos(19))
                            {
                                _checkCassetteCase = 320;
                            }
                            else if (!MotorMoving)
                                _motorXAxis.MoveToPosition(19, 1);
                            break;

                        case 320: // Axis Z Move To Sink (P11)
                            if (_motorZAxis.GetInPos(10))
                            {
                                _checkCassetteCase = 330;
                            }
                            else if (!MotorMoving)
                                _motorZAxis.MoveToPosition(10, 1);
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
                                _motorZAxis.MoveToPosition(0, 1);
                            break;

                        case 410: // Axis X Back To Original
                            if (_motorXAxis.GetInPos(0))
                            {
                                _checkCassetteCase = 420;
                            }
                            else if (!MotorMoving)
                                _motorXAxis.MoveToPosition(0, 1);
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
