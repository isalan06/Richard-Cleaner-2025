using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.Modbus.Models
{
    public enum ModbusFunctionCode
    {
        ReadCoils = 1,
        ReadDiscreteInputs = 2,
        ReadHoldingRegisters = 3,
        ReadInputRegisters = 4,
        WriteSingleCoil = 5,
        WriteSingleRegister = 6,
        WriteMultipleCoils = 15,
        WriteMultipleRegisters = 16
    }
}
