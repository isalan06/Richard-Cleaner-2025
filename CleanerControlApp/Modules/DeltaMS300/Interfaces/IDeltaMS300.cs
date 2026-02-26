using CleanerControlApp.Modules.Modbus.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.DeltaMS300.Interfaces
{
    public interface IDeltaMS300
    {
        float Frquency_Command { get; } // unit : Hz
        float Frquency_Output { get; } // unit : Hz
        float Frquency_Set { get; set; } // unit : Hz

        void SetData(ushort[]? data);
        void SetRead2102H2103H(ushort value1, ushort value2);
        void SetRead2001(ushort value);

        bool IsRunning { get; }

        void Start();

        void Stop();

        bool DeviceConnected { get; }
        bool DeviceError { get; }
        bool DeviceTimeout { get; }

        IModbusRTUService? ModbusRTUService { get; }

        // Diagnostics: loop timing and command execution statistics
        long LoopIterationCount { get; }
        double LastLoopDurationMilliseconds { get; }
        double AverageLoopDurationMilliseconds { get; }
        long CommandQueueExecutedCount { get; }

        void SetFrequency(float value);
    }
}
