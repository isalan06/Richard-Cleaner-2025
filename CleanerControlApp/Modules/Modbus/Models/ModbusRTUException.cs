using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.Modbus.Models
{
    public class ModbusRTUException : Exception
    {
        public byte SlaveAddress { get; set; }
        public byte FunctionCode { get; set; }
        public byte ExceptionCode { get; set; }
        public ModbusRTUException(string message, byte slaveAddress, byte functionCode, byte exceptionCode) : base(message)
        {
            SlaveAddress = slaveAddress;
            FunctionCode = functionCode;
            ExceptionCode = exceptionCode;
        }

        public override string ToString()
        {
            return $"ModbusRTUException: SlaveAddress={SlaveAddress}, FunctionCode={FunctionCode}, ExceptionCode={ExceptionCode}, Message={Message}";
        }
    }
}
