using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.Modbus.Models
{
    public class ModbusRTUFrame
    {
        public byte SlaveAddress { get; set; }
        public byte FunctionCode { get; set; }
        public ushort StartAddress { get; set; }
        public ushort DataNumber { get; set; }
        public ushort[]? Data { get; set; } = Array.Empty<ushort>();

        public bool HasResponse { get; set; } = false;
        public bool HasException { get; set; } = false;
        public bool IsRead { get; internal set; } = false;
        public bool IsWrite { get; internal set; } = false;

        public bool EmptyCommand { get; set; } = false;

        public bool FinalCommand { get; set; } = false;

        public ModbusRTUFrame()
        {
            SlaveAddress = 1;
            FunctionCode = 0x03; // Read Holding Registers
            StartAddress = 0;
            DataNumber = 1;
        }
        public ModbusRTUFrame(byte slaveAddress, byte functionCode, ushort startAddress, ushort dataNumber, ushort[] data, bool finalCommand = false, bool emptyCommand = false)
        {
            Set(slaveAddress, functionCode, startAddress, dataNumber, data, finalCommand: finalCommand, emptyCommand: emptyCommand);
        }

        public ModbusRTUFrame(ModbusRTUFrame frame)
        {
            this.Clone(frame);
        }

        public void Set(byte slaveAddress, byte functionCode, ushort startAddress, ushort dataNumber, ushort[] data, bool hasResponse = false, bool hasException = false, bool finalCommand = false, bool emptyCommand = false)
        {
            SlaveAddress = slaveAddress;
            FunctionCode = functionCode;
            StartAddress = startAddress;
            DataNumber = dataNumber;
            HasResponse = hasResponse;
            HasException = hasException;
            FinalCommand = finalCommand;
            EmptyCommand = emptyCommand;
            if (data != null)
            {
                Data = new ushort[data.Length];
                Array.Copy(data, Data, data.Length);
            }

            bool _read = false;
            if ((functionCode == 0x1) || (functionCode == 0x2) || (functionCode == 0x3) || (functionCode == 0x4)) _read = true;

            IsRead = _read;
            IsWrite = !_read;

        }
        public void Set(ushort[] data)
        {
            if (data != null)
            {
                Data = new ushort[data.Length];
                Array.Copy(data, Data, data.Length);
            }
            else
                Data = null;
        }

        public void Clone(ModbusRTUFrame frame)
        {
            Set(frame.SlaveAddress, frame.FunctionCode, frame.StartAddress, frame.DataNumber, frame.Data, frame.HasResponse, frame.HasException, frame.FinalCommand, frame.EmptyCommand);
        }

        public ModbusRTUFrame Clone()
        {
            return new ModbusRTUFrame(this);
        }
    }
}
