using CleanerControlApp.Hardwares.DryingTank.Interfacaes;
using CleanerControlApp.Hardwares.Shuttle.Interfaces;
using CleanerControlApp.Hardwares.Sink.Interfaces;
using CleanerControlApp.Hardwares.SoakingTank.Interfaces;
using CleanerControlApp.Modules.DeltaMS300.Interfaces;
using CleanerControlApp.Modules.MitsubishiPLC.Interfaces;
using CleanerControlApp.Modules.Modbus.Interfaces;
using CleanerControlApp.Modules.TempatureController.Interfaces;
using CleanerControlApp.Modules.UltrasonicDevice.Interfaces;
using CleanerControlApp.Modules.UserManagement.Services;
using CleanerControlApp.Utilities;
using CleanerControlApp.Modules.UserManagement.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CleanerControlApp.Utilities.Alarm;

namespace CleanerControlApp.Hardwares
{
    public class HardwareManager : IDisposable
    {

        #region attribute

        private readonly ILogger<HardwareManager> _logger;

        private readonly UnitSettings _unitSettings;
        private readonly ModuleSettings _modbusSettings;

        private readonly ISink? _sink;
        private readonly ISoakingTank? _soakingTank;
        private readonly IDryingTank[]? _dryingTanks;
        private readonly IShuttle? _shuttle;

        private readonly IModbusTCPService? _modbusTCPService;
        private readonly IModbusRTUPollService? _modbusRTUPollService;

        private readonly IDeltaMS300[]? _deltaMS300s;
        private readonly IPLCService? _plcService;
        private readonly ITemperatureControllers? _temperatureControllers;
        private readonly IUltrasonicDevice? _ultrasonicDevice;

        // background loop
        private CancellationTokenSource? _cts;
        private Task? _loopTask;
        private readonly TimeSpan _loopInterval = TimeSpan.FromMilliseconds(10);

        private bool _running;

        #endregion

        #region constructor

        public HardwareManager(ILogger<HardwareManager> logger, UnitSettings unitSettings, ModuleSettings modbusSettings,
            ISink? sink, ISoakingTank? soakingTank, IDryingTank[]? dryingTanks, IShuttle? shuttle,
            IModbusTCPService? modbusTCPService, IModbusRTUPollService? modbusRTUPollService,
            IDeltaMS300[]? deltaMS300s, IPLCService? plcService, ITemperatureControllers temperatureControllers, IUltrasonicDevice ultrasonicDevice)
        { 
            _logger = logger;

            _unitSettings = unitSettings;
            _modbusSettings = modbusSettings;

            _sink = sink;
            _soakingTank = soakingTank;
            _dryingTanks = dryingTanks;
            _shuttle = shuttle;

            _modbusTCPService = modbusTCPService;
            _modbusRTUPollService = modbusRTUPollService;

            _deltaMS300s = deltaMS300s;
            _plcService = plcService;
            _temperatureControllers = temperatureControllers;
            _ultrasonicDevice = ultrasonicDevice;


            // Alarm
            AlarmManager.AttachFlagGetter("ALM001", () => _communication_alarm);

            StartLoop();

            Start();
        }

        #endregion

        #region destructor and IDisposable

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
        ~HardwareManager()
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

        #region Communication

        public bool ModbusTCPConnected => _modbusTCPService != null && _modbusTCPService.IsConnected;
        public bool ModbusRTU1Connected => _modbusRTUPollService != null && _modbusRTUPollService[0] != null && _modbusRTUPollService[0].IsRunning;
        public bool ModbusRTU2Connected => _modbusRTUPollService != null && _modbusRTUPollService[1] != null && _modbusRTUPollService[1].IsRunning;
        public bool ModbusRTU3Connected => _modbusRTUPollService != null && _modbusRTUPollService[2] != null && _modbusRTUPollService[2].IsRunning;
        public bool ModbusRTU4Connected => _modbusRTUPollService != null && _modbusRTUPollService[3] != null && _modbusRTUPollService[3].IsRunning;

        public bool Check_All_Modbus_Connected => (UserManager.CanPassCheck || (ModbusTCPConnected && ModbusRTU1Connected && ModbusRTU2Connected && ModbusRTU3Connected && ModbusRTU4Connected));

        public void CommunicationConnect(bool connect)
        {
            if (connect)
            {
                _modbusTCPService?.Connect();
                _modbusRTUPollService?.ToList().ForEach(m => m?.Start());
            }
            else
            {
                _modbusTCPService?.Disconnect();
                _modbusRTUPollService?.ToList().ForEach(m => m?.Stop());
            }
        }

        // New asynchronous version to avoid blocking callers (e.g. UI or startup)
        public async Task CommunicationConnectAsync(bool connect)
        {
            if (connect)
            {
                var tasks = new List<Task>();

                if (_modbusTCPService != null)
                {
                    try
                    {
                        tasks.Add(_modbusTCPService.ConnectAsync());
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error starting Modbus TCP connect async");
                    }
                }

                if (_modbusRTUPollService != null)
                {
                    try
                    {
                        var list = _modbusRTUPollService.ToList();
                        tasks.AddRange(list.Select(m => Task.Run(() => { try { m?.Start(); } catch (Exception ex) { _logger?.LogError(ex, "Error starting Modbus RTU service"); } })));
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error starting Modbus RTU services async");
                    }
                }

                try
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "One or more connect tasks failed");
                }
            }
            else
            {
                var tasks = new List<Task>();

                if (_modbusTCPService != null)
                {
                    try
                    {
                        tasks.Add(_modbusTCPService.DisconnectAsync());
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error disconnecting Modbus TCP async");
                    }
                }

                if (_modbusRTUPollService != null)
                {
                    try
                    {
                        var list = _modbusRTUPollService.ToList();
                        tasks.AddRange(list.Select(m => Task.Run(() => { try { m?.Stop(); } catch (Exception ex) { _logger?.LogError(ex, "Error stopping Modbus RTU service"); } })));
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error stopping Modbus RTU services async");
                    }
                }

                try
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "One or more disconnect tasks failed");
                }
            }
        }

        #endregion

        #region Module Status

        public void ModuleRunning(bool run)
        {
            _deltaMS300s?.ToList().ForEach(m => { if (run) m.Start(); else m.Stop(); });
            if(run) _temperatureControllers?.Start(); else _temperatureControllers?.Stop();
            if(run) _ultrasonicDevice?.Start(); else _ultrasonicDevice?.Stop();
            if(run) _plcService?.Start(); else _plcService?.Stop();

        }

        // Async counterpart to start/stop modules without blocking caller
        public async Task ModuleRunningAsync(bool run)
        {
            var tasks = new List<Task>();

            if (_deltaMS300s != null)
            {
                tasks.AddRange(_deltaMS300s.Select(m => Task.Run(() => { try { if (run) m.Start(); else m.Stop(); } catch (Exception ex) { _logger?.LogError(ex, "Error starting/stopping DeltaMS300 module"); } })));
            }

            if (_temperatureControllers != null)
            {
                tasks.Add(Task.Run(() => { try { if (run) _temperatureControllers.Start(); else _temperatureControllers.Stop(); } catch (Exception ex) { _logger?.LogError(ex, "Error starting/stopping temperature controllers"); } }));
            }

            if (_ultrasonicDevice != null)
            {
                tasks.Add(Task.Run(() => { try { if (run) _ultrasonicDevice.Start(); else _ultrasonicDevice.Stop(); } catch (Exception ex) { _logger?.LogError(ex, "Error starting/stopping ultrasonic device"); } }));
            }

            if (_plcService != null)
            { 
                tasks.Add(Task.Run(() => { try { if (run) _plcService.Start(); else _plcService.Stop(); } catch (Exception ex) { _logger?.LogError(ex, "Error starting/stopping PLC service"); } }));
            }

            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "One or more module start/stop tasks failed");
            }
        }

        #endregion

        #region Task

        public bool IsRunning => _running;
        public void Start() { _running = true; }
        public void Stop() { _running = false; }

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
                    if (_running)
                    {
                        await PollFunctionAsync(token).ConfigureAwait(false);
                    }

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
                }
            }
        }

        private async Task PollFunctionAsync(CancellationToken token)
        {

            await Task.Yield();
        }

        #endregion

        #region Alarm

        private bool _communication_alarm => !Check_All_Modbus_Connected; 

        #endregion
    }
}
