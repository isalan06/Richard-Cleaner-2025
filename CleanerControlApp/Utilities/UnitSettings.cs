using CleanerControlApp.Hardwares.SoakingTank.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CleanerControlApp.Utilities
{
    public class UnitSettings
    {
        // keep friendly property names used throughout the code
        public List<US_DryingTanks>? DryingTanks { get; set; }
        public US_Sink? Sink { get; set; }
        public US_HeatingTank? HeatingTank { get; set; }
        public US_SoakingTank? SoakingTank { get; set; }
        public US_Shuttle? Shuttle { get; set; }
        public List<US_Motor>? Motors { get; set; }

        public US_System? System { get; set; }


        // JSON uses keys like 'US_DryingTanks', 'US_Sink', 'US_HeatingTank'
        // provide passthrough properties so binding from appsettings.json works
        [JsonPropertyName("US_DryingTanks")]
        public List<US_DryingTanks>? US_DryingTanks
        {
            get => DryingTanks;
            set => DryingTanks = value;
        }

        [JsonPropertyName("US_Sink")]
        public US_Sink? US_Sink
        {
            get => Sink;
            set => Sink = value;
        }

        [JsonPropertyName("US_HeatingTank")]
        public US_HeatingTank? US_HeatingTank
        {
            get => HeatingTank;
            set => HeatingTank = value;
        }

        [JsonPropertyName("US_SoakingTank")]
        public US_SoakingTank? US_SoakingTank
        {
            get => SoakingTank;
            set => SoakingTank = value;
        }

        [JsonPropertyName("US_Shuttle")]
        public US_Shuttle? US_Shuttle
        {
            get => Shuttle;
            set => Shuttle = value;
        }

        [JsonPropertyName("US_Motors")]
        public List<US_Motor>? US_Motors
        { 
            get => Motors;
            set => Motors = value;
        }

        [JsonPropertyName("US_System")]
        public US_System? US_System
        { 
            get=> System;
            set => System = value;
        }
    }

    public class US_DryingTanks
    { 
        public float UnitTransfer { get; set; }
        public int SV_Low_Limit { get; set; }
        public int SV_High_Limit { get; set; }
        public int PV_Low_Timeout_Second { get; set; }
        public int PV_High_Timeout_Second { get; set; }
        public int Cover_Open_Timeout_Second { get; set; }
        public int Cover_Close_Timeout_Second { get; set; }
        public int SV_CheckOffet { get; set; }
        public int ActTime_Limit_Second { get; set; }
    }

    public class US_Sink
    {
        public float UnitTransfer { get; set; }
        public int SV_Low_Limit { get; set; }
        public int SV_High_Limit { get; set; }
        public int PV_Low_Timeout_Second { get; set; }
        public int PV_High_Timeout_Second { get; set; }
        public int Cover_Open_Timeout_Second { get; set; }
        public int Cover_Close_Timeout_Second { get; set; }
        public int SV_CheckOffet { get; set; }
        public int ActTime_Limit_Second { get; set; }
        public float MotorUnitTransfer { get; set; }

    }

    public class US_HeatingTank
    {
        public float UnitTransfer { get; set; }
        public int SV_Low_Limit { get; set; }
        public int SV_High_Limit { get; set; }
        public int PV_Low_Timeout_Second { get; set; }
        public int PV_High_Timeout_Second { get; set; }
        public int INV_High_Timeout_Second { get; set; }
        public int INV_Low_Timeout_Second { get; set; }
        public int INV_Zero_Timeout_Second { get; set; }
        public int SV_CheckOffet { get; set; }
        public float INV_CheckOffset { get; set; }

    }

    public class US_SoakingTank
    {
        public int Cover_Open_Timeout_Second { get; set; }
        public int Cover_Close_Timeout_Second { get; set; }
        public int ActTime_Limit_Second { get; set; }
        public float MotorUnitTransfer { get; set; }
        public float UltrasonicCurrentMaxLimit { get; set; }

    }

    public class US_Shuttle
    { 
        public int Clamper_Timeout_Second { get; set; }
    }

    public class US_Motor
    { 
        public float UnitTransfer { get; set; }
    }

    public class US_System
    { 
    
    }
}
