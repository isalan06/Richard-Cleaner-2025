using CleanerControlApp.Hardwares.DryingTank.Interfaces;
using CleanerControlApp.Modules.MitsubishiPLC.Interfaces;
using CleanerControlApp.Modules.Modbus.Interfaces;
using CleanerControlApp.Modules.Modbus.Models;
using CleanerControlApp.Modules.TempatureController.Interfaces;
using CleanerControlApp.Modules.UltrasonicDevice.Models;
using CleanerControlApp.Modules.UltrasonicDevice.Services;
using CleanerControlApp.Utilities;
using CleanerControlApp.Utilities.Alarm;
using CleanerControlApp.Utilities.Log;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Hardwares.DryingTank.Services
{
    public class DryingTank : IDryingTank, IDisposable
    {

        #region Constant

        public static readonly int DryingTankCount = 2;

        #endregion

        #region attribute

        private readonly ILogger<DryingTank>? _logger;

        // background loop
        private CancellationTokenSource? _cts;
        private Task? _loopTask;
        private readonly TimeSpan _loopInterval = TimeSpan.FromMilliseconds(10);

        //private IModbusRTUService? _modbusService;

        private bool _running;

        private int _moduleIndex = 0;

        private int ModuleIndex => (_moduleIndex == 0) ? 2 : 3;

        private readonly UnitSettings _unitSettings;
        private readonly ModuleSettings _moduleSettings;

        private readonly IPLCOperator? _plcService;

        private readonly ISingleTemperatureController? _temperatureController;
        private readonly ITemperatureControllers? _temperatureControllers;

        private bool _auto = false;
        private bool _pausing = false;
        private bool _heating = false;
        private bool _cassette = false;
        private bool _initialized = false;
        private bool _autoStopFlag = false;

        private int PV_Check_High => ((_moduleSettings.DryingTanks != null ? _moduleSettings.DryingTanks[_moduleIndex].SV_High : 0)) - 
            (_unitSettings.DryingTanks!= null ? _unitSettings.DryingTanks[_moduleIndex].SV_CheckOffet : 0);
        private int PV_Check_Low => ((_moduleSettings.DryingTanks != null ? _moduleSettings.DryingTanks[_moduleIndex].SV_Low : 0)) +
            (_unitSettings.DryingTanks != null ? _unitSettings.DryingTanks[_moduleIndex].SV_CheckOffet : 0);

        private static int _pvLowTimeoutThreshold_Value = 30;
        private static int _pvHighTimeoutThreshold_Value = 30;
        private static int _coverOpenTimeoutThreshold_Value = 30;
        private static int _coverCloseTimeoutThreshold_Value = 30;

        private bool _sim_pv = false;

        #endregion

        #region constructor

        public DryingTank(int moduleIndex, ILogger<DryingTank>? logger, IPLCOperator? plcService, ITemperatureControllers? temperatureControllers, UnitSettings unitSettings, ModuleSettings moduleSettings)
        {
            _moduleIndex = moduleIndex;

            _unitSettings = unitSettings;
            _moduleSettings = moduleSettings;

            _logger = logger;

            _plcService = plcService;

            _temperatureController = temperatureControllers?[ModuleIndex];
            _temperatureControllers = temperatureControllers;

            RefreshTimeoutValue();

            // Alarm
            AlarmManager.AttachFlagGetter((moduleIndex == 0) ? "ALM401" : "ALM501", () => _PV_Low_Timeout);
            AlarmManager.AttachFlagGetter((moduleIndex == 0) ? "ALM402" : "ALM502", () => _PV_High_Timeout);
            AlarmManager.AttachFlagGetter((moduleIndex == 0) ? "ALM403" : "ALM503", () => _Cover_Open_Timeout);
            AlarmManager.AttachFlagGetter((moduleIndex == 0) ? "ALM404" : "ALM504", () => _Cover_Close_Timeout);

            StartLoop();

            Start();
        }

        #endregion

        #region destructor and dispose pattern

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
        ~DryingTank()
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

        #region IDryingTank


        public bool IsRunning => _running;
        public void Start() { _running = true; }
        public void Stop() { _running = false; }

        public bool Sensor_CoverOpen
        {
            get
            {
                if (_moduleIndex == 0)
                {
                    return (_plcService != null) && _plcService.Heater1CoverFIn;
                }
                else
                {
                    return (_plcService != null) && _plcService.Heater2CoverFIn;
                }
            }
        }
        public bool Sensor_CoverClose
        {
            get
            {
                if (_moduleIndex == 0)
                {
                    return (_plcService != null) && _plcService.Heater1CoverBIn;
                }
                else
                {
                    return (_plcService != null) && _plcService.Heater2CoverBIn;
                }
            }
        }

        public bool Command_HeaterCoverClose
        {
            get
            {
                if (_moduleIndex == 0)
                {
                    return (_plcService != null) && _plcService.Command_Heater1CoverOpen;
                }
                else
                {
                    return (_plcService != null) && _plcService.Command_Heater2CoverOpen;
                }
            }
            set
            {
                if (_moduleIndex == 0)
                {
                    if (_plcService != null)
                    {
                        _plcService.Command_Heater1CoverOpen = value;
                    }
                }
                else
                {
                    if (_plcService != null)
                    {
                        _plcService.Command_Heater2CoverOpen = value;
                    }
                }
            }
        }

        public bool Command_HeaterAirOpen
        {
            get
            {
                if (_moduleIndex == 0)
                {
                    return (_plcService != null) && _plcService.Command_Heater1AirOpen;
                }
                else
                {
                    return (_plcService != null) && _plcService.Command_Heater2AirOpen;
                }
            }
            set
            {
                if (_moduleIndex == 0)
                {
                    if (_plcService != null)
                    {
                        _plcService.Command_Heater1AirOpen = value;
                    }
                }
                else
                {
                    if (_plcService != null)
                    {
                        _plcService.Command_Heater2AirOpen = value;
                    }
                }
            }
        }

        public bool Command_HeaterBlower
        {
            get
            {
                if (_moduleIndex == 0)
                {
                    return (_plcService != null) && _plcService.Command_Heater1Blower;
                }
                else
                {
                    return (_plcService != null) && _plcService.Command_Heater2Blower;
                }
            }
            set
            {
                if (_moduleIndex == 0)
                {
                    if (_plcService != null)
                    {
                        _plcService.Command_Heater1Blower = value;
                    }
                }
                else
                {
                    if (_plcService != null)
                    {
                        _plcService.Command_Heater2Blower = value;
                    }
                }
            }
        }

        public int PV
        {
            get => (_temperatureController != null) ? _temperatureController.PV : 0;
        }
        public int SV
        {
            get => (_temperatureController != null) ? _temperatureController.SV : 0;
        }
        public float PV_Value
        {
            get => (_temperatureController != null && _unitSettings.DryingTanks != null) ? (float)(_temperatureController.PV * _unitSettings.DryingTanks[_moduleIndex].UnitTransfer) : 0f;
        }
        public float SV_Value
        {
            get => (_temperatureController != null && _unitSettings.DryingTanks != null) ? (float)(_temperatureController.SV * _unitSettings.DryingTanks[_moduleIndex].UnitTransfer) : 0f;
        }
        public void SetSV(int value)
        {
            if (_temperatureControllers != null)
            {
                _temperatureControllers.SetSV(ModuleIndex, value);
            }
        }
        public void SetSV(float value)
        {
            float setValue = 0f;

            setValue = value / ((_unitSettings.DryingTanks != null) ? _unitSettings.DryingTanks[_moduleIndex].UnitTransfer : 1f);

            SetSV((int)(setValue));
        }

        public bool Auto => _auto;
        public bool Pausing => _pausing;
        public bool Heating => _heating;
        public bool Cassette => _cassette;
        public bool Initialized => _initialized;
        public bool Idle => Sensor_CoverOpen && !_heating && !_cassette && _initialized && IsNormalStatus;

        public bool HighTemperature => (PV > PV_Check_High) || _sim_pv;
        public bool LowTemperature => PV < PV_Check_Low;
        public bool HeatingOP(bool heating)
        {
            bool result = false;


            if (_moduleSettings.DryingTanks != null)
            {
                SetSV(heating ? _moduleSettings.DryingTanks[_moduleIndex].SV_High : _moduleSettings.DryingTanks[_moduleIndex].SV_Low);
                _heating = heating;
                result = true;
            }

            return result;
        }
        public bool ManualHeatingOP(bool heating)
        {
            bool result = false;

            if (!_auto)
            {
                result = HeatingOP(heating);
                OperateLog.Log($"烘乾槽#{ModuleIndex + 1} 手動加熱 " + (heating ? "開" : "關"), $"烘乾槽#{ModuleIndex + 1} 手動加熱 " + (heating ? "開" : "關"));
            }

            return false;
        }
        public bool AirOP(bool air)
        {
            bool result = true;
            if (_plcService != null)
            {
                Command_HeaterAirOpen = air;
            }
            else
                result = false;

            return result;
        }
        public bool ManualAirOP(bool air)
        {
            bool result = false;

            if (_plcService != null)
            {
                result = AirOP(air);
                OperateLog.Log($"烘乾槽#{ModuleIndex + 1} 手動氣閥 " + (air ? "開" : "關"), $"烘乾槽#{ModuleIndex + 1} 手動氣閥 " + (air ? "開" : "關"));
            }

            return result;
        }
        public bool BlowerOP(bool blow)
        {
            bool result = true;
            if (_plcService != null)
            {
                Command_HeaterBlower = blow;
            }
            else
                result = false;

            return result;
        }
        public bool ManualBlowerOP(bool blow)
        {
            bool result = false;

            if (_plcService != null)
            {
                result = BlowerOP(blow);
                OperateLog.Log($"烘乾槽#{ModuleIndex + 1} 手動風扇 " + (blow ? "開" : "關"), $"烘乾槽#{ModuleIndex + 1} 手動風扇 " + (blow ? "開" : "關"));
            }

            return result;
        }
        public bool CoverClose(bool close)
        {
            bool result = true;
            if (_plcService != null)
            {
                Command_HeaterCoverClose = close;
            }
            else
                result = false;

            return result;
        }
        public bool ManualCoverClose(bool close)
        {
            bool result = false;

            if (_plcService != null)
            {
                result = CoverClose(close);
                OperateLog.Log($"烘乾槽#{ModuleIndex + 1} 手動蓋子 " + (close ? "關" : "開"), $"烘乾槽#{ModuleIndex + 1} 手動蓋子 " + (close ? "關" : "開"));
            }

            return result;
        }

        public bool HS_ClamperMoving { get; set; }
        public bool HS_ClamperPickFinished { get; set; }
        public bool HS_ClamperPlaceFinished { get; set; }
        public bool HS_InputPermit => Idle && !_pausing && !HS_ClamperMoving && _auto;
        public bool HS_ActFinished => _cassette && Sensor_CoverOpen && !HS_ClamperMoving && !Heating && _actFinished;

        public int ElpasedHeatingTime_Seconds => (int)(_elapsedTime != null ? _elapsedTime.Value.TotalSeconds : 0);
        public int RemainingHeatingTime_Seconds => (_moduleSettings.DryingTanks != null) ? _moduleSettings.DryingTanks[_moduleIndex].ActTime_Second - ElpasedHeatingTime_Seconds : 0;

        public bool ModulePass { get; set; }
        public bool HasWarning => _PV_Low_Timeout || _PV_High_Timeout || _Cover_Open_Timeout || _Cover_Close_Timeout;
        public bool HasAlarm => false; // 根據需求定義警報條件，可能包括超時或其他異常狀態
        public bool IsNormalStatus => !HasWarning && !HasAlarm;
        public void AutoStop()
        {
            if (_auto)
            {
                _autoStopFlag = true;
            }
        }
        public void WarningStop()
        { 
            _pausing = true;
        }
        public void AlarmStop()
        {
            EMOStop();
        }
        public void AutoStart()
        {
            _autoStopFlag = false;
            _pausing = false;
            if (!_auto && _initialized && IsNormalStatus)
            {
                _auto = true;
                ActStartStatus();
            }
        }
        public void AutoPause()
        {
            if (_auto && !_pausing)
            {
                _pausing = true;
            }
        }
        public void AlarmReset()
        {
            ResetTimeoutFlag();
        }
        public void ModuleReset()
        {
            Initialize();
        }

        public void SimHiTemperature(bool pv)
        {
            _sim_pv = pv;
        }

        public string Hint()
        {
            var sb = new StringBuilder();

            // Basic status
            sb.AppendLine($" - 烘乾槽#{ModuleIndex -1} 狀態概覽:");
            sb.AppendLine($" 已初始化: {_initialized}");
            sb.AppendLine($" 自動模式: {_auto}");
            sb.AppendLine($" 暫停中: {_pausing}");
            sb.AppendLine($" 加熱中: {_heating}");
            sb.AppendLine($" 是否有卡匣: {_cassette}");
            sb.AppendLine($" 蓋子開啟感測: {Sensor_CoverOpen}");
            sb.AppendLine($" 蓋子關閉感測: {Sensor_CoverClose}");
            sb.AppendLine($" PV: {PV} SV: {SV} (PV_Value={PV_Value:F1}, SV_Value={SV_Value:F1})");
            sb.AppendLine($" 馬達/其他狀態: Idle={Idle}, IsNormalStatus={IsNormalStatus}");

            // Alarms / warnings
            if (HasAlarm)
            {
                sb.AppendLine(" - 模組發生錯誤(Alarm)。請先排除錯誤後再繼續操作。可呼叫 AlarmReset() 嘗試清除逾時或馬達錯誤（視實作而定）。");
            }
            if (HasWarning)
            {
                sb.AppendLine(" - 模組發生警告(Warning)。請檢查溫度/蓋子感測或其他警告來源，必要時呼叫 WarningStop() 暫停自動。");
            }

            // Initialization guidance
            if (!_initialized)
            {
                sb.AppendLine(" - 尚未初始化：請呼叫 ModuleReset() 或 Initialize() 啟動模組初始化流程。初始化會重設狀態並準備開始自動運作。");
                return sb.ToString();
            }

            // Manual guidance when not in auto
            if (!_auto)
            {
                if (IsNormalStatus)
                    sb.AppendLine(" - 模組已初始化且狀態正常：可呼叫 AutoStart() 開始自動流程。");
                else
                    sb.AppendLine(" - 模組狀態不完全正常，請先解除警告/錯誤後再啟動自動。");

                sb.AppendLine(" - 手動操作建議：");
                sb.AppendLine(" * 可呼叫 HeatingOP(true/false)進行手動加熱開/關。");
                sb.AppendLine(" * 可呼叫 ManualBlowerOP(true/false) 控制風扇；ManualAirOP(true/false) 控制氣閥。 ");
                sb.AppendLine(" * 可呼叫 CoverClose(true/false) 控制蓋子。 ");
                sb.AppendLine(" * 可呼叫 SetSV(value) 設定目標溫度(SV)。");

                return sb.ToString();
            }

            // Auto mode guidance
            sb.AppendLine(" - 模組處於自動模式：");
            if (_autoStopFlag)
                sb.AppendLine(" * 自動流程已被要求停止，系統會在空閒時停止，請觀察 Idle 狀態。");
            if (_pausing)
                sb.AppendLine(" * 自動流程暫停中：呼叫 AutoStart() 可恢復，或 AutoStop(force=true) 強制停止以結束自動。");

            if (!_actFinished)
            {
                sb.AppendLine(" * 未完成烘乾階段（正在等待放置/加熱）：");

                if (!_cassette)
                {
                    if (Command_HeaterCoverClose)
                        sb.AppendLine(" - 蓋子目前要求關閉：若需放入卡匣可呼叫 CoverClose(false) 打開蓋子並放入卡匣。 ");
                    if (Sensor_CoverOpen && !Idle)
                        sb.AppendLine(" - 蓋子已開但模組不在完全空閒狀態，確認馬達/機構狀態後再放入卡匣。 ");
                }
                else // 已有卡匣
                {
                    if (!Command_HeaterCoverClose)
                        sb.AppendLine(" - 已放入卡匣且需要關蓋：系統會嘗試關蓋並啟動加熱。若長時間未關蓋請檢查蓋子感測或機構。 ");

                    if (Sensor_CoverClose && !_heating && !_pausing)
                        sb.AppendLine(" - 卡匣蓋關閉且狀態允許：系統將啟動加熱與風扇。 ");

                    if (_heating)
                    {
                        sb.AppendLine($" - 正在加熱中，已加熱時間: {ElpasedHeatingTime_Seconds} 秒，剩餘約: {RemainingHeatingTime_Seconds} 秒。 ");
                        if (LowTemperature)
                            sb.AppendLine(" - 檢測到溫度偏低，系統會嘗試加熱至設定值。若持續偏低請檢查加熱元件或設定值(SV)。");
                        if (HighTemperature)
                            sb.AppendLine(" - 檢測到溫度偏高，系統可能關閉加熱以回復安全狀態。請檢查 PV/SV 與相關逾時警告。 ");
                    }
                }
            }
            else // act finished
            {
                sb.AppendLine(" * 已完成烘乾階段：");
                if (_cassette && Command_HeaterCoverClose && !_heating)
                    sb.AppendLine(" - 系統會打開蓋子以便取出卡匣；若需立即取卡可暫停自動或呼叫 AutoStop()。 ");

                if (HS_ClamperPickFinished)
                    sb.AppendLine(" -夾爪已取卡完成，系統會重置狀態準備下一循環。 ");
            }

            sb.AppendLine(" - 若發生警告/錯誤，請先處理後再繼續自動流程。 ");

            return sb.ToString();
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
            RefreshTimeoutValue();
            CheckTimeout();
            // Ensure AlarmManager polls registered flag getters so changes (like _Cover_Open_Timeout)
            // are detected and logged.
            //AlarmManager.CheckFlagGetters();

            AutoProcedure();

            await Task.Yield();
        }

        #endregion

        #region Auto Procedure

        private bool _actFinished = false;

        // Heating duration tracking
        private static int _heatingDuration_Minutes =10; // default minutes, change later as needed
        private readonly TimeSpan _heatingDurationThreshold = TimeSpan.FromMinutes(_heatingDuration_Minutes);
        private DateTime? _heatingStartTime = null;

        private TimeSpan? _elapsedTime = new TimeSpan();

        

        private void Initialize()
        {
            ActStartStatus();

            _auto = false;
            _cassette = false;
            _pausing = false;
            _autoStopFlag = false;

            ResetTimeoutFlag();
            _initialized = true;

            HS_ClamperMoving = false;
            _elapsedTime = new TimeSpan();
        }

        private void ActStartStatus()
        {
            HeatingOP(false);
            AirOP(true);
            BlowerOP(false);
            CoverClose(false);
        }

        private void EMOStop()
        {
            HeatingOP(false);
            AirOP(false);
            BlowerOP(false);

            _auto = false;
            _initialized = false;

            HS_ClamperMoving = false;
            _elapsedTime = new TimeSpan();
        }

        private void AutoProcedure()
        {
            if (_PV_High_Timeout && Heating) HeatingOP(false);

            if (_auto)
            {
                if (Idle && _autoStopFlag)
                {
                    _autoStopFlag = false;
                    _auto = false;
                }


                // 未烘乾完成前程序
                if (!_actFinished)
                {
                    // 蓋子打開等待卡匣放入
                    if (!_cassette && Command_HeaterCoverClose)
                    {
                        CoverClose(false);
                    }

                    //卡匣放入後蓋子關閉
                    if (_cassette && !Command_HeaterCoverClose)
                    {
                        CoverClose(true);
                    }

                    // 有卡匣且蓋子關閉後開始加熱
                    if (_cassette && Sensor_CoverClose)
                    {
                        if (!Heating && !_pausing) // 開始加熱
                        {
                            HeatingOP(true);
                            BlowerOP(true);
                            AirOP(true);
                        }

                        if (Heating) // 加熱過程
                        {
                            if (_pausing) // 加熱過程中暫停
                            {
                                HeatingOP(false);
                            }

                        }
                    }

                    // 加熱時間計算
                    if (Heating && !_pausing && Sensor_CoverClose)
                    { 
                        // start timer when condition becomes true
                        if (_heatingStartTime == null)
                        {
                            _heatingStartTime = DateTime.UtcNow;
                        }
                        else
                        {
                            _elapsedTime += DateTime.UtcNow - _heatingStartTime.Value;
                            _heatingStartTime = DateTime.UtcNow;
                            if (_elapsedTime >= TimeSpan.FromSeconds((double)(_moduleSettings.DryingTanks != null ? _moduleSettings.DryingTanks[_moduleIndex].ActTime_Second : 60.0)))
                            {
                                HeatingOP(false);
                                _actFinished = true;
                            }
                        }
                    }
                    else
                    {
                        // reset timer when condition no longer holds
                        _heatingStartTime = null;
                    }


                    // Clamper放置完成後確認並設定有卡匣
                    if (HS_ClamperPlaceFinished)
                    {
                        HS_ClamperPlaceFinished = false;
                        _cassette = true;
                    }
                }
                else // 烘乾完成程序 
                {
                    // 蓋子打開等待卡匣取出
                    if (_cassette && Command_HeaterCoverClose && !_heating)
                    {
                        CoverClose(false);
                    }

                    // 卡匣取出後流程結束
                    if (HS_ClamperPickFinished)
                    {
                        HS_ClamperPickFinished = false;
                        _cassette = false;
                        _actFinished = false;
                        BlowerOP(false);
                        _elapsedTime = new TimeSpan();
                    }
                }
            }
        }

        #endregion

        #region Timeout

        private bool _PV_Low_Timeout = false;
        private bool _PV_High_Timeout = false;
        private bool _Cover_Open_Timeout = false;
        private bool _Cover_Close_Timeout = false;

        private void RefreshTimeoutValue()
        {
            if (_unitSettings.DryingTanks != null)
            {
                _pvLowTimeoutThreshold_Value = _unitSettings.DryingTanks[_moduleIndex].PV_Low_Timeout_Second;
                _pvHighTimeoutThreshold_Value = _unitSettings.DryingTanks[_moduleIndex].PV_High_Timeout_Second;
                _coverOpenTimeoutThreshold_Value = _unitSettings.DryingTanks[_moduleIndex].Cover_Close_Timeout_Second;
                _coverCloseTimeoutThreshold_Value = _unitSettings.DryingTanks[_moduleIndex].Cover_Open_Timeout_Second;

                _pvLowTimeoutThreshold = TimeSpan.FromSeconds(_pvLowTimeoutThreshold_Value);
                _pvHighTimeoutThreshold = TimeSpan.FromSeconds(_pvHighTimeoutThreshold_Value);
                _coverOpenTimeoutThreshold = TimeSpan.FromSeconds(_coverOpenTimeoutThreshold_Value);
                _coverCloseTimeoutThreshold = TimeSpan.FromSeconds(_coverCloseTimeoutThreshold_Value);
            }
        }

        // Timer thresholds (can be adjusted). If module settings provide timeout values they can be used here instead.
        private TimeSpan _pvLowTimeoutThreshold = TimeSpan.FromSeconds(_pvLowTimeoutThreshold_Value);
        private TimeSpan _pvHighTimeoutThreshold = TimeSpan.FromSeconds(_pvHighTimeoutThreshold_Value);
        private TimeSpan _coverOpenTimeoutThreshold = TimeSpan.FromSeconds(_coverOpenTimeoutThreshold_Value);
        private TimeSpan _coverCloseTimeoutThreshold = TimeSpan.FromSeconds(_coverCloseTimeoutThreshold_Value);

        // Start times for each monitored condition (null when condition not active)
        private DateTime? _pvLowStart;
        private DateTime? _pvHighStart;
        private DateTime? _coverOpenStart;
        private DateTime? _coverCloseStart;

        private void ResetTimeoutFlag()
        { 
            _PV_Low_Timeout = false;
            _PV_High_Timeout = false;
            _Cover_Open_Timeout = false;
            _Cover_Close_Timeout = false;

            _pvLowStart = null;
            _pvHighStart = null;
            _coverOpenStart = null;
            _coverCloseStart = null;
        }

        private void CheckTimeout()
        { 
            // Conditions to monitor (per request):
            // PV low timeout condition active when: !Heating && !LowTemperature
            bool pvLowCondition = !Heating && !LowTemperature && false; // 因為低溫目的是在將加熱關掉，在本專案溫度控制器並無法降溫，所以暫時不啟用此條件
            // PV high timeout condition active when: Heating && !HighTemperature
            bool pvHighCondition = Heating && !HighTemperature;
            // Cover open timeout when: !Command_HeaterCoverClose && !Sensor_CoverOpen
            bool coverOpenCondition = !Command_HeaterCoverClose && !Sensor_CoverOpen;
            // Cover close timeout when: Command_HeaterCoverClose && !Sensor_CoverClose
            bool coverCloseCondition = Command_HeaterCoverClose && !Sensor_CoverClose;

            var now = DateTime.UtcNow;

            // PV Low
            if (pvLowCondition)
            {
                if (_pvLowStart == null)
                    _pvLowStart = now;
                else if (!_PV_Low_Timeout && now - _pvLowStart >= _pvLowTimeoutThreshold)
                    _PV_Low_Timeout = true;
            }
            else
            {
                _pvLowStart = null;
                _PV_Low_Timeout = false;
            }

            // PV High
            if (pvHighCondition)
            {
                if (_pvHighStart == null)
                    _pvHighStart = now;
                else if (!_PV_High_Timeout && now - _pvHighStart >= _pvHighTimeoutThreshold)
                    _PV_High_Timeout = true;
            }
            else
            {
                _pvHighStart = null;
                _PV_High_Timeout = false;
            }

            // Cover Open
            if (coverOpenCondition)
            {
                if (_coverOpenStart == null)
                    _coverOpenStart = now;
                else if (!_Cover_Open_Timeout && now - _coverOpenStart >= _coverOpenTimeoutThreshold)
                    _Cover_Open_Timeout = true;
            }
            else
            {
                _coverOpenStart = null;
                _Cover_Open_Timeout = false;
            }

            // Cover Close
            if (coverCloseCondition)
            {
                if (_coverCloseStart == null)
                    _coverCloseStart = now;
                else if (!_Cover_Close_Timeout && now - _coverCloseStart >= _coverCloseTimeoutThreshold)
                    _Cover_Close_Timeout = true;
            }
            else
            {
                _coverCloseStart = null;
                _Cover_Close_Timeout = false;
            }
        }

        #endregion
    }
}
