using CleanerControlApp.Modules.TempatureController.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Hardwares.DryingTank.Interfacaes
{
    public interface IDryingTank
    {
        bool IsRunning { get; }
        void Start();
        void Stop();

        bool Sensor_CoverOpen { get; }
        bool Sensor_CoverClose { get; }

        bool Command_HeaterCoverClose { get; set; }
        bool Command_HeaterAirOpen { get; set; }
        bool Command_HeaterBlower { get; set; }

        int PV { get; }
        int SV { get; }
        float PV_Value { get; }
        float SV_Value { get; }

        void SetSV(int value);
        void SetSV(float value);

        bool Auto { get; }
        bool Pasuing { get; }
        bool Heating { get; }
        bool Cassette { get; }
        bool Initialized { get; }
        bool Idle { get; }

        bool HighTemperature { get; }
        bool LowTemperature { get; }
        bool HeatingOP(bool heating);
        bool ManualHeatingOP(bool heating);
        bool AirOP(bool air);
        bool ManualAirOP(bool air);
        bool BlowerOP(bool blow);
        bool ManualBlowerOP(bool blow);
        bool CoverClose(bool close);
        bool ManualCoverClose(bool close);

        int ElpasedHeatingTime_Seconds { get; }
        int RemainingHeatingTime_Seconds { get; }

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
    }
}
