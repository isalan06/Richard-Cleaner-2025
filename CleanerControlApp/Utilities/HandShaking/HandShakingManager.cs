using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Utilities.HandShaking
{
    public class HandShakingManager :
        ISignalA_DryingTankToShuttle, ISignalA_ShuttleToDryingTank
    {
        public HandShakingManager()
        {
        }
    }
}
