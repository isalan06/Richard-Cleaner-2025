using CleanerControlApp.Modules.Modbus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.UltrasonicDevice.Models
{
    public class UDCommandFrame
    {
        public int Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;

        public ModbusRTUFrame? CommandFrame { get; set; } = new ModbusRTUFrame();

        public UDCommandFrame()
        {
            Id = 0;
            Name = string.Empty;
            CommandFrame = new ModbusRTUFrame();
        }

        public UDCommandFrame(int id, string name, int moduleIndex, ModbusRTUFrame? commandFrame)
        {
            Id = id;
            Name = name;
            CommandFrame = commandFrame;
        }

        public void SetResponse(ushort[]? response)
        {
            CommandFrame?.Set(CommandFrame.SlaveAddress, CommandFrame.FunctionCode, CommandFrame.StartAddress, CommandFrame.DataNumber, response, hasResponse: true);
        }
    }
}
