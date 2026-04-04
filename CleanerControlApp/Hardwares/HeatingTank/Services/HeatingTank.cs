using CleanerControlApp.Hardwares.HeatingTank.Interfaces;
using CleanerControlApp.Modules.DeltaMS300.Interfaces;
using CleanerControlApp.Modules.DeltaMS300.Services;
using CleanerControlApp.Modules.MitsubishiPLC.Interfaces;
using CleanerControlApp.Modules.MitsubishiPLC.Services;
using CleanerControlApp.Modules.Modbus.Interfaces;
using CleanerControlApp.Modules.TempatureController.Interfaces;
using CleanerControlApp.Modules.TempatureController.Services;
using CleanerControlApp.Utilities;
using CleanerControlApp.Utilities.Alarm;
using CleanerControlApp.Utilities.Log;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Hardwares.HeatingTank.Services
{
    public class HeatingTank : IHeatingTank, IDisposable
    {
        #region Constant

        public static readonly int TC_Index = 0; // TC-1
        public static readonly int MS300_Index = 1; // MS300-2

        #endregion

        #region attribute

        private readonly ILogger<HeatingTank>? _logger;

        // background loop
        private CancellationTokenSource? _cts;
        private Task? _loopTask;
        private readonly TimeSpan _loopInterval = TimeSpan.FromMilliseconds(10);

        //private IModbusRTUService? _modbusService;

        private bool _running;

        private readonly UnitSettings _unitSettings;
        private readonly ModuleSettings _moduleSettings;

        private readonly IPLCOperator? _plcService;
        private readonly IDeltaMS300? _deltaMS300;

        private readonly ISingleTemperatureController? _temperatureController;
        private readonly ITemperatureControllers? _temperatureControllers;

        private bool _auto = false;
        private bool _pausing = false;
        private bool _heating = false;
        private bool _initialized = false;
        private bool _autoStopFlag = false;

        private int PV_Check_High => ((_moduleSettings.HeatingTank != null ? _moduleSettings.HeatingTank.SV_High : 0)) -
            (_unitSettings.HeatingTank != null ? _unitSettings.HeatingTank.SV_CheckOffet : 0);
        private int PV_Check_Low => ((_moduleSettings.HeatingTank != null ? _moduleSettings.HeatingTank.SV_Low : 0)) +
            (_unitSettings.HeatingTank != null ? _unitSettings.HeatingTank.SV_CheckOffet : 0);
        private float INV_Check_High => (_moduleSettings.HeatingTank != null ? _moduleSettings.HeatingTank.INV_High : 0f) -
            (_unitSettings.HeatingTank != null ? _unitSettings.HeatingTank.INV_CheckOffset : 0f);
        private float INV_Check_Low_L => (_moduleSettings.HeatingTank != null ? _moduleSettings.HeatingTank.INV_Low : 0f) -
            (_unitSettings.HeatingTank != null ? _unitSettings.HeatingTank.INV_CheckOffset : 0f);
        private float INV_Check_Low_H => (_moduleSettings.HeatingTank != null ? _moduleSettings.HeatingTank.INV_Low : 0f) +
            (_unitSettings.HeatingTank != null ? _unitSettings.HeatingTank.INV_CheckOffset : 0f);
        private float INV_Check_Zero => (_moduleSettings.HeatingTank != null ? _moduleSettings.HeatingTank.INV_Zero : 0f) +
            (_unitSettings.HeatingTank != null ? _unitSettings.HeatingTank.INV_CheckOffset : 0f);


        private static int _pvLowTimeoutThreshold_Value = 30;
        private static int _pvHighTimeoutThreshold_Value = 30;
        private static int _invHighTimeoutThreshold_Value = 30;
        private static int _invLowTimeoutThreshold_Value = 30;
        private static int _invZeroTimeoutThreshold_Value = 30;

        private bool _sim_pv = false;
        private bool _sim_inv_high = false;
        private bool _sim_inv_low = false;

        private int _inv_op_index = 0; // 0: Zero; 1: Low; 2: High

        #endregion

        #region constructor

        public HeatingTank(ILogger<HeatingTank>? logger, IPLCOperator? plcService, ITemperatureControllers? temperatureControllers, UnitSettings unitSettings, ModuleSettings moduleSettings, IDeltaMS300 deltaMS300)
        {
            _unitSettings = unitSettings;
            _moduleSettings = moduleSettings;

            _logger = logger;

            _plcService = plcService;

            _deltaMS300 = deltaMS300;

            _temperatureController = temperatureControllers?[TC_Index];
            _temperatureControllers = temperatureControllers;

            RefreshTimeoutValue();

            // Alarm
            AlarmManager.AttachFlagGetter("ALM601", () => _PV_Low_Timeout);
            AlarmManager.AttachFlagGetter("ALM602", () => _PV_High_Timeout);
            AlarmManager.AttachFlagGetter("ALM603", () => _INV_High_Timeout);
            AlarmManager.AttachFlagGetter("ALM604", () => _INV_Low_Timeout);
            AlarmManager.AttachFlagGetter("ALM605", () => _INV_Zero_Timeout);
            AlarmManager.AttachFlagGetter("ALM606", () => _invErrorAlarm);
            AlarmManager.AttachFlagGetter("ALM607", () => _invWarningAlarm);
            AlarmManager.AttachFlagGetter("ALM608", () => _tankLLAlarm);
            AlarmManager.AttachFlagGetter("ALM609", () => _tankHHAlarm);

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
        ~HeatingTank()
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

        #region IHeatingTank implementation


        public bool IsRunning => _running;
        public void Start() { _running = true; }
        public void Stop() { _running = false; }

        public bool Sensor_Liquid_HH => _plcService != null && _plcService.HotWaterPosHH;
        public bool Sensor_Liquid_H => _plcService != null && _plcService.HotWaterPosH;
        public bool Sensor_Liquid_L => _plcService != null && _plcService.HotWaterPosL;
        public bool Sensor_Liquid_LL => _plcService != null && _plcService.HotWaterPosLL;

        public bool Command_WaterIn
        {
            get => _plcService != null && _plcService.Command_InputWaterValveOpen;
            set
            {
                if (_plcService != null)
                {
                    _plcService.Command_InputWaterValveOpen = value;
                }
            }
        }
        public bool Command_WaterOut
        {
            get => _plcService != null && _plcService.Command_HeaterTankSwitchValveOpen;
            set
            {
                if (_plcService != null)
                {
                    _plcService.Command_HeaterTankSwitchValveOpen = value;
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
            get => (_temperatureController != null && _unitSettings.HeatingTank != null) ? (float)(_temperatureController.PV * _unitSettings.HeatingTank.UnitTransfer) : 0f;
        }
        public float SV_Value
        {
            get => (_temperatureController != null && _unitSettings.HeatingTank != null) ? (float)(_temperatureController.SV * _unitSettings.HeatingTank.UnitTransfer) : 0f;
        }
        public void SetSV(int value)
        {
            if (_temperatureControllers != null)
            {
                _temperatureControllers.SetSV(TC_Index, value);
            }
        }
        public void SetSV(float value)
        {
            float setValue = 0f;

            setValue = value / ((_unitSettings.DryingTanks != null) ? _unitSettings.DryingTanks[TC_Index].UnitTransfer : 1f);

            SetSV((int)(setValue));
        }

        public bool Auto => _auto;
        public bool Pausing => _pausing;
        public bool Heating => _heating;
        public bool Initialized => _initialized;
        public bool Idle => _initialized && IsNormalStatus;

        public bool HighTemperature => (PV > PV_Check_High) || _sim_pv;
        public bool LowTemperature => PV < PV_Check_Low;
        public bool HeatingOP(bool heating)
        {
            bool result = false;


            if (_moduleSettings.HeatingTank != null)
            {
                SetSV(heating ? _moduleSettings.HeatingTank.SV_High : _moduleSettings.HeatingTank.SV_Low);
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
                OperateLog.Log($"加熱槽 手動加熱 " + (heating ? "開" : "關"), $"加熱槽 手動加熱 " + (heating ? "開" : "關"));
            }

            return false;
        }
        public bool WaterInOP(bool water)
        {
            bool result = false;

            if (_plcService != null && !_tankHHAlarm && !_private_waste_HAlarm)
            {
                Command_WaterIn = water;
                result = true;
            }

            return result;
        }
        public bool ManualWaterInOP(bool water)
        {
            bool result = false;

            if (!_auto)
            {
                result = WaterInOP(water);
                OperateLog.Log($"加熱槽 手動加水 " + (water ? "開" : "關"), $"加熱槽 手動加水 " + (water ? "開" : "關"));
            }

            return result;
        }
        public bool WaterOutOP(bool water)
        {
            bool result = false;

            if (_plcService != null && !_tankHHAlarm && (!_tankLLAlarm || !_auto) && !_private_waste_HAlarm)
            {
                Command_WaterOut = water;
                result = true;
            }

            return result;
        }
        public bool ManualWaterOutOP(bool water)
        {
            bool result = false;

            if (!_auto)
            {
                result = WaterOutOP(water);
                OperateLog.Log($"加熱槽 手動出水 " + (water ? "開" : "關"), $"加熱槽 手動出水 " + (water ? "開" : "關"));
            }

            return result;
        }

        public bool HS_RequestWater { get; set; }

        public bool ModulePass { get; set; }
        public bool HasWarning => _PV_Low_Timeout || _PV_High_Timeout || _INV_Zero_Timeout || _INV_High_Timeout || _INV_Low_Timeout || _invErrorAlarm;
        public bool HasAlarm => _tankHHAlarm || _tankLLAlarm;
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
        public void SimTemperature()
        {
            _sim_pv = !_sim_pv;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="freq">1: Simulated Low Frequency; 2: Simulated High Frequency; other: nothing</param>
        public void SimFrequency(int freq)
        {
            if (freq == 1) _sim_inv_low = !_sim_inv_low;
            else if (freq == 2) _sim_inv_high = !_sim_inv_high;
        }



        public int InvErrorCode => _deltaMS300 != null ? _deltaMS300.ErrorCode : 0;
        public int InvWarningCode => _deltaMS300 != null ? _deltaMS300.WarningCode : 0;
        public float InvCommandFrequency => _deltaMS300 != null ? _deltaMS300.Frquency_Command : 0f;
        public float InvActualFrequency => _deltaMS300 != null ? _deltaMS300.Frquency_Output : 0f;

        public bool IsHighFrequency => (_deltaMS300 != null && _deltaMS300.Frquency_Output > INV_Check_High) || _sim_inv_high;
        public bool IsLowFrequency => (_deltaMS300 != null && _deltaMS300.Frquency_Output < INV_Check_Low_H && _deltaMS300.Frquency_Output > INV_Check_Low_L) || _sim_inv_low;
        public bool IsZeroFrequency => _deltaMS300 != null && _deltaMS300.Frquency_Output < INV_Check_Zero && !_sim_inv_low && !_sim_inv_high;
        public bool DoHighFrequency => _inv_op_index == 2;
        public bool DoLowFrequency => _inv_op_index == 1;
        public bool DoZeroFrequency => _inv_op_index == 0;
        public bool IsFrequencyRunning => _deltaMS300 != null && _deltaMS300.Frquency_Output > INV_Check_Zero;
        public bool HighFrequencyOP()
        {
            bool result = false;
            if (_deltaMS300 != null && !_tankLLAlarm && !_tankLLAlarm && !_private_waste_HAlarm)
            {
                _deltaMS300.SetFrequency(_moduleSettings.HeatingTank != null ? _moduleSettings.HeatingTank.INV_High : 0f);
                _inv_op_index = 2;
                result = true;
            }
            return result;
        }
        public bool LowFrequencyOP()
        {
            bool result = false;
            if (_deltaMS300 != null && !_tankLLAlarm && !_tankLLAlarm && !_private_waste_HAlarm)
            {
                _deltaMS300.SetFrequency(_moduleSettings.HeatingTank != null ? _moduleSettings.HeatingTank.INV_Low : 0f);
                _inv_op_index = 1;
                result = true;
            }
            return result;
        }
        public bool ZeroFrequencyOP()
        {
            bool result = false;
            if (_deltaMS300 != null)
            {
                _deltaMS300.SetFrequency(_moduleSettings.HeatingTank != null ? _moduleSettings.HeatingTank.INV_Zero : 0f);
                _inv_op_index = 0;
                result = true;
            }
            return result;
        }

        /// <summary>
        /// Sets the output frequency manually based on the specified frequency code.
        /// </summary>
        /// <remarks>This method has no effect if automatic mode is enabled.</remarks>
        /// <param name="freq">The frequency code to set. Specify <c>1</c> for low frequency, <c>2</c> for high frequency, or any other
        /// value for zero frequency.</param>
        /// <returns><see langword="true"/> if the operation succeeds; otherwise, <see langword="false"/>.</returns>
        public bool ManualFrequencyOP(int freq)
        {
            bool result = false;

            if (!_auto)
            {
                if (freq == 1) result = LowFrequencyOP();
                else if (freq == 2) result = HighFrequencyOP();
                else result = ZeroFrequencyOP();

                OperateLog.Log($"加熱槽 手動調整頻率 " + (freq == 1 ? "低" : (freq == 2) ? "高" : "關"), $"加熱槽 手動調整頻率 " + (freq == 1 ? "低" : (freq == 2) ? "高" : "關"));
            }

            return result;
        }

        public string Hint()
        {
            var sb = new StringBuilder();

            // Basic status
            sb.AppendLine(" - 加熱槽狀態概覽:");
            sb.AppendLine($" 已初始化: {_initialized}");
            sb.AppendLine($" 自動模式: {_auto}");
            sb.AppendLine($" 暫停中: {_pausing}");
            sb.AppendLine($" 加熱中: {_heating}");
            sb.AppendLine($" 水位: HH={Sensor_Liquid_HH}, H={Sensor_Liquid_H}, L={Sensor_Liquid_L}, LL={Sensor_Liquid_LL}");
            sb.AppendLine($" PV: {PV} SV: {SV} (PV_Value={PV_Value:F1}, SV_Value={SV_Value:F1})");
            sb.AppendLine($" INV 頻率: Command={InvCommandFrequency:F1}Hz, Actual={InvActualFrequency:F1}Hz, 狀態 High={IsHighFrequency}, Low={IsLowFrequency}, Zero={IsZeroFrequency}");

            // Alarms / warnings
            if (HasAlarm)
            {
                sb.AppendLine(" - 模組發生嚴重錯誤(Alarm)。請先排除水位或其他硬體錯誤，必要時停止自動與手動處理。可呼叫 AlarmReset() 嘗試清除逾時旗標。");
            }
            if (HasWarning)
            {
                sb.AppendLine(" - 模組發生警告(Warning)。請檢查 PV/SV 與變頻器狀態，或檢視 INV 錯誤代碼並處理。必要時呼叫 WarningStop() 暫停自動。");
                sb.AppendLine($" INV 錯誤碼: {InvErrorCode}, 警告碼: {InvWarningCode}");
            }

            // Initialization guidance
            if (!_initialized)
            {
                sb.AppendLine(" - 尚未初始化：請呼叫 ModuleReset() 或 Initialize()以初始化模組（會設定頻率、設定水系統初始狀態等）。");
                return sb.ToString();
            }

            // Manual guidance when not in auto
            if (!_auto)
            {
                if (IsNormalStatus)
                    sb.AppendLine(" - 模組已初始化且狀態正常：可呼叫 AutoStart() 開始自動流程。");
                else
                    sb.AppendLine(" - 模組狀態不完全正常，請先解除警告/錯誤後再啟動自動。");

                sb.AppendLine(" - 手動操作建議:");
                sb.AppendLine(" * 呼叫 HeatingOP(true/false) 手動開關加熱。 ");
                sb.AppendLine(" * 呼叫 ManualWaterInOP(true/false) 或 ManualWaterOutOP(true/false) 控制進/出水。 ");
                sb.AppendLine(" * 呼叫 ManualFrequencyOP(1/2/other) 設定變頻器頻率 (1=低,2=高,其他=關/0頻率)。 ");
                sb.AppendLine(" * 可呼叫 SetSV(value) 調整目標溫度。 ");

                return sb.ToString();
            }

            // Auto mode guidance
            sb.AppendLine(" - 模組處於自動模式：");
            if (_autoStopFlag)
                sb.AppendLine(" * 自動流程已被要求停止，系統會在空閒時停止，請觀察 Idle 狀態確認是否已停止。 ");
            if (_pausing)
                sb.AppendLine(" * 自動流程暫停中：呼叫 AutoStart() 恢復，或 AutoStop(force=true) 強制停止。 ");

            // Water system and INV guidance derived from AutoProcedure
            if (_private_waste_HAlarm || _tankHHAlarm || _tankLLAlarm)
            {
                sb.AppendLine(" * 偵測到水系統或廢水高位錯誤：系統已停止或會嘗試停止加熱與變頻器輸出。請排除水位或廢水高位問題後再繼續。 ");
            }

            // Water controls
            if (!Command_WaterIn && !Sensor_Liquid_L && !_private_waste_HAlarm)
                sb.AppendLine(" * 水位過低 (L=false)：系統會啟動注水 (WaterInOP)。如需手動操作請呼叫 ManualWaterInOP(true)。");
            if (Command_WaterIn && Sensor_Liquid_H)
                sb.AppendLine(" * 水位已達 H：系統會停止注水 (WaterInOP=false)。");

            if (Command_WaterOut && !IsHighFrequency)
                sb.AppendLine(" * 若開啟出水且變頻器未切到高頻，系統會設定為高頻 (HighFrequencyOP)。");
            if (!Command_WaterOut && IsHighFrequency)
                sb.AppendLine(" * 未要求出水但變頻器處於高頻：系統會嘗試切回低頻 (LowFrequencyOP)。");

            // Heating guidance
            if (!_heating && Sensor_Liquid_L)
                sb.AppendLine(" * 水位充足且未加熱：系統會啟動加熱 (HeatingOP(true))。如需手動操作可呼叫 ManualHeatingOP(true)。");
            if (Heating)
            {
                sb.AppendLine(" * 正在加熱中，若出現 PV 高/低逾時或異常，系統會自動關閉加熱。請檢查 PV/SV 與溫度控制器。 ");
                if (LowTemperature)
                    sb.AppendLine(" - 檢測到溫度偏低，系統會維持或啟動加熱以追趕目標。若長時間偏低請檢查加熱元件或 SV 值。");
                if (HighTemperature)
                    sb.AppendLine(" - 檢測到溫度偏高，系統可能會暫停加熱以回復安全狀態。請檢查溫度與設定值。 ");
            }

            // INV guidance
            if (DoHighFrequency && !IsHighFrequency)
                sb.AppendLine(" * 系統設定為 High 頻率但實際尚未到達：請檢查變頻器通訊或狀態 (InvError/Warning)。");
            if (DoLowFrequency && !IsLowFrequency)
                sb.AppendLine(" * 系統設定為 Low 頻率但實際尚未到達：請檢查變頻器通訊或狀態。 ");
            if (DoZeroFrequency && !IsZeroFrequency)
                sb.AppendLine(" * 系統設定為 Zero 頻率但實際頻率不為零：請檢查變頻器輸出或通訊。 ");

            sb.AppendLine(" - 自動模式下若發生警告/錯誤，系統會暫停/停止相應動作。請先處理警報再繼續自動流程。 ");

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

            AutoProcedure();

            await Task.Yield();
        }

        #endregion

        #region Auto Procedure

        private void Initialize()
        {
            ActStartStatus();

            _auto = false;
            _pausing = false;
            _autoStopFlag = false;

            ResetTimeoutFlag();
            _initialized = true;

        }

        private void ActStartStatus()
        {
            HeatingOP(true);
            WaterInOP(false);
            WaterOutOP(false);
            LowFrequencyOP();
        }

        private void EMOStop()
        {
            HeatingOP(false);
            WaterInOP(false);
            WaterOutOP(false);

            _auto = false;
            _initialized = false;

            ZeroFrequencyOP();

        }

        private void AutoProcedure()
        {
            // Water System Error handling: if Waste Water High Alarm is on, stop water in and out regardless of other conditions; if Liquid HH alarm is on, stop water in and allow water out; if Liquid LL alarm is on, stop water out and allow water in
            if (_private_waste_HAlarm || _tankHHAlarm || _tankLLAlarm)
            {
                if(!IsZeroFrequency) ZeroFrequencyOP();
                if(Heating) HeatingOP(false);
                if (_private_waste_HAlarm || _tankHHAlarm)
                {
                    WaterInOP(false);
                }
                else if (_tankLLAlarm && _auto)
                {
                    WaterOutOP(false);
                }
            }

            if (!_private_waste_HAlarm && !_tankHHAlarm && _initialized)
            {
                if (!Sensor_Liquid_L && !Command_WaterIn) WaterInOP(true);

                if (Sensor_Liquid_H && Command_WaterIn) WaterInOP(false);

                if (Command_WaterOut && !IsHighFrequency) HighFrequencyOP();

                if (!Command_WaterOut && IsHighFrequency) LowFrequencyOP();

                if(!Heating && Sensor_Liquid_L) HeatingOP(true);

                if (HS_RequestWater && !Command_WaterOut) WaterOutOP(true);
                else if (!HS_RequestWater && Command_WaterOut) WaterOutOP(false);
            }

            if (_PV_High_Timeout && Heating) HeatingOP(false);

            if (_auto)
            {
                if (Idle && _autoStopFlag)
                {
                    _autoStopFlag = false;
                    _auto = false;
                }

               
            }
        }

        #endregion

        #region Timeout

        private bool _PV_Low_Timeout = false;
        private bool _PV_High_Timeout = false;
        private bool _INV_High_Timeout = false;
        private bool _INV_Low_Timeout = false;
        private bool _INV_Zero_Timeout = false;

        private void RefreshTimeoutValue()
        {
            if (_unitSettings.HeatingTank != null)
            {
                _pvLowTimeoutThreshold_Value = _unitSettings.HeatingTank.PV_Low_Timeout_Second;
                _pvHighTimeoutThreshold_Value = _unitSettings.HeatingTank.PV_High_Timeout_Second;
                _invHighTimeoutThreshold_Value = _unitSettings.HeatingTank.INV_High_Timeout_Second;
                _invLowTimeoutThreshold_Value = _unitSettings.HeatingTank.INV_Low_Timeout_Second;
                _invZeroTimeoutThreshold_Value = _unitSettings.HeatingTank.INV_Zero_Timeout_Second;

                _pvLowTimeoutThreshold = TimeSpan.FromSeconds(_pvLowTimeoutThreshold_Value);
                _pvHighTimeoutThreshold = TimeSpan.FromSeconds(_pvHighTimeoutThreshold_Value);
                _invHighTimeoutThreshold = TimeSpan.FromSeconds(_invHighTimeoutThreshold_Value);
                _invLowTimeoutThreshold = TimeSpan.FromSeconds(_invLowTimeoutThreshold_Value);
                _invZeroTimeoutThreshold = TimeSpan.FromSeconds(_invZeroTimeoutThreshold_Value);
            }
        }

        // Timer thresholds (can be adjusted). If module settings provide timeout values they can be used here instead.
        private TimeSpan _pvLowTimeoutThreshold = TimeSpan.FromSeconds(_pvLowTimeoutThreshold_Value);
        private TimeSpan _pvHighTimeoutThreshold = TimeSpan.FromSeconds(_pvHighTimeoutThreshold_Value);
        private TimeSpan _invHighTimeoutThreshold = TimeSpan.FromSeconds(_invHighTimeoutThreshold_Value);
        private TimeSpan _invLowTimeoutThreshold = TimeSpan.FromSeconds(_invLowTimeoutThreshold_Value);
        private TimeSpan _invZeroTimeoutThreshold = TimeSpan.FromSeconds(_invZeroTimeoutThreshold_Value);

        // Start times for each monitored condition (null when condition not active)
        private DateTime? _pvLowStart;
        private DateTime? _pvHighStart;
        private DateTime? _invHighStart;
        private DateTime? _invLowStart;
        private DateTime? _invZeroStart;

        private void ResetTimeoutFlag()
        {
            _PV_Low_Timeout = false;
            _PV_High_Timeout = false;
            _INV_High_Timeout = false;
            _INV_High_Timeout = false;
            _INV_Zero_Timeout = false;

            _pvLowStart = null;
            _pvHighStart = null;
            _invHighStart = null;
            _invLowStart = null;
            _invZeroStart = null;
        }

        private void CheckTimeout()
        {
            // Conditions to monitor (per request):
            // PV low timeout condition active when: !Heating && !LowTemperature
            bool pvLowCondition = !Heating && !LowTemperature && false; // 因為低溫目的是在將加熱關掉，在本專案溫度控制器並無法降溫，所以暫時不啟用此條件
            // PV high timeout condition active when: Heating && !HighTemperature
            bool pvHighCondition = Heating && !HighTemperature;
            // Cover open timeout when: !Command_HeaterCoverClose && !Sensor_CoverOpen
            bool invHighCondition = DoHighFrequency && !IsHighFrequency;
            // Cover close timeout when: Command_HeaterCoverClose && !Sensor_CoverClose
            bool invLowCondition = DoLowFrequency && !IsLowFrequency;
            bool invZeroCondition = DoZeroFrequency && !IsZeroFrequency;

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

            // INV High
            if (invHighCondition)
            {
                if (_invHighStart == null)
                    _invHighStart = now;
                else if (!_INV_High_Timeout && now - _invHighStart >= _invHighTimeoutThreshold)
                    _INV_High_Timeout = true;
            }
            else
            {
                _invHighStart = null;
                _INV_High_Timeout = false;
            }

            // INV Low
            if (invLowCondition)
            {
                if (_invLowStart == null)
                    _invLowStart = now;
                else if (!_INV_Low_Timeout && now - _invLowStart >= _invLowTimeoutThreshold)
                    _INV_Low_Timeout = true;
            }
            else
            {
                _invLowStart = null;
                _INV_Low_Timeout = false;
            }

            // INV Zero
            if (invZeroCondition)
            {
                if (_invZeroStart == null)
                    _invZeroStart = now;
                else if (!_INV_Zero_Timeout && now - _invZeroStart >= _invZeroTimeoutThreshold)
                    _INV_Zero_Timeout = true;
            }
            else
            {
                _invZeroStart = null;
                _INV_Zero_Timeout = false;
            }
        }

        #endregion

        #region Alarm


        private bool _invErrorAlarm => (_deltaMS300 != null) && (_deltaMS300.ErrorCode != 0);
        private bool _invWarningAlarm => (_deltaMS300 != null) && (_deltaMS300.WarningCode != 0);
        private bool _tankLLAlarm => !Sensor_Liquid_LL;
        private bool _tankHHAlarm => Sensor_Liquid_HH;

        private bool _private_waste_HAlarm => (_plcService != null) && _plcService.WasteWaterPosH;

        #endregion
    }
}

