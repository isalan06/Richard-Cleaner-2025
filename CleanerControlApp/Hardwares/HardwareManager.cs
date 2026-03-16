using CleanerControlApp.Hardwares.DryingTank.Interfaces;
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
using System.Threading;
using System.Threading.Tasks;
using CleanerControlApp.Utilities.Alarm;
using System.Security.Policy;
using CleanerControlApp.Hardwares.HeatingTank.Interfaces;
using System.Windows.Media.Animation;
using System.Numerics;
using CleanerControlApp.Utilities.Log;

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
        private readonly IHeatingTank? _heatingTank;

        private readonly IModbusTCPService? _modbusTCPService;
        private readonly IModbusRTUPollService? _modbusRTUPollService;

        private readonly IDeltaMS300[]? _deltaMS300s;
        private readonly IPLCService? _plcService;
        private readonly IPLCOperator? _plcOperator;
        private readonly ITemperatureControllers? _temperatureControllers;
        private readonly IUltrasonicDevice? _ultrasonicDevice;

        // background loop
        private CancellationTokenSource? _cts;
        private Task? _loopTask;
        private readonly TimeSpan _loopInterval = TimeSpan.FromSeconds(1);

        private bool _running;

        #endregion

        #region constructor

        public HardwareManager(ILogger<HardwareManager> logger, UnitSettings unitSettings, ModuleSettings modbusSettings,
            ISink? sink, ISoakingTank? soakingTank, IDryingTank[]? dryingTanks, IShuttle? shuttle, IHeatingTank? heatingTank,
            IModbusTCPService? modbusTCPService, IModbusRTUPollService? modbusRTUPollService,
            IDeltaMS300[]? deltaMS300s, IPLCService? plcService, IPLCOperator? plcOperator, ITemperatureControllers? temperatureControllers, IUltrasonicDevice? ultrasonicDevice)
        {
            _logger = logger;

            try
            {
                _unitSettings = unitSettings;
                _modbusSettings = modbusSettings;

                _sink = sink;
                _soakingTank = soakingTank;
                _dryingTanks = dryingTanks;
                _shuttle = shuttle;
                _heatingTank = heatingTank;

                _modbusTCPService = modbusTCPService;
                _modbusRTUPollService = modbusRTUPollService;

                _deltaMS300s = deltaMS300s;
                _plcService = plcService;
                _plcOperator = plcOperator;
                _temperatureControllers = temperatureControllers;
                _ultrasonicDevice = ultrasonicDevice;


                // Alarm
                AlarmManager.AttachFlagGetter("ALM001", () => _communication_alarm);
                AlarmManager.AttachFlagGetter("ALM002", () => _emo_alarm);
                AlarmManager.AttachFlagGetter("ALM003", () => _main_air_alarm);
                AlarmManager.AttachFlagGetter("ALM004", () => _door_alarm);
                AlarmManager.AttachFlagGetter("ALM005", () => _leakage_alarm);
                AlarmManager.AttachFlagGetter("ALM006", () => _wasteTankH_alarm);
                AlarmManager.AttachFlagGetter("ALM007", () => _checkCassette_alarm);
                AlarmManager.AttachFlagGetter("ALM008", () => _initializingTimeout_alarm);

                StartLoop();

                Start();
            }
            catch (Exception ex)
            {
                // Log and show a message to aid debugging during startup
                try { _logger?.LogError(ex, "HardwareManager constructor failed"); } catch { }
                try { System.Windows.MessageBox.Show($"HardwareManager ctor exception: {ex}", "Startup Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error); } catch { }
                throw;
            }
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
            if (run) _temperatureControllers?.Start(); else _temperatureControllers?.Stop();
            if (run) _ultrasonicDevice?.Start(); else _ultrasonicDevice?.Stop();
            if (run) _plcService?.Start(); else _plcService?.Stop();

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

        #region IO

        public bool LoaderCassetteInPosition1 => _plcOperator != null && _plcOperator.InSlotExist1;
        public bool LoaderCassetteInPosition2 => _plcOperator != null && _plcOperator.InSlotExist2;
        public bool LoaderCassetteInPosition3 => _plcOperator != null && _plcOperator.InSlotExist3;
        public bool LoaderCassetteInPosition4 => _plcOperator != null && _plcOperator.InSlotExist4;
        public bool LoaderCassetteInPosition5 => _plcOperator != null && _plcOperator.InSlotExist5;
        public bool UnloadCassetteInPosition1 => _plcOperator != null && _plcOperator.OutSlotExist1;
        public bool UnloadCassetteInPosition2 => _plcOperator != null && _plcOperator.OutSlotExist2;
        public bool UnloadCassetteInPosition3 => _plcOperator != null && _plcOperator.OutSlotExist3;
        public bool UnloadCassetteInPosition4 => _plcOperator != null && _plcOperator.OutSlotExist4;
        public bool UnloadCassetteInPosition5 => _plcOperator != null && _plcOperator.OutSlotExist5;

        public bool EMOSign => _plcOperator != null && _plcOperator.EMOSign;
        public bool MainAirSign => _plcOperator != null && _plcOperator.MainAirSign;
        public bool FrontDoor1Sign => _plcOperator != null && _plcOperator.FrontDoor1;
        public bool FrontDoor2Sign => _plcOperator != null && _plcOperator.FrontDoor2;
        public bool FrontDoor3Sign => _plcOperator != null && _plcOperator.FrontDoor3;
        public bool FrontDoor4Sign => _plcOperator != null && _plcOperator.FrontDoor4;
        public bool SideDoor1Sign => _plcOperator != null && _plcOperator.SideDoor1;
        public bool SideDoor2Sign => _plcOperator != null && _plcOperator.SideDoor2;
        public bool Leakage1Sign => _plcOperator != null && _plcOperator.Leakage1;
        public bool Leakage2Sign => _plcOperator != null && _plcOperator.Leakage2;

        public bool WasteTankH => _plcOperator != null && _plcOperator.WasteWaterPosH;

        public bool Tower_Red
        {
            get => _plcOperator != null && _plcOperator.Command_LighterRed;
            set { if (_plcOperator != null) _plcOperator.Command_LighterRed = value; }
        }
        public bool Tower_Yellow
        {
            get => _plcOperator != null && _plcOperator.Command_LighterYellow;
            set { if (_plcOperator != null) _plcOperator.Command_LighterYellow = value; }
        }
        public bool Tower_Green
        {
            get => _plcOperator != null && _plcOperator.Command_LighterGreen;
            set { if (_plcOperator != null) _plcOperator.Command_LighterGreen = value; }
        }
        public bool Tower_Buzzer
        {
            get => _plcOperator != null && _plcOperator.Command_LighterBuzzer;
            set { if (_plcOperator != null) _plcOperator.Command_LighterBuzzer = value; }
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
            AlarmTriggerToModule();
            WanringTriggerToModule();

            InitializedProcedure();

            AutoProcedure();

            await Task.Yield();
        }

        #endregion

        #region Alarm

        private bool _communication_alarm => !Check_All_Modbus_Connected;
        private bool _emo_alarm => !EMOSign;
        private bool _main_air_alarm => !MainAirSign;
        private bool _door_alarm => FrontDoor1Sign || FrontDoor2Sign || FrontDoor3Sign || FrontDoor4Sign || SideDoor1Sign || SideDoor2Sign;
        private bool _leakage_alarm => Leakage1Sign || Leakage2Sign;
        private bool _wasteTankH_alarm => WasteTankH;
        private bool _checkCassette_alarm { get; set; }

        private bool _initializingTimeout_alarm { get; set; }


        public bool HasAlarm => _communication_alarm || _emo_alarm || _leakage_alarm || _wasteTankH_alarm || _checkCassette_alarm || _initializingTimeout_alarm;
        public bool HasWarning => _main_air_alarm || _door_alarm;

        public bool HasShuttleAlarm => _shuttle != null && _shuttle.HasAlarm;
        public bool HasSinkAlarm => _sink != null && _sink.HasAlarm;
        public bool HasSoakingTankAlarm => _soakingTank != null && _soakingTank.HasAlarm;
        public bool HasDryingTank1Alarm => _dryingTanks != null && _dryingTanks.Length > 0 && _dryingTanks[0].HasAlarm;
        public bool HasDryingTank2Alarm => _dryingTanks != null && _dryingTanks.Length > 1 && _dryingTanks[1].HasAlarm;
        public bool HasHeatingTankAlarm => _heatingTank != null && _heatingTank.HasAlarm;

        public bool HasShuttleWarning => _shuttle != null && _shuttle.HasWarning;
        public bool HasSinkWarning => _sink != null && _sink.HasWarning;
        public bool HasSoakingTankWarning => _soakingTank != null && _soakingTank.HasWarning;
        public bool HasDryingTank1Warning => _dryingTanks != null && _dryingTanks.Length > 0 && _dryingTanks[0].HasWarning;
        public bool HasDryingTank2Warning => _dryingTanks != null && _dryingTanks.Length > 1 && _dryingTanks[1].HasWarning;
        public bool HasHeatingTankWarning => _heatingTank != null && _heatingTank.HasWarning;

        public bool HasSystemAlarm => HasAlarm || HasShuttleAlarm || HasSinkAlarm || HasSoakingTankAlarm || HasDryingTank1Alarm || HasDryingTank2Alarm || HasHeatingTankAlarm || IsAlarm;
        public bool HasSystemWarning => HasWarning || HasShuttleWarning || HasSinkWarning || HasSoakingTankWarning || HasDryingTank1Warning || HasDryingTank2Warning || HasHeatingTankWarning || IsWarning;



        private async Task AlarmReset()
        {
            OperateLog.Log("錯誤重置", "按下 [錯誤重置] 後會對系統進行清除錯誤狀態及下達錯誤重置命令給各個模組。");

            _checkCassette_alarm = false;
            _initializingTimeout_alarm = false;

            if (_shuttle != null) _shuttle.AlarmReset();
            if (_sink != null) _sink.AlarmReset();
            if (_soakingTank != null) _soakingTank.AlarmReset();
            if (_dryingTanks != null)
            {
                foreach (var tank in _dryingTanks)
                {
                    tank.AlarmReset();
                }
            }
            if (_heatingTank != null) _heatingTank.AlarmReset();

            await Task.Delay(_loopInterval);

            _firstAlarm = false;
            _firstWarning = false;
            _buzzer_stop = false;
        }

        // Expose AlarmReset as public async method for UI to call
        public Task AlarmResetAsync()
        {
            // Call the private AlarmReset method and return the Task
            return AlarmReset();
        }

        private bool _firstAlarm = false;
        private bool _firstWarning = false;


        public bool IsAlarm => _firstAlarm;
        public bool IsWarning => _firstWarning;

        private void AlarmTriggerToModule()
        {
            if (!_firstAlarm && HasSystemAlarm)
            {
                _auto_procedure_trigger = false;
                _initializing = false;
                _firstAlarm = true;
                if (_shuttle != null) _shuttle.AlarmStop();
                if (_sink != null) _sink.AlarmStop();
                if (_soakingTank != null) _soakingTank.AlarmStop();
                if (_dryingTanks != null)
                {
                    foreach (var tank in _dryingTanks)
                    {
                        tank.AlarmStop();
                    }
                }
                if (_heatingTank != null) _heatingTank.AlarmStop();
            }
        }

        private void WanringTriggerToModule()
        {
            if (!_firstWarning && HasWarning)
            {
                _firstWarning = true;
            }

            if (_shuttle != null && !_shuttle.Pausing && _shuttle.HasWarning) _shuttle.WarningStop();
            if (_sink != null && !_sink.Pausing && _sink.HasWarning) _sink.WarningStop();
            if (_soakingTank != null && !_soakingTank.Pausing && _soakingTank.HasWarning) _soakingTank.WarningStop();
            if (_dryingTanks != null && _dryingTanks.Length > 1)
            {
                foreach (var tank in _dryingTanks)
                {
                    if (!tank.Pausing && tank.HasWarning)
                        tank.WarningStop();
                }
            }
            if (_heatingTank != null && !_heatingTank.Pausing && _heatingTank.HasWarning) _heatingTank.WarningStop();
        }

        #endregion

        #region Loader & Unloader & Module Status

        public int LoaderCassetteCount
        {
            get
            {
                int result = 0;

                if (LoaderCassetteInPosition1) result++;
                if (LoaderCassetteInPosition2) result++;
                if (LoaderCassetteInPosition3) result++;
                if (LoaderCassetteInPosition4) result++;
                if (LoaderCassetteInPosition5) result++;

                return result;
            }
        }

        public bool LoaderCanPick => LoaderCassetteCount > 0;
        public int LoaderFirstCassettePosition
        {
            get
            {
                if (LoaderCassetteInPosition5) return 5;
                if (LoaderCassetteInPosition4) return 4;
                if (LoaderCassetteInPosition3) return 3;
                if (LoaderCassetteInPosition2) return 2;
                if (LoaderCassetteInPosition1) return 1;
                return 0; // no cassette
            }
        }

        public int UnloaderCassetteCount
        {
            get
            {
                int result = 0;

                if (UnloadCassetteInPosition1) result++;
                if (UnloadCassetteInPosition2) result++;
                if (UnloadCassetteInPosition3) result++;
                if (UnloadCassetteInPosition4) result++;
                if (UnloadCassetteInPosition5) result++;

                return result;
            }
        }
        public bool UnloaderCanPlace => UnloaderCassetteCount < 5;
        public int UnloaderFirstEmptyPosition
        {
            get
            {
                if (!UnloadCassetteInPosition1) return 14;
                if (!UnloadCassetteInPosition2) return 13;
                if (!UnloadCassetteInPosition3) return 12;
                if (!UnloadCassetteInPosition4) return 11;
                if (!UnloadCassetteInPosition5) return 10;
                return 0; // no empty position
            }
        }

        public bool ShuttleIdle => _shuttle != null && _shuttle.Idle;
        public bool SinkIdle => _sink != null && _sink.Idle;
        public bool SoakingTankIdle => _soakingTank != null && _soakingTank.Idle;
        public bool DryingTank1Idle => _dryingTanks != null && _dryingTanks.Length > 0 && _dryingTanks[0].Idle;
        public bool DryingTank2Idle => _dryingTanks != null && _dryingTanks.Length > 1 && _dryingTanks[1].Idle;
        public bool HeatingTankIdle => _heatingTank != null && _heatingTank.Idle;

        public bool ShuttleInitialized => _shuttle != null && _shuttle.Initialized;
        public bool SinkInitialized => _sink != null && _sink.Initialized;
        public bool SoakingTankInitialized => _soakingTank != null && _soakingTank.Initialized;
        public bool DryingTank1Initialized => _dryingTanks != null && _dryingTanks.Length > 0 && _dryingTanks[0].Initialized;
        public bool DryingTank2Initialized => _dryingTanks != null && _dryingTanks.Length > 1 && _dryingTanks[1].Initialized;
        public bool HeatingTankInitialized => _heatingTank != null && _heatingTank.Initialized;

        public bool SystemInitialized => ShuttleInitialized && SinkInitialized && SoakingTankInitialized && DryingTank1Initialized && DryingTank2Initialized && HeatingTankInitialized && !_initializing;

        public bool ShuttleAuto => _shuttle != null && _shuttle.Auto;
        public bool SinkAuto => _sink != null && _sink.Auto;
        public bool SoakingTankAuto => _soakingTank != null && _soakingTank.Auto;
        public bool DryingTank1Auto => _dryingTanks != null && _dryingTanks.Length > 0 && _dryingTanks[0].Auto;
        public bool DryingTank2Auto => _dryingTanks != null && _dryingTanks.Length > 1 && _dryingTanks[1].Auto;
        public bool HeatingTankAuto => _heatingTank != null && _heatingTank.Auto;

        public bool SystemAuto => ShuttleAuto && SinkAuto && SoakingTankAuto && DryingTank1Auto && DryingTank2Auto && HeatingTankAuto;
        public bool HasAutoStatus => ShuttleAuto || SinkAuto || SoakingTankAuto || DryingTank1Auto || DryingTank2Auto || HeatingTankAuto;

        #endregion

        #region Initial

        private bool _initializing = false;
        public bool Initializing => _initializing;
        // track when initialization started to support timeout
        private DateTime? _initializingStartTime = null;



        private bool _shuttle_initialized_trigger = false;
        private bool _sink_initialized_trigger = false;
        private bool _soakingTank_initialized_trigger = false;
        private bool _dryingTank1_initialized_trigger = false;
        private bool _dryingTank2_initialized_trigger = false;
        private bool _heatingTank_initialized_trigger = false;
        private bool _shuttle_check_cassette_trigger = false;

        private bool _clamperCloseExist => _shuttle != null && _shuttle.HasCassette;

        private void ResetInitializedTrigger()
        {
            _shuttle_initialized_trigger = false;
            _sink_initialized_trigger = false;
            _soakingTank_initialized_trigger = false;
            _dryingTank1_initialized_trigger = false;
            _dryingTank2_initialized_trigger = false;
            _heatingTank_initialized_trigger = false;
            _shuttle_check_cassette_trigger = false;
        }

        public void AllMotorStop()
        {
            if (_sink != null) _sink.MotorStop();
            if (_soakingTank != null) _soakingTank.MotorStop();
            if (_shuttle != null) _shuttle.AllMotorStop();
        }

        public int CheckCanInitialize(out string status)
        {
            int result = 0; // Can Initialize
            status = "可以初始化";

            if (HasAutoStatus)
            {
                status = "設備中還有模組正處理自動狀態，因此無法初始化";
                result = 1; // one or more module has auto status
                OperateLog.Log("自動狀態下無法初始化", status);
            }
            if (HasSystemAlarm)
            {
                status = "設備中還有錯誤狀態發生，因此無法初始化，請先將錯誤狀態解除";
                result = 2; // there are alarms in the system
                OperateLog.Log("錯誤狀態下無法初始化", status);
            }
            if (_initializing)
            {
                status = "設備正在初始化，請勿重複進行初始化";
                result = 3; // the system is running initializing
                OperateLog.Log("初始化狀態下無法初始化", status);
            }
            if (_clamperCloseExist)
            {
                status = "設備夾爪上有卡匣存在，請先將卡匣取出";
                result = 4; // the clamper close and cassette exist
                OperateLog.Log("夾爪有卡匣狀態下無法初始化", status);
            }



            return result;
        }
        public bool Initialize(bool force = false)
        {
            bool result = false;

            if ((!HasSystemAlarm && !HasAutoStatus && _initializing) || force)
            {
                if (force)
                {
                    AllMotorStop();
                    OperateLog.Log("強制初始化", "按下 [強制初始化] 後會無視警報狀態對系統進行初始化，請確認狀態後再使用此功能。");
                }
                else
                    OperateLog.Log("開始初始化", "開始進行初始化動作");

                ResetInitializedTrigger();

                _initializing = true;
                try { _initializingStartTime = DateTime.UtcNow; } catch { _initializingStartTime = null; }

                result = true;
            }


            return result;
        }

        private void InitializedProcedure()
        {
            if (_initializing)
            {
                // Timeout check: if initialization takes longer than configured threshold, cancel
                try
                {
                    int timeoutSec = 0;
                    try { timeoutSec = _unitSettings?.System?.Initialization_Timeout_Second ?? 0; } catch { timeoutSec = 0; }
                    if (timeoutSec > 0 && _initializingStartTime.HasValue)
                    {
                        var elapsed = DateTime.UtcNow - _initializingStartTime.Value;
                        if (elapsed.TotalSeconds >= timeoutSec)
                        {
                            // initialization timed out -> cancel and raise alarm
                            _initializing = false;
                            _initializingStartTime = null;
                            _initializingTimeout_alarm = true;
                            OperateLog.Log("初始化逾時", $"Initialization timed out after {timeoutSec} seconds.");
                            return;
                        }
                    }
                }
                catch { }


                if (_shuttle_initialized_trigger && _shuttle != null && _shuttle.Initialized && _sink != null && _soakingTank != null)
                {
                    if (_sink_initialized_trigger && _soakingTank_initialized_trigger)
                    {
                        if (_sink.Initialized && _shuttle.Initialized)
                        {
                            // Check Procedure
                            if (!_shuttle_check_cassette_trigger)
                            {
                                _shuttle_check_cassette_trigger = true;
                                _shuttle.CheckTankCassetteExist();
                            }

                            if (_shuttle_check_cassette_trigger && _shuttle.HS_Check_Cassette_Finished)
                            {
                                bool cassetteExist = _shuttle.HS_Check_SinkCassetteExist || _shuttle.HS_Check_SoakingTankCassetteExist || _shuttle.HS_Check_DryingTank1CassetteExist || _shuttle.HS_Check_DryingTank2CassetteExist || _shuttle.HS_Check_DryingTank2CassetteExist;
                                _checkCassette_alarm = cassetteExist;
                                _initializing = false;
                                _initializingStartTime = null;
                            }
                        }
                    }

                    if (!_sink_initialized_trigger) { _sink_initialized_trigger = true; _sink.ModuleReset(); }
                    if (!_soakingTank_initialized_trigger) { _soakingTank_initialized_trigger = true; _soakingTank.ModuleReset(); }


                }

                if (_heatingTank != null && !_heatingTank_initialized_trigger) { _heatingTank_initialized_trigger = true; _heatingTank.ModuleReset(); }
                if (_dryingTanks != null && _dryingTanks.Length > 0 && !_dryingTank1_initialized_trigger) { _dryingTank1_initialized_trigger = true; _dryingTanks[0].ModuleReset(); }
                if (_dryingTanks != null && _dryingTanks.Length > 1 && !_dryingTank2_initialized_trigger) { _dryingTank2_initialized_trigger = true; _dryingTanks[1].ModuleReset(); }
                if (_shuttle != null && !_shuttle_initialized_trigger) { _shuttle_initialized_trigger = true; _shuttle.ModuleReset(); }
            }
            else
            {

            }
        }

        #endregion

        #region Auto Procedure

        private bool _auto_procedure_trigger = false;
        private bool _auto_procedure_executing = false;
        private bool _auto_procedure_pick_executing = false;
        private bool _auto_procedure_place_executing = false;
        private bool _auto_procedure_backtoP0_executing= false;
        private int _auto_procedure_current_pick_position = -1;
        private int _auto_procedure_current_place_position = -1;
        
        private bool CheckPickAndPlacePosition(out int pickPosition, out int placePosition)
        {
            pickPosition = -1; 
            placePosition = -1;


            if (!_auto_procedure_trigger && HasAutoStatus && _sink != null && _soakingTank != null && _dryingTanks != null && _dryingTanks.Length > 1)
            {
                if (UnloaderCanPlace)
                {
                    if (!_dryingTanks[1].ModulePass && _dryingTanks[1].HS_ActFinished) { pickPosition = 9; placePosition = UnloaderFirstEmptyPosition; return true; }
                    if (!_dryingTanks[0].ModulePass && _dryingTanks[0].HS_ActFinished) { pickPosition = 8; placePosition = UnloaderFirstEmptyPosition; return true; }
                    if (!_soakingTank.ModulePass && _dryingTanks[0].ModulePass && _dryingTanks[1].ModulePass && _soakingTank.HS_ActFinished) { pickPosition = 7; placePosition = UnloaderFirstEmptyPosition; return true; }
                    if (!_sink.ModulePass && _soakingTank.ModulePass && _dryingTanks[0].ModulePass && _dryingTanks[1].ModulePass && _sink.HS_ActFinished) { pickPosition = 6; placePosition = UnloaderFirstEmptyPosition; return true; }
                    if (_sink.ModulePass && _soakingTank.ModulePass && _dryingTanks[0].ModulePass && _dryingTanks[1].ModulePass && LoaderCanPick) { pickPosition = LoaderFirstCassettePosition; placePosition = UnloaderFirstEmptyPosition; return true; }
                }
                if (_dryingTanks[1].HS_InputPermit && !_dryingTanks[1].ModulePass)
                {
                    if (!_soakingTank.ModulePass && _soakingTank.HS_ActFinished) { pickPosition = 7; placePosition = 9;  return true; }
                    if(!_sink.ModulePass && _soakingTank.ModulePass && _sink.HS_ActFinished) { pickPosition = 6; placePosition = 9; return true; }
                    if(_sink.ModulePass && _soakingTank.ModulePass && LoaderCanPick) { pickPosition = LoaderFirstCassettePosition; placePosition = 9; return true; }
                }
                if (_dryingTanks[0].HS_InputPermit && !_dryingTanks[0].ModulePass)
                {
                    if (!_soakingTank.ModulePass && _soakingTank.HS_ActFinished) { pickPosition = 7; placePosition = 8; return true; }
                    if (!_sink.ModulePass && _soakingTank.ModulePass && _sink.HS_ActFinished) { pickPosition = 6; placePosition = 8; return true; }
                    if (_sink.ModulePass && _soakingTank.ModulePass && LoaderCanPick) { pickPosition = LoaderFirstCassettePosition; placePosition = 8; return true; }
                }
                if (_soakingTank.HS_InputPermit && !_soakingTank.ModulePass)
                { 
                    if(!_sink.ModulePass && _sink.HS_ActFinished) { pickPosition = 6; placePosition = 7; return true; }
                    if(_sink.ModulePass && LoaderCanPick) { pickPosition = LoaderFirstCassettePosition; placePosition = 7; return true; }
                }
                if(_sink.HS_InputPermit && !_sink.ModulePass)
                {
                    if (LoaderCanPick) { pickPosition = LoaderFirstCassettePosition; placePosition = 6; return true; }
                }

            }

            return false;
        }

        private void SetMoving(int position, bool moving)
        {
            if (_sink != null && _soakingTank != null && _dryingTanks != null && _dryingTanks.Length > 1)
            {
                if (position == 6) _sink.HS_ClamperMoving = moving;
                if (position == 7) _soakingTank.HS_ClamperMoving = moving;
                if (position == 8) _dryingTanks[0].HS_ClamperMoving = moving;
                if (position == 9) _dryingTanks[1].HS_ClamperMoving = moving;
            }
        }
        private void SetPickFinished(int position, bool pickFinished)
        {
            if (_sink != null && _soakingTank != null && _dryingTanks != null && _dryingTanks.Length > 1)
            {
                if (position == 6) _sink.HS_ClamperPickFinished = pickFinished;
                if (position == 7) _soakingTank.HS_ClamperPickFinished = pickFinished;
                if (position == 8) _dryingTanks[0].HS_ClamperPickFinished = pickFinished;
                if (position == 9) _dryingTanks[1].HS_ClamperPickFinished = pickFinished;
            }
        }
        private void SetPlaceFinished(int position, bool placeFinished)
        {
            if (_sink != null && _soakingTank != null && _dryingTanks != null && _dryingTanks.Length > 1)
            {
                if (position == 6) _sink.HS_ClamperPlaceFinished = placeFinished;
                if (position == 7) _soakingTank.HS_ClamperPlaceFinished = placeFinished;
                if (position == 8) _dryingTanks[0].HS_ClamperPlaceFinished = placeFinished;
                if (position == 9) _dryingTanks[1].HS_ClamperPlaceFinished = placeFinished;
            }
        }

        private string GetPositionName(int position)
        {
            if (position == 6) return "沖水槽";
            if (position == 7) return "浸泡槽";
            if (position == 8) return "烘乾槽#1";
            if (position == 9) return "烘乾槽#2";
            if (position >= 10 && position <= 14) return $"Unloader# {15 - position}";
            if (position >= 1 && position <= 5) return $"Loader# {position}";
            return $"Unknown Position {position}";
        }

        private void AutoProcedure()
        {
            if (_auto_procedure_trigger)
            {
                if (!ShuttleAuto)
                {
                    _auto_procedure_trigger = false;
                    OperateLog.Log("自動狀態異常", "系統處於自動狀態，但模組不在自動模式，已停止自動流程，請檢查模組狀態。");
                }
                else
                {
                    if (CheckPickAndPlacePosition(out int pickPosition, out int placePosition) && !_auto_procedure_executing)
                    {
                        _auto_procedure_executing = true;
                        _auto_procedure_current_pick_position = pickPosition;
                        _auto_procedure_current_place_position = placePosition;
                        _auto_procedure_pick_executing = false;
                        _auto_procedure_place_executing = false;
                        _auto_procedure_backtoP0_executing = false;
                        OperateLog.Log("自動流程啟動", $"系統自動流程啟動，Shuttle 將 卡匣 從 {GetPositionName(pickPosition)} 移動到 {GetPositionName(placePosition)}。");
                    }

                    if (_auto_procedure_executing && _shuttle != null)
                    {
                        if (!_auto_procedure_pick_executing)
                        {
                            _auto_procedure_pick_executing = _shuttle.PickCassette(_auto_procedure_current_pick_position);
                            SetMoving(_auto_procedure_current_pick_position, true);
                        }

                        if (_auto_procedure_pick_executing && !_auto_procedure_place_executing && !_shuttle.Moving && _shuttle.Cassette)
                        { 
                            SetMoving(_auto_procedure_current_pick_position, false);
                            SetPickFinished(_auto_procedure_current_pick_position, true);
                            _auto_procedure_place_executing = _shuttle.PlaceCassette(_auto_procedure_current_place_position);
                            SetMoving(_auto_procedure_current_place_position, true);
                        }

                        if (_auto_procedure_pick_executing && _auto_procedure_place_executing && !_auto_procedure_backtoP0_executing && !_shuttle.Moving && !_shuttle.Cassette)
                        { 
                            SetMoving(_auto_procedure_current_place_position, false);
                            SetPlaceFinished(_auto_procedure_current_place_position, true);
                            _auto_procedure_backtoP0_executing = _shuttle.BackToOriginalPosition();
                        }

                        if (_auto_procedure_backtoP0_executing && _shuttle.ShuttleXMotor != null && _shuttle.ShuttleXMotor.GetInPos(0))
                        {
                            _auto_procedure_executing = false;
                            _auto_procedure_pick_executing = false;
                            _auto_procedure_place_executing = false;
                            _auto_procedure_backtoP0_executing = false;
                        }
                    }


                }
            }
            else
            {
                _auto_procedure_executing = false;
                _auto_procedure_pick_executing = false;
                _auto_procedure_place_executing = false;
                _auto_procedure_backtoP0_executing = false;
            }
        }

        #endregion

       #region Auto & Stop & Pause Trigger

        public bool AutoStart()
        {
            bool result = false;

            if (SystemInitialized)
            {
                if (_shuttle != null && _sink != null && _soakingTank != null && _dryingTanks != null && _dryingTanks.Length > 1 && _heatingTank != null)
                {
                    _shuttle.AutoStart();
                    _sink.AutoStart();
                    _soakingTank.AutoStart();
                    _dryingTanks[0].AutoStart();
                    _dryingTanks[1].AutoStart();
                    _heatingTank.AutoStart();
                    OperateLog.Log("開始自動", "按下 [自動] 後會對系統進行啟動自動。");
                    result = true;
                }
            }

            return result;
        }

        public bool AutoStop(bool force = false)
        {
            bool result = false;

            if (SystemInitialized)
            {
                if (_shuttle != null && _sink != null && _soakingTank != null && _dryingTanks != null && _dryingTanks.Length > 1 && _heatingTank != null)
                {
                    if (force)
                    {
                        _shuttle.AlarmStop();
                        _sink.AlarmStop();
                        _soakingTank.AlarmStop();
                        _dryingTanks[0].AlarmStop();
                        _dryingTanks[1].AlarmStop();
                        _heatingTank.AlarmStop();
                        OperateLog.Log("強制停止自動", "按下 [強制停止] 後會對系統進行強制停止，所有元件都需要再重新初始化。");
                    }
                    else
                    {
                        _shuttle.AutoStop();
                        _sink.AutoStop();
                        _soakingTank.AutoStop();
                        _dryingTanks[0].AutoStop();
                        _dryingTanks[1].AutoStop();
                        _heatingTank.AutoStop();
                        OperateLog.Log("停止自動", "按下 [停止] 後會對系統進行停止，需要等所有模組進行完流程才會解除自動狀態。");
                    }
                    result = true;
                }
            }

            return result;
        }

        public bool AutoPause()
        {
            bool result = false;

            if (SystemInitialized)
            {
                if (_shuttle != null && _sink != null && _soakingTank != null && _dryingTanks != null && _dryingTanks.Length > 1 && _heatingTank != null)
                {
                    _shuttle.AutoPause();
                    _sink.AutoPause();
                    _soakingTank.AutoPause();
                    _dryingTanks[0].AutoPause();
                    _dryingTanks[1].AutoPause();
                    _heatingTank.AutoPause();
                    result = true; // indicate action performed
                    OperateLog.Log("動作暫停", "按下 [暫停] 後會對系統進行暫停，所有正在進行的動作會暫停在當前狀態，直到再次按下 [自動] 後才會繼續。");
                }
            }

            return result;
        }

        #endregion

        #region Light Tower

        private bool _buzzer_stop = false;

        public bool BuzzerStop => _buzzer_stop;

        public void CheckLightTower()
        {
            Tower_Red = HasSystemAlarm;
            Tower_Yellow = HasSystemWarning;
            Tower_Green = SystemAuto;
            Tower_Buzzer = !_buzzer_stop && HasSystemAlarm;
        }
        public void Buzzer_Stop()
        {
            if (!_buzzer_stop) OperateLog.Log("停止蜂鳴器", "按下 [停止蜂鳴器] 後停止蜂鳴等待，按下 [錯誤重置] 可以解除該狀態。");
            _buzzer_stop = true;

        }

        #endregion
    }
}
