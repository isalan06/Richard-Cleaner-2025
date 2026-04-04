using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Hardwares.Sink.Interfaces
{
    public interface ISink
    {
        bool IsRunning { get; }
        void Start();
        void Stop();

        bool Sensor_CoverOpen { get; }
        bool Sensor_CoverClose { get; }

        bool Command_CleanerCoverClose { get; set; }
        bool Command_CleanerAirOpen { get; set; }

        int PV { get; }
        int SV { get; }
        float PV_Value { get; }
        float SV_Value { get; }

        void SetSV(int value);
        void SetSV(float value);

        bool Auto { get; }
        bool Pausing { get; }
        bool Pressure { get; }
        bool Cassette { get; }
        bool Initialized { get; }
        bool Idle { get; }

        bool HighPressure { get; }
        bool LowPressure { get; }
        bool PressureOP(bool pressure);
        bool ManualPressureOP(bool pressure);
        bool AirOP(bool air);
        bool ManualAirOP(bool air);

        bool CoverClose(bool close);
        bool ManualCoverClose(bool close);

        bool HS_ClamperMoving { get; set; }
        bool HS_ClamperPickFinished { get; set; }
        bool HS_ClamperPlaceFinished { get; set; }
        bool HS_WaterSystemError { get; set; }
        bool HS_InputPermit { get; }
        bool HS_ActFinished { get; }
        

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

        void SimHiPressure();
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

        int InvErrorCode { get; }
        int InvWarningCode { get; }
        float InvCommandFrequency { get; }
        float InvActualFrequency { get; }

        string Hint();

    }
}
