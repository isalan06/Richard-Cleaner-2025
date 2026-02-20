using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
