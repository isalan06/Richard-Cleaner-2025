using CleanerControlApp.Modules.Motor.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Hardwares.Shuttle.Interfaces
{
    public interface IShuttle
    {
        bool IsRunning { get; }
        void Start();
        void Stop();

        ISingleAxisMotor? ShuttleXMotor { get; }
        ISingleAxisMotor? ShuttleZMotor { get; }

        bool Sensor_ClamperFrontOpen { get; }
        bool Sensor_ClamperFrontClose { get; }
        bool Sensor_ClamperBackOpen { get; }
        bool Sensor_ClamperBackClose { get; }
        bool Check_ClamperOpen { get; }
        bool Check_ClamperClose { get; }
        
        bool Sensor_CassetteExist1 { get; }
        bool Sensor_CassetteExist2 { get; }
        bool Check_ClamperCassetteExist { get; }
        bool Check_TankCassetteExist { get; }

        bool Command_ClamperClose { get; set; }
        bool Command_ClamperOpen { get; set; }


        bool Auto { get; }
        bool Pausing { get; }
        bool Moving { get; }
        bool Cassette { get; }
        bool Initialized { get; }
        bool Idle { get; }

        bool ClamperCloseOP(bool close);
        bool ManualClamperCloseOP(bool close);
        bool ClamperOpenOP(bool open);
        bool ManualClamperOpenOP(bool open);

        bool HasWarning { get; }
        bool HasAlarm { get; }
        bool IsNormalStatus { get; }
        void AutoStop();
        void WarningStop();
        void AlarmStop();
        void AutoStart();
        void AutoPause();
        void AlarmReset();
        void ModuleReset();

        void SimMotorPass();


        bool MotorServoOn { get; }
        bool MotorIdle { get; }
        bool MotorBusy { get; }
        bool MotorAlarm { get; }
        bool MotorHoming { get; }
        bool MotorMoving { get; }
        bool MotorHome { get; }

        bool HasCassette { get; }
        bool IsEmpty { get; }

        bool PickCassette(int position);
        bool PlaceCassette(int position);
        bool CheckTankCassetteExist();

        bool HS_Check_SinkCassetteExist { get; set; }
        bool HS_Check_SoakingTankCassetteExist { get; set; }
        bool HS_Check_DryingTank1CassetteExist { get; set; }
        bool HS_Check_DryingTank2CassetteExist { get; set; }

        bool HS_Check_Cassette_Finished { get; set; }

    }
}
