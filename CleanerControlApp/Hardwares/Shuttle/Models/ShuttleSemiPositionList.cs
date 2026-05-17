using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Hardwares.Shuttle.Models
{
    public class ShuttleSemiPositionList
    {
        public static string[] Names = new string[]
        {
            "P01-載入槽#1位置",
            "P02-載入槽#2位置",
            "P03-載入槽#3位置",
            "P04-載入槽#4位置",
            "P05-載入槽#5位置",
            "P06-沖水槽位置",
            "P07-浸泡槽位置",
            "P08-烘乾槽#1位置",
            "P09-烘乾槽#2位置",
            "P10-載出槽#1位置",
            "P11-載出槽#2位置",
            "P12-載出槽#3位置",
            "P13-載出槽#4位置",
            "P14-載出槽#5位置"
        };

        public static void GetSemiPositionTransferToRealPoint(int semiPosIndex, out int shuttleXPosIndex, out int shuttleZPosIndex)
        {
            shuttleXPosIndex = -1; 
            shuttleZPosIndex = -1;
            if (semiPosIndex == 0) { shuttleXPosIndex = 1; shuttleZPosIndex = 1; } //P01-載入槽#1位置 
            if (semiPosIndex == 1) { shuttleXPosIndex = 2; shuttleZPosIndex = 1; } //P02-載入槽#1位置 
            if (semiPosIndex == 2) { shuttleXPosIndex = 3; shuttleZPosIndex = 1; } //P03-載入槽#1位置 
            if (semiPosIndex == 3) { shuttleXPosIndex = 4; shuttleZPosIndex = 1; } //P04-載入槽#1位置 
            if (semiPosIndex == 4) { shuttleXPosIndex = 5; shuttleZPosIndex = 1; } //P05-載入槽#1位置 
            if (semiPosIndex == 5) { shuttleXPosIndex = 6; shuttleZPosIndex = 2; } //P06-沖水槽位置
            if (semiPosIndex == 6) { shuttleXPosIndex = 7; shuttleZPosIndex = 3; } //P07-浸泡槽位置
            if (semiPosIndex == 7) { shuttleXPosIndex = 8; shuttleZPosIndex = 4; } //P08-烘乾槽#1位置
            if (semiPosIndex == 8) { shuttleXPosIndex = 9; shuttleZPosIndex = 5; } //P09-烘乾槽#2位置
            if (semiPosIndex == 9) { shuttleXPosIndex = 10; shuttleZPosIndex = 6; } //P10-載出槽#1位置
            if (semiPosIndex == 10) { shuttleXPosIndex = 11; shuttleZPosIndex = 6; } //P11-載出槽#2位置
            if (semiPosIndex == 11) { shuttleXPosIndex = 12; shuttleZPosIndex = 6; } //P12-載出槽#3位置
            if (semiPosIndex == 12) { shuttleXPosIndex = 13; shuttleZPosIndex = 6; } //P13-載出槽#4位置
            if (semiPosIndex == 13) { shuttleXPosIndex = 14; shuttleZPosIndex = 6; } //P14-載出槽#5位置

        }
    }
}
