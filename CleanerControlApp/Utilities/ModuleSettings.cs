using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace CleanerControlApp.Utilities
{
    public class ModuleSettings
    {
        public List<MS_DryingTanks>? MS_DryingTanks { get; set; }

        [JsonIgnore]
        public List<MS_DryingTanks>? DryingTanks
        {
            get => MS_DryingTanks;
            set => MS_DryingTanks = value;
        }

        // Keep a property that matches the JSON key 'MS_Sink'
        public MS_Sink? MS_Sink { get; set; }

        [JsonIgnore]
        public MS_Sink? Sink
        {
            get => MS_Sink;
            set => MS_Sink = value;
        }

        public MS_HeatingTank? MS_HeatingTank { get; set; }

        [JsonIgnore]
        public MS_HeatingTank? HeatingTank
        {
            get => MS_HeatingTank;
            set => MS_HeatingTank = value;
        }

        public MS_SoakingTank? MS_SoakingTank { get; set; }

        [JsonIgnore]
        public MS_SoakingTank? SoakingTank
        {
            get => MS_SoakingTank;
            set => MS_SoakingTank = value;
        }

        public MS_Shuttle? MS_Shuttle { get; set; }

        [JsonIgnore]
        public MS_Shuttle? Shuttle
        {
            get => MS_Shuttle;
            set => MS_Shuttle = value;
        }

        public List<MS_Motor>? MS_Motors { get; set; }
        [JsonIgnore]
        public List<MS_Motor>? Motors
        {
            get => MS_Motors;
            set => MS_Motors = value;
        }

        public MS_System? MS_System { get; set; }
        public MS_System System
        {
            get => MS_System;
            set => MS_System = value;
        }

    }

    public class MS_DryingTanks
    {
        public int SV_Low { get; set; }
        public int SV_High { get; set; }
        public int ActTime_Second { get; set; }

    }
    public class MS_Sink
    {
        public int SV_Low { get; set; }
        public int SV_High { get; set; }
        public int ActTime_Second { get; set; }

        // Property names aligned with JSON keys in appsettings.json
        public int MotorPosition_01 { get; set; }
        public int MotorPosition_02 { get; set; }
        public int MotorPosition_03 { get; set; }
        public int MotorVelocity_01 { get; set; }
        public int MotorVelocity_02 { get; set; }
        public int AirKnifeRetryCount { get; set; }

    }

    public class MS_HeatingTank
    {
        public int SV_Low { get; set; }
        public int SV_High { get; set; }

        public float INV_High { get; set; }
        public float INV_Low { get; set; }
        public float INV_Zero { get; set; }
        public int Water_H_CheckDelay_Second { get; set; }
        public int Water_L_CheckDelay_Second { get; set; }
    }

    public class MS_SoakingTank
    {
        public int ActTime_Second { get; set; }

        // Property names aligned with JSON keys in appsettings.json
        public int MotorPosition_01 { get; set; }
        public int MotorPosition_02 { get; set; }
        public int MotorPosition_03 { get; set; }
        public int MotorVelocity_01 { get; set; }
        public int MotorVelocity_02 { get; set; }
        public int AirKnifeRetryCount { get; set; }
        public float UltrasonicSetCurrent { get; set; }

    }

    public class MS_Shuttle
    {
        public int Shuttle_ZAxis_StableTime_Second { get; set; }
    }

    public class MS_Motor
    { 
        public List<int>? Positions { get; set; }
        public List<int>? Velocities { get; set; }
    }

    public class MS_System
    { 
        public int SinkModulePass { get; set; }
        public int SoakingTankModulePass { get; set; }
        public int DryingTank1ModulePass { get; set; }
        public int DryingTank2ModulePass { get; set; }
    }

}
