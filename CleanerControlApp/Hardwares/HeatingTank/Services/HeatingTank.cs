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
        public bool HasAlarm => false;
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

            if (!_private_waste_HAlarm && !_tankHHAlarm && _auto)
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

