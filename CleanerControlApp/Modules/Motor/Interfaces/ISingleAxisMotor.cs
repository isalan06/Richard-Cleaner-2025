using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.Motor.Interfaces
{
    public interface ISingleAxisMotor
    {
        void SimMotorPass();

        bool MotorServoOn { get; }
        bool MotorNLimit { get; }
        bool MotorPLimit { get; }
        bool MotorIdle { get; }
        bool MotorBusy { get; }
        bool MotorAlarm { get; }
        bool MotorHoming { get; }
        bool MotorMoving { get; }
        bool MotorHome { get; }
        int Position { get; }
        float Position_Value { get; }

        void ServoOn(bool servo);
        void Jog(bool jog, int dir, int speed);
        void Home();
        void MoveToPosition(int position, int speed);
        void MotorStop();

        bool GetInPos(int position);

        void Teach(int position);

        bool ErrorLimitN { get; }
        bool ErrorLimitP { get; }
        bool ErrorHomeTimeout { get; }
        bool ErrorCommandTimeout { get; }

        void Command_AlarmReset();


    }
}
