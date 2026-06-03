using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Utilities
{
    public class CommonFunction
    {

        public static int DefaultIntervalValue = 2;
        public static bool MoveEndDelayPassed(ref DateTime? startTimestamp, bool condition, int intervalTime)
        {
            try
            {
                int ms = intervalTime;
                if (ms <= 0) return condition; // no delay configured

                if (!condition)
                {
                    startTimestamp = null;
                    return false;
                }

                if (startTimestamp == null)
                    startTimestamp = DateTime.UtcNow;

                return (DateTime.UtcNow - startTimestamp.Value) >= TimeSpan.FromMilliseconds(ms);
            }
            catch
            {
                // on error, behave as no delay
                startTimestamp = null;
                return condition;
            }
        }

        public static bool CheckPositionInRange(int currentPosition, int targetPosition, int intervalValue = -1)
        {
            if (intervalValue < 0)
            {
                intervalValue = DefaultIntervalValue;
            }
            return currentPosition >= (targetPosition - intervalValue) && currentPosition <= (targetPosition + intervalValue);
        }
    }
}
