using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.TempatureController.Interfaces
{
    public interface ISingleTemperatureController
    {
        public int SV { get; set; }
        public int PV { get; }
        public int Un { get; }
        public float Ctu { get; }
        public ushort Status { get; }
        public int AL1 { get; }
        public int AL2 { get; }
        public float HB { get; }

        public void SetData(ushort[]? data);
    }
}
