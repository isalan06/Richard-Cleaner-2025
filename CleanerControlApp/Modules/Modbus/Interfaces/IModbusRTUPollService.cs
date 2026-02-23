using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.Modbus.Interfaces
{
    public interface IModbusRTUPollService
    {
        /// <summary>
        /// Get individual Modbus RTU service by index.
        /// </summary>
        IModbusRTUService this[int index] { get; }

        /// <summary>
        /// Number of pooled RTU services.
        /// </summary>
        int Count { get; }
    }
}
