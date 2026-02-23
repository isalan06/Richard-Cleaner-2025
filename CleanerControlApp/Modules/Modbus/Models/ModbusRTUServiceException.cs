using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.Modbus.Models
{
    public class ModbusRTUServiceException : Exception
    {
        public string PortName { get; set; }

        public ModbusRTUServiceException(string message, string portName) : base(message)
        {
            PortName = portName;
        }

        public override string ToString()
        {
            return $"ModbusRTUServiceException: PortName={PortName}, Message={Message}";
        }
    }
}
