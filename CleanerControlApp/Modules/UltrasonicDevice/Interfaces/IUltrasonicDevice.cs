using CleanerControlApp.Modules.Modbus.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.UltrasonicDevice.Interfaces
{
    public interface IUltrasonicDevice
    {
        bool UltrasonicEnabled { get; set; }
        float SettingCurrent { get; set; } // Unit : A
        float Frequency { get; } // Unit : kHz
        int Time { get; } // Unit : second
        int Power { get; } // Unit : %
        void SetData(ushort[]? data);

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

        void UltrasonicOperate(bool enable);

        void SetUltrasonicCurrent(float current);
    }
}
