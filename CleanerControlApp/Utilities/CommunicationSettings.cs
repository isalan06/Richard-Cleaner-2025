using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Utilities
{
    public class CommunicationSettings
    {
        public ModbusTCPParameter? ModbusTCPParameter { get; set; }

        public ModbusRTUParameter? ModbusRTUParameter { get; set; }
        public List<ModbusRTUParameter>? ModbusRTUPoolParameter { get; set; }
    }

    public class ModbusTCPParameter
    {
        public string IP { get; set; } = "127.0.0.1";

        public int Port { get; set; } = 502;

        
    }

    public class ModbusRTUParameter
    {
        public string PortName { get; set; } = "COM1";
        public int BaudRate { get; set; } = 9600;
        public int DataBits { get; set; } = 8;
        public string Parity { get; set; } = "None";
        public int StopBits { get; set; } = 1;
    }
}
