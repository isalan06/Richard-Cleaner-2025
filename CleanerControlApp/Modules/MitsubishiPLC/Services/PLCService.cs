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
        private const int DefaultCommandCount = 9;
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
