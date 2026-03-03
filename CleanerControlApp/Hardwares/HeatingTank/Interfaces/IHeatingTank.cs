using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Hardwares
{
    public interface IHeatingTank
    {
        bool IsRunning { get; }
        void Start();
        void Stop();
    }
}
