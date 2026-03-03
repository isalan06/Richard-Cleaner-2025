using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Utilities
{
    public class UnitSettings
    {
        public List<DryingTanks>? DryingTanks { get; set; }
    }

    public class DryingTanks
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
}
