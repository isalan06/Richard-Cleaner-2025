using CleanerControlApp.Modules.Modbus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.Modbus.Interfaces
{
    public interface IModbusTCPService
    {
        string Ip { get; set; }
        int Port { get; set; }

        bool IsConnected { get; }

        bool Connect();
        bool Disconnect();

        // Asynchronous counterparts
        Task<bool> ConnectAsync();
        Task<bool> DisconnectAsync();

        ModbusTCPFrame ExecuteFrame { get; set; }

        bool Execute();

        // Asynchronous execute that accepts a frame and returns the resulting frame (or null on failure)
        Task<ModbusTCPFrame?> ExecuteAsync(ModbusTCPFrame? frame);
    }
}
