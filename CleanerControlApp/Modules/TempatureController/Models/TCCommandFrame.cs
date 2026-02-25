using CleanerControlApp.Modules.Modbus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.TempatureController.Models
{
    public class TCCommandFrame
    {
        public int Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public int ModuleIndex { get; set; } = 0;

        public ModbusRTUFrame? CommandFrame { get; set; } = new ModbusRTUFrame();

        public TCCommandFrame()
        {
            Id = 0;
            Name = string.Empty;
            ModuleIndex = 0;
            CommandFrame = new ModbusRTUFrame();
        }

        public TCCommandFrame(int id, string name, int moduleIndex, ModbusRTUFrame? commandFrame)
        {
            Id = id;
            Name = name;
            ModuleIndex = moduleIndex;
            CommandFrame = commandFrame;
        }

        public void SetResponse(ushort[]? response)
        { 
            CommandFrame?.Set(CommandFrame.SlaveAddress, CommandFrame.FunctionCode, CommandFrame.StartAddress, CommandFrame.DataNumber, response, hasResponse: true);
        }
    }
}
