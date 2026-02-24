using CleanerControlApp.Modules.Modbus.Interfaces;
using CleanerControlApp.Modules.Modbus.Models;
using Microsoft.Extensions.Logging;
using Modbus.Device;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;


namespace CleanerControlApp.Modules.Modbus.Services
{
    public class ModbusRTUService : IModbusRTUService, IDisposable
    {
        #region attribute

        private SerialPort _serialPort = new SerialPort();

        private string _portName = "COM1";
        public string PortName
        {
            get => _portName;
            set
            {
                if (string.Equals(_portName, value, StringComparison.Ordinal)) return;
                _portName = value;
                ApplySettingsToSerialPort();
            }
        }

        private int _baudRate =230400;
        public int BaudRate
        {
            get => _baudRate;
            set
            {
                if (_baudRate == value) return;
                _baudRate = value;
                ApplySettingsToSerialPort();
            }
        }

        private Parity _parity = Parity.Even;
        public Parity Parity
        {
            get => _parity;
            set
            {
                if (_parity == value) return;
                _parity = value;
                ApplySettingsToSerialPort();
            }
        }

        private int _dataBits =8;
        public int DataBits
        {
            get => _dataBits;
            set
            {
                if (_dataBits == value) return;
                _dataBits = value;
                ApplySettingsToSerialPort();
            }
        }

        private StopBits _stopBits = StopBits.One;
        public StopBits StopBits
        {
            get => _stopBits;
            set
            {
                if (_stopBits == value) return;
                _stopBits = value;
                ApplySettingsToSerialPort();
            }
        }

        public bool IsRunning { get; internal set; } = false;

        private IModbusSerialMaster? _master = null;

        public int Timeout { get; set; } =5000;

        private bool _readResult = false;
        private ushort[]? _readData = null;
        private List<byte> _buffer = new List<byte>();

        public bool IsConnected { get { return _serialPort.IsOpen; } }

        private CancellationTokenSource source = new CancellationTokenSource();

        public static double InterFrameActMilliseconds =0.0; // Modbus RTU inter-frame Act in milliseconds
        public static double FrameReadMilliseconds =0.0; // Modbus RTU frame read in milliseconds

        private DateTime dt_next = DateTime.Now;

        private readonly ILogger<ModbusRTUService>? _logger;

        private readonly object _sync = new object();

        #endregion

        #region Constructor

        public ModbusRTUService()
        {
            _serialPort.DataReceived += _serialPort_DataReceived;
            _master = ModbusSerialMaster.CreateRtu(_serialPort);
            // apply initial settings
            ApplySettingsToSerialPort(doNotReopen: true);
        }

        public ModbusRTUService(ILogger<ModbusRTUService>? logger) : this()
        { _logger = logger; }

        #endregion

        #region IDisposable Support and Destructor

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 處置受控狀態 (受控物件)
                    source.Cancel();
                    IsRunning = false;
                    try { _serialPort?.Dispose(); } catch { }
                }

                // TODO:釋出非受控資源 (非受控物件) 並覆寫完成項
                // TODO: 將大型欄位設為 Null
                disposedValue = true;
            }
        }

        // // TODO: 僅有當 'Dispose(bool disposing)'具有會釋出非受控資源的程式碼時，才覆寫完成項
        ~ModbusRTUService()
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

        #region Events

        private void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {

        }

        #endregion

        #region Task

        private Task DoWork()
        {

            CancellationToken ct = source.Token;
            return Task.Run(() =>
            {
                while (!ct.IsCancellationRequested && IsRunning)
                {
                    //Console.WriteLine("ModbusRTUService is running...");
                    // Your periodic work here




                    Thread.Sleep(10); // Adjust the delay as needed
                }
            }, ct);
        }

        #endregion

        #region Functions

        private void ApplySettingsToSerialPort(bool doNotReopen = false)
        {
            lock (_sync)
            {
                bool wasOpen = false;
                try
                {
                    wasOpen = _serialPort.IsOpen;
                }
                catch { wasOpen = false; }

                try
                {
                    if (wasOpen)
                    {
                        try { _serialPort.Close(); } catch { }
                    }

                    _serialPort.PortName = _portName ?? "";
                    _serialPort.BaudRate = _baudRate;
                    _serialPort.Parity = _parity;
                    _serialPort.DataBits = _dataBits;
                    _serialPort.StopBits = _stopBits;

                    // keep reasonable timeouts
                    _serialPort.ReadTimeout =1000;
                    _serialPort.WriteTimeout =1000;

                    if (wasOpen && !doNotReopen)
                    {
                        try { _serialPort.Open(); } catch (Exception ex)
                        {
                            if (_logger != null) _logger.LogWarning(ex, "Failed to reopen serial port after applying settings");
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (_logger != null) _logger.LogError(ex, "Error applying serial port settings");
                }
            }
        }

        public bool Open()
        {
            bool result = false;

            if (_master == null)
                _master = ModbusSerialMaster.CreateRtu(_serialPort);

            if (!_serialPort.IsOpen)
            {
                try
                {
                    // ensure serial port uses current properties
                    ApplySettingsToSerialPort(doNotReopen: true);

                    _serialPort.Open();
                    result = true;
                    IsRunning = true;
                    DoWork();

                }
                catch (Exception ex)
                {
                    if (_logger != null)
                        _logger.LogError($"{_serialPort.PortName} Open Error: ${ex.Message}");
                    else
                        Console.WriteLine(ex.Message);
                    Console.WriteLine($"COM Port List: {string.Join(", ", System.IO.Ports.SerialPort.GetPortNames())}");
                    throw new ModbusRTUServiceException(ex.Message, PortName);
                }
            }

            return result;
        }
        public void Close()
        {
            if (_serialPort.IsOpen)
            {
                try
                {
                    IsRunning = false;

                    _serialPort.Close();
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                        _logger.LogError($"{_serialPort.PortName} Close Error: ${ex.Message}");
                    else
                        Console.WriteLine(ex.Message);
                    throw new ModbusRTUServiceException(ex.Message, PortName);
                }
            }


        }

        #endregion

        public async Task<ModbusRTUFrame?> Act(ModbusRTUFrame? command)
        {
            ModbusRTUFrame? _frame = null;

            if (command != null && command.EmptyCommand)
            {
                _frame = new ModbusRTUFrame(command);
                _frame.DataNumber =0;
                _frame.HasResponse = true;
                return _frame;
            }

            if (_serialPort.IsOpen && command != null)
            {
                // 清空所有狀態
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();
                _buffer.Clear();
                _readResult = false;
                _readData = null;

                _frame = new ModbusRTUFrame(command);
                _frame.HasResponse = false;
                _frame.HasException = false;


                DateTime dt = DateTime.Now;

                if (_master != null)
                {
                    switch (_frame.FunctionCode)
                    {
                        case 0x1:
                            var data1 = await _master.ReadCoilsAsync(_frame.SlaveAddress, _frame.StartAddress, _frame.DataNumber);
                            _frame.Set(data1);
                            break;

                        case 0x2:
                            var data2 = await _master.ReadInputsAsync(_frame.SlaveAddress, _frame.StartAddress, _frame.DataNumber);
                            _frame.Set(data2);
                            break;

                        case 0x3:
                            var data3 = await _master.ReadHoldingRegistersAsync(_frame.SlaveAddress, _frame.StartAddress, _frame.DataNumber);
                            _frame.Set(data3);
                            break;

                        case 0x4:
                            var data4 = await _master.ReadInputRegistersAsync(_frame.SlaveAddress, _frame.StartAddress, _frame.DataNumber);
                            _frame.Set(data4);
                            break;

                        case 0x5:
                            if (_frame.BoolData != null && _frame.BoolData.Length > 0)
                                await _master.WriteSingleCoilAsync(_frame.SlaveAddress, _frame.StartAddress, _frame.BoolData[0]);
                            break;

                        case 0x6:
                            if (_frame.Data != null && _frame.Data.Length >0)
                                await _master.WriteSingleRegisterAsync(_frame.SlaveAddress, _frame.StartAddress, _frame.Data[0]);
                            break;

                        case 0xF:
                            if (_frame.BoolData != null && _frame.BoolData.Length > 0)
                                await _master.WriteMultipleCoilsAsync(_frame.SlaveAddress, _frame.StartAddress, _frame.BoolData);
                            break;

                        case 0x10:
                            if (_frame.Data != null && _frame.Data.Length >0)
                                await _master.WriteMultipleRegistersAsync(_frame.SlaveAddress, _frame.StartAddress, _frame.Data);
                            break;
                    }
                }


                FrameReadMilliseconds = DateTime.Now.Subtract(dt).TotalMilliseconds; // 記錄讀取時間
                InterFrameActMilliseconds = DateTime.Now.Subtract(dt_next).TotalMilliseconds; // 記錄與上次Act的間隔時間
                dt_next = DateTime.Now; // 更新下一次的時間點

            }

            return _frame;
        }
    }
}
