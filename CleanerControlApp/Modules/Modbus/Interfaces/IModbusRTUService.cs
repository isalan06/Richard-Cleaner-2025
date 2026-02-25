using CleanerControlApp.Modules.Modbus.Models;
using CleanerControlApp.Utilities;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.Modbus.Interfaces
{
    public interface IModbusRTUService
    {
        string PortName { get; set; }
        int BaudRate { get; set; }
        Parity Parity { get; set; }
        int DataBits { get; set; }
        StopBits StopBits { get; set; }

        bool IsRunning { get; }

        int Timeout { get; set; }

        bool Open();
        void Close();

        Task<ModbusRTUFrame?> Act(ModbusRTUFrame? coammand);

        void RefreshSerialPortSettings(CommunicationSettings? settings);
    }
}
