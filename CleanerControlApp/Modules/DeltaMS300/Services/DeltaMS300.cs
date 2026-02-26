using CleanerControlApp.Modules.DeltaMS300.Interfaces;
using CleanerControlApp.Modules.DeltaMS300.Models;
using CleanerControlApp.Modules.Modbus.Interfaces;
using CleanerControlApp.Modules.Modbus.Models;
using CleanerControlApp.Modules.UltrasonicDevice.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.DeltaMS300.Services
{
    public class DeltaMS300 : IDeltaMS300, IDisposable
    {
        #region Constants

        public static readonly int BUFFER_SIZE = 3;

        public static readonly int ModuleCount = 2;
        public static readonly int[] ModbusRTUIndex = new int[] { 0, 1 };

        #endregion

        #region attribute

        private readonly ILogger<DeltaMS300>? _logger;

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

        private Queue<DMCommandFrame> _commandQueue = new Queue<DMCommandFrame>();

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

        public DeltaMS300(int modbusRtuIndex, IModbusRTUPollService? modbusPool, ILogger<DeltaMS300>? logger = null)
        {
            _logger = logger;

            _modbusService = modbusPool != null && modbusPool.Count > ModuleCount ? modbusPool[ModbusRTUIndex[modbusRtuIndex]] : null;
            _buffers = new ushort[BUFFER_SIZE];

            StartLoop();
        }

        #endregion

        #region destructor & IDisposable

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
        ~DeltaMS300()
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

        #region IDeltaMS300 implementation

        public float Frquency_Command => _buffers != null && _buffers.Length >= BUFFER_SIZE ? _buffers[0] / 100f : 0f; // Unit Transfer 1 = 0.01 Hz
        public float Frquency_Output => _buffers != null && _buffers.Length >= BUFFER_SIZE ? _buffers[1] / 100f : 0f; // Unit Transfer 1 = 0.01 Hz

        public float Frquency_Set
        {
            get => _buffers != null && _buffers.Length >= BUFFER_SIZE ? _buffers[2] / 100f : 0f; // Unit Transfer 1 = 0.01 Hz
            set            
            {
                if (_buffers != null && _buffers.Length >= BUFFER_SIZE)
                {
                    _buffers[2] = (ushort)(value * 100f); // Unit Transfer 1 = 0.01 Hz
                }
            }
        }

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

        public void SetRead2102H2103H(ushort value1, ushort value2)
        {
            if (_buffers != null && _buffers.Length >= BUFFER_SIZE)
            {
                _buffers[0] = value1;
                _buffers[1] = value2;
            }
        }

        public void SetRead2001(ushort value)
        {
            if (_buffers != null && _buffers.Length >= BUFFER_SIZE)
            {
                _buffers[2] = value;
            }
        }

        public bool IsRunning => _running;
        public void Start() { _running = true; }
        public void Stop() { _running = false; }

        public bool DeviceConnected => _deviceConnected;
        public bool DeviceTimeout => _deviceTimeout;
        public bool DeviceError => _deviceError;

        public IModbusRTUService? ModbusRTUService => _modbusService;

        public void SetFrequency(float value)
        {
            DMCommandFrame cmd = new DMCommandFrame()
            {
                Id = 11,
                Name = "Write Command For Frequency",
                CommandFrame = new ModbusRTUFrame()
                {
                    SlaveAddress = 1,
                    FunctionCode = 0x6, // Write Single Register
                    StartAddress = 8193, // 2001H
                    DataNumber = 1,
                    Data = new ushort[] { (ushort)(value * 100f) } // Unit Transfer 1 = 0.01 Hz
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
                                    if (_routeProcess[_routeIndex].Id == 1)
                                    {
                                        if(data.Data != null && data.Data.Length >= 2)
                                            this.SetRead2102H2103H(data.Data[0], data.Data[1]);
                                    }
                                    else if (_routeProcess[_routeIndex].Id == 2)
                                    {
                                        if (data.Data != null && data.Data.Length >= 1)
                                            this.SetRead2001(data.Data[0]);
                                    }

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

        private DMCommandFrame[] _routeProcess = new DMCommandFrame[]
        {
            new DMCommandFrame()
            {
                Id =1,
                Name = "Read Value",
                CommandFrame = new ModbusRTUFrame()
                {
                    SlaveAddress =1,
                    FunctionCode =0x3,
                    StartAddress =8450, // 2102H
                    DataNumber = 2
                }
            },
            new DMCommandFrame()
            {
                Id =2,
                Name = "Read Value For Frequency Setting",
                CommandFrame = new ModbusRTUFrame()
                {
                    SlaveAddress =1,
                    FunctionCode =0x3,
                    StartAddress =8193, // 2001H
                    DataNumber = 1
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
