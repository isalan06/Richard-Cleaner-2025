using CleanerControlApp.Utilities;
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

        void RefreshSerialPortSettings(CommunicationSettings? settings);

        /// <summary>
        /// Returns a List of the pooled Modbus RTU services. Useful when you need to call List.ForEach.
        /// </summary>
        List<IModbusRTUService> ToList();
    }
}
