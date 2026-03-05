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

    }

}
