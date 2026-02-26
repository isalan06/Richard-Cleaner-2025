using CleanerControlApp.Modules.Modbus.Interfaces;
using CleanerControlApp.Modules.Modbus.Models;
using CleanerControlApp.Modules.TempatureController.Interfaces;
using CleanerControlApp.Modules.TempatureController.Models;
using CleanerControlApp.Modules.TempatureController.Services;
using CleanerControlApp.Modules.UltrasonicDevice.Interfaces;
using CleanerControlApp.Modules.UltrasonicDevice.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.UltrasonicDevice.Services
{
    public class UltrasonicDevice : IUltrasonicDevice, IDisposable
    {
        #region Constants

        public static readonly int RTUAssemblyIndex = 3;
        public static readonly int BUFFER_SIZE = 6;

        #endregion

        #region attribute

        private readonly ILogger<UltrasonicDevice>? _logger;

        // background loop
        private CancellationTokenSource? _cts;
        private Task? _loopTask;
        private readonly TimeSpan _loopInterval = TimeSpan.FromMilliseconds(10);

        private IModbusRTUService? _modbusService;

        private bool _running;

        private int _routeIndex = 0;

        private ushort[]? _buffers = null;

        private bool _deviceConnected = false;
        private bool _deviceTimeout = false;
        private bool _deviceError = false;

        private Queue<UDCommandFrame> _commandQueue = new Queue<UDCommandFrame>();

        // diagnostics: measure loop iteration time and command execution count
        private long _loopIterationCount = 0; // number of completed loop iterations
        private long _totalLoopTicks = 0; // accumulated Stopwatch ticks for all iterations
        private long _lastLoopTicks = 0; // last iteration ticks
        private long _commandExecutedCount = 0; // number of times command Act was invoked

        // per-device timeout timers: when a device times out, start a timer to clear timeout after a delay
        private Timer? _timeoutTimers = null;

        // timeout duration for retry
        private static readonly TimeSpan DeviceTimeoutClearDelay = TimeSpan.FromMinutes(10);

        #endregion

        #region constructor

        public UltrasonicDevice(IModbusRTUPollService? modbusPool, ILogger<UltrasonicDevice>? logger = null)
        {
            _logger = logger;

            _modbusService = modbusPool != null && modbusPool.Count > RTUAssemblyIndex ? modbusPool[RTUAssemblyIndex] : null;
            _buffers = new ushort[BUFFER_SIZE];

            StartLoop();
        }

        #endregion

        #region destrictor and dispose pattern

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // stop background loop
                    try
                    {
                        _cts?.Cancel();
                        if (_loopTask != null)
                        {
                            _loopTask.Wait(500);
                        }
                    }
                    catch (AggregateException) { }
                    catch (Exception) { }
                    finally
                    {
                        _cts?.Dispose();
                        _cts = null;
                        _loopTask = null;
                    }

                    // TODO: 處置受控狀態 (受控物件)
                }

                // TODO: 釋出非受控資源 (非受控物件) 並覆寫完成項
                // TODO: 將大型欄位設為 Null
                disposedValue = true;
            }
        }

        // TODO: 僅有當 'Dispose(bool disposing)' 具有會釋出非受控資源的程式碼時，才覆寫完成項
        ~UltrasonicDevice()
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


        #region IUltrasonicDevice

        public bool UltrasonicEnabled
        {
            get => _buffers != null && _buffers.Length > 0 ? (_buffers[0] != 0) : false;
            set
            {
                if (_buffers != null && _buffers.Length > 0)
                {
                    _buffers[0] = (ushort)(value ? 1 : 0);
                }
            }
        }
        public float SettingCurrent
        {
            get => _buffers != null && _buffers.Length > 1 ? ((float)_buffers[1] / 100f) : 0f; // Unit Transfer 1 = 0.01A
            set
            {
                if (_buffers != null && _buffers.Length > 1)
                {
                    _buffers[1] = (ushort)(value * 100f);
                }
            }
        }
        public float Frequency => _buffers != null && _buffers.Length > 2 ? ((float)_buffers[2] / 10f) : 0f; // Unut Transfer 1 = 0.1 kHz
        public int Time => _buffers != null && _buffers.Length > 3 ? _buffers[3] : 0; // Unit Transfer 1 = 1 second
        public int Power => _buffers != null && _buffers.Length > 4 ? _buffers[4] : 0; // Unit Transfer 1 = 1%

        public void SetData(ushort[]? data)
        {
            if (data == null || data.Length < BUFFER_SIZE)
            {
                // handle error or ignore
                return;
            }
            if (_buffers == null || _buffers.Length != BUFFER_SIZE)
            {
                _buffers = new ushort[BUFFER_SIZE];
            }
            Array.Copy(data, _buffers, BUFFER_SIZE);
        }

        public bool IsRunning => _running;
        public void Start() { _running = true; }
        public void Stop() { _running = false; }

        public bool DeviceConnected => _deviceConnected;
        public bool DeviceTimeout => _deviceTimeout;
        public bool DeviceError => _deviceError;

        public IModbusRTUService? ModbusRTUService => _modbusService;

        public void UltrasonicOperate(bool enable)
        {
            UDCommandFrame cmd = new UDCommandFrame()
            {
                Id = 0,
                Name = "Ultrasonic Operate",
                CommandFrame = new ModbusRTUFrame()
                {
                    SlaveAddress = 1,
                    FunctionCode = 0x6, // Write Single Register
                    StartAddress = 0,
                    DataNumber = 1,
                    Data = new ushort[] { (ushort)(enable ? 1 : 0) }
                }
            };

            _commandQueue.Enqueue(cmd);
        }

        public void SetUltrasonicCurrent(float current)
        {
            UDCommandFrame cmd = new UDCommandFrame()
            {
                Id = 0,
                Name = "Set Ultrasonic Current",
                CommandFrame = new ModbusRTUFrame()
                {
                    SlaveAddress = 1,
                    FunctionCode = 0x6, // Write Single Register
                    StartAddress = 1,
                    DataNumber = 1,
                    Data = new ushort[] { (ushort)(current * 100f) } // Unit Transfer 1 = 0.01A
                }
            };
            _commandQueue.Enqueue(cmd);
        }

        #endregion

        #region Function

        private void StartLoop()
        {
            // ensure previous canceled
            _cts?.Cancel();
            _cts?.Dispose();

            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _loopTask = Task.Run(() => LoopAsync(token), token);
        }

        private async Task LoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    // Poll modbus if available
                    if (_modbusService != null && _running)
                    {
                        await PollModbusAsync(token).ConfigureAwait(false);

                        if (_commandQueue.Count > 0)
                        {
                            var command = _commandQueue.Dequeue();
                            if (_deviceConnected)
                            {
                                if (_modbusService != null && _modbusService.IsRunning && _running)
                                {
                                    // count command execution attempts
                                    Interlocked.Increment(ref _commandExecutedCount);
                                    var data = await _modbusService.Act(command.CommandFrame);
                                    if (data != null && data.HasException)
                                    {
                                        _deviceError = true;
                                    }

                                    if (data != null && !data.HasException)
                                    {
                                        _deviceError = false;
                                    }
                                }
                            }
                        }

                    }

                    // Place for other periodic work (e.g., update StatusIO, process commands)

                    await Task.Delay(_loopInterval, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    // swallow or consider logging
                }
                finally
                {
                    sw.Stop();
                    Interlocked.Increment(ref _loopIterationCount);
                    Interlocked.Exchange(ref _lastLoopTicks, sw.ElapsedTicks);
                    Interlocked.Add(ref _totalLoopTicks, sw.ElapsedTicks);
                }
            }
        }

        private async Task PollModbusAsync(CancellationToken token)
        {
            // minimal polling logic: ensure connected
            try
            {
                // TODO: add real read/write frames using _modbusService.ExecuteAsync
                if (_modbusService != null && _modbusService.IsRunning && _running)
                {
                    if (!_deviceTimeout)
                    {
                        var data = await _modbusService.Act(_routeProcess[_routeIndex].CommandFrame);

                        if (data != null)
                        {
                            if (data is ModbusRTUFrame)
                            {
                                if (!data.HasTimeout)
                                {
                                    this.SetData(data.Data);
                                    _deviceConnected = true;

                                    // clear timeout flag and cancel any pending timer
                                    _deviceTimeout = false;
                                    try
                                    {
                                        if (_timeoutTimers != null)
                                        {

                                            try { _timeoutTimers.Dispose(); } catch { }
                                            _timeoutTimers = null;
                                        }
                                    }
                                    catch { }

                                }
                                else
                                {
                                    _deviceConnected = false;

                                    // set timeout flag and start a timer to clear it after delay
                                    _deviceTimeout = true;

                                    // dispose existing timer if any
                                    try
                                    {
                                        if (_timeoutTimers != null)
                                        {
                                            try { _timeoutTimers?.Dispose(); } catch { }
                                            _timeoutTimers = null;
                                        }
                                    }
                                    catch { }

                                    // capture local idx for callback

                                    if (_timeoutTimers != null)
                                    {
                                        _timeoutTimers = new Timer(state =>
                                        {
                                            try
                                            {
                                                _deviceTimeout = false;
                                            }
                                            catch { }
                                            try
                                            {
                                                if (_timeoutTimers != null)
                                                {
                                                    try { _timeoutTimers?.Dispose(); } catch { }
                                                    _timeoutTimers = null;
                                                }
                                            }
                                            catch { }
                                        }, null, DeviceTimeoutClearDelay, Timeout.InfiniteTimeSpan);
                                    }
                                }
                            }
                        }
                    }

                    if (++_routeIndex >= _routeProcess.Length)
                        _routeIndex = 0;
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception)
            {
                // ignore or log
            }
        }

        #endregion

        #region Route

        private UDCommandFrame[] _routeProcess = new UDCommandFrame[]
        {
            new UDCommandFrame()
            {
                Id =1,
                Name = "Read Value",
                CommandFrame = new ModbusRTUFrame()
                {
                    SlaveAddress =1,
                    FunctionCode =0x3,
                    StartAddress =0,
                    DataNumber = (ushort)UltrasonicDevice.BUFFER_SIZE
                }
            },
            
        };



        #endregion

        #region Diagnostics properties

        public long LoopIterationCount => Interlocked.Read(ref _loopIterationCount);
        public double LastLoopDurationMilliseconds => (double)Interlocked.Read(ref _lastLoopTicks) * 1000.0 / Stopwatch.Frequency;
        public double AverageLoopDurationMilliseconds => LoopIterationCount > 0 ? ((double)Interlocked.Read(ref _totalLoopTicks) * 1000.0 / Stopwatch.Frequency) / LoopIterationCount : 0.0;
        public long CommandQueueExecutedCount => Interlocked.Read(ref _commandExecutedCount);

        #endregion
    }
}
