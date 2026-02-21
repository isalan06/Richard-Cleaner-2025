using CleanerControlApp.Modules.MitsubishiPLC.Interfaces;
using CleanerControlApp.Modules.MitsubishiPLC.Models;
using CleanerControlApp.Modules.Modbus.Interfaces;
using CleanerControlApp.Modules.Modbus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.MitsubishiPLC.Services
{
    public class PLCService : IPLCService, IDisposable, IPLCOperator
    {

        #region attribute

        // 預設 DIO X 數量（可視需求調整）
        private const int DefaultDICount = 5;
        private PLC_Bit_Union[] _dioX = Array.Empty<PLC_Bit_Union>();

        // 預設 DIO Y (DO) 數量
        private const int DefaultDOCount = 4;
        private PLC_Bit_Union[] _dioY = Array.Empty<PLC_Bit_Union>();

        // 預設 Status IO 數量
        private const int DefaultStatusIOCount = 7;
        private PLC_Bit_Union[] _statusIO = Array.Empty<PLC_Bit_Union>();

        // 預設 Motion Position (DWord) 數量
        private const int DefaultMotionPosCount = 4;
        private PLC_DWord_Union[] _motionPos = Array.Empty<PLC_DWord_Union>();

        // 預設 Command 數量
        private const int DefaultCommandCount = 10;
        private PLC_Bit_Union[] _command = Array.Empty<PLC_Bit_Union>();

        // 預設 MoveInfo (DWord) 數量
        private const int DefaultMoveInfoCount = 8;
        private PLC_DWord_Union[] _moveInfo = Array.Empty<PLC_DWord_Union>();

        // 預設 ParamMotionInfo (DWord) 數量
        private const int DefaultParamMotionInfoCount = 24;
        private PLC_DWord_Union[] _paramMotionInfo = Array.Empty<PLC_DWord_Union>();

        // 預設 ParamTimeout (Word) 數量
        private const int DefaultParamTimeoutCount = 8;
        private PLC_Word_Union[] _paramTimeout = Array.Empty<PLC_Word_Union>();

        // 預設 ParamMotionInfoW (DWord) 數量
        private const int DefaultParamMotionInfoWCount = 24;
        private PLC_DWord_Union[] _paramMotionInfoW = Array.Empty<PLC_DWord_Union>();

        // 預設 ParamTimeoutW (Word) 數量
        private const int DefaultParamTimeoutWCount = 8;
        private PLC_Word_Union[] _paramTimeoutW = Array.Empty<PLC_Word_Union>();

        // 注入的 Modbus服務（可為 null，視註冊情況）
        private readonly IModbusTCPService? _modbusService;

        // background loop
        private CancellationTokenSource? _cts;
        private Task? _loopTask;
        private readonly TimeSpan _loopInterval = TimeSpan.FromMilliseconds(50);

        private bool _running;

        private bool _readparameter;
        private bool _writeparameter;

        private int _routeIndex = 0;

        #endregion

        #region constructor

        //參數化建構子供 DI 使用
        public PLCService(IModbusTCPService? modbusService)
        {
            _modbusService = modbusService;
            InitializeArrays();
            StartLoop();
        }

        // 保留無參數建構子以維持相容性（會呼叫參數化建構子）
        public PLCService() : this(null)
        {
        }

        private void InitializeArrays()
        {
            _dioX = new PLC_Bit_Union[DefaultDICount];
            _dioY = new PLC_Bit_Union[DefaultDOCount];
            _statusIO = new PLC_Bit_Union[DefaultStatusIOCount];
            _motionPos = new PLC_DWord_Union[DefaultMotionPosCount];
            _command = new PLC_Bit_Union[DefaultCommandCount];
            _moveInfo = new PLC_DWord_Union[DefaultMoveInfoCount];
            _paramMotionInfo = new PLC_DWord_Union[DefaultParamMotionInfoCount];
            _paramTimeout = new PLC_Word_Union[DefaultParamTimeoutCount];
            _paramMotionInfoW = new PLC_DWord_Union[DefaultParamMotionInfoWCount];
            _paramTimeoutW = new PLC_Word_Union[DefaultParamTimeoutWCount];
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

                // TODO:釋出非受控資源 (非受控物件) 並覆寫完成項
                // TODO: 將大型欄位設為 Null
                disposedValue = true;
            }
        }

        // TODO: 僅有當 'Dispose(bool disposing)'具有會釋出非受控資源的程式碼時，才覆寫完成項
        ~PLCService()
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

        #region function

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
                    if (_modbusService != null)
                    {
                        await PollModbusAsync(token).ConfigureAwait(false);

                        if (_readparameter)
                        {
                            _readparameter = false;
                            await readParameter();
                        }

                        if (_writeparameter)
                        {
                            _writeparameter = false;
                            await writeParameter();
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
                if (_modbusService != null && !_modbusService.IsConnected)
                {
                    // try async connect if available
                    await _modbusService.ConnectAsync().ConfigureAwait(false);
                }

                // TODO: add real read/write frames using _modbusService.ExecuteAsync
                if (_modbusService != null && _modbusService.IsConnected && _running)
                {
                    SetModbusTCPFrame(_routeProcess[_routeIndex]);

                    var data = await _modbusService.ExecuteAsync(_routeProcess[_routeIndex].DataFrame);

                    if (data != null)
                    {
                        if (data is ModbusTCPFrame)
                        {
                            AnalysisModbusTCPFrame(_routeProcess[_routeIndex], data);
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

        private void AnalysisModbusTCPFrame(PLCFrame command, ModbusTCPFrame frame)
        {
            if (command.Id == 0 && frame.Data != null)
            {
                for (int i = 0; i < _dioX.Length && i < frame.Data.Length; i++)
                    _dioX[i].Data = frame.Data[i];

                for (int i = 0; i < _dioY.Length && (i + 6) < frame.Data.Length; i++)
                    _dioY[i].Data = frame.Data[i + 6];

                for (int i = 0; i < _statusIO.Length && (i + 11) < frame.Data.Length; i++)
                    _statusIO[i].Data = frame.Data[i + 11];

                for (int i = 0; i < _motionPos.Length && (i * 2 + 19) < frame.Data.Length; i++)
                    _motionPos[i].Set(frame.Data[i * 2 + 18], frame.Data[i * 2 + 19]);
            }
        }

        private void SetModbusTCPFrame(PLCFrame command)
        {
            if (command.Id == 1)
            {
                // Ensure the target frame instance exists to avoid CS8602 (DataFrame may be null)
                var targetFrame = _routeProcess[1].DataFrame;
                if (targetFrame == null)
                {
                    targetFrame = new ModbusTCPFrame();
                    _routeProcess[1].DataFrame = targetFrame;
                }

                targetFrame.Data = new ushort[]
                {
                    _command[0].Data, _command[1].Data, _command[2].Data, _command[3].Data, _command[4].Data,
                    _command[5].Data, _command[6].Data, _command[7].Data, _command[8].Data, _command[9].Data,
                    _moveInfo[0].LowWord, _moveInfo[0].HighWord, _moveInfo[1].LowWord, _moveInfo[1].HighWord,
                    _moveInfo[2].LowWord, _moveInfo[2].HighWord, _moveInfo[3].LowWord, _moveInfo[3].HighWord,
                    _moveInfo[4].LowWord, _moveInfo[4].HighWord, _moveInfo[5].LowWord, _moveInfo[5].HighWord,
                    _moveInfo[6].LowWord, _moveInfo[6].HighWord, _moveInfo[7].LowWord, _moveInfo[7].HighWord,
                };
            }
        }

        // Event to notify UI when parameter read completes
        public event EventHandler? ParametersReadCompleted;
        // Event to notify UI when parameter write completes
        public event EventHandler? ParametersWriteCompleted;

        private async Task readParameter()
        {
            if (_modbusService != null && _modbusService.IsConnected)
            {
                PLCFrame plcFrame = new PLCFrame()
                {
                    Id = 11,
                    Name = "Read Parameter",
                    DataFrame = new ModbusTCPFrame()
                    {
                        SlaveAddress = 1,
                        FunctionCode = 0x3,
                        StartAddress = 700,
                        DataNumber = 64
                    }
                };

                var data = await _modbusService.ExecuteAsync(plcFrame.DataFrame);

                if (data != null)
                {
                    if (data is ModbusTCPFrame)
                    {
                        if (data.Data != null && data.Data.Length == 64)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                _paramMotionInfo[i * 6].Set(data.Data[i * 16], data.Data[i * 16 + 1]);
                                _paramMotionInfo[i * 6 + 1].Set(data.Data[i * 16 + 2], data.Data[i * 16 + 3]);
                                _paramMotionInfo[i * 6 + 2].Set(data.Data[i * 16 + 4], data.Data[i * 16 + 5]);
                                _paramMotionInfo[i * 6 + 3].Set(data.Data[i * 16 + 6], data.Data[i * 16 + 7]);
                                _paramMotionInfo[i * 6 + 4].Set(data.Data[i * 16 + 8], data.Data[i * 16 + 9]);
                                _paramMotionInfo[i * 6 + 5].Set(data.Data[i * 16 + 10], data.Data[i * 16 + 11]);
                                _paramTimeout[i * 2].Data = data.Data[i * 16 + 12];
                                _paramTimeout[i * 2 + 1].Data = data.Data[i * 16 + 13];
                            }
                        }
                    }
                }
            }

            // notify listeners that parameter read completed (even if modbus not connected or data null)
            try
            {
                ParametersReadCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch { }
        }

        private async Task writeParameter()
        {
            if (_modbusService != null && _modbusService.IsConnected)
            {
                PLCFrame plcFrame = new PLCFrame()
                {
                    Id = 11,
                    Name = "Write Parameter",
                    DataFrame = new ModbusTCPFrame()
                    {
                        SlaveAddress = 1,
                        FunctionCode = 0x10,
                        StartAddress = 800,
                        DataNumber = 64
                    }
                };

                plcFrame.DataFrame.Data = new ushort[]
                {
                    _paramMotionInfoW[0].LowWord, _paramMotionInfoW[0].HighWord, _paramMotionInfoW[1].LowWord, _paramMotionInfoW[1].HighWord,
                    _paramMotionInfoW[2].LowWord, _paramMotionInfoW[2].HighWord, _paramMotionInfoW[3].LowWord, _paramMotionInfoW[3].HighWord,
                    _paramMotionInfoW[4].LowWord, _paramMotionInfoW[4].HighWord, _paramMotionInfoW[5].LowWord, _paramMotionInfoW[5].HighWord,
                    _paramTimeoutW[0].Data, _paramTimeoutW[1].Data, 0, 0,
                    _paramMotionInfoW[6].LowWord, _paramMotionInfoW[6].HighWord, _paramMotionInfoW[7].LowWord, _paramMotionInfoW[7].HighWord,
                    _paramMotionInfoW[8].LowWord, _paramMotionInfoW[8].HighWord, _paramMotionInfoW[9].LowWord, _paramMotionInfoW[9].HighWord,
                    _paramMotionInfoW[10].LowWord, _paramMotionInfoW[10].HighWord, _paramMotionInfoW[11].LowWord, _paramMotionInfoW[11].HighWord,
                    _paramTimeoutW[2].Data, _paramTimeoutW[3].Data, 0, 0,
                    _paramMotionInfoW[12].LowWord, _paramMotionInfoW[12].HighWord, _paramMotionInfoW[13].LowWord, _paramMotionInfoW[13].HighWord,
                    _paramMotionInfoW[14].LowWord, _paramMotionInfoW[14].HighWord, _paramMotionInfoW[15].LowWord, _paramMotionInfoW[15].HighWord,
                    _paramMotionInfoW[16].LowWord, _paramMotionInfoW[16].HighWord, _paramMotionInfoW[17].LowWord, _paramMotionInfoW[17].HighWord,
                    _paramTimeoutW[4].Data, _paramTimeoutW[5].Data, 0, 0,
                    _paramMotionInfoW[18].LowWord, _paramMotionInfoW[18].HighWord, _paramMotionInfoW[19].LowWord, _paramMotionInfoW[19].HighWord,
                    _paramMotionInfoW[20].LowWord, _paramMotionInfoW[20].HighWord, _paramMotionInfoW[21].LowWord, _paramMotionInfoW[21].HighWord,
                    _paramMotionInfoW[22].LowWord, _paramMotionInfoW[22].HighWord, _paramMotionInfoW[23].LowWord, _paramMotionInfoW[23].HighWord,
                    _paramTimeoutW[6].Data, _paramTimeoutW[7].Data, 0, 0
                };

                var data = await _modbusService.ExecuteAsync(plcFrame.DataFrame);
            }

            // notify listeners that parameter write completed (even if modbus not connected or data null)
            try
            {
                ParametersWriteCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch { }
        }

        #endregion

        #region IPLCService

        public PLC_Bit_Union[] DIO_X
        {
            get => _dioX;
            set => _dioX = value ?? Array.Empty<PLC_Bit_Union>();
        }

        public PLC_Bit_Union[] DIO_Y
        {
            get => _dioY;
            set => _dioY = value ?? Array.Empty<PLC_Bit_Union>();
        }

        // Status IO 陣列（可讀寫）
        public PLC_Bit_Union[] StatusIO
        {
            get => _statusIO;
            set => _statusIO = value ?? Array.Empty<PLC_Bit_Union>();
        }

        // Motion positions (DWord) 陣列
        public PLC_DWord_Union[] MotionPos
        {
            get => _motionPos;
            set => _motionPos = value ?? Array.Empty<PLC_DWord_Union>();
        }

        // Command (bit unions) 陣列
        public PLC_Bit_Union[] Command
        {
            get => _command;
            set => _command = value ?? Array.Empty<PLC_Bit_Union>();
        }

        // MoveInfo (DWord) 陣列
        public PLC_DWord_Union[] MoveInfo
        {
            get => _moveInfo;
            set => _moveInfo = value ?? Array.Empty<PLC_DWord_Union>();
        }

        // ParamMotionInfo (DWord) 陣列
        public PLC_DWord_Union[] ParamMotionInfo
        {
            get => _paramMotionInfo;
            set => _paramMotionInfo = value ?? Array.Empty<PLC_DWord_Union>();
        }

        // ParamTimeout (Word) 陣列
        public PLC_Word_Union[] ParamTimeout
        {
            get => _paramTimeout;
            set => _paramTimeout = value ?? Array.Empty<PLC_Word_Union>();
        }

        // ParamMotionInfoW (DWord) 陣列
        public PLC_DWord_Union[] ParamMotionInfoW
        {
            get => _paramMotionInfoW;
            set => _paramMotionInfoW = value ?? Array.Empty<PLC_DWord_Union>();
        }

        // ParamTimeoutW (Word) 陣列
        public PLC_Word_Union[] ParamTimeoutW
        {
            get => _paramTimeoutW;
            set => _paramTimeoutW = value ?? Array.Empty<PLC_Word_Union>();
        }

        public bool IsRunning { get { return _running; } }

        public void Start() { _running = true; }
        public void Stop() { _running = false; }

        public void ReadParameter() { _readparameter = true; }

        public void WriteParameter() { _writeparameter = true; }

        #endregion

        #region IPLCOperator

        #region DI

        public bool ShuttleXLimitN => _dioX[0].Bit0;
        public bool ShuttleXLimitP => _dioX[0].Bit1;
        public bool ShuttleZLimitN => _dioX[0].Bit2;
        public bool ShuttleZLimitP => _dioX[0].Bit3;
        public bool CleanerZLimitN => _dioX[0].Bit4;
        public bool CleanerZLimitP => _dioX[0].Bit5;
        public bool TankZLimitN => _dioX[0].Bit6;
        public bool TankZLimitP => _dioX[0].Bit7;

        public bool ShuttleXIdle => _dioX[0].Bit8;
        public bool ShuttleXInPos => _dioX[0].Bit9;
        public bool ShuttleXAlarm => _dioX[0].Bit10;
        public bool ShuttleXHome => _dioX[0].Bit11;

        public bool ShuttleZIdle => _dioX[0].Bit12;
        public bool ShuttleZInPos => _dioX[0].Bit13;
        public bool ShuttleZAlarm => _dioX[0].Bit14;
        public bool ShuttleZHome => _dioX[0].Bit15;

        public bool CleanerZIdle => _dioX[1].Bit0;
        public bool CleanerZInPos => _dioX[1].Bit1;
        public bool CleanerZAlarm => _dioX[1].Bit2;
        public bool CleanerZHome => _dioX[1].Bit3;
        public bool TankZIdle => _dioX[1].Bit4;
        public bool TankZInPos => _dioX[1].Bit5;
        public bool TankZAlarm => _dioX[1].Bit6;
        public bool TankZHome => _dioX[1].Bit7;

        public bool ShuttleZClamperExist1 => _dioX[1].Bit8;
        public bool ShuttleZClamperExist2 => _dioX[1].Bit9;
        public bool ShuttleZFClamperOpen => _dioX[1].Bit10;
        public bool ShuttleZFClamperClose => _dioX[1].Bit11;
        public bool ShuttleZBClamperOpen => _dioX[1].Bit12;
        public bool ShuttleZBClamperClose => _dioX[1].Bit13;
        public bool InSlotExist1 => _dioX[1].Bit14;
        public bool InSlotExist2 => _dioX[1].Bit15;

        public bool InSlotExist3 => _dioX[2].Bit0;
        public bool InSlotExist4 => _dioX[2].Bit1;
        public bool InSlotExist5 => _dioX[2].Bit2;
        public bool OutSlotExist1 => _dioX[2].Bit3;
        public bool OutSlotExist2 => _dioX[2].Bit4;
        public bool OutSlotExist3 => _dioX[2].Bit5;
        public bool OutSlotExist4 => _dioX[2].Bit6;
        public bool OutSlotExist5 => _dioX[2].Bit7;

        public bool CleanerCoverFIn => _dioX[2].Bit8;
        public bool CleanerCoverBIn => _dioX[2].Bit9;
        public bool TankCoverFIn => _dioX[2].Bit10;
        public bool TankCoverBIn => _dioX[2].Bit11;
        public bool TankWaterPosL => _dioX[2].Bit12;
        public bool TankWaterPosH => _dioX[2].Bit13;
        public bool Heater1CoverFIn => _dioX[2].Bit14;
        public bool Heater1CoverBIn => _dioX[2].Bit15;

        public bool Heater2CoverFIn => _dioX[3].Bit0;
        public bool Hater2CoverBIn => _dioX[3].Bit1;
        public bool HotWaterPosLL => _dioX[3].Bit2;
        public bool HotWaterPosL => _dioX[3].Bit3;
        public bool HotWaterPosH => _dioX[3].Bit4;
        public bool HotWaterPosHH => _dioX[3].Bit5;
        public bool WasteWaterPosH => _dioX[3].Bit6;

        public bool EMOSign => _dioX[3].Bit8;
        public bool MaintainSign => _dioX[3].Bit9;
        public bool ShuttleZClamperOpenSign => _dioX[3].Bit10;
        public bool ShuttleZClamperCloseSign => _dioX[3].Bit11;
        public bool MainPowerSign => _dioX[3].Bit12;
        public bool FrontDoor1 => _dioX[3].Bit13;
        public bool FrontDoor2 => _dioX[3].Bit14;
        public bool FrontDoor3 => _dioX[3].Bit15;

        public bool FrontDoor4 => _dioX[4].Bit0;
        public bool SideDoor1 => _dioX[4].Bit1;
        public bool SideDoor2 => _dioX[4].Bit2;
        public bool Leakage1 => _dioX[4].Bit3;
        public bool Leakage2 => _dioX[4].Bit4;

        #endregion

        #region DO

        public bool ShuttleXServoMotorPLS => _dioY[0].Bit0;
        public bool ShuttleZServoMotorPLS => _dioY[0].Bit1;
        public bool CleanerZServoMotorPLS => _dioY[0].Bit2;
        public bool TankZServoMotorPLS => _dioY[0].Bit3;
        public bool ShuttleXServoMotorSIGN => _dioY[0].Bit4;
        public bool ShuttleZServoMotorSIGN => _dioY[0].Bit5;
        public bool CleanerZServoMotorSIGN => _dioY[0].Bit6;
        public bool TankZServoMotorSIGN => _dioY[0].Bit7;

        public bool ShuttleXServoPosCommandStop => _dioY[0].Bit8;
        public bool ShuttleXServoAlarmReset => _dioY[0].Bit9;
        public bool ShuttleXServoServoOn => _dioY[0].Bit10;
        public bool ShuttleZServoPosCommandStop => _dioY[0].Bit11;
        public bool ShuttleZServoAlarmReset => _dioY[0].Bit12;
        public bool ShuttleZServoServoOn => _dioY[0].Bit13;
        public bool CleanerZServoPosCommandStop => _dioY[0].Bit14;
        public bool CleanerZServoAlarmReset => _dioY[0].Bit15;

        public bool CleanerZServoServoOn => _dioY[1].Bit0;
        public bool TankZServoPosCommandStop => _dioY[1].Bit1;
        public bool TankZServoAlarmReset => _dioY[1].Bit2;
        public bool TankZServoServoOn => _dioY[1].Bit3;

        public bool ShuttleZServoMotorBrake => _dioY[1].Bit8;
        public bool CleanerZServoMotorBrake => _dioY[1].Bit9;
        public bool TankZServoMotorBrake => _dioY[1].Bit10;
        public bool Heater1Blower => _dioY[1].Bit11;
        public bool Heater2Blower => _dioY[1].Bit12;

        public bool ShuttleZClampOpen => _dioY[2].Bit0;
        public bool ShuttleZClampClose => _dioY[2].Bit1;
        public bool InputWaterValveOpen => _dioY[2].Bit2;
        public bool TankOutputWaterValveOpen => _dioY[2].Bit3;
        public bool HeaterTankSwitchValveOpen => _dioY[2].Bit4;

        public bool CleanerCoverOpen => _dioY[2].Bit8;
        public bool TankCoverOpen => _dioY[2].Bit9;
        public bool Heater1CoverOpen => _dioY[2].Bit10;
        public bool Heater2CoverOpen => _dioY[2].Bit11;
        public bool CleanerAirKnifeOpen => _dioY[2].Bit12;
        public bool TankAirKnifeOpen => _dioY[2].Bit13;
        public bool Heater1AirOpen => _dioY[2].Bit14;
        public bool Heater2AirOpen => _dioY[2].Bit15;

        public bool LighterRed => _dioY[3].Bit0;
        public bool LighterYellow => _dioY[3].Bit1;
        public bool LighterGreen => _dioY[3].Bit2;
        public bool LighterBuzzer => _dioY[3].Bit3;

        #endregion

        #region Status

        public bool SystemError => _statusIO[0].Bit0;
        public bool Axis1Error => _statusIO[0].Bit4;
        public bool Axis2Error => _statusIO[0].Bit5;
        public bool Axis3Error => _statusIO[0].Bit6;
        public bool Axis4Error => _statusIO[0].Bit7;

        public bool Axis1ErrorAlarm => _statusIO[1].Bit0;
        public bool Axis1ErrorLimitN => _statusIO[1].Bit4;
        public bool Axis1ErrorLimitP => _statusIO[1].Bit5;
        public bool Axis1ErrorHomeTimeout => _statusIO[1].Bit6;
        public bool Axis1ErrorCommandTimeout => _statusIO[1].Bit7;

        public bool Axis2ErrorAlarm => _statusIO[2].Bit0;
        public bool Axis2ErrorLimitN => _statusIO[2].Bit4;
        public bool Axis2ErrorLimitP => _statusIO[2].Bit5;
        public bool Axis2ErrorHomeTimeout => _statusIO[2].Bit6;
        public bool Axis2ErrorCommandTimeout => _statusIO[2].Bit7;

        public bool Axis3ErrorAlarm => _statusIO[3].Bit0;
        public bool Axis3ErrorLimitN => _statusIO[3].Bit4;
        public bool Axis3ErrorLimitP => _statusIO[3].Bit5;
        public bool Axis3ErrorHomeTimeout => _statusIO[3].Bit6;
        public bool Axis3ErrorCommandTimeout => _statusIO[3].Bit7;

        public bool Axis4ErrorAlarm => _statusIO[4].Bit0;
        public bool Axis4ErrorLimitN => _statusIO[4].Bit4;
        public bool Axis4ErrorLimitP => _statusIO[4].Bit5;
        public bool Axis4ErrorHomeTimeout => _statusIO[4].Bit6;
        public bool Axis4ErrorCommandTimeout => _statusIO[4].Bit7;

        public bool Axis1HomeComplete => _statusIO[5].Bit0;
        public bool Axis2HomeComplete => _statusIO[5].Bit1;
        public bool Axis3HomeComplete => _statusIO[5].Bit2;
        public bool Axis4HomeComplete => _statusIO[5].Bit3;
        public bool Axis1HomeProcedure => _statusIO[5].Bit4;
        public bool Axis1CommandProcedure => _statusIO[5].Bit5;
        public bool Axis2HomeProcedure => _statusIO[5].Bit6;
        public bool Axis2CommandProcedure => _statusIO[5].Bit7;
        public bool Axis3HomeProcedure => _statusIO[5].Bit8;
        public bool Axis3CommandProcedure => _statusIO[5].Bit9;
        public bool Axis4HomeProcedure => _statusIO[5].Bit10;
        public bool Axis4CommandProcedure => _statusIO[5].Bit11;
        public bool Axis1CommandDriving => _statusIO[5].Bit12;
        public bool Axis2CommandDriving => _statusIO[5].Bit13;
        public bool Axis3CommandDriving => _statusIO[5].Bit14;
        public bool Axis4CommandDriving => _statusIO[5].Bit15;

        public bool Axis1OutputPulseStop => _statusIO[6].Bit0;
        public bool Axis2OutputPulseStop => _statusIO[6].Bit1;
        public bool Axis3OutputPulseStop => _statusIO[6].Bit2;
        public bool Axis4OutputPulseStop => _statusIO[6].Bit3;

        #endregion

        #region MotorPos

        public int Axis1Pos => _motionPos[0].IntValue;
        public int Axis2Pos => _motionPos[1].IntValue;
        public int Axis3Pos => _motionPos[2].IntValue;
        public int Axis4Pos => _motionPos[3].IntValue;

        #endregion

        #region Command

        public bool Command_AutoStart
        {
            get { return _command[0].Bit0; }
            set { _command[0].Bit0 = value; }
        }
        public bool Command_AlarmReset
        {
            get { return _command[0].Bit1; }
            set { _command[0].Bit1 = value; }
        }

        public bool Command_WriteParameter
        {
            get { return _command[0].Bit2; }
            set { _command[0].Bit2 = value; }
        }

        public bool Command_Axis1JogP
        {
            get { return _command[1].Bit0; }
            set { _command[1].Bit0 = value; }
        }
        public bool Command_Axis1JogN
        {
            get { return _command[1].Bit1; }
            set { _command[1].Bit1 = value; }
        }
        public bool Command_Axis1JogSpeedH
        {
            get { return _command[1].Bit2; }
            set { _command[1].Bit2 = value; }
        }
        public bool Command_Axis1JogSpeedM
        {
            get { return _command[1].Bit3; }
            set { _command[1].Bit3 = value; }
        }
        public bool Command_Axis1Home
        {
            get { return _command[1].Bit4; }
            set { _command[1].Bit4 = value; }
        }
        public bool Command_Axis1Stop
        {
            get { return _command[1].Bit5; }
            set { _command[1].Bit5 = value; }
        }
        public bool Command_Axis1Command
        {
            get { return _command[1].Bit6; }
            set { _command[1].Bit6 = value; }
        }
        public bool Command_Axis1ServoOn
        {
            get { return _command[1].Bit8; }
            set { _command[1].Bit8 = value; }
        }
        public bool Command_Axis1AlarmReset
        {
            get { return _command[1].Bit9; }
            set { _command[1].Bit9 = value; }
        }

        public bool Command_Axis2JogP
        {
            get { return _command[2].Bit0; }
            set { _command[2].Bit0 = value; }
        }
        public bool Command_Axis2JogN
        {
            get { return _command[2].Bit1; }
            set { _command[2].Bit1 = value; }
        }
        public bool Command_Axis2JogSpeedH
        {
            get { return _command[2].Bit2; }
            set { _command[2].Bit2 = value; }
        }
        public bool Command_Axis2JogSpeedM
        {
            get { return _command[2].Bit3; }
            set { _command[2].Bit3 = value; }
        }
        public bool Command_Axis2Home
        {
            get { return _command[2].Bit4; }
            set { _command[2].Bit4 = value; }
        }
        public bool Command_Axis2Stop
        {
            get { return _command[2].Bit5; }
            set { _command[2].Bit5 = value; }
        }
        public bool Command_Axis2Command
        {
            get { return _command[2].Bit6; }
            set { _command[2].Bit6 = value; }
        }
        public bool Command_Axis2ServoOn
        {
            get { return _command[2].Bit8; }
            set { _command[2].Bit8 = value; }
        }
        public bool Command_Axis2AlarmReset
        {
            get { return _command[2].Bit9; }
            set { _command[2].Bit9 = value; }
        }

        public bool Command_Axis3JogP
        {
            get { return _command[3].Bit0; }
            set { _command[3].Bit0 = value; }
        }
        public bool Command_Axis3JogN
        {
            get { return _command[3].Bit1; }
            set { _command[3].Bit1 = value; }
        }
        public bool Command_Axis3JogSpeedH
        {
            get { return _command[3].Bit2; }
            set { _command[3].Bit2 = value; }
        }
        public bool Command_Axis3JogSpeedM
        {
            get { return _command[3].Bit3; }
            set { _command[3].Bit3 = value; }
        }
        public bool Command_Axis3Home
        {
            get { return _command[3].Bit4; }
            set { _command[3].Bit4 = value; }
        }
        public bool Command_Axis3Stop
        {
            get { return _command[3].Bit5; }
            set { _command[3].Bit5 = value; }
        }
        public bool Command_Axis3Command
        {
            get { return _command[3].Bit6; }
            set { _command[3].Bit6 = value; }
        }
        public bool Command_Axis3ServoOn
        {
            get { return _command[3].Bit8; }
            set { _command[3].Bit8 = value; }
        }
        public bool Command_Axis3AlarmReset
        {
            get { return _command[3].Bit9; }
            set { _command[3].Bit9 = value; }
        }

        public bool Command_Axis4JogP
        {
            get { return _command[4].Bit0; }
            set { _command[4].Bit0 = value; }
        }
        public bool Command_Axis4JogN
        {
            get { return _command[4].Bit1; }
            set { _command[4].Bit1 = value; }
        }
        public bool Command_Axis4JogSpeedH
        {
            get { return _command[4].Bit2; }
            set { _command[4].Bit2 = value; }
        }
        public bool Command_Axis4JogSpeedM
        {
            get { return _command[4].Bit3; }
            set { _command[4].Bit3 = value; }
        }
        public bool Command_Axis4Home
        {
            get { return _command[4].Bit4; }
            set { _command[4].Bit4 = value; }
        }
        public bool Command_Axis4Stop
        {
            get { return _command[4].Bit5; }
            set { _command[4].Bit5 = value; }
        }
        public bool Command_Axis4Command
        {
            get { return _command[4].Bit6; }
            set { _command[4].Bit6 = value; }
        }
        public bool Command_Axis4ServoOn
        {
            get { return _command[4].Bit8; }
            set { _command[4].Bit8 = value; }
        }
        public bool Command_Axis4AlarmReset
        {
            get { return _command[4].Bit9; }
            set { _command[4].Bit9 = value; }
        }

        #endregion

        #region Command DO

        public bool Command_ShuttleXServoMotorPLS
        {
            get { return _command[5].Bit0; }
            set { _command[5].Bit0 = value; }
        }
        public bool Command_ShuttleZServoMotorPLS
        {
            get { return _command[5].Bit1; }
            set { _command[5].Bit1 = value; }
        }
        public bool Command_CleanerZServoMotorPLS
        {
            get { return _command[5].Bit2; }
            set { _command[5].Bit2 = value; }
        }
        public bool Command_TankZServoMotorPLS
        {
            get { return _command[5].Bit3; }
            set { _command[5].Bit3 = value; }
        }
        public bool Command_ShuttleXServoMotorSIGN
        {
            get { return _command[5].Bit4; }
            set { _command[5].Bit4 = value; }
        }
        public bool Command_ShuttleZServoMotorSIGN
        {
            get { return _command[5].Bit5; }
            set { _command[5].Bit5 = value; }
        }
        public bool Command_CleanerZServoMotorSIGN
        {
            get { return _command[5].Bit6; }
            set { _command[5].Bit6 = value; }
        }
        public bool Command_TankZServoMotorSIGN
        {
            get { return _command[5].Bit7; }
            set { _command[5].Bit7 = value; }
        }

        public bool Command_ShuttleXServoPosCommandStop
        {
            get { return _command[5].Bit8; }
            set { _command[5].Bit8 = value; }
        }
        public bool Command_ShuttleXServoAlarmReset
        {
            get { return _command[5].Bit9; }
            set { _command[5].Bit9 = value; }
        }
        public bool Command_ShuttleXServoServoOn
        {
            get { return _command[5].Bit10; }
            set { _command[5].Bit10 = value; }
        }
        public bool Command_ShuttleZServoPosCommandStop
        {
            get { return _command[5].Bit11; }
            set { _command[5].Bit11 = value; }
        }
        public bool Command_ShuttleZServoAlarmReset
        {
            get { return _command[5].Bit12; }
            set { _command[5].Bit12 = value; }
        }
        public bool Command_ShuttleZServoServoOn
        {
            get { return _command[5].Bit13; }
            set { _command[5].Bit13 = value; }
        }
        public bool Command_CleanerZServoPosCommandStop
        {
            get { return _command[5].Bit14; }
            set { _command[5].Bit14 = value; }
        }
        public bool Command_CleanerZServoAlarmReset
        {
            get { return _command[5].Bit15; }
            set { _command[5].Bit15 = value; }
        }

        public bool Command_CleanerZServoServoOn
        {
            get { return _command[6].Bit0; }
            set { _command[6].Bit0 = value; }
        }
        public bool Command_TankZServoPosCommandStop
        {
            get { return _command[6].Bit1; }
            set { _command[6].Bit1 = value; }
        }
        public bool Command_TankZServoAlarmReset
        {
            get { return _command[6].Bit2; }
            set { _command[6].Bit2 = value; }
        }
        public bool Command_TankZServoServoOn
        {
            get { return _command[6].Bit3; }
            set { _command[6].Bit3 = value; }
        }

        public bool Command_ShuttleZServoMotorBrake
        {
            get { return _command[6].Bit8; }
            set { _command[6].Bit8 = value; }
        }
        public bool Command_CleanerZServoMotorBrake
        {
            get { return _command[6].Bit9; }
            set { _command[6].Bit9 = value; }
        }
        public bool Command_TankZServoMotorBrake
        {
            get { return _command[6].Bit10; }
            set { _command[6].Bit10 = value; }
        }
        public bool Command_Heater1Blower
        {
            get { return _command[6].Bit11; }
            set { _command[6].Bit11 = value; }
        }
        public bool Command_Heater2Blower
        {
            get { return _command[6].Bit12; }
            set { _command[6].Bit12 = value; }
        }

        public bool Command_ShuttleZClampOpen
        {
            get { return _command[7].Bit0; }
            set { _command[7].Bit0 = value; }
        }
        public bool Command_ShuttleZClampClose
        {
            get { return _command[7].Bit1; }
            set { _command[7].Bit1 = value; }
        }
        public bool Command_InputWaterValveOpen
        {
            get { return _command[7].Bit2; }
            set { _command[7].Bit2 = value; }
        }
        public bool Command_TankOutputWaterValveOpen
        {
            get { return _command[7].Bit3; }
            set { _command[7].Bit3 = value; }
        }
        public bool Command_HeaterTankSwitchValveOpen
        {
            get { return _command[7].Bit4; }
            set { _command[7].Bit4 = value; }
        }

        public bool Command_CleanerCoverOpen
        {
            get { return _command[7].Bit8; }
            set { _command[7].Bit8 = value; }
        }
        public bool Command_TankCoverOpen
        {
            get { return _command[7].Bit9; }
            set { _command[7].Bit9 = value; }
        }
        public bool Command_Heater1CoverOpen
        {
            get { return _command[7].Bit10; }
            set { _command[7].Bit10 = value; }
        }
        public bool Command_Heater2CoverOpen
        {
            get { return _command[7].Bit11; }
            set { _command[7].Bit11 = value; }
        }
        public bool Command_CleanerAirKnifeOpen
        {
            get { return _command[7].Bit12; }
            set { _command[7].Bit12 = value; }
        }
        public bool Command_TankAirKnifeOpen
        {
            get { return _command[7].Bit13; }
            set { _command[7].Bit13 = value; }
        }
        public bool Command_Heater1AirOpen
        {
            get { return _command[7].Bit14; }
            set { _command[7].Bit14 = value; }
        }
        public bool Command_Heater2AirOpen
        {
            get { return _command[7].Bit15; }
            set { _command[7].Bit15 = value; }
        }

        public bool Command_LighterRed
        {
            get { return _command[8].Bit0; }
            set { _command[8].Bit0 = value; }
        }
        public bool Command_LighterYellow
        {
            get { return _command[8].Bit1; }
            set { _command[8].Bit1 = value; }
        }
        public bool Command_LighterGreen
        {
            get { return _command[8].Bit2; }
            set { _command[8].Bit2 = value; }
        }
        public bool Command_LighterBuzzer
        {
            get { return _command[8].Bit3; }
            set { _command[8].Bit3 = value; }
        }

        #endregion

        #region Move Info

        public int Command_Axis1Pos
        {
            get { return _moveInfo[0].IntValue; }
            set { _moveInfo[0].IntValue = value; }
        }
        public int Command_Axis1Speed
        {
            get { return _moveInfo[1].IntValue; }
            set { _moveInfo[1].IntValue = value; }
        }
        public int Command_Axis2Pos
        {
            get { return _moveInfo[2].IntValue; }
            set { _moveInfo[2].IntValue = value; }
        }
        public int Command_Axis2Speed
        {
            get { return _moveInfo[3].IntValue; }
            set { _moveInfo[3].IntValue = value; }
        }
        public int Command_Axis3Pos
        {
            get { return _moveInfo[4].IntValue; }
            set { _moveInfo[4].IntValue = value; }
        }
        public int Command_Axis3Speed
        {
            get { return _moveInfo[5].IntValue; }
            set { _moveInfo[5].IntValue = value; }
        }
        public int Command_Axis4Pos
        {
            get { return _moveInfo[6].IntValue; }
            set { _moveInfo[6].IntValue = value; }
        }
        public int Command_Axis4Speed
        {
            get { return _moveInfo[7].IntValue; }
            set { _moveInfo[7].IntValue = value; }
        }

        #endregion

        #region Parameter Read

        public int Param_Read_Axis1JogSpeedH => _paramMotionInfo[0].IntValue;
        public int Param_Read_Axis1JogSpeedM => _paramMotionInfo[1].IntValue;
        public int Param_Read_Axis1JogSpeedL => _paramMotionInfo[2].IntValue;
        public int Param_Read_Axis1HomeSpeedH => _paramMotionInfo[3].IntValue;
        public int Param_Read_Axis1HomeSpeedM => _paramMotionInfo[4].IntValue;
        public int Param_Read_Axis1HomeSpeedL => _paramMotionInfo[5].IntValue;
        public int Param_Read_Axis1HomeTimeoutValue_ms => _paramTimeout[0].IntValue;
        public int Param_Read_Axis1CommandTimeoutValue_ms => _paramTimeout[1].IntValue;

        public int Param_Read_Axis2JogSpeedH => ParamMotionInfo[6].IntValue;
        public int Param_Read_Axis2JogSpeedM => ParamMotionInfo[7].IntValue;
        public int Param_Read_Axis2JogSpeedL => ParamMotionInfo[8].IntValue;
        public int Param_Read_Axis2HomeSpeedH => ParamMotionInfo[9].IntValue;
        public int Param_Read_Axis2HomeSpeedM => ParamMotionInfo[10].IntValue;
        public int Param_Read_Axis2HomeSpeedL => ParamMotionInfo[11].IntValue;
        public int Param_Read_Axis2HomeTimeoutValue_ms => _paramTimeout[2].IntValue;
        public int Param_Read_Axis2CommandTimeoutValue_ms => _paramTimeout[3].IntValue;

        public int Param_Read_Axis3JogSpeedH => _paramMotionInfo[12].IntValue;
        public int Param_Read_Axis3JogSpeedM => _paramMotionInfo[13].IntValue;
        public int Param_Read_Axis3JogSpeedL => _paramMotionInfo[14].IntValue;
        public int Param_Read_Axis3HomeSpeedH => _paramMotionInfo[15].IntValue;
        public int Param_Read_Axis3HomeSpeedM => _paramMotionInfo[16].IntValue;
        public int Param_Read_Axis3HomeSpeedL => _paramMotionInfo[17].IntValue;
        public int Param_Read_Axis3HomeTimeoutValue_ms => _paramTimeout[4].IntValue;
        public int Param_Read_Axis3CommandTimeoutValue_ms => _paramTimeout[5].IntValue;

        public int Param_Read_Axis4JogSpeedH => _paramMotionInfo[18].IntValue;
        public int Param_Read_Axis4JogSpeedM => _paramMotionInfo[19].IntValue;
        public int Param_Read_Axis4JogSpeedL => _paramMotionInfo[20].IntValue;
        public int Param_Read_Axis4HomeSpeedH => _paramMotionInfo[21].IntValue;
        public int Param_Read_Axis4HomeSpeedM => _paramMotionInfo[22].IntValue;
        public int Param_Read_Axis4HomeSpeedL => _paramMotionInfo[23].IntValue;
        public int Param_Read_Axis4HomeTimeoutValue_ms => _paramTimeout[6].IntValue;
        public int Param_Read_Axis4CommandTimeoutValue_ms => _paramTimeout[7].IntValue;

        #endregion

        #region Parameter Write

        public int Param_Write_Axis1JogSpeedH
        {
            get { return _paramMotionInfoW[0].IntValue; }
            set { _paramMotionInfoW[0].IntValue = value; }
        }
        public int Param_Write_Axis1JogSpeedM
        {
            get { return _paramMotionInfoW[1].IntValue; }
            set { _paramMotionInfoW[1].IntValue = value; }
        }
        public int Param_Write_Axis1JogSpeedL
        {
            get { return _paramMotionInfoW[2].IntValue; }
            set { _paramMotionInfoW[2].IntValue = value; }
        }
        public int Param_Write_Axis1HomeSpeedH
        {
            get { return _paramMotionInfoW[3].IntValue; }
            set { _paramMotionInfoW[3].IntValue = value; }
        }
        public int Param_Write_Axis1HomeSpeedM
        {
            get { return _paramMotionInfoW[4].IntValue; }
            set { _paramMotionInfoW[4].IntValue = value; }
        }
        public int Param_Write_Axis1HomeSpeedL
        {
            get { return _paramMotionInfoW[5].IntValue; }
            set { _paramMotionInfoW[5].IntValue = value; }
        }
        public int Param_Write_Axis1HomeTimeoutValue_ms
        { 
            get { return _paramTimeoutW[0].IntValue; }
            set { _paramTimeoutW[0].IntValue = value; } 
        }
        public int Param_Write_Axis1CommandTimeoutValue_ms
        {
            get { return _paramTimeoutW[1].IntValue; }
            set { _paramTimeoutW[1].IntValue = value; }
        }

        public int Param_Write_Axis2JogSpeedH
        {
            get { return _paramMotionInfoW[6].IntValue; }
            set { _paramMotionInfoW[6].IntValue = value; }
        }
        public int Param_Write_Axis2JogSpeedM
        {
            get { return _paramMotionInfoW[7].IntValue; }
            set { _paramMotionInfoW[7].IntValue = value; }
        }
        public int Param_Write_Axis2JogSpeedL
        {
            get { return _paramMotionInfoW[8].IntValue; }
            set { _paramMotionInfoW[8].IntValue = value; }
        }
        public int Param_Write_Axis2HomeSpeedH
        {
            get { return _paramMotionInfoW[9].IntValue; }
            set { _paramMotionInfoW[9].IntValue = value; }
        }
        public int Param_Write_Axis2HomeSpeedM
        {
            get { return _paramMotionInfoW[10].IntValue; }
            set { _paramMotionInfoW[10].IntValue = value; }
        }
        public int Param_Write_Axis2HomeSpeedL
        {
            get { return _paramMotionInfoW[11].IntValue; }
            set { _paramMotionInfoW[11].IntValue = value; }
        }
        public int Param_Write_Axis2HomeTimeoutValue_ms
        {
            get { return _paramTimeoutW[2].IntValue; }
            set { _paramTimeoutW[2].IntValue = value; }
        }
        public int Param_Write_Axis2CommandTimeoutValue_ms
        {
            get { return _paramTimeoutW[3].IntValue; }
            set { _paramTimeoutW[3].IntValue = value; }
        }

        public int Param_Write_Axis3JogSpeedH
        {
            get { return _paramMotionInfoW[12].IntValue; }
            set { _paramMotionInfoW[12].IntValue = value; }
        }
        public int Param_Write_Axis3JogSpeedM
        {
            get { return _paramMotionInfoW[13].IntValue; }
            set { _paramMotionInfoW[13].IntValue = value; }
        }
        public int Param_Write_Axis3JogSpeedL
        {
            get { return _paramMotionInfoW[14].IntValue; }
            set { _paramMotionInfoW[14].IntValue = value; }
        }
        public int Param_Write_Axis3HomeSpeedH
        {
            get { return _paramMotionInfoW[15].IntValue; }
            set { _paramMotionInfoW[15].IntValue = value; }
        }
        public int Param_Write_Axis3HomeSpeedM
        {
            get { return _paramMotionInfoW[16].IntValue; }
            set { _paramMotionInfoW[16].IntValue = value; }
        }
        public int Param_Write_Axis3HomeSpeedL
        {
            get { return _paramMotionInfoW[17].IntValue; }
            set { _paramMotionInfoW[17].IntValue = value; }
        }
        public int Param_Write_Axis3HomeTimeoutValue_ms
        {
            get { return _paramTimeoutW[4].IntValue; }
            set { _paramTimeoutW[4].IntValue = value; }
        }
        public int Param_Write_Axis3CommandTimeoutValue_ms
        {
            get { return _paramTimeoutW[5].IntValue; }
            set { _paramTimeoutW[5].IntValue = value; }
        }

        public int Param_Write_Axis4JogSpeedH
        {
            get { return _paramMotionInfoW[18].IntValue; }
            set { _paramMotionInfoW[18].IntValue = value; }
        }
        public int Param_Write_Axis4JogSpeedM
        {
            get { return _paramMotionInfoW[19].IntValue; }
            set { _paramMotionInfoW[19].IntValue = value; }
        }
        public int Param_Write_Axis4JogSpeedL
        {
            get { return _paramMotionInfoW[20].IntValue; }
            set { _paramMotionInfoW[20].IntValue = value; }
        }
        public int Param_Write_Axis4HomeSpeedH
        {
            get { return _paramMotionInfoW[21].IntValue; }
            set { _paramMotionInfoW[21].IntValue = value; }
        }
        public int Param_Write_Axis4HomeSpeedM
        {
            get { return _paramMotionInfoW[22].IntValue; }
            set { _paramMotionInfoW[22].IntValue = value; }
        }
        public int Param_Write_Axis4HomeSpeedL
        {
            get { return _paramMotionInfoW[23].IntValue; }
            set { _paramMotionInfoW[23].IntValue = value; }
        }
        public int Param_Write_Axis4HomeTimeoutValue_ms
        {
            get { return _paramTimeoutW[6].IntValue; }
            set { _paramTimeoutW[6].IntValue = value; }
        }
        public int Param_Write_Axis4CommandTimeoutValue_ms
        {
            get { return _paramTimeoutW[7].IntValue; }
            set { _paramTimeoutW[7].IntValue = value; }
        }

        #endregion

        #endregion

        #region Route

        private PLCFrame[] _routeProcess = new PLCFrame[]
            {
                new PLCFrame()
                {
                    Id = 0,
                    Name = "READ IO, Status and Move Info",
                    DataFrame = new ModbusTCPFrame()
                    {
                        FunctionCode = 0x03,
                        StartAddress = 500,
                        DataNumber = 26
                    }
                },
                new PLCFrame()
                { 
                    Id = 1, 
                    Name = "",
                    DataFrame = new ModbusTCPFrame()
                    { 
                        FunctionCode = 0x10,
                        StartAddress = 600,
                        DataNumber = 26
                    }
                }
            };

        #endregion
    }
}
