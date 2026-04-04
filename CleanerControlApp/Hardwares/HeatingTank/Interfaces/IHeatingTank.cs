using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Hardwares.HeatingTank.Interfaces
{
    public interface IHeatingTank
    {
        bool IsRunning { get; }
        void Start();
        void Stop();

        bool Sensor_Liquid_HH { get; }
        bool Sensor_Liquid_H { get; }
        bool Sensor_Liquid_L { get; }
        bool Sensor_Liquid_LL { get; }

        bool Command_WaterIn { get; set; }
        bool Command_WaterOut { get; set; }

        int PV { get; }
        int SV { get; }
        float PV_Value { get; }
        float SV_Value { get; }

        void SetSV(int value);
        void SetSV(float value);

        bool Auto { get; }
        bool Pausing { get; }
        bool Heating { get; }
        bool Initialized { get; }
        bool Idle { get; }

        bool HighTemperature { get; }
        bool LowTemperature { get; }
        bool HeatingOP(bool heating);
        bool ManualHeatingOP(bool heating);
        bool WaterInOP(bool water);
        bool ManualWaterInOP(bool water);
        bool WaterOutOP(bool water);
        bool ManualWaterOutOP(bool water);

        bool HS_RequestWater { get; set; }

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

        void SimHiTemperature(bool pv);
        void SimTemperature();
        void SimFrequency(int freq);

        int InvErrorCode { get; }
        int InvWarningCode { get; }
        float InvCommandFrequency { get; }
        float InvActualFrequency { get; }

        bool IsHighFrequency { get; }
        bool IsLowFrequency { get; }
        bool IsZeroFrequency { get; }
        bool DoHighFrequency { get; }
        bool DoLowFrequency { get; }
        bool DoZeroFrequency { get; }
        bool IsFrequencyRunning { get; }
        bool HighFrequencyOP();
        bool LowFrequencyOP();
        bool ZeroFrequencyOP();

        bool ManualFrequencyOP(int freq);

        string Hint();
    }
}
