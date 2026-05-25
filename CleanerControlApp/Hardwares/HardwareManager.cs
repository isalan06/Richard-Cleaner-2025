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
using NLog.LayoutRenderers;

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
                AlarmManager.AttachFlagGetter("ALM009", () => _plc_System_alarm);

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
        public bool FrontDoor1Sign => _plcOperator != null && !_plcOperator.FrontDoor1;
        public bool FrontDoor2Sign => _plcOperator != null && !_plcOperator.FrontDoor2;
        public bool FrontDoor3Sign => _plcOperator != null && !_plcOperator.FrontDoor3;
        public bool FrontDoor4Sign => _plcOperator != null && !_plcOperator.FrontDoor4;
        public bool SideDoor1Sign => _plcOperator != null && !_plcOperator.SideDoor1;
        public bool SideDoor2Sign => _plcOperator != null && !_plcOperator.SideDoor2;
        public bool Leakage1Sign => _plcOperator != null && _plcOperator.Leakage1;
        public bool Leakage2Sign => _plcOperator != null && _plcOperator.Leakage2;

        public bool WasteTankH => _plcOperator != null && !_plcOperator.WasteWaterPosH;

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
            // Use LongRunning to create a dedicated thread for continuous system management
            _loopTask = Task.Factory.StartNew(
                () => LoopAsync(token).GetAwaiter().GetResult(),
                token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
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
            DryRunProcedure();

            await Task.Yield();
        }

        #endregion

        #region Alarm

        private bool _communication_alarm => !Check_All_Modbus_Connected;
        private bool _emo_alarm => EMOSign;
        private bool _main_air_alarm => !MainAirSign;
        private bool _door_alarm => !FrontDoor1Sign || !FrontDoor2Sign || !FrontDoor3Sign || !FrontDoor4Sign || !SideDoor1Sign || !SideDoor2Sign;
        private bool _leakage_alarm => !Leakage1Sign || !Leakage2Sign;
        private bool _wasteTankH_alarm => WasteTankH;
        private bool _checkCassette_alarm { get; set; }

        private bool _initializingTimeout_alarm { get; set; }

        private bool _plc_System_alarm => _plcOperator != null &&_plcOperator.SystemError;


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
            if (_plcOperator != null)
            {
                // 立刻下達 PLC 錯誤重置命令
                _plcOperator.Command_AlarmReset = true;

                // 非同步背景任務：1 秒後清除指令（fire-and-forget，不阻塞呼叫端）
                var plcRef = _plcOperator;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                        try { plcRef.Command_AlarmReset = false; } catch { }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Background task clearing PLC Command_AlarmReset failed");
                    }
                });
            }

            await Task.Delay(_loopInterval).ConfigureAwait(false);

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
                _dryrun_procedure_case = 0;
                _dryrun_procedure_trigger = false;
            }

            if (_sink != null && _soakingTank != null && _heatingTank != null)
            {
                _sink.HS_WaterSystemError = _wasteTankH_alarm || _leakage_alarm || _heatingTank.HasAlarm;
                _soakingTank.HS_WaterSystemError = _wasteTankH_alarm || _leakage_alarm || _heatingTank.HasAlarm;
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
                int pos = 0;
                if (UnloadCassetteInPosition1) return pos;
                else
                {
                    pos = 10;
                    if (UnloadCassetteInPosition2) return pos;
                    else
                    {
                        pos = 11;
                        if (UnloadCassetteInPosition3) return pos;
                        else
                        {
                            pos = 12;
                            if (UnloadCassetteInPosition4) return pos;
                            else
                            {
                                pos = 13;
                                if (UnloadCassetteInPosition5) return pos;
                                else pos = 14;
                            }
                        }
                    }
                }
                return pos; // no empty position
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

        // Expose PassClamperCheckCassette from shuttle for UI toggling
        public bool ShuttlePassClamperCheckCassette
        {
            get => _shuttle != null && _shuttle.PassClamperCheckCassette;
            set
            {
                try
                {
                    if (_shuttle != null)
                        _shuttle.PassClamperCheckCassette = value;
                }
                catch { }
            }
        }

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

            _dryrun_procedure_trigger = false;
            _dryrun_procedure_case = 0;
        }

        public void AllMotorStop()
        {
            if (_sink != null) _sink.MotorStop();
            if (_soakingTank != null) _soakingTank.MotorStop();
            if (_shuttle != null) _shuttle.AllMotorStop();
        }
        public int CheckCanInitialize(out string status, bool logRecord = true)
        {
            int result = 0; // Can Initialize
            status = "可以初始化";

            if (HasAutoStatus)
            {
                status = "設備中還有模組正處理自動狀態，因此無法初始化";
                result = 1; // one or more module has auto status
                if(logRecord) OperateLog.Log("自動狀態下無法初始化", status);
            }
            if (HasSystemAlarm)
            {
                status = "設備中還有錯誤狀態發生，因此無法初始化，請先將錯誤狀態解除";
                result = 2; // there are alarms in the system
                if (logRecord) OperateLog.Log("錯誤狀態下無法初始化", status);
            }
            if (_initializing)
            {
                status = "設備正在初始化，請勿重複進行初始化";
                result = 3; // the system is running initializing
                if (logRecord) OperateLog.Log("初始化狀態下無法初始化", status);
            }
            if (_clamperCloseExist)
            {
                status = "設備夾爪上有卡匣存在，請先將卡匣取出";
                result = 4; // the clamper close and cassette exist
                if (logRecord) OperateLog.Log("夾爪有卡匣狀態下無法初始化", status);
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
                            if(!_sink.InPos1 && _sink.MotorHome && _sink.MotorIdle && _sink.Sensor_CoverOpen)
                            {
                                _sink.MoveToPosition(0, 0);
                            }

                            if(!_soakingTank.InPos1 && _soakingTank.MotorHome && _soakingTank.MotorIdle && _soakingTank.Sensor_CoverOpen)
                            {
                                _soakingTank.MoveToPosition(0, 0);
                            }

                            // Check Procedure
                            if (!_shuttle_check_cassette_trigger && _sink.InPos1 && _soakingTank.InPos1)
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

        private bool _dryrun_procedure_trigger = false;
        private int _dryrun_procedure_case = 0;
        private int _dryrun_procedure_loader_position = -1;
        private int _dryrun_procedure_unloader_position = -1;
        private string _dryrun_procedure_status = string.Empty;

        public bool DryRunProcedureStatus => _dryrun_procedure_trigger;
        public string DryRunProcedureStatusString => _dryrun_procedure_status;
        public bool StartDryRunProcedure()
        {
            if (!_auto_procedure_trigger && !_dryrun_procedure_trigger)
            {
                _dryrun_procedure_trigger = true;
                _dryrun_procedure_case = 0; // start from case 0
                _dryrun_procedure_status = "Dry Run Procedure Started";
                OperateLog.Log("開始 Dry Run 程序", "開始執行 Dry Run 程序，系統會進行各槽位取放卡匣的動作，無卡匣也可以運作，請確認狀態後再使用此功能。");
                return true;
            }
            return false;
        }
        public bool StopDryRunProcedure()
        {
            if (_dryrun_procedure_trigger)
            {
                _dryrun_procedure_trigger = false;
                _dryrun_procedure_case = 0;
                _dryrun_procedure_status = "Dry Run Procedure Stopped";
                OperateLog.Log("停止 Dry Run 程序", "停止 Dry Run 程序，系統會結束 Dry Run 程序的動作，請確認狀態後再使用此功能。");
                return true;
            }
            return false;
        }

        private bool CheckPickAndPlacePosition(out int pickPosition, out int placePosition)
        {
            pickPosition = -1; 
            placePosition = -1;


            if (_sink != null && _soakingTank != null && _dryingTanks != null && _dryingTanks.Length > 1)
            {
                if (UnloaderCanPlace)
                {
                    if (!_dryingTanks[1].ModulePass && _dryingTanks[1].HS_ActFinished) { pickPosition = 9; placePosition = UnloaderFirstEmptyPosition;  return true; }
                    if (!_dryingTanks[0].ModulePass && _dryingTanks[0].HS_ActFinished) { pickPosition = 8; placePosition = UnloaderFirstEmptyPosition;  return true; }
                    if (!_soakingTank.ModulePass && _dryingTanks[0].ModulePass && _dryingTanks[1].ModulePass && _soakingTank.HS_ActFinished) { pickPosition = 7; placePosition = UnloaderFirstEmptyPosition;  return true; }
                    if (!_sink.ModulePass && _soakingTank.ModulePass && _dryingTanks[0].ModulePass && _dryingTanks[1].ModulePass && _sink.HS_ActFinished) { pickPosition = 6; placePosition = UnloaderFirstEmptyPosition;  return true; }
                    if (_sink.ModulePass && _soakingTank.ModulePass && _dryingTanks[0].ModulePass && _dryingTanks[1].ModulePass && LoaderCanPick) { pickPosition = LoaderFirstCassettePosition; placePosition = UnloaderFirstEmptyPosition; return true; }
                }
                if (_dryingTanks[1].HS_InputPermit && !_dryingTanks[1].ModulePass)
                {
                    if (!_soakingTank.ModulePass && _soakingTank.HS_ActFinished) { pickPosition = 7; placePosition = 9;  return true; }
                    if(!_sink.ModulePass && _soakingTank.ModulePass && _sink.HS_ActFinished) { pickPosition = 6; placePosition = 9;  return true; }
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

        private bool IsZInOriPos => _shuttle!=null && _shuttle.ShuttleZMotor != null  && _shuttle.ShuttleZMotor.GetInPos(0);
        private bool IsXInOriPos => _shuttle != null && _shuttle.ShuttleXMotor != null && _shuttle.ShuttleXMotor.GetInPos(0);

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
                        OperateLog.Log("自動流程啟動", $"系統自動流程啟動，移載組 將 卡匣 從 {GetPositionName(pickPosition)} 移動到 {GetPositionName(placePosition)}。");
                    }

                    if (_auto_procedure_executing && _shuttle != null)
                    {
                        if (!_auto_procedure_pick_executing && IsZInOriPos)
                        {
                            if (_auto_procedure_pick_executing = _shuttle.PickCassette(_auto_procedure_current_pick_position))
                            {
                                SetMoving(_auto_procedure_current_pick_position, true);
                                OperateLog.Log("自動流程取卡啟動", $"移載組 開始從 {GetPositionName(_auto_procedure_current_pick_position)} 取卡匣。");
                            }
                        }

                        if (_auto_procedure_pick_executing && !_auto_procedure_place_executing && !_shuttle.Moving && _shuttle.Cassette && IsZInOriPos)
                        {
                            if (_auto_procedure_place_executing = _shuttle.PlaceCassette(_auto_procedure_current_place_position))
                            {
                                SetMoving(_auto_procedure_current_pick_position, false);
                                SetPickFinished(_auto_procedure_current_pick_position, true);

                                SetMoving(_auto_procedure_current_place_position, true);
                                OperateLog.Log("自動流程放卡啟動", $"移載組 開始將卡匣放到 {GetPositionName(_auto_procedure_current_place_position)}。");
                            }
                        }

                        if (_auto_procedure_pick_executing && _auto_procedure_place_executing && !_auto_procedure_backtoP0_executing && !_shuttle.Moving && !_shuttle.Cassette && IsZInOriPos)
                        {
                            if (_auto_procedure_backtoP0_executing = _shuttle.BackToOriginalPosition())
                            {
                                SetMoving(_auto_procedure_current_place_position, false);
                                SetPlaceFinished(_auto_procedure_current_place_position, true);

                                OperateLog.Log("自動流程回原點啟動", $"移載組 開始回到原點。");
                            }
                        }

                        if (_auto_procedure_backtoP0_executing && _shuttle.ShuttleXMotor != null && _shuttle.ShuttleXMotor.GetInPos(0))
                        {
                            _auto_procedure_executing = false;
                            _auto_procedure_pick_executing = false;
                            _auto_procedure_place_executing = false;
                            _auto_procedure_backtoP0_executing = false;
                            OperateLog.Log("自動流程完成", $"移載組 已完成從 {GetPositionName(_auto_procedure_current_pick_position)} 移動到 {GetPositionName(_auto_procedure_current_place_position)} 的流程，並回到原點。");
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

        private void DryRunProcedure()
        {
            if (_dryrun_procedure_trigger)
            {
                switch (_dryrun_procedure_case)
                {
                    case 0: // check loader and unloader
                        if (_shuttle != null)
                        {
                            _dryrun_procedure_status = $"Case 0: Loader Position: {_dryrun_procedure_loader_position}, Unloader Position: {_dryrun_procedure_unloader_position}, Home(true): {_shuttle.MotorHome}, Idle(true): {_shuttle.MotorIdle}, IsZInOriPos(true): {IsZInOriPos}";
                            if (_shuttle.MotorHome && _shuttle.MotorIdle && IsZInOriPos)
                            {
                                _dryrun_procedure_loader_position = LoaderFirstCassettePosition;
                                _dryrun_procedure_unloader_position = UnloaderFirstEmptyPosition;

                                if (_dryrun_procedure_loader_position > 0 && _dryrun_procedure_unloader_position > 0)
                                    _dryrun_procedure_case = 1; // move to next case
                                else
                                {
                                    _dryrun_procedure_status = $"Case 0: Failed to get valid Loader or Unloader position. Loader Position: {_dryrun_procedure_loader_position}, Unloader Position: {_dryrun_procedure_unloader_position}";
                                    _dryrun_procedure_case = 99; // error case
                                }
                            }
                        }
                        break;

                    case 1: // pick from loader
                        if (_shuttle != null)
                        {
                            bool result = _shuttle.PickCassette(_dryrun_procedure_loader_position, true);
                            if (result)
                            {
                                _dryrun_procedure_status = $"Case 1: Picked from Loader Position: {_dryrun_procedure_loader_position}";
                                _dryrun_procedure_case = 2; // move to next case
                            }
                            else
                            {
                                _dryrun_procedure_status = $"Case 1: Failed to pick from Loader Position: {_dryrun_procedure_loader_position}";
                                _dryrun_procedure_case = 99; // error case
                            }
                        }
                        break;

                    case 2: // wait for finish of picking fron loader
                        if (_shuttle != null)
                        {
                            _dryrun_procedure_status = $"Case 2: Waiting for finishing picking from Loader Position: {_dryrun_procedure_loader_position}, Cassette(true): {_shuttle.Cassette}, Idle(true): {_shuttle.MotorIdle}, IsZInOriPos(true): {IsZInOriPos}";
                            if (_shuttle.Cassette && _shuttle.MotorIdle && IsZInOriPos)
                            {
                                _dryrun_procedure_case = 3; // move to next case
                            }
                        }
                        break;

                    case 3: // place to sink
                        if (_shuttle != null)
                        {
                            int _placePosition = 6; // Sink position for dry run
                            _dryrun_procedure_status = $"Case 3: Placed to Sink Position: {_placePosition}, Cassette(true): {_shuttle.Cassette}, NormalStatus(true): {_shuttle.IsNormalStatus}";
                            if (_shuttle.Cassette && _shuttle.IsNormalStatus)
                            {

                                bool result = _shuttle.PlaceCassette(_placePosition, true);
                                if (result)
                                {
                                    _dryrun_procedure_case = 4; // move to next case
                                }
                                else
                                {
                                    _dryrun_procedure_status = $"Case 3: Failed to place to Sink Position: {_placePosition}";
                                    _dryrun_procedure_case = 99; // error case
                                }
                            }
                        }
                        break;

                    case 4: // wait for finish of placing to sink
                        if (_shuttle != null)
                        {
                            int _placePosition = 6; // Sink position for dry run
                            _dryrun_procedure_status = $"Case 4: Waiting for finishing placing to Sink Position: {_placePosition}, Cassette(false): {_shuttle.Cassette}, Idle(true): {_shuttle.MotorIdle}, IsZInOriPos(true): {IsZInOriPos}";
                            if (!_shuttle.Cassette && _shuttle.MotorIdle && IsZInOriPos)
                            {
                                _dryrun_procedure_case = 5; // move to next case
                            }
                        }
                        break;

                    case 5: // pick from sink
                        if (_shuttle != null)
                        {
                            int _pickPosition = 6; // Sink position for pick
                            _dryrun_procedure_status = $"Case 5: Picked from Sink Position: {_pickPosition}, Cassette(false): {_shuttle.Cassette}, NormalStatus(true): {_shuttle.IsNormalStatus}";
                            if (!_shuttle.Cassette && _shuttle.IsNormalStatus)
                            {
                                
                                bool result = _shuttle.PickCassette(_pickPosition, true);
                                if (result)
                                {
                                    _dryrun_procedure_case = 6; // move to next case
                                }
                                else
                                {
                                    _dryrun_procedure_status = $"Case 5: Failed to pick from Sink Position: {_pickPosition}";
                                    _dryrun_procedure_case = 99; // error case
                                }
                            }
                        }
                        break;

                    case 6: // wait for finish of picking from sink
                        if (_shuttle != null)
                        {
                            int _pickPosition = 6; // Sink position for pick
                            _dryrun_procedure_status = $"Case 6: Waiting for finishing picking from Sink Position: {_pickPosition}, Cassette(true): {_shuttle.Cassette}, Idle(true): {_shuttle.MotorIdle}, IsZInOriPos(true): {IsZInOriPos}";
                            if (_shuttle.Cassette && _shuttle.MotorIdle && IsZInOriPos)
                            {
                                _dryrun_procedure_case = 7; // move to next case
                            }
                        }
                        break;

                    case 7: // place to soaking tank
                        if (_shuttle != null)
                        {
                            int _placePosition = 7; // soaking tank position for dry run
                            _dryrun_procedure_status = $"Case 7: Placed to Unloader Position: {_placePosition}, Cassette(true): {_shuttle.Cassette}, NormalStatus(true): {_shuttle.IsNormalStatus}";
                            if (_shuttle.Cassette && _shuttle.IsNormalStatus)
                            {
                                
                                bool result = _shuttle.PlaceCassette(_placePosition, true);
                                if (result)
                                {
                                    _dryrun_procedure_case = 8; // move to next case
                                }
                                else
                                {
                                    _dryrun_procedure_status = $"Case 7: Failed to place to Unloader Position: {_placePosition}";
                                    _dryrun_procedure_case = 99; // error case
                                }
                            }
                        }
                        break;

                    case 8: // wait for finish of placing to soaking tank
                        if (_shuttle != null)
                        {
                            int _placePosition = 7; // soaking tank position for dry run
                            _dryrun_procedure_status = $"Case 8: Waiting for finishing placing to Soaking Tank Position: {_placePosition}, Cassette(false): {_shuttle.Cassette}, Idle(true): {_shuttle.MotorIdle}, IsZInOriPos(true): {IsZInOriPos}";
                            if (!_shuttle.Cassette && _shuttle.MotorIdle && IsZInOriPos)
                            {
                                _dryrun_procedure_case = 9; // move to next case
                            }
                        }
                        break;

                    case 9: // pick from soaking tank
                        if (_shuttle != null)
                        {
                            int _pickPosition = 7; // soaking tank position for pick
                            _dryrun_procedure_status = $"Case 9: Picked from Soaking Tank Position: {_pickPosition}, Cassette(false): {_shuttle.Cassette}, NormalStatus(true): {_shuttle.IsNormalStatus}";
                            if (!_shuttle.Cassette && _shuttle.IsNormalStatus)
                            {

                                bool result = _shuttle.PickCassette(_pickPosition, true);
                                if (result)
                                {
                                    _dryrun_procedure_case = 10; // move to next case
                                }
                                else
                                {
                                    _dryrun_procedure_status = $"Case 5: Failed to pick from Soaking Position: {_pickPosition}";
                                    _dryrun_procedure_case = 99; // error case
                                }
                            }
                        }
                        break;

                    case 10: // wait for finish of picking from soaking tank
                        if (_shuttle != null)
                        {
                            int _pickPosition = 7; // soaking tank position for pick
                            _dryrun_procedure_status = $"Case 10: Waiting for finishing picking from Soaking Tank Position: {_pickPosition}, Cassette(true): {_shuttle.Cassette}, Idle(true): {_shuttle.MotorIdle}, IsZInOriPos(true): {IsZInOriPos}";
                            if (_shuttle.Cassette && _shuttle.MotorIdle && IsZInOriPos)
                            {
                                _dryrun_procedure_case = 11; // move to next case
                            }
                        }
                        break;

                    case 11: // place to drying tank #1
                        if (_shuttle != null)
                        { 
                            int _placePosition = 8; // Drying Tank #1 position for dry run 
                            _dryrun_procedure_status = $"Case 11: Placed to Drying Tank #1 Position: {_placePosition}, Cassette(true): {_shuttle.Cassette}, NormalStatus(true): {_shuttle.IsNormalStatus}";
                            if(_shuttle.Cassette && _shuttle.IsNormalStatus)
                            {
                                bool result = _shuttle.PlaceCassette(_placePosition, true);
                                if (result)
                                {
                                    _dryrun_procedure_case = 12; // move to next case
                                }
                                else
                                {
                                    _dryrun_procedure_status = $"Case 11: Failed to place to Drying Tank #1 Position: {_placePosition}";
                                    _dryrun_procedure_case = 99; // error case
                                }
                            }
                        }
                        break;

                    case 12: // wait for finish of placing to drying tank #1
                        if (_shuttle != null)
                        { 
                            int _placePosition = 8; // Drying Tank #1 position for dry run 
                            _dryrun_procedure_status = $"Case 12: Waiting for finishing placing to Drying Tank #1 Position: {_placePosition}, Cassette(false): {_shuttle.Cassette}, Idle(true): {_shuttle.MotorIdle}, IsZInOriPos(true): {IsZInOriPos}";
                            if (!_shuttle.Cassette && _shuttle.MotorIdle && IsZInOriPos)
                            {
                                _dryrun_procedure_case = 13; // move to next case
                            }
                        }
                        break;

                    case 13: // pick from drying tank #1
                        if (_shuttle != null)
                        { 
                            int _pickPosition = 8; // Drying Tank #1 position for pick
                            _dryrun_procedure_status = $"Case 13: Picked from Drying Tank #1 Position: {_pickPosition}, Cassette(false): {_shuttle.Cassette}, NormalStatus(true): {_shuttle.IsNormalStatus}";
                            if(!_shuttle.Cassette && _shuttle.IsNormalStatus)
                            {
                                bool result = _shuttle.PickCassette(_pickPosition, true);
                                if (result)
                                {
                                    _dryrun_procedure_case = 14; // move to next case
                                }
                                else
                                {
                                    _dryrun_procedure_status = $"Case 13: Failed to pick from Drying Tank #1 Position: {_pickPosition}";
                                    _dryrun_procedure_case = 99; // error case
                                }
                            }
                        }
                        break;

                    case 14: // wait for finish of picking from drying tank #1
                        if (_shuttle != null)
                        { 
                            int _pickPosition = 8; // Drying Tank #1 position for pick
                            _dryrun_procedure_status = $"Case 14: Waiting for finishing picking from Drying Tank #1 Position: {_pickPosition}, Cassette(true): {_shuttle.Cassette}, Idle(true): {_shuttle.MotorIdle}, IsZInOriPos(true): {IsZInOriPos}";
                            if (_shuttle.Cassette && _shuttle.MotorIdle && IsZInOriPos)
                            {
                                _dryrun_procedure_case = 15; // move to next case
                            }
                        }
                        break;

                    case 15: // place to drying tank #2
                        if (_shuttle != null)
                        { 
                            int _placePosition = 9; // Drying Tank #2 position for dry run
                            _dryrun_procedure_status = $"Case 15: Placed to Drying Tank #2 Position: {_placePosition}, Cassette(true): {_shuttle.Cassette}, NormalStatus(true): {_shuttle.IsNormalStatus}";
                            if(_shuttle.Cassette && _shuttle.IsNormalStatus)
                            {
                                bool result = _shuttle.PlaceCassette(_placePosition, true);
                                if (result)
                                {
                                    _dryrun_procedure_case = 16; // move to next case
                                }
                                else
                                {
                                    _dryrun_procedure_status = $"Case 15: Failed to place to Drying Tank #2 Position: {_placePosition}";
                                    _dryrun_procedure_case = 99; // error case
                                }
                            }
                        }
                        break;

                    case 16: // wait for finish of placing to drying tank #2
                        if (_shuttle != null)
                        { 
                            int _placePosition = 9; // Drying Tank #2 position for dry run
                            _dryrun_procedure_status = $"Case 16: Waiting for finishing placing to Drying Tank #2 Position: {_placePosition}, Cassette(false): {_shuttle.Cassette}, Idle(true): {_shuttle.MotorIdle}, IsZInOriPos(true): {IsZInOriPos}";
                            if (!_shuttle.Cassette && _shuttle.MotorIdle && IsZInOriPos)
                            {
                                _dryrun_procedure_case = 17; // move to next case
                            }
                        }
                        break;

                    case 17: // pick from drying tank #2
                        if (_shuttle != null)
                        { 
                            int _pickPosition = 9; // Drying Tank #2 position for pick
                            _dryrun_procedure_status = $"Case 17: Picked from Drying Tank #2 Position: {_pickPosition}, Cassette(false): {_shuttle.Cassette}, NormalStatus(true): {_shuttle.IsNormalStatus}";
                            if(!_shuttle.Cassette && _shuttle.IsNormalStatus)
                            {
                                bool result = _shuttle.PickCassette(_pickPosition, true);
                                if (result)
                                {
                                    _dryrun_procedure_case = 18; // move to next case
                                }
                                else
                                {
                                    _dryrun_procedure_status = $"Case 17: Failed to pick from Drying Tank #2 Position: {_pickPosition}";
                                    _dryrun_procedure_case = 99; // error case
                                }
                            }
                        }
                        break;

                    case 18: // wait for finish of picking from drying tank #2
                        if (_shuttle != null)
                        { 
                            int _pickPosition = 9; // Drying Tank #2 position for pick
                            _dryrun_procedure_status = $"Case 18: Waiting for finishing picking from Drying Tank #2 Position: {_pickPosition}, Cassette(true): {_shuttle.Cassette}, Idle(true): {_shuttle.MotorIdle}, IsZInOriPos(true): {IsZInOriPos}";
                            if (_shuttle.Cassette && _shuttle.MotorIdle && IsZInOriPos)
                            {
                                _dryrun_procedure_case = 19; // move to next case
                            }
                        }
                        break;

                    case 19: // place back to unloader
                        if (_shuttle != null)
                        {
                            int _placePosition = _dryrun_procedure_unloader_position; // place back to unloader position for dry run
                            _dryrun_procedure_status = $"Case 19: Placed back to Unloader Position: {_placePosition}, Cassette(true): {_shuttle.Cassette}, NormalStatus(true): {_shuttle.IsNormalStatus}";
                            if(_shuttle.Cassette && _shuttle.IsNormalStatus)
                            {
                                bool result = _shuttle.PlaceCassette(_placePosition, true);
                                if (result)
                                {
                                    _dryrun_procedure_case = 20; // move to next case
                                }
                                else
                                {
                                    _dryrun_procedure_status = $"Case 19: Failed to place back to Unloader Position: {_placePosition}";
                                    _dryrun_procedure_case = 99; // error case
                                }
                            }
                        }
                        break;

                    case 20: // wait for finish of placing back to unloader
                        if (_shuttle != null)
                        {
                            int _placePosition = _dryrun_procedure_unloader_position; // place back to unloader position for dry run
                            _dryrun_procedure_status = $"Case 20: Waiting for finishing placing back to Unloader Position: {_placePosition}, Cassette(false): {_shuttle.Cassette}, Idle(true): {_shuttle.MotorIdle}, IsZInOriPos(true): {IsZInOriPos}";
                            if (!_shuttle.Cassette && _shuttle.MotorIdle && IsZInOriPos)
                            {
                                _dryrun_procedure_case = 21; // move to next case
                            }
                        }
                        break;

                    case 21: // check shuttle is empty and back to original position
                        if (_shuttle != null)
                        { 
                            _dryrun_procedure_status = $"Case 21: Checking if shuttle is empty and back to original position. Cassette(false): {_shuttle.Cassette}, NormalStatus(true): {_shuttle.IsNormalStatus}";
                            if (!_shuttle.Cassette && _shuttle.IsNormalStatus)
                            {
                                bool result = _shuttle.BackToOriginalPosition();
                                if (result)
                                {
                                    _dryrun_procedure_case = 22; // move to next case
                                }
                                else
                                {
                                    _dryrun_procedure_status = $"Case 21: Waiting for shuttle to be empty and back to original position. Cassette: {_shuttle.Cassette}, Idle: {_shuttle.MotorIdle}, IsZInOriPos: {IsZInOriPos}";
                                    _dryrun_procedure_case = 99; // error case
                                }
                            }
                            
                        }
                        break;

                    case 22: // dry run procedure completed
                        if (_shuttle != null)
                        { 
                            _dryrun_procedure_status = $"Case 22: Waiting for Dry run procedure completed., Idle(true): {_shuttle.MotorIdle}, IsXInOriPos(true): {IsXInOriPos}";
                            if(_shuttle.MotorIdle && IsXInOriPos)
                            {
                                _dryrun_procedure_case = 24; // final case to default value for finishing procedure
                                _dryrun_procedure_status = $"Case 22: Dry run procedure completed successfully.";
                            }
                        }
                        break;

                    default:
                        _dryrun_procedure_trigger = false;
                        _dryrun_procedure_case = 0; 
                        break;
                }
            }
            else
            {
                _dryrun_procedure_case = 0;
                _dryrun_procedure_loader_position = -1;
                _dryrun_procedure_unloader_position = -1;
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
                    _auto_procedure_trigger = true;
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

        #region Hint

        public string Hint()
        { 
            var sb = new StringBuilder();

            // Overall system status
            sb.AppendLine($"整體系統狀態:");
            sb.AppendLine($" - 背景循環運行: {_running}");
            sb.AppendLine($" - 系統初始化中: {_initializing}");
            if (_initializing && _initializingStartTime.HasValue)
            {
                var elapsed = DateTime.UtcNow - _initializingStartTime.Value;
                sb.AppendLine($" - 初始化已進行: {elapsed.TotalSeconds:F0} 秒");
            }
            sb.AppendLine($" - 系統已初始化: {SystemInitialized}");
            sb.AppendLine($" - 系統自動模式: {SystemAuto}");
            sb.AppendLine($" - 系統有錯誤(Alarm): {HasSystemAlarm}");
            sb.AppendLine($" - 系統有警告(Warning): {HasSystemWarning}");

            // Communication
            sb.AppendLine("通訊狀態:");
            sb.AppendLine($" - Modbus TCP:  {ModbusTCPConnected}");
            sb.AppendLine($" - Modbus RTU1: {ModbusRTU1Connected}");
            sb.AppendLine($" - Modbus RTU2: {ModbusRTU2Connected}");
            sb.AppendLine($" - Modbus RTU3: {ModbusRTU3Connected}");
            sb.AppendLine($" - Modbus RTU4: {ModbusRTU4Connected}");
            sb.AppendLine($" - 所有 Modbus連線 (或已繞過檢查): {Check_All_Modbus_Connected}");

            // Module summary
            sb.AppendLine("模組狀態:");
            if (_shuttle != null)
                sb.AppendLine($" - 移載組: \t已初始化={ShuttleInitialized}, \t空閒={ShuttleIdle}, \t自動={ShuttleAuto}, \t錯誤={HasShuttleAlarm}, \t警告={HasShuttleWarning}");
            if (_sink != null)
                sb.AppendLine($" - 沖水槽: \t已初始化={SinkInitialized}, \t空閒={SinkIdle}, \t自動={SinkAuto}, \t錯誤={HasSinkAlarm}, \t警告={HasSinkWarning}");
            if (_soakingTank != null)
                sb.AppendLine($" - 浸泡槽: \t已初始化={SoakingTankInitialized}, \t空閒={SoakingTankIdle}, \t自動={SoakingTankAuto}, \t錯誤={HasSoakingTankAlarm}, \t警告={HasSoakingTankWarning}");
            if (_dryingTanks != null && _dryingTanks.Length >0)
                sb.AppendLine($" - 烘乾槽#1: \t已初始化={DryingTank1Initialized}, \t空閒={DryingTank1Idle}, \t自動={DryingTank1Auto}, \t錯誤={HasDryingTank1Alarm}, \t警告={HasDryingTank1Warning}");
            if (_dryingTanks != null && _dryingTanks.Length >1)
                sb.AppendLine($" - 烘乾槽#2: \t已初始化={DryingTank2Initialized}, \t空閒={DryingTank2Idle}, \t自動={DryingTank2Auto}, \t錯誤={HasDryingTank2Alarm}, \t警告={HasDryingTank2Warning}");
            if (_heatingTank != null)
                sb.AppendLine($" - 加熱槽: \t已初始化={HeatingTankInitialized}, \t空閒={HeatingTankIdle}, \t自動={HeatingTankAuto}, \t錯誤={HasHeatingTankAlarm}, \t警告={HasHeatingTankWarning}");

            // Alarms / Warnings details
            sb.AppendLine("錯誤/警告 明細:");
            sb.AppendLine($" - 通訊錯誤: {!Check_All_Modbus_Connected}");
            sb.AppendLine($" - EMO(急停) 錯誤: {!EMOSign}");
            sb.AppendLine($" - 主氣壓 錯誤: {!MainAirSign}");
            sb.AppendLine($" - 門開關 錯誤: {FrontDoor1Sign || FrontDoor2Sign || FrontDoor3Sign || FrontDoor4Sign || SideDoor1Sign || SideDoor2Sign}");
            sb.AppendLine($" - 漏水 錯誤: {Leakage1Sign || Leakage2Sign}");
            sb.AppendLine($" - 廢水槽高位: {WasteTankH}");
            sb.AppendLine($" - 初始化逾時警告: {_initializingTimeout_alarm}");
            sb.AppendLine($" - 卡匣檢查警告: {_checkCassette_alarm}");

            // Next possible actions / suggestions
            sb.AppendLine("建議下一步:");

            _Next(ref sb);

            // Small operational hints
            sb.AppendLine("操作提示:");
            sb.AppendLine(" - 自動流程會等待移載組的 移動及卡匣 狀態，以及各模組的 交握 訊號。");
            sb.AppendLine(" - 初始化會對每個模組呼叫 初始化流程，之後由移載組執行卡匣檢查；初始化時間會依設定逾時。");

            return sb.ToString();
        }

        private void _Next(ref StringBuilder sb)
        {
            if (sb != null)
            {

                if (HasSystemAlarm)
                {
                    sb.AppendLine(" - 系統目前有錯誤。請先排除硬體問題，之後長按[錯誤重置]1秒以清除錯誤狀態。");
                    sb.AppendLine(" - 也可暫時停止蜂鳴器：按下[蜂鳴器停止]。");
                }

                if (!_initializing && !HasSystemAlarm && !HasAutoStatus)
                {
                    sb.AppendLine(" - 系統可進行初始化：長按[初始化]1秒 。");
                }
                else if (HasAutoStatus)
                {
                    sb.AppendLine(" - 目前有模組處於自動模式。若要重新初始化，請先按下 [停止] 停止自動。");
                }

                if (SystemInitialized && !SystemAuto && !HasSystemAlarm)
                {
                    sb.AppendLine(" - 系統已初始化並可開始自動：按下 [啟動] 開始自動運作。");
                }
                if (SystemAuto)
                {
                    sb.AppendLine(" - 系統目前處於自動模式：可按下 [暫停] 讓設備暫時停止或 長按 [停止] 強制停止。");
                }

                if (!Check_All_Modbus_Connected)
                {
                    sb.AppendLine(" - 通訊尚未完全連線。請檢查網路/序列埠連線。");
                }

                // If initialization was attempted but rejected, include reason
                var canInitCode = CheckCanInitialize(out string initStatus, false);
                if (canInitCode != 0)
                {
                    sb.AppendLine($" - 初始化被阻止：{initStatus}");
                    if (canInitCode == 1)
                        sb.AppendLine(" (請停止模組自動模式以允許初始化。)");
                    if (canInitCode == 2)
                        sb.AppendLine(" (請先解除錯誤後再初始化。)");
                    if (canInitCode == 3)
                        sb.AppendLine(" (系統正在初始化中。)");
                    if (canInitCode == 4)
                        sb.AppendLine(" (請先移除夾爪上的卡匣。)");
                }
            }
        }
        public string Next()
        {
            var sb = new StringBuilder();

            _Next(ref sb);

            return sb.ToString();
        }

        #endregion
    }
}
