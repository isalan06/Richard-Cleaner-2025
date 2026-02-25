using CleanerControlApp.Modules.Modbus.Interfaces;
using CleanerControlApp.Modules.Modbus.Models;
using Microsoft.Extensions.Logging;
using Modbus.Device;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace CleanerControlApp.Modules.Modbus.Services
{
    public class ModbusTCPService : IModbusTCPService, IDisposable
    {

        #region attribute

        protected string _ip = "192.168.3.20";
        protected int _port = 502;
        protected bool disposedValue;

        protected TcpClient _tcpClient = new TcpClient();
        protected IModbusMaster? _master;
        private readonly ILogger<ModbusTCPService>? _logger;

        // Semaphore to allow only one operation (connect/disconnect/execute) at a time
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private readonly object _sync = new object();

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
                    try
                    {
                        _semaphore?.Dispose();
                    }
                    catch { }
                }

                // TODO:釋出非受控資源 (非受控物件) 並覆寫完成項
                // TODO: 將大型欄位設為 Null
                disposedValue = true;
            }
        }

        // TODO: 僅有當 'Dispose(bool disposing)'具有會釋出非受控資源的程式碼時，才覆寫完成項
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

        public bool IsConnected { get => _tcpClient?.Connected ?? false; }

        public bool Connect()
        {
            bool result = false;

            _semaphore.Wait();
            try
            {
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
            }
            finally
            {
                _semaphore.Release();
            }

            return result;
        }

        public async Task<bool> ConnectAsync()
        {
            bool result = false;
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!IsConnected)
                {
                    try
                    {
                        _tcpClient = new TcpClient();
                        await _tcpClient.ConnectAsync(_ip, _port).ConfigureAwait(false);
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
            }
            finally
            {
                _semaphore.Release();
            }

            return result;
        }

        public bool Disconnect()
        {
            bool result = false;

            _semaphore.Wait();
            try
            {
                if (IsConnected)
                {
                    try
                    {
                        _tcpClient.Close();
                        _master?.Dispose();
                        _master = null;
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
            }
            finally
            {
                _semaphore.Release();
            }

            return result;
        }

        public async Task<bool> DisconnectAsync()
        {
            bool result = false;

            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (IsConnected)
                {
                    try
                    {
                        _tcpClient.Close();
                        _master?.Dispose();
                        _master = null;
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
            }
            finally
            {
                _semaphore.Release();
            }

            return result;
        }

        public Models.ModbusTCPFrame ExecuteFrame { get; set; } = new Models.ModbusTCPFrame();

        public bool Execute()
        {
            bool result = false;
            _semaphore.Wait();
            try
            {
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
                                //讀取資料
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
                                switch (functionCode)
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
            }
            finally
            {
                _semaphore.Release();
            }
            return result;
        }

        public async Task<ModbusTCPFrame?> ExecuteAsync(ModbusTCPFrame? frame)
        {
            if (frame == null) return null;

            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (IsConnected && _master != null)
                {
                    try
                    {
                        // Work on a clone to avoid mutating caller's instance
                        var localFrame = frame.Clone();

                        byte slaveAddress = localFrame.SlaveAddress;
                        ModbusFunctionCode functionCode = localFrame.FunctionCodeName;
                        ushort startAddress = localFrame.StartAddress;
                        ushort dataNumber = localFrame.DataNumber;

                        ushort[]? readData = localFrame.Data != null ? (ushort[])localFrame.Data.Clone() : null;
                        bool[]? boolData = localFrame.BoolData != null ? (bool[])localFrame.BoolData.Clone() : null;

                        if (localFrame.IsRead)
                        {
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

                            if (readData != null)
                                localFrame.Set(readData);

                            if (boolData != null)
                                localFrame.BoolData = (bool[])boolData.Clone();

                            return localFrame;
                        }
                        else
                        {
                            // write
                            switch (functionCode)
                            {
                                case ModbusFunctionCode.WriteSingleCoil:
                                    if (localFrame.BoolData != null && localFrame.BoolData.Length > 0)
                                        _master.WriteSingleCoil(slaveAddress, startAddress, localFrame.BoolData[0]);
                                    break;
                                case ModbusFunctionCode.WriteSingleRegister:
                                    if (localFrame.Data != null && localFrame.Data.Length > 0)
                                        _master.WriteSingleRegister(slaveAddress, startAddress, localFrame.Data[0]);
                                    break;
                                case ModbusFunctionCode.WriteMultipleCoils:
                                    if (localFrame.BoolData != null)
                                        _master.WriteMultipleCoils(slaveAddress, startAddress, localFrame.BoolData);
                                    break;
                                case ModbusFunctionCode.WriteMultipleRegisters:
                                    if (localFrame.Data != null)
                                        _master.WriteMultipleRegisters(slaveAddress, startAddress, localFrame.Data);
                                    break;
                            }

                            return localFrame;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (_logger != null)
                            _logger.LogError(ex, "Modbus TCP 執行命令失敗: {Message}", ex.Message);
                        else
                            System.Diagnostics.Debug.WriteLine($"Modbus TCP 執行命令失敗: {ex.Message}");

                        return null;
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }

            return null;
        }

        #endregion

        #region Function

        private void ApplySettingsToTcpClient(bool doNotReopen = false)
        {
            lock (_sync)
            {
                bool wasOpen = false;
                try
                {
                    wasOpen = _tcpClient.Connected;
                }
                catch { wasOpen = false; }

                try
                {
                    if (wasOpen)
                    {
                        try {
                            _tcpClient.Close();
                            _master?.Dispose();
                            _master = null;
                        } 
                        catch { }
                    }

                    if (wasOpen && !doNotReopen)
                    {
                        try 
                        {
                            _tcpClient = new TcpClient();
                            _tcpClient.Connect(_ip, _port);
                            _master = ModbusIpMaster.CreateIp(_tcpClient);
                        }
                        catch (Exception ex)
                        {
                            if (_logger != null) _logger.LogWarning(ex, "Failed to reopen tcp client after applying settings");
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null) _logger.LogError(ex, "Error applying tcp client settings");
                }
            }
        }

        #endregion
    }
}
