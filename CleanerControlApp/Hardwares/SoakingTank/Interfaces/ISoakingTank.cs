using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Hardwares.SoakingTank.Interfaces
{
    public interface ISoakingTank
    {
        bool IsRunning { get; }
        void Start();
        void Stop();

        bool Sensor_CoverOpen { get; }
        bool Sensor_CoverClose { get; }

        bool Sensor_Liquid_H { get; }
        bool Sensor_Liquid_L { get; }

        bool Command_CleanerCoverClose { get; set; }
        bool Command_CleanerAirOpen { get; set; }
        bool Command_CleanerWaterOutputOpen { get; set; }
        bool Command_CleanerUltrasonicOpen { get; set; }

        float UD_Frequency { get; }
        float UD_SetCurrent { get; }
        int UD_Time { get; }
        int UD_Power { get; }

        void SetCurrent(float current);

        bool Auto { get; }
        bool Pausing { get; }
        bool Ultrasonic { get; }
        bool Cassette { get; }
        bool Initialized { get; }
        bool Idle { get; }

        bool AirOP(bool air);
        bool ManualAirOP(bool air);

        bool WaterOutputOP(bool water);
        bool ManualWaterOutputOP(bool water);

        bool CoverClose(bool close);
        bool ManualCoverClose(bool close);

        bool UltrasonicOP(bool ultrasonic);
        bool ManualUltrasonicOP(bool ultrasonic);

        bool WaterInOP(bool water);
        bool ManualWaterInOP(bool water);

        bool HS_ClamperMoving { get; set; }
        bool HS_ClamperPickFinished { get; set; }
        bool HS_ClamperPlaceFinished { get; set; }
        bool HS_WaterSystemError { get; set; }
        bool HS_InputPermit { get; }
        bool HS_ActFinished { get; }
        bool HS_RequestWater { get; }


        int ElpasedPressureTime_Seconds { get; }
        int RemainingPressureTime_Seconds { get; }

        bool ModulePass { get; set; }
        bool HasWarning { get; }
        bool HasAlarm { get; }
        bool IsNormalStatus { get; }
        void AutoStop();
        void WarningStop();
        void AlarmStop();
        void AutoStart();
        void AutoPause();
        void AlarmReset();
        void ModuleReset();

        void SimMotorPass();


        bool MotorServoOn { get; }
        bool MotorUpLimit { get; }
        bool MotorDownLimit { get; }
        bool MotorIdle { get; }
        bool MotorBusy { get; }
        bool MotorAlarm { get; }
        bool MotorHoming { get; }
        bool MotorMoving { get; }
        bool MotorHome { get; }
        int Posiition { get; }
        float Position_Value { get; }

        void ServoOn(bool servo);
        void Jog(bool jog, int dir, int speed);
        void Home();
        void MoveToPosition(int position, int speed);
        void MotorStop();

        bool InPos1 { get; }
        bool InPos2 { get; }
        bool InPos3 { get; }

        void Teach(int position);
    }
}
