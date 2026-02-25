using CleanerControlApp.Modules.Modbus.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.TempatureController.Interfaces
{
    public interface ITemperatureControllers
    {
        bool IsRunning { get; }

        void Start();

        void Stop();

        bool[]? DeviceConnected { get; }

        ISingleTemperatureController? this[int index] { get; }

        int Count { get; }

        void SetSV(int moduleIndex, int value);

        IModbusRTUService? ModbusRTUService { get; }
    }
}
