using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Policy;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.MitsubishiPLC.Interfaces
{
    public interface IPLCOperator
    {
        #region DI
        public bool ShuttleXLimitN { get; }
        public bool ShuttleXLimitP { get; }
        public bool ShuttleZLimitN { get; }
        public bool ShuttleZLimitP { get; }
        public bool CleanerZLimitN { get; }
        public bool CleanerZLimitP { get; }
        public bool TankZLimitN { get; }
        public bool TankZLimitP { get; }

        public bool ShuttleXIdle { get; }
        public bool ShuttleXInPos { get; }
        public bool ShuttleXAlarm { get; }
        public bool ShuttleXHome { get; }
        public bool ShuttleZIdle { get; }
        public bool ShuttleZInPos { get; }
        public bool ShuttleZAlarm { get; }
        public bool ShuttleZHome { get; }

        public bool CleanerZIdle { get; }
        public bool CleanerZInPos { get; }
        public bool CleanerZAlarm { get; }
        public bool CleanerZHome { get; }
        public bool TankZIdle { get; }
        public bool TankZInPos { get; }
        public bool TankZAlarm { get; }
        public bool TankZHome { get; }

        public bool ShuttleZClamperExist1 { get; }
        public bool ShuttleZClamperExist2 { get; }
        public bool ShuttleZFClamperOpen { get; }
        public bool ShuttleZFClamperClose { get; }
        public bool ShuttleZBClamperOpen { get; }
        public bool ShuttleZBClamperClose { get; }
        public bool InSlotExist1 { get; }
        public bool InSlotExist2 { get; }

        public bool InSlotExist3 { get; }
        public bool InSlotExist4 { get; }
        public bool InSlotExist5 { get; }
        public bool OutSlotExist1 { get; }
        public bool OutSlotExist2 { get; }
        public bool OutSlotExist3 { get; }
        public bool OutSlotExist4 { get; }
        public bool OutSlotExist5 { get; }

        public bool CleanerCoverFIn { get; }
        public bool CleanerCoverBIn { get; }
        public bool TankCoverFIn { get; }
        public bool TankCoverBIn { get; }
        public bool TankWaterPosL { get; }
        public bool TankWaterPosH { get; }
        public bool Heater1CoverFIn { get; }
        public bool Heater1CoverBIn { get; }

        public bool Heater2CoverFIn { get; }
        public bool Hater2CoverBIn { get; }
        public bool HotWaterPosLL { get; }
        public bool HotWaterPosL { get; }
        public bool HotWaterPosH { get; }
        public bool HotWaterPosHH { get; }
        public bool WasteWaterPosH { get; }

        public bool EMOSign { get; }
        public bool MaintainSign { get; }
        public bool ShuttleZClamperOpenSign { get; }
        public bool ShuttleZClamperCloseSign { get; }
        public bool MainPowerSign { get; }
        public bool FrontDoor1 { get; }
        public bool FrontDoor2 { get; }
        public bool FrontDoor3 { get; }

        public bool FrontDoor4 { get; }
        public bool SideDoor1 { get; }
        public bool SideDoor2 { get; }
        public bool Leakage1 { get; }
        public bool Leakage2 { get; }

        #endregion

        #region DO

        public bool ShuttleXServoMotorPLS { get; }
        public bool ShuttleZServoMotorPLS { get; }
        public bool CleanerZServoMotorPLS { get; }
        public bool TankZServoMotorPLS { get; }
        public bool ShuttleXServoMotorSIGN { get; }
        public bool ShuttleZServoMotorSIGN { get; }
        public bool CleanerZServoMotorSIGN { get; }
        public bool TankZServoMotorSIGN { get; }

        public bool ShuttleXServoPosCommandStop { get; }
        public bool ShuttleXServoAlarmReset { get; }
        public bool ShuttleXServoServoOn { get; }
        public bool ShuttleZServoPosCommandStop { get; }
        public bool ShuttleZServoAlarmReset { get; }
        public bool ShuttleZServoServoOn { get; }
        public bool CleanerZServoPosCommandStop { get; }
        public bool CleanerZServoAlarmReset { get; }

        public bool CleanerZServoServoOn { get; }
        public bool TankZServoPosCommandStop { get; }
        public bool TankZServoAlarmReset { get; }
        public bool TankZServoServoOn { get; }

        public bool ShuttleZServoMotorBrake { get; }
        public bool CleanerZServoMotorBrake { get; }
        public bool TankZServoMotorBrake { get; }
        public bool Heater1Blower { get; }
        public bool Heater2Blower { get; }

        public bool ShuttleZClampOpen { get; }
        public bool ShuttleZClampClose { get; }
        public bool InputWaterValveOpen { get; }
        public bool TankOutputWaterValveOpen { get; }
        public bool HeaterTankSwitchValveOpen { get; }

        public bool CleanerCoverOpen { get; }
        public bool TankCoverOpen { get; }
        public bool Heater1CoverOpen { get; }
        public bool Heater2CoverOpen { get; }
        public bool CleanerAirKnifeOpen { get; }
        public bool TankAirKnifeOpen { get; }
        public bool Heater1AirOpen { get; }
        public bool Heater2AirOpen { get; }

        public bool LighterRed { get; }
        public bool LighterYellow { get; }
        public bool LighterGreen { get; }
        public bool LighterBuzzer { get; }

        #endregion

        #region Status

        public bool SystemError { get; }
        public bool Axis1Error { get; }
        public bool Axis2Error { get; }
        public bool Axis3Error { get; }
        public bool Axis4Error { get; }

        public bool Axis1ErrorAlarm { get; }
        public bool Axis1ErrorLimitN { get; }
        public bool Axis1ErrorLimitP { get; }
        public bool Axis1ErrorHomeTimeout { get; }
        public bool Axis1ErrorCommandTimeout { get; }

        public bool Axis2ErrorAlarm { get; }
        public bool Axis2ErrorLimitN { get; }
        public bool Axis2ErrorLimitP { get; }
        public bool Axis2ErrorHomeTimeout { get; }
        public bool Axis2ErrorCommandTimeout { get; }

        public bool Axis3ErrorAlarm { get; }
        public bool Axis3ErrorLimitN { get; }
        public bool Axis3ErrorLimitP { get; }
        public bool Axis3ErrorHomeTimeout { get; }
        public bool Axis3ErrorCommandTimeout { get; }

        public bool Axis4ErrorAlarm { get; }
        public bool Axis4ErrorLimitN { get; }
        public bool Axis4ErrorLimitP { get; }
        public bool Axis4ErrorHomeTimeout { get; }
        public bool Axis4ErrorCommandTimeout { get; }

        public bool Axis1HomeComplete { get; }
        public bool Axis2HomeComplete { get; }
        public bool Axis3HomeComplete { get; }
        public bool Axis4HomeComplete { get; }
        public bool Axis1HomeProcedure { get; }
        public bool Axis1CommandProcedure { get; }
        public bool Axis2HomeProcedure { get; }
        public bool Axis2CommandProcedure { get; }
        public bool Axis3HomeProcedure { get; }
        public bool Axis3CommandProcedure { get; }
        public bool Axis4HomeProcedure { get; }
        public bool Axis4CommandProcedure { get; }
        public bool Axis1CommandDriving { get; }
        public bool Axis2CommandDriving { get; }
        public bool Axis3CommandDriving { get; }
        public bool Axis4CommandDriving { get; }

        public bool Axis1OutputPulseStop { get; }
        public bool Axis2OutputPulseStop { get; }
        public bool Axis3OutputPulseStop { get; }
        public bool Axis4OutputPulseStop { get; }

        #endregion

        #region MotorPos

        public int Axis1Pos { get; }
        public int Axis2Pos { get; }
        public int Axis3Pos { get; }
        public int Axis4Pos { get; }

        #endregion

        #region Command

        public bool Command_AutoStart { get; set; }
        public bool Command_AlarmReset { get; set; }
        public bool Command_WriteParameter { get; set; }

        public bool Command_Axis1JogP { get; set; }
        public bool Command_Axis1JogN { get; set; }
        public bool Command_Axis1JogSpeedH { get; set; }
        public bool Command_Axis1JogSpeedM { get; set; }
        public bool Command_Axis1Home { get; set; }
        public bool Command_Axis1Stop { get; set; }
        public bool Command_Axis1Command { get; set; }
        public bool Command_Axis1ServoOn { get; set; }
        public bool Command_Axis1AlarmReset { get; set; }

        public bool Command_Axis2JogP { get; set; }
        public bool Command_Axis2JogN { get; set; }
        public bool Command_Axis2JogSpeedH { get; set; }
        public bool Command_Axis2JogSpeedM { get; set; }
        public bool Command_Axis2Home { get; set; }
        public bool Command_Axis2Stop { get; set; }
        public bool Command_Axis2Command { get; set; }
        public bool Command_Axis2ServoOn { get; set; }
        public bool Command_Axis2AlarmReset { get; set; }

        public bool Command_Axis3JogP { get; set; }
        public bool Command_Axis3JogN { get; set; }
        public bool Command_Axis3JogSpeedH { get; set; }
        public bool Command_Axis3JogSpeedM { get; set; }
        public bool Command_Axis3Home { get; set; }
        public bool Command_Axis3Stop { get; set; }
        public bool Command_Axis3Command { get; set; }
        public bool Command_Axis3ServoOn { get; set; }
        public bool Command_Axis3AlarmReset { get; set; }

        public bool Command_Axis4JogP { get; set; }
        public bool Command_Axis4JogN { get; set; }
        public bool Command_Axis4JogSpeedH { get; set; }
        public bool Command_Axis4JogSpeedM { get; set; }
        public bool Command_Axis4Home { get; set; }
        public bool Command_Axis4Stop { get; set; }
        public bool Command_Axis4Command { get; set; }
        public bool Command_Axis4ServoOn { get; set; }
        public bool Command_Axis4AlarmReset { get; set; }

        #endregion

        #region Command DO

        public bool Command_ShuttleXServoMotorPLS { get; }
        public bool Command_ShuttleZServoMotorPLS { get; }
        public bool Command_CleanerZServoMotorPLS { get; }
        public bool Command_TankZServoMotorPLS { get; }
        public bool Command_ShuttleXServoMotorSIGN { get; }
        public bool Command_ShuttleZServoMotorSIGN { get; }
        public bool Command_CleanerZServoMotorSIGN { get; }
        public bool Command_TankZServoMotorSIGN { get; }

        public bool Command_ShuttleXServoPosCommandStop { get; }
        public bool Command_ShuttleXServoAlarmReset { get; }
        public bool Command_ShuttleXServoServoOn { get; }
        public bool Command_ShuttleZServoPosCommandStop { get; }
        public bool Command_ShuttleZServoAlarmReset { get; }
        public bool Command_ShuttleZServoServoOn { get; }
        public bool Command_CleanerZServoPosCommandStop { get; }
        public bool Command_CleanerZServoAlarmReset { get; }

        public bool Command_CleanerZServoServoOn { get; }
        public bool Command_TankZServoPosCommandStop { get; }
        public bool Command_TankZServoAlarmReset { get; }
        public bool Command_TankZServoServoOn { get; }

        public bool Command_ShuttleZServoMotorBrake { get; }
        public bool Command_CleanerZServoMotorBrake { get; }
        public bool Command_TankZServoMotorBrake { get; }
        public bool Command_Heater1Blower { get; }
        public bool Command_Heater2Blower { get; }

        public bool Command_ShuttleZClampOpen { get; }
        public bool Command_ShuttleZClampClose { get; }
        public bool Command_InputWaterValveOpen { get; }
        public bool Command_TankOutputWaterValveOpen { get; }
        public bool Command_HeaterTankSwitchValveOpen { get; }

        public bool Command_CleanerCoverOpen { get; }
        public bool Command_TankCoverOpen { get; }
        public bool Command_Heater1CoverOpen { get; }
        public bool Command_Heater2CoverOpen { get; }
        public bool Command_CleanerAirKnifeOpen { get; }
        public bool Command_TankAirKnifeOpen { get; }
        public bool Command_Heater1AirOpen { get; }
        public bool Command_Heater2AirOpen { get; }

        public bool Command_LighterRed { get; }
        public bool Command_LighterYellow { get; }
        public bool Command_LighterGreen { get; }
        public bool Command_LighterBuzzer { get; }

        #endregion

        #region Move Info

        public int Command_Axis1Pos { get; set; }
        public int Command_Axis1Speed { get; set; }
        public int Command_Axis2Pos { get; set; }
        public int Command_Axis2Speed { get; set; }
        public int Command_Axis3Pos { get; set; }
        public int Command_Axis3Speed { get; set; }
        public int Command_Axis4Pos { get; set; }
        public int Command_Axis4Speed { get; set; }

        #endregion

        #region Parameter Read

        public int Param_Read_Axis1JogSpeedH { get; }
        public int Param_Read_Axis1JogSpeedM { get; }
        public int Param_Read_Axis1JogSpeedL { get; }
        public int Param_Read_Axis1HomeSpeedH { get; }
        public int Param_Read_Axis1HomeSpeedM { get; }
        public int Param_Read_Axis1HomeSpeedL { get; }
        public int Param_Read_Axis1HomeTimeoutValue_ms { get; }
        public int Param_Read_Axis1CommandTimeoutValue_ms { get; }

        public int Param_Read_Axis2JogSpeedH { get; }
        public int Param_Read_Axis2JogSpeedM { get; }
        public int Param_Read_Axis2JogSpeedL { get; }
        public int Param_Read_Axis2HomeSpeedH { get; }
        public int Param_Read_Axis2HomeSpeedM { get; }
        public int Param_Read_Axis2HomeSpeedL { get; }
        public int Param_Read_Axis2HomeTimeoutValue_ms { get; }
        public int Param_Read_Axis2CommandTimeoutValue_ms { get; }

        public int Param_Read_Axis3JogSpeedH { get; }
        public int Param_Read_Axis3JogSpeedM { get; }
        public int Param_Read_Axis3JogSpeedL { get; }
        public int Param_Read_Axis3HomeSpeedH { get; }
        public int Param_Read_Axis3HomeSpeedM { get; }
        public int Param_Read_Axis3HomeSpeedL { get; }
        public int Param_Read_Axis3HomeTimeoutValue_ms { get; }
        public int Param_Read_Axis3CommandTimeoutValue_ms { get; }

        public int Param_Read_Axis4JogSpeedH { get; }
        public int Param_Read_Axis4JogSpeedM { get; }
        public int Param_Read_Axis4JogSpeedL { get; }
        public int Param_Read_Axis4HomeSpeedH { get; }
        public int Param_Read_Axis4HomeSpeedM { get; }
        public int Param_Read_Axis4HomeSpeedL { get; }
        public int Param_Read_Axis4HomeTimeoutValue_ms { get; }
        public int Param_Read_Axis4CommandTimeoutValue_ms { get; }

        #endregion

        #region Parameter Write

        public int Param_Write_Axis1JogSpeedH { get; set; }
        public int Param_Write_Axis1JogSpeedM { get; set; }
        public int Param_Write_Axis1JogSpeedL { get; set; }
        public int Param_Write_Axis1HomeSpeedH { get; set; }
        public int Param_Write_Axis1HomeSpeedM { get; set; }
        public int Param_Write_Axis1HomeSpeedL { get; set; }
        public int Param_Write_Axis1HomeTimeoutValue_ms { get; set; }
        public int Param_Write_Axis1CommandTimeoutValue_ms { get; set; }

        public int Param_Write_Axis2JogSpeedH { get; set; }
        public int Param_Write_Axis2JogSpeedM { get; set; }
        public int Param_Write_Axis2JogSpeedL { get; set; }
        public int Param_Write_Axis2HomeSpeedH { get; set; }
        public int Param_Write_Axis2HomeSpeedM { get; set; }
        public int Param_Write_Axis2HomeSpeedL { get; set; }
        public int Param_Write_Axis2HomeTimeoutValue_ms { get; set; }
        public int Param_Write_Axis2CommandTimeoutValue_ms { get; set; }

        public int Param_Write_Axis3JogSpeedH { get; set; }
        public int Param_Write_Axis3JogSpeedM { get; set; }
        public int Param_Write_Axis3JogSpeedL { get; set; }
        public int Param_Write_Axis3HomeSpeedH { get; set; }
        public int Param_Write_Axis3HomeSpeedM { get; set; }
        public int Param_Write_Axis3HomeSpeedL { get; set; }
        public int Param_Write_Axis3HomeTimeoutValue_ms { get; set; }
        public int Param_Write_Axis3CommandTimeoutValue_ms { get; set; }

        public int Param_Write_Axis4JogSpeedH { get; set; }
        public int Param_Write_Axis4JogSpeedM { get; set; }
        public int Param_Write_Axis4JogSpeedL { get; set; }
        public int Param_Write_Axis4HomeSpeedH { get; set; }
        public int Param_Write_Axis4HomeSpeedM { get; set; }
        public int Param_Write_Axis4HomeSpeedL { get; set; }
        public int Param_Write_Axis4HomeTimeoutValue_ms { get; set; }
        public int Param_Write_Axis4CommandTimeoutValue_ms { get; set; }

        #endregion

    }
}
