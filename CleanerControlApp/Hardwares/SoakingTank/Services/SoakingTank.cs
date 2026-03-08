using CleanerControlApp.Hardwares.HeatingTank.Interfaces;
using CleanerControlApp.Hardwares.SoakingTank.Interfaces;
using CleanerControlApp.Modules.MitsubishiPLC.Interfaces;
using CleanerControlApp.Modules.Modbus.Interfaces;
using CleanerControlApp.Modules.TempatureController.Interfaces;
using CleanerControlApp.Modules.UltrasonicDevice.Interfaces;
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

namespace CleanerControlApp.Hardwares.SoakingTank.Services
{
    public class SoakingTank: ISoakingTank, IDisposable
    {
        #region Constant

        public static readonly int Motor_Index = 3; // Axis-4

        #endregion

        #region attribute

        private readonly ILogger<SoakingTank>? _logger;

        // background loop
        private CancellationTokenSource? _cts;
        private Task? _loopTask;
        private readonly TimeSpan _loopInterval = TimeSpan.FromMilliseconds(10);

        private bool _running;

        private readonly UnitSettings _unitSettings;
        private readonly ModuleSettings _moduleSettings;

        private readonly IPLCOperator? _plcService;
        private readonly IHeatingTank? _heatingTank;
        private readonly IUltrasonicDevice? _ultrasonicDevice;

        private bool _auto = false;
        private bool _pausing = false;
        private bool _ultrasonic = false;
        private bool _cassette = false;
        private bool _initialized = false;
        private bool _autoStopFlag = false;

        private static int _coverOpenTimeoutThreshold_Value = 30;
        private static int _coverCloseTimeoutThreshold_Value = 30;

        private bool _sim_pass_motor = false;

        private bool _motor_commanding = false;
        private int _motor_air_retry_count = 0;
        private bool _motor_air_up_flag = false;
        private bool RetryAirFinished => _moduleSettings.Sink != null && _moduleSettings.Sink.AirKnifeRetryCount == _motor_air_retry_count;

        #endregion

        #region constructor

        public SoakingTank(ILogger<SoakingTank>? logger, IPLCOperator? plcService, UnitSettings unitSettings, ModuleSettings moduleSettings, IHeatingTank? heatingTank, IUltrasonicDevice ultrasonicDevice)
        {
            _unitSettings = unitSettings;
            _moduleSettings = moduleSettings;

            _logger = logger;

            _plcService = plcService;
            _heatingTank = heatingTank;
            _ultrasonicDevice = ultrasonicDevice;

            RefreshTimeoutValue();

            // Alarm
            AlarmManager.AttachFlagGetter("ALM301", () => _Cover_Open_Timeout);
            AlarmManager.AttachFlagGetter("ALM302", () => _Cover_Close_Timeout);
            AlarmManager.AttachFlagGetter("ALM303", () => _motorAlarm);
            AlarmManager.AttachFlagGetter("ALM304", () => _motorAlarmLimitN);
            AlarmManager.AttachFlagGetter("ALM305", () => _motorAlarmLimitP);
            AlarmManager.AttachFlagGetter("ALM306", () => _motorAlarmHomeTimeout);
            AlarmManager.AttachFlagGetter("ALM307", () => _motorAlarmMoveTimeout);

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
        ~SoakingTank()
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

        #region ISoakingTank implementation


        public bool IsRunning => _running;
        public void Start() { _running = true; }
        public void Stop() { _running = false; }

        public bool Sensor_CoverOpen
        {
            get => (_plcService != null) && _plcService.TankCoverFIn;
        }
        public bool Sensor_CoverClose
        {
            get => (_plcService != null) && _plcService.TankCoverBIn;

        }

        public bool Sensor_Liquid_H => _plcService != null && _plcService.TankWaterPosH;
        public bool Sensor_Liquid_L => _plcService != null && _plcService.TankWaterPosL;

        public bool Command_CleanerCoverClose
        {
            get => (_plcService != null) && _plcService.Command_TankCoverOpen;
            set
            {
                if (_plcService != null)
                    _plcService.Command_TankCoverOpen = value;
            }
        }

        public bool Command_CleanerAirOpen
        {
            get => (_plcService != null) && _plcService.Command_TankAirKnifeOpen;

            set
            {
                if (_plcService != null)
                    _plcService.Command_TankAirKnifeOpen = value;
            }
        }

        public bool Command_CleanerWaterOutputOpen
        { 
            get => (_plcService != null) && _plcService.Command_TankOutputWaterValveOpen;
            set
            { 
                if(_plcService != null)
                    _plcService.Command_TankOutputWaterValveOpen = value;
            }
        }

        public bool Command_CleanerUltrasonicOpen
        {
            get => (_ultrasonicDevice != null) && _ultrasonicDevice.UltrasonicEnabled;
            set
            {
                if (_ultrasonicDevice != null)
                {
                    _ultrasonicDevice.UltrasonicOperate(value);
                }
            }
        }

        public float UD_Frequency => _ultrasonicDevice != null ? _ultrasonicDevice.Frequency : 0f;
        public float UD_SetCurrent => _ultrasonicDevice != null ? _ultrasonicDevice.SettingCurrent : 0f;
        public int UD_Time => _ultrasonicDevice != null ? _ultrasonicDevice.Time : 0;
        public int UD_Power => _ultrasonicDevice != null ? _ultrasonicDevice.Power : 0;

        public void SetCurrent(float current)
        { 
            if(_ultrasonicDevice != null)
                _ultrasonicDevice.SetUltrasonicCurrent(current);
        }

        public bool Auto => _auto;
        public bool Pausing => _pausing;
        public bool Ultrasonic => _ultrasonic;
        public bool Cassette => _cassette;
        public bool Initialized => _initialized && (_sim_pass_motor || MotorHome);
        public bool Idle => Sensor_CoverOpen && !_ultrasonic && !_cassette && _initialized && IsNormalStatus && (_sim_pass_motor || (MotorServoOn && MotorIdle && MotorHome));

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
                OperateLog.Log($"浸泡槽 手動氣閥 " + (air ? "開" : "關"), $"浸泡槽 手動氣閥 " + (air ? "開" : "關"));
            }

            return result;
        }

        public bool WaterOutputOP(bool water)
        {
            bool result = false;

            if(_plcService != null)
            {
                Command_CleanerWaterOutputOpen = water;
                result = true;
            }

            return result;
        }
        public bool ManualWaterOutputOP(bool water)
        {
            bool result = false;

            if (_plcService != null)
            { 
                result = WaterOutputOP(water);
                OperateLog.Log($"浸泡槽 手動排水 " + (water ? "開" : "關"), $"浸泡槽 手動排水 " + (water ? "開" : "關"));
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
                OperateLog.Log($"浸泡槽 手動蓋子 " + (close ? "關" : "開"), $"浸泡槽 手動蓋子 " + (close ? "關" : "開"));
            }

            return result;
        }

        public bool UltrasonicOP(bool ultrasonic)
        {
            bool result = false;

            if(_ultrasonicDevice != null)
            {
                if(ultrasonic) SetCurrent(_moduleSettings.SoakingTank != null ? _moduleSettings.SoakingTank.UltrasonicSetCurrent : 1f);
                Command_CleanerUltrasonicOpen = ultrasonic;
                result = true;
            }

            return result;
        }
        public bool ManualUltrasonicOP(bool ultrasonic)
        {
            bool result = false;

            if(_ultrasonicDevice != null)
            {
                result = UltrasonicOP(ultrasonic);
                OperateLog.Log($"浸泡槽 手動超音波 " + (ultrasonic ? "開" : "關"), $"浸泡槽 手動超音波 " + (ultrasonic ? "開" : "關"));
            }

            return result;
        }

        public bool HS_ClamperMoving { get; set; }
        public bool HS_ClamperPickFinished { get; set; }
        public bool HS_ClamperPlaceFinished { get; set; }
        public bool HS_WaterSystemError { get; set; }
        public bool HS_InputPermit => Idle && !_pausing && !HS_ClamperMoving && _auto && InPos1;
        public bool HS_ActFinished => _cassette && Sensor_CoverOpen && !HS_ClamperMoving && !Ultrasonic && _actFinished && InPos1 && RetryAirFinished;

        public int ElpasedPressureTime_Seconds => (int)(_elapsedTime != null ? _elapsedTime.Value.TotalSeconds : 0);
        public int RemainingPressureTime_Seconds => (_moduleSettings.Sink != null) ? _moduleSettings.Sink.ActTime_Second - ElpasedPressureTime_Seconds : 0;

        public bool ModulePass { get; set; }
        public bool HasWarning => _Cover_Open_Timeout || _Cover_Close_Timeout || _motorAlarmHomeTimeout || _motorAlarmMoveTimeout;
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

        public void SimMotorPass()
        {
            _sim_pass_motor = !_sim_pass_motor;
        }

        public bool MotorServoOn => _plcService != null && _plcService.TankZServoServoOn;
        public bool MotorUpLimit => _plcService != null && _plcService.TankZLimitN;
        public bool MotorDownLimit => _plcService != null && _plcService.TankZLimitP;
        public bool MotorIdle => _plcService != null && _plcService.TankZIdle && !_plcService.Axis4CommandDriving && !_plcService.Axis4CommandProcedure && !_plcService.Axis4HomeProcedure;
        public bool MotorBusy => !MotorIdle && _plcService != null && MotorServoOn;
        public bool MotorAlarm => _plcService != null && _plcService.TankZAlarm;
        public bool MotorHoming => _plcService != null && _plcService.Axis4HomeProcedure;
        public bool MotorMoving => _plcService != null && _plcService.Axis4CommandProcedure;
        public bool MotorHome => _plcService != null && _plcService.Axis4HomeComplete;
        public int Posiition => _plcService != null ? _plcService.Axis4Pos : 0;
        public float Position_Value => (_plcService != null && _unitSettings.SoakingTank != null) ? ((float)_plcService.Axis4Pos * _unitSettings.SoakingTank.MotorUnitTransfer) : 1f;

        public void ServoOn(bool servo)
        {
            if (_plcService != null)
            {
                _plcService.Command_Axis4ServoOn = servo;
            }
        }
        public void Jog(bool jog, int dir, int speed) // dir: 0: Down(+), 1: Up(-); speed: 0: low, 1: medium, 2:high
        {
            if (_plcService != null && !MotorAlarm && MotorServoOn && !_auto && !_plcService.Axis4CommandProcedure && !_plcService.Axis4HomeProcedure)
            {
                if (speed == 2)
                    _plcService.Command_Axis4JogSpeedH = true;
                else if (speed == 1)
                {
                    _plcService.Command_Axis4JogSpeedH = false;
                    _plcService.Command_Axis4JogSpeedM = true;
                }
                else
                {
                    _plcService.Command_Axis4JogSpeedH = false;
                    _plcService.Command_Axis4JogSpeedM = false;
                }

                if (jog)
                {
                    if (dir == 0)
                        _plcService.Command_Axis4JogP = true;
                    else
                        _plcService.Command_Axis4JogN = true;
                }
                else
                {
                    _plcService.Command_Axis4JogP = false;
                    _plcService.Command_Axis4JogN = false;
                }

            }
        }
        public void Home()
        {
            if (_plcService != null && !MotorAlarm && MotorServoOn && Idle)
            {
                try
                {
                    _plcService.Command_Axis4Home = true;

                    // fire-and-forget task to reset the command after a delay
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                            if (_plcService != null)
                                _plcService.Command_Axis4Home = false;
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
                    int setPos = _moduleSettings.SoakingTank != null ? _moduleSettings.SoakingTank.MotorPosition_01 : 0; // default to position 1
                    if (position == 2)
                        setPos = _moduleSettings.SoakingTank != null ? _moduleSettings.SoakingTank.MotorPosition_02 : 0;
                    else if (position == 3)
                        setPos = _moduleSettings.SoakingTank != null ? _moduleSettings.SoakingTank.MotorPosition_03 : 0;

                    int setVel = _moduleSettings.SoakingTank != null ? _moduleSettings.SoakingTank.MotorVelocity_01 : 0; // default to velocity 1
                    if (speed == 1)
                        setVel = _moduleSettings.SoakingTank != null ? _moduleSettings.SoakingTank.MotorVelocity_02 : 0;

                    _plcService.Command_Axis4Pos = setPos;
                    _plcService.Command_Axis4Speed = setVel;
                    _plcService.Command_Axis4Command = true;
                    // fire-and-forget task to reset the command after a delay

                    _motor_commanding = true;

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                            if (_plcService != null)
                                _plcService.Command_Axis4Command = false;

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
                    _plcService.Command_Axis4Stop = true;

                    // fire-and-forget task to reset the command after a delay
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                            if (_plcService != null)
                                _plcService.Command_Axis4Stop = false;
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

        public bool InPos1 => ((_plcService != null) && (Posiition == (_moduleSettings.SoakingTank != null ? _moduleSettings.SoakingTank.MotorPosition_01 : 0))) || _sim_pass_motor;
        public bool InPos2 => ((_plcService != null) && (Posiition == (_moduleSettings.SoakingTank != null ? _moduleSettings.SoakingTank.MotorPosition_02 : 0))) || _sim_pass_motor;
        public bool InPos3 => ((_plcService != null) && (Posiition == (_moduleSettings.SoakingTank != null ? _moduleSettings.SoakingTank.MotorPosition_03 : 0))) || _sim_pass_motor;

        public void Teach(int position)
        {
            if (_plcService != null && _moduleSettings.SoakingTank != null)
            {
                if (position == 0)
                    _moduleSettings.SoakingTank.MotorPosition_01 = Posiition;
                else if (position == 1)
                    _moduleSettings.SoakingTank.MotorPosition_02 = Posiition;
                else if (position == 2)
                    _moduleSettings.SoakingTank.MotorPosition_03 = Posiition;

                if (position >= 0 && position < 3)
                    ConfigLoader.SetModuleSettings(_moduleSettings);
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
                if (Ultrasonic) UltrasonicOP(false);
            }

            await AutoProcedure();

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
            UltrasonicOP(false);
            AirOP(false);
            CoverClose(false);
            WaterOutputOP(true);
        }

        private void EMOStop()
        {
            MotorStop();
            UltrasonicOP(false);
            AirOP(false);
            WaterOutputOP(false);

            _auto = false;
            _initialized = false;

            HS_ClamperMoving = false;
            _elapsedTime = new TimeSpan();
        }

        private async Task AutoProcedure()
        {
            if (_private_waste_HAlarm || HS_WaterSystemError)
            {
                if(_heatingTank != null && _heatingTank.HS_RequestWater) _heatingTank.HS_RequestWater = false;
                if (Ultrasonic) UltrasonicOP(false);
                if(Command_CleanerWaterOutputOpen) Command_CleanerWaterOutputOpen = false;
            }

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
                        if (!InPos3 && !_motor_commanding && !_pausing)
                            MoveToPosition(2, 0);

                        if (InPos3)
                            CoverClose(true);
                    }

                    // 有卡匣但非滿水位時入水
                    if(_cassette && !Sensor_Liquid_H)
                    {
                        if (Command_CleanerWaterOutputOpen)
                            WaterOutputOP(false);
                        if (_heatingTank != null && !Command_CleanerWaterOutputOpen && !_heatingTank.HS_RequestWater)
                            _heatingTank.HS_RequestWater = true;
                    }
                    else
                    {
                        if (_heatingTank != null && _heatingTank.HS_RequestWater)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false); // delay to ensure water level sensor updates
                            _heatingTank.HS_RequestWater = false;
                        }

                        if (Command_CleanerWaterOutputOpen)
                            Command_CleanerWaterOutputOpen = false;
                    }

                    // 有卡匣及滿水位且蓋子關閉後開始
                    if (_cassette && Sensor_CoverClose)
                    {
                        if (!Ultrasonic && !_pausing && Sensor_Liquid_H) // 開始沖水
                        {
                            UltrasonicOP(true);
                        }

                        if (Ultrasonic) // 沖水過程
                        {
                            if (_pausing || !Sensor_Liquid_H) // 沖水過程中暫停
                            {
                                UltrasonicOP(false);
                            }

                        }
                    }

                    // 沖水時間計算
                    if (Ultrasonic && !_pausing && Sensor_CoverClose)
                    {
                        // start timer when condition becomes true
                        if (_heatingStartTime == null)
                        {
                            _heatingStartTime = DateTime.UtcNow;
                        }
                        else
                        {
                            // 馬達往復搖擺流程
                            if (InPos3 && MotorIdle && !_motor_commanding && !_pausing)
                            {
                                MoveToPosition(2, 0);
                            }
                            else if (InPos2 && MotorIdle && !_motor_commanding && !_pausing)
                            {
                                MoveToPosition(1, 0);
                            }


                            // 計算時間
                            _elapsedTime += DateTime.UtcNow - _heatingStartTime.Value;
                            _heatingStartTime = DateTime.UtcNow;
                            if (_elapsedTime >= TimeSpan.FromSeconds((double)(_moduleSettings.Sink != null ? _moduleSettings.Sink.ActTime_Second : 60.0)))
                            {
                                UltrasonicOP(false);
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
                else // 超音波完成程序 
                {
                    if(Sensor_Liquid_L && !Command_CleanerWaterOutputOpen) 
                        Command_CleanerWaterOutputOpen = true;

                    // 蓋子打開等待卡匣取出
                    if (_cassette && Command_CleanerCoverClose && !Ultrasonic && !Sensor_Liquid_L)
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

        private bool _Cover_Open_Timeout = false;
        private bool _Cover_Close_Timeout = false;

        private void RefreshTimeoutValue()
        {
            if (_unitSettings.Sink != null)
            {
                _coverOpenTimeoutThreshold_Value = _unitSettings.Sink.Cover_Close_Timeout_Second;
                _coverCloseTimeoutThreshold_Value = _unitSettings.Sink.Cover_Open_Timeout_Second;

                _coverOpenTimeoutThreshold = TimeSpan.FromSeconds(_coverOpenTimeoutThreshold_Value);
                _coverCloseTimeoutThreshold = TimeSpan.FromSeconds(_coverCloseTimeoutThreshold_Value);
            }
        }

        // Timer thresholds (can be adjusted). If module settings provide timeout values they can be used here instead.
        private TimeSpan _coverOpenTimeoutThreshold = TimeSpan.FromSeconds(_coverOpenTimeoutThreshold_Value);
        private TimeSpan _coverCloseTimeoutThreshold = TimeSpan.FromSeconds(_coverCloseTimeoutThreshold_Value);

        // Start times for each monitored condition (null when condition not active)
        private DateTime? _coverOpenStart;
        private DateTime? _coverCloseStart;

        private void ResetTimeoutFlag()
        {
            _Cover_Open_Timeout = false;
            _Cover_Close_Timeout = false;

            _coverOpenStart = null;
            _coverCloseStart = null;
        }

        private void CheckTimeout()
        {
            // Conditions to monitor (per request):
            bool coverOpenCondition = !Command_CleanerCoverClose && !Sensor_CoverOpen;
            bool coverCloseCondition = Command_CleanerCoverClose && !Sensor_CoverClose;

            var now = DateTime.UtcNow;

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
                    _plcService.Command_Axis4AlarmReset = true;

                    // fire-and-forget task to reset the command after a delay
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                            if (_plcService != null)
                                _plcService.Command_Axis4AlarmReset = false;
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

        private bool _motorAlarm => (_plcService != null) && _plcService.Axis4ErrorAlarm;
        private bool _motorAlarmLimitN => (_plcService != null) && _plcService.Axis4ErrorLimitN;
        private bool _motorAlarmLimitP => (_plcService != null) && _plcService.Axis4ErrorLimitP;
        private bool _motorAlarmHomeTimeout => (_plcService != null) && _plcService.Axis4ErrorHomeTimeout;
        private bool _motorAlarmMoveTimeout => (_plcService != null) && _plcService.Axis4ErrorCommandTimeout;

        private bool _private_waste_HAlarm => (_plcService != null) && _plcService.WasteWaterPosH;

        #endregion
    }
}
