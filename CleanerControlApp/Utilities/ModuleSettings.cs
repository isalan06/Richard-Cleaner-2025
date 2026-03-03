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
    }

    public class MS_DryingTanks
    {
        public int SV_Low { get; set; }
        public int SV_High { get; set; }
        public int ActTime_Second { get; set; }


    }
}
