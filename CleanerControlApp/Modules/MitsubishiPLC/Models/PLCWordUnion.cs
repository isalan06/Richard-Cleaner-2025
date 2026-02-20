using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.MitsubishiPLC.Models
{
    public struct PLC_Word_Union
    {
        // 儲存整個 Word 值 (16-bit)
        public ushort Data { get; set; }

        //以 signed16-bit (short) 解讀，對外型別為 int以方便使用
        public int IntValue
        {
            get => (short)Data; //以 signed16-bit 解讀
            set
            {
                if (value < short.MinValue || value > short.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(value), $"IntValue 必須在 {short.MinValue}..{short.MaxValue}之間");
                Data = (ushort)(short)value;
            }
        }

        //以 unsigned16-bit 解讀，對外型別為 uint
        public uint UIntValue
        {
            get => Data;
            set
            {
                if (value > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(value), $"UIntValue 必須在0..{ushort.MaxValue}之間");
                Data = (ushort)value;
            }
        }

        // 建構子
        public PLC_Word_Union(ushort data)
        {
            Data = data;
        }

        public PLC_Word_Union(int intValue)
        {
            if (intValue < short.MinValue || intValue > short.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(intValue));
            Data = (ushort)(short)intValue;
        }

        public PLC_Word_Union(uint uintValue)
        {
            if (uintValue > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(uintValue));
            Data = (ushort)uintValue;
        }

        public override string ToString()
        {
            return $"0x{Data:X4} (UInt={UIntValue}, Int={IntValue})";
        }
    }
}
