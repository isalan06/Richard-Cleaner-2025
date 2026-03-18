using CleanerControlApp.Hardwares.Sink.Services;
using CleanerControlApp.Modules.DeltaMS300.Interfaces;
using CleanerControlApp.Modules.MitsubishiPLC.Interfaces;
using CleanerControlApp.Modules.Motor.Interfaces;
using CleanerControlApp.Modules.TempatureController.Interfaces;
using CleanerControlApp.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.Motor.Services
{
    public class SingleAxisMotor : ISingleAxisMotor, IDisposable
    {

        #region attribute

        private readonly ILogger<SingleAxisMotor>? _logger;

        private readonly ModuleSettings _moduleSettings;
        private readonly US_Motor _unitMotor;
        private readonly MS_Motor _moduleMotor;

        private readonly IPLCOperator? _plcService;

        private int _moduleIndex = 0;

        private bool _sim_pass_motor = false;

        private bool _motor_commanding = false;

        #endregion

        #region constructor

        public SingleAxisMotor(int moduleIndex, ILogger<SingleAxisMotor>? logger, UnitSettings unitSettings, ModuleSettings moduleSettings, IPLCOperator? plcService)
        {
            _moduleIndex = moduleIndex;
            _logger = logger;
            _moduleSettings = moduleSettings;
            _unitMotor = unitSettings.Motors != null ? unitSettings.Motors[moduleIndex] : new US_Motor();
            _moduleMotor = moduleSettings.Motors != null ? moduleSettings.Motors[moduleIndex] : new MS_Motor();
            _plcService = plcService;

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
                    // TODO: 處置受控狀態 (受控物件)
                }

                // TODO: 釋出非受控資源 (非受控物件) 並覆寫完成項
                // TODO: 將大型欄位設為 Null
                disposedValue = true;
            }
        }

        // TODO: 僅有當 'Dispose(bool disposing)' 具有會釋出非受控資源的程式碼時，才覆寫完成項
        ~SingleAxisMotor()
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

        #region ISingleAxisMotor

        public void SimMotorPass()
        {
            _sim_pass_motor = !_sim_pass_motor;
        }
        public void SimMotorPass(bool pass)
        {
            _sim_pass_motor = pass;
        }

        public bool MotorServoOn => _plcService != null && (_moduleIndex == 1 ? _plcService.ShuttleZServoServoOn : _plcService.ShuttleXServoServoOn);
        public bool MotorNLimit => _plcService != null && (_moduleIndex == 1 ? _plcService.ShuttleZLimitN : _plcService.ShuttleXLimitN);
        public bool MotorPLimit => _plcService != null && (_moduleIndex == 1 ? _plcService.ShuttleZLimitP : _plcService.ShuttleXLimitP);
        public bool MotorIdle => _plcService != null && (_moduleIndex == 1 ? (_plcService.ShuttleZIdle && !_plcService.Axis2CommandDriving && !_plcService.Axis2CommandProcedure && !_plcService.Axis2HomeProcedure) :
            (_plcService.ShuttleXIdle && !_plcService.Axis1CommandDriving && !_plcService.Axis1CommandProcedure && !_plcService.Axis1HomeProcedure));
        public bool MotorBusy => !MotorIdle && _plcService != null && MotorServoOn;
        public bool MotorAlarm => _plcService != null && (_moduleIndex == 1 ? _plcService.ShuttleZAlarm : _plcService.ShuttleXAlarm);
        public bool MotorHoming => _plcService != null && (_moduleIndex == 1 ? _plcService.Axis2HomeProcedure : _plcService.Axis1HomeProcedure);
        public bool MotorMoving => _plcService != null && (_moduleIndex == 1 ? _plcService.Axis2CommandProcedure : _plcService.Axis1CommandProcedure);
        public bool MotorHome => _plcService != null && (_moduleIndex == 1 ? _plcService.Axis2HomeComplete : _plcService.Axis1HomeComplete);
        public int Position => _plcService != null ? (_moduleIndex == 1 ? _plcService.Axis2Pos : _plcService.Axis1Pos) : 0;
        public float Position_Value => _plcService != null ? ((float)(_moduleIndex == 1 ? _plcService.Axis2Pos : _plcService.Axis1Pos) * _unitMotor.UnitTransfer) : 1f;

        public void ServoOn(bool servo)
        {
            if (_plcService != null)
            {
                if(_moduleIndex == 1)
                    _plcService.Command_Axis2ServoOn = servo;
                else
                    _plcService.Command_Axis1ServoOn = servo;
            }
        }
        public void Jog(bool jog, int dir, int speed) // dir: 0: Down(+), 1: Up(-); speed: 0: low, 1: medium, 2:high
        {
            if (_moduleIndex == 1)
            {
                if (_plcService != null && !MotorAlarm && MotorServoOn && !_plcService.Axis2CommandProcedure && !_plcService.Axis2HomeProcedure)
                {
                    if (speed == 2)
                        _plcService.Command_Axis2JogSpeedH = true;
                    else if (speed == 1)
                    {
                        _plcService.Command_Axis2JogSpeedH = false;
                        _plcService.Command_Axis2JogSpeedM = true;
                    }
                    else
                    {
                        _plcService.Command_Axis2JogSpeedH = false;
                        _plcService.Command_Axis2JogSpeedM = false;
                    }

                    if (jog)
                    {
                        if (dir == 0)
                            _plcService.Command_Axis2JogP = true;
                        else
                            _plcService.Command_Axis2JogN = true;
                    }
                    else
                    {
                        _plcService.Command_Axis2JogP = false;
                        _plcService.Command_Axis2JogN = false;
                    }

                }
            }
            else
            {
                if (_plcService != null && !MotorAlarm && MotorServoOn && !_plcService.Axis1CommandProcedure && !_plcService.Axis1HomeProcedure)
                {
                    if (speed == 2)
                        _plcService.Command_Axis1JogSpeedH = true;
                    else if (speed == 1)
                    {
                        _plcService.Command_Axis1JogSpeedH = false;
                        _plcService.Command_Axis1JogSpeedM = true;
                    }
                    else
                    {
                        _plcService.Command_Axis1JogSpeedH = false;
                        _plcService.Command_Axis1JogSpeedM = false;
                    }

                    if (jog)
                    {
                        if (dir == 0)
                            _plcService.Command_Axis1JogP = true;
                        else
                            _plcService.Command_Axis1JogN = true;
                    }
                    else
                    {
                        _plcService.Command_Axis1JogP = false;
                        _plcService.Command_Axis1JogN = false;
                    }

                }
            }
        }
        public void Home()
        {
            if (_plcService != null && !MotorAlarm && MotorServoOn && MotorIdle)
            {
                try
                {
                    if (_moduleIndex == 1)
                    {
                        _plcService.Command_Axis2Home = true;

                        // fire-and-forget task to reset the command after a delay
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                                if (_plcService != null)
                                    _plcService.Command_Axis2Home = false;
                            }
                            catch
                            {
                                // swallow exceptions from the background task
                            }
                        });
                    }
                    else
                    {
                        _plcService.Command_Axis1Home = true;

                        // fire-and-forget task to reset the command after a delay
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                                if (_plcService != null)
                                    _plcService.Command_Axis1Home = false;
                            }
                            catch
                            {
                                // swallow exceptions from the background task
                            }
                        });
                    }
                }
                catch { }
            }
        }
        public void MoveToPosition(int position, int speed)
        {
            if (_plcService != null && !MotorAlarm && MotorServoOn && MotorIdle)
            {
                try
                {

                    int setPos = 0;
                    int setVel = 0;

                    if(_moduleMotor.Positions != null && position >= 0 && position <= _moduleMotor.Positions.Count)
                        setPos = _moduleMotor.Positions[position];
                    if(_moduleMotor.Velocities != null && speed >= 0 && speed <= _moduleMotor.Velocities.Count)
                        setVel = _moduleMotor.Velocities[speed]; 

                    if(_moduleIndex == 1)
                    {
                        _plcService.Command_Axis2Pos = setPos;
                        _plcService.Command_Axis2Speed = setVel;
                        _plcService.Command_Axis2Command = true;
                    }
                    else
                    {
                        _plcService.Command_Axis1Pos = setPos;
                        _plcService.Command_Axis1Speed = setVel;
                        _plcService.Command_Axis1Command = true;
                    }
                    // fire-and-forget task to reset the command after a delay

                    _motor_commanding = true;

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                            if (_plcService != null)
                            {
                                if(_moduleIndex == 1)
                                    _plcService.Command_Axis2Command = false;
                                else
                                    _plcService.Command_Axis1Command = false;
                            }

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
                    if(_moduleIndex == 1)
                        _plcService.Command_Axis2Stop = true;
                    else
                        _plcService.Command_Axis1Stop = true;

                    // fire-and-forget task to reset the command after a delay
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                            if (_plcService != null)
                            {
                                if(_moduleIndex == 1)
                                    _plcService.Command_Axis2Stop = false;
                                else
                                    _plcService.Command_Axis1Stop = false;
                            }
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


        public bool GetInPos(int position)
        {
            bool result = false;

            if (_plcService != null)
            {
                result = (Position == (_moduleMotor.Positions != null ? _moduleMotor.Positions[position] : 0) || _sim_pass_motor);

            }

            return result;
        }

        public void Teach(int position)
        {
            if (_plcService != null && _moduleMotor.Positions != null && position >= 0 && position < _moduleMotor.Positions.Count)
            {
                _moduleMotor.Positions[position] = Position;

                ConfigLoader.SetModuleSettings(_moduleSettings);
            }
        }

        public bool ErrorLimitN => _plcService != null && (_moduleIndex == 1 ? _plcService.Axis2ErrorLimitN : _plcService.Axis1ErrorLimitN);
        public bool ErrorLimitP =>_plcService != null && (_moduleIndex == 1 ? _plcService.Axis2ErrorLimitP : _plcService.Axis1ErrorLimitP);
        public bool ErrorHomeTimeout => _plcService != null && (_moduleIndex == 1 ? _plcService.Axis2ErrorHomeTimeout : _plcService.Axis1ErrorHomeTimeout);
        public bool ErrorCommandTimeout => _plcService != null && (_moduleIndex == 1 ? _plcService.Axis2ErrorCommandTimeout : _plcService.Axis1ErrorCommandTimeout);

        public void Command_AlarmReset()
        {
            if (_plcService != null)
            {
                try
                {
                    if(_moduleIndex == 1)
                        _plcService.Command_Axis2AlarmReset = true;
                    else
                        _plcService.Command_Axis1AlarmReset = true;
                    // fire-and-forget task to reset the command after a delay
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
                            if (_plcService != null)
                            {
                                if(_moduleIndex == 1)
                                    _plcService.Command_Axis2AlarmReset = false;
                                else
                                    _plcService.Command_Axis1AlarmReset = false;
                            }
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

        #endregion
    }
}
