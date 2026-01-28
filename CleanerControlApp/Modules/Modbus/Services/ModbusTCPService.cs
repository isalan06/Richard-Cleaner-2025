using CleanerControlApp.Modules.Modbus.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using Modbus.Device;
using System.Net.Sockets;
using CleanerControlApp.Modules.Modbus.Models;
using Microsoft.Extensions.Logging;

namespace CleanerControlApp.Modules.Modbus.Services
{
    public class ModbusTCPService : IModbusTCPService, IDisposable
    {

        #region attribute

        protected string _ip = "127.0.0.1";
        protected int _port = 502;
        protected bool disposedValue;

        protected TcpClient _tcpClient = new TcpClient();
        protected IModbusMaster? _master;
        private readonly ILogger<ModbusTCPService>? _logger;

        #endregion

        #region constructor

        // Parameterless constructor kept for compatibility
        public ModbusTCPService()
        {
        }

        // Constructor used by DI to provide logger
        public ModbusTCPService(ILogger<ModbusTCPService> logger)
        {
            _logger = logger;
        }

        #endregion

        #region IDisposable & destructor

       

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 處置受控狀態 (受控物件)
                    Disconnect();
                }

                // TODO: 釋出非受控資源 (非受控物件) 並覆寫完成項
                // TODO: 將大型欄位設為 Null
                disposedValue = true;
            }
        }

        // TODO: 僅有當 'Dispose(bool disposing)' 具有會釋出非受控資源的程式碼時，才覆寫完成項
        ~ModbusTCPService()
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

        #region IModbusTCPService
        public string Ip { get => _ip; set => _ip = value; }
        public int Port { get => _port; set => _port = value; }

        public bool IsConnected { get => _tcpClient.Connected; }

        public bool Connect()
        {
            bool result = false;

            if (!IsConnected)
            {
                try
                {
                    _tcpClient = new TcpClient();
                    _tcpClient.Connect(_ip, _port);
                    _master = ModbusIpMaster.CreateIp(_tcpClient);
                    result = true;
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                        _logger.LogError(ex, "Modbus TCP連線失敗: {Message}", ex.Message);
                    else
                        System.Diagnostics.Debug.WriteLine($"Modbus TCP連線失敗: {ex.Message}");
                }
            }

            return result;
        }

        public bool Disconnect()
        {
            bool result = false;

            if (IsConnected)
            {
                try
                {
                    _tcpClient.Close();
                    _master?.Dispose();
                    result = true;
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                        _logger.LogError(ex, "Modbus TCP斷線失敗: {Message}", ex.Message);
                    else
                        System.Diagnostics.Debug.WriteLine($"Modbus TCP斷線失敗: {ex.Message}");
                }
            }

            return result;
        }

        public Models.ModbusTCPFrame ExecuteFrame { get; set; } = new Models.ModbusTCPFrame();

        public bool Execute()
        {
            bool result = false;
            if (IsConnected && _master != null)
            {
                try
                {
                    byte slaveAddress = ExecuteFrame.SlaveAddress;
                    ModbusFunctionCode functionCode = ExecuteFrame.FunctionCodeName;
                    ushort startAddress = ExecuteFrame.StartAddress;
                    ushort dataNumber = ExecuteFrame.DataNumber;
                    var readData = ExecuteFrame.Data != null ? (ushort[])ExecuteFrame.Data.Clone() : null;
                    var boolData = ExecuteFrame.BoolData != null ? (bool[])ExecuteFrame.BoolData.Clone() : null;
                    if (ExecuteFrame.IsRead)
                    {
                        try
                        {
                            // 讀取資料
                            switch (functionCode)
                            {
                                case ModbusFunctionCode.ReadCoils:
                                    boolData = _master.ReadCoils(slaveAddress, startAddress, dataNumber);
                                    break;

                                case ModbusFunctionCode.ReadDiscreteInputs:
                                    boolData = _master.ReadInputs(slaveAddress, startAddress, dataNumber);
                                    break;

                                case ModbusFunctionCode.ReadHoldingRegisters:
                                    readData = _master.ReadHoldingRegisters(slaveAddress, startAddress, dataNumber);
                                    break;

                                case ModbusFunctionCode.ReadInputRegisters:
                                    readData = _master.ReadInputRegisters(slaveAddress, startAddress, dataNumber);
                                    break;
                            }

                            // Copy read results into ExecuteFrame to avoid exposing internal references
                            if (readData != null)
                            {
                                // Use ExecuteFrame.Set to copy ushort[] safely
                                ExecuteFrame.Set(readData);
                            }

                            if (boolData != null)
                            {
                                // Clone bool array before assigning
                                ExecuteFrame.BoolData = (bool[])boolData.Clone();
                            }

                            result = true;
                        }
                        catch (Exception ex)
                        {
                            // optionally log
                            if (_logger != null)
                                _logger.LogError(ex, "Modbus TCP讀取失敗: {Message}", ex.Message);
                            else
                                System.Diagnostics.Debug.WriteLine($"Modbus TCP讀取失敗: {ex.Message}");
                        }
                        
                    }
                    else
                    {
                        // 寫入資料
                        try
                        {
                            switch(functionCode)
                            {
                                case ModbusFunctionCode.WriteSingleCoil:
                                    if (ExecuteFrame.BoolData != null && ExecuteFrame.BoolData.Length > 0)
                                    {
                                        _master.WriteSingleCoil(slaveAddress, startAddress, ExecuteFrame.BoolData[0]);
                                        result = true;
                                    }
                                    break;
                                case ModbusFunctionCode.WriteSingleRegister:
                                    if (ExecuteFrame.Data != null && ExecuteFrame.Data.Length > 0)
                                    {
                                        _master.WriteSingleRegister(slaveAddress, startAddress, ExecuteFrame.Data[0]);
                                        result = true;
                                    }
                                    break;
                                case ModbusFunctionCode.WriteMultipleCoils:
                                    if (ExecuteFrame.BoolData != null)
                                    {
                                        _master.WriteMultipleCoils(slaveAddress, startAddress, ExecuteFrame.BoolData);
                                        result = true;
                                    }
                                    break;
                                case ModbusFunctionCode.WriteMultipleRegisters:
                                    if (ExecuteFrame.Data != null)
                                    {
                                        _master.WriteMultipleRegisters(slaveAddress, startAddress, ExecuteFrame.Data);
                                        result = true;
                                    }
                                    break;
                            }
                            
                        }
                        catch (Exception ex)
                        {
                            // optionally log
                            if (_logger != null)
                                _logger.LogError(ex, "Modbus TCP 寫入失敗: {Message}", ex.Message);
                            else
                                System.Diagnostics.Debug.WriteLine($"Modbus TCP 寫入失敗: {ex.Message}");
                        }
                    }
                    
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                        _logger.LogError(ex, "Modbus TCP 執行命令失敗: {Message}", ex.Message);
                    else
                        System.Diagnostics.Debug.WriteLine($"Modbus TCP 執行命令失敗: {ex.Message}");
                }
            }
            return result;
        }

        #endregion
    }
}
