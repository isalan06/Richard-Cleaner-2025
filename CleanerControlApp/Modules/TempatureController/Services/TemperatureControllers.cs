using CleanerControlApp.Modules.Modbus.Interfaces;
using CleanerControlApp.Modules.Modbus.Models;
using CleanerControlApp.Modules.TempatureController.Interfaces;
using CleanerControlApp.Modules.TempatureController.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.TempatureController.Services
{
    public class TemperatureControllers : ITemperatureControllers, IDisposable
    {
        #region Constants

        public static readonly int ModuleCount = 4;
        public static readonly int RTUAssemblyIndex = 2;

        #endregion

        #region attribute

        private ISingleTemperatureController[]? _controllers = null;

        private readonly ILogger<TemperatureControllers>? _logger;

        // background loop
        private CancellationTokenSource? _cts;
        private Task? _loopTask;
        private readonly TimeSpan _loopInterval = TimeSpan.FromMilliseconds(50);

        private IModbusRTUService? _modbusService;

        private bool _running;

        private int _routeIndex = 0;

        private bool[]? _deviceConnected = null;

        private Queue<TCCommandFrame> _commandQueue = new Queue<TCCommandFrame>();

        #endregion

        #region constructor

        // Use constructor injection for dependencies instead of service locator
        public TemperatureControllers(IModbusRTUPollService? modbusPool, ILogger<TemperatureControllers>? logger)
        {
            _logger = logger;

            _deviceConnected = new bool[ModuleCount];

            _controllers = new ISingleTemperatureController[ModuleCount];
            for (int i = 0; i < ModuleCount; i++)
            {
                _controllers[i] = new SingleTemperatureController();
            }

            _modbusService = modbusPool != null && modbusPool.Count > RTUAssemblyIndex ? modbusPool[RTUAssemblyIndex] : null;

            StartLoop();
        }

        #endregion


        #region desturctor & IDisposable

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
        ~TemperatureControllers()
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

        #region ITemperatureControllers

        public bool IsRunning => _running;
        public void Start() { _running = true; }
        public void Stop() { _running = false; }

        public bool[]? DeviceConnected => _deviceConnected;

        public ISingleTemperatureController? this[int index]
        {
            get
            {
                if (_controllers != null && index >= 0 && index < _controllers.Length)
                {
                    return _controllers[index];
                }
                return null;
            }
        }

        public int Count => _controllers?.Length ?? 0;

        public void SetSV(int moduleIndex, int value)
        {
            if (_controllers != null && moduleIndex >= 0 && moduleIndex < _controllers.Length)
            {
                TCCommandFrame commandFrame = new TCCommandFrame()
                {
                    Id = 11,
                    Name = $"TC - {moduleIndex + 1} Set Value ({value})",
                    ModuleIndex = moduleIndex,
                    CommandFrame = new ModbusRTUFrame()
                    {
                        SlaveAddress = (byte)(moduleIndex+1),
                        FunctionCode = 0x10,
                        StartAddress = 64,
                        DataNumber = 1,
                        Data = new ushort[] { (ushort)value }
                    }
                };

                _commandQueue.Enqueue(commandFrame);
            }
        }

        public IModbusRTUService? ModbusRTUService => _modbusService;

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
                try
                {
                    // Poll modbus if available
                    if (_modbusService != null && _running)
                    {
                        await PollModbusAsync(token).ConfigureAwait(false);

                        if (_commandQueue.Count > 0)
                        {
                            var command = _commandQueue.Dequeue();
                            if (_deviceConnected != null && _deviceConnected[command.ModuleIndex])
                            {
                                if (_modbusService != null && _modbusService.IsRunning && _running)
                                {
                                    var data = await _modbusService.Act(command.CommandFrame);
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
                    var data = await _modbusService.Act(_routeProcess[_routeIndex].CommandFrame);

                    if (data != null)
                    {
                        if (_controllers != null && data is ModbusRTUFrame)
                        {
                            if (!data.HasTimeout)
                            {
                                _controllers[_routeProcess[_routeIndex].ModuleIndex].SetData(data.Data);
                                if (_deviceConnected != null)
                                    _deviceConnected[_routeProcess[_routeIndex].ModuleIndex] = true;
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

        #region Route process

        private TCCommandFrame[] _routeProcess = new TCCommandFrame[]
            {
                new TCCommandFrame()
                {
                    Id = 1,
                    Name = "Read Value of TC 1",
                    ModuleIndex = 0,
                    CommandFrame = new ModbusRTUFrame()
                    {
                        SlaveAddress = 1,
                        FunctionCode = 0x3,
                        StartAddress = 64,
                        DataNumber = (ushort)SingleTemperatureController.BUFFER_SIZE
                    }
                },
                new TCCommandFrame()
                {
                    Id = 2,
                    Name = "Read Value of TC 2",
                    ModuleIndex = 1,
                    CommandFrame = new ModbusRTUFrame()
                    {
                        SlaveAddress = 2,
                        FunctionCode = 0x3,
                        StartAddress = 64,
                        DataNumber = (ushort)SingleTemperatureController.BUFFER_SIZE
                    }
                },
                new TCCommandFrame()
                {
                    Id = 3,
                    Name = "Read Value of TC 3",
                    ModuleIndex = 2,
                    CommandFrame = new ModbusRTUFrame()
                    {
                        SlaveAddress = 3,
                        FunctionCode = 0x3,
                        StartAddress = 64,
                        DataNumber = (ushort)SingleTemperatureController.BUFFER_SIZE
                    }
                },
                new TCCommandFrame()
                {
                    Id = 4,
                    Name = "Read Value of TC 4",
                    ModuleIndex = 3,
                    CommandFrame = new ModbusRTUFrame()
                    {
                        SlaveAddress = 4,
                        FunctionCode = 0x3,
                        StartAddress = 64,
                        DataNumber = (ushort)SingleTemperatureController.BUFFER_SIZE
                    }
                },
            };

        #endregion
    }
}
