using CleanerControlApp.Modules.Modbus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.MitsubishiPLC.Models
{
    public class PLCFrame
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ModbusTCPFrame? DataFrame { get; set; } = new ModbusTCPFrame();

        public PLCFrame Clone()
        {
            return new PLCFrame
            {
                Id = this.Id,
                Name = this.Name,
                DataFrame = this.DataFrame != null ? new ModbusTCPFrame(this.DataFrame) : null,
            };
        }

    }
}
