using CleanerControlApp.Modules.Modbus.Interfaces;
using CleanerControlApp.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.Modbus.Services
{
    public class ModbusRTUPoolService : IModbusRTUPollService, IDisposable
    {
        #region attribute

        private ModbusRTUService[]? _services = null;
        private readonly ILogger<ModbusRTUPoolService>? _logger;

        #endregion

        #region constructor

        public ModbusRTUPoolService(ILogger<ModbusRTUPoolService>? logger, int serviceNumber)
        {
            _logger = logger;

            if (serviceNumber <= 0)
            {
                serviceNumber = 1;
            }

            _services = new ModbusRTUService[serviceNumber];
            for (int i = 0; i < serviceNumber; i++)
            {
                // Use parameterless constructor for individual services. If you want to pass a logger
                // to each ModbusRTUService, modify ModbusRTUService to accept ILogger<ModbusRTUService> here.
                _services[i] = new ModbusRTUService();
            }
        }

        // New constructor: accepts a list of ModbusRTUParameter to configure each pool item
        public ModbusRTUPoolService(ILogger<ModbusRTUPoolService>? logger, IList<ModbusRTUParameter>? poolParameters)
        {
            _logger = logger;

            int serviceNumber = (poolParameters == null || poolParameters.Count == 0) ? 1 : poolParameters.Count;

            _services = new ModbusRTUService[serviceNumber];
            for (int i = 0; i < serviceNumber; i++)
            {
                var svc = new ModbusRTUService();

                var param = (poolParameters != null && i < poolParameters.Count) ? poolParameters[i] : null;
                if (param != null)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(param.PortName)) svc.PortName = param.PortName;
                        if (param.BaudRate > 0) svc.BaudRate = param.BaudRate;
                        if (param.DataBits > 0) svc.DataBits = param.DataBits;

                        if (!string.IsNullOrWhiteSpace(param.Parity))
                        {
                            if (Enum.TryParse<System.IO.Ports.Parity>(param.Parity, true, out var parity))
                                svc.Parity = parity;
                            else
                            {
                                switch (param.Parity.Trim().ToLowerInvariant())
                                {
                                    case "none": svc.Parity = System.IO.Ports.Parity.None; break;
                                    case "odd": svc.Parity = System.IO.Ports.Parity.Odd; break;
                                    case "even": svc.Parity = System.IO.Ports.Parity.Even; break;
                                    case "mark": svc.Parity = System.IO.Ports.Parity.Mark; break;
                                    case "space": svc.Parity = System.IO.Ports.Parity.Space; break;
                                }
                            }
                        }

                        switch (param.StopBits)
                        {
                            case 0: svc.StopBits = System.IO.Ports.StopBits.None; break;
                            case 1: svc.StopBits = System.IO.Ports.StopBits.One; break;
                            case 2: svc.StopBits = System.IO.Ports.StopBits.Two; break;
                            case 3: svc.StopBits = System.IO.Ports.StopBits.OnePointFive; break;
                            default: svc.StopBits = System.IO.Ports.StopBits.One; break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error applying Modbus RTU pool parameter to service #{Index}", i);
                    }
                }

                _services[i] = svc;
            }
        }


        #endregion

        #region destructor && IDisposable

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 處置受控狀態 (受控物件)
                    if (_services != null)
                    {
                        foreach (var svc in _services)
                        {
                            try
                            {
                                svc?.Dispose();
                            }
                            catch { }
                        }
                    }
                }

                // TODO:釋出非受控資源 (非受控物件) 並覆寫完成項
                // TODO: 將大型欄位設為 Null
                _services = null;
                disposedValue = true;
            }
        }

        // TODO: 僅有當 'Dispose(bool disposing)'具有會釋出非受控資源的程式碼時，才覆寫完成項
        ~ModbusRTUPoolService()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }


        #endregion

        #region IModbusRTUPoolService


        public IModbusRTUService this[int index]
        {
            get
            {
                if (_services == null)
                {
                    throw new IndexOutOfRangeException("Modbus RTU service pool is not initialized.");
                }

                if (index < 0 || index >= _services.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return _services[index];
            }
        }

        public int Count => _services?.Length ?? 0;

        public void RefreshSerialPortSettings(CommunicationSettings? settings)
        {
            if (_services == null)
            {
                _logger?.LogWarning("Modbus RTU service pool is not initialized. Cannot refresh serial port settings.");
                return;
            }
            foreach (var svc in _services)
            {
                try
                {
                    svc?.RefreshSerialPortSettings(settings);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error refreshing serial port settings for a Modbus RTU service.");
                }
            }
        }

        #endregion

    }
}
