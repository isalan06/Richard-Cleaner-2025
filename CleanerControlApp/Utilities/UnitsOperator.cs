using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace CleanerControlApp.Utilities
{
    public class UnitsOperator
    {
        public void RefreshParameter()
        {
            try
            {
                // Reload configuration from appsettings.json
                ConfigLoader.Load();

                // Get CommunicationSettings from file
                var commFromFile = ConfigLoader.GetCommunicationSettings();

                // If application host is available, update the DI-registered singleton instance
                var host = global::CleanerControlApp.App.AppHost;
                if (host != null)
                {
                    var diComm = host.Services.GetService<CommunicationSettings>();
                    if (diComm != null && commFromFile != null)
                    {
                        // Copy properties to the DI singleton so existing consumers see updated values
                        diComm.ModbusTCPParameter = commFromFile.ModbusTCPParameter;
                        diComm.ModbusRTUParameter = commFromFile.ModbusRTUParameter;
                        diComm.ModbusRTUPoolParameter = commFromFile.ModbusRTUPoolParameter;
                    }
                }
            }
            catch
            {
                // swallow exceptions to keep UI responsive; callers may show errors as needed
            }
        }

        public void ResetParameterToUnits(bool reopen)
        { 
        
        }
    }
}
