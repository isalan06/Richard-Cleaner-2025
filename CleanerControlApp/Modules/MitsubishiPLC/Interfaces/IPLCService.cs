using CleanerControlApp.Modules.MitsubishiPLC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.MitsubishiPLC.Interfaces
{
    public interface IPLCService
    {
        // DIO_X 改為陣列（可讀寫），代表多個 PLC_Bit_Union 條目
        public PLC_Bit_Union[] DIO_X { get; set; }

        // 新增 DIO_Y（數位輸出）陣列
        public PLC_Bit_Union[] DIO_Y { get; set; }

        // Status IO 陣列（可讀寫）
        public PLC_Bit_Union[] StatusIO { get; set; }

        // Motion positions (DWord) 陣列
        public PLC_DWord_Union[] MotionPos { get; set; }

        // Command (bit unions) 陣列
        public PLC_Bit_Union[] Command { get; set; }

        // MoveInfo (DWord) 陣列
        public PLC_DWord_Union[] MoveInfo { get; set; }

        // ParamMotionInfo (DWord) 陣列
        public PLC_DWord_Union[] ParamMotionInfo { get; set; }

        // ParamTimeout (Word) 陣列
        public PLC_Word_Union[] ParamTimeout { get; set; }

        // ParamMotionInfoW (DWord) 陣列
        public PLC_DWord_Union[] ParamMotionInfoW { get; set; }

        // ParamTimeoutW (Word) 陣列
        public PLC_Word_Union[] ParamTimeoutW { get; set; }

        public bool IsRunning { get; }

        public void Start();

        public void Stop();

        public void ReadParameter();

        public void WriteParameter();

        // Event fired when parameter read operation completes (successful or not)
        public event EventHandler? ParametersReadCompleted;

        // Event fired when parameter write operation completes (successful or not)
        public event EventHandler? ParametersWriteCompleted;
    }
}
