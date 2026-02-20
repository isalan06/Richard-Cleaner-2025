using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.MitsubishiPLC.Models
{
    public struct PLC_DWord_Union
    {
        // 儲存兩個16-bit word，預設為 little-endian：Data[0]=低位元字（低16位），Data[1]=高位元字（高16位）
        public ushort[] Data { get; set; }

        public PLC_DWord_Union(ushort lowWord, ushort highWord)
        {
            Data = new ushort[2];
            Data[0] = lowWord;
            Data[1] = highWord;
        }

        public PLC_DWord_Union(ushort[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length != 2) throw new ArgumentException("Data array must have length 2", nameof(data));
            Data = new ushort[2];
            Data[0] = data[0];
            Data[1] = data[1];
        }

        public PLC_DWord_Union(uint uintValue)
        {
            Data = new ushort[2];
            Data[0] = (ushort)(uintValue & 0xFFFF);
            Data[1] = (ushort)((uintValue >> 16) & 0xFFFF);
        }

        public PLC_DWord_Union(int intValue)
        {
            uint u = unchecked((uint)intValue);
            Data = new ushort[2];
            Data[0] = (ushort)(u & 0xFFFF);
            Data[1] = (ushort)((u >> 16) & 0xFFFF);
        }

        //低位元字與高位元字
        public ushort LowWord
        {
            get => Data != null && Data.Length > 0 ? Data[0] : (ushort)0;
            set
            {
                if (Data == null || Data.Length != 2) Data = new ushort[2];
                Data[0] = value;
            }
        }

        public ushort HighWord
        {
            get => Data != null && Data.Length > 1 ? Data[1] : (ushort)0;
            set
            {
                if (Data == null || Data.Length != 2) Data = new ushort[2];
                Data[1] = value;
            }
        }

        // 新增：同時設定低位元字與高位元字
        public void Set(ushort value_low, ushort value_high)
        {
            if (Data == null || Data.Length != 2) Data = new ushort[2];
            Data[0] = value_low;
            Data[1] = value_high;
        }

        //以 unsigned32-bit 解讀/寫入
        public uint UIntValue
        {
            get
            {
                uint low = LowWord;
                uint high = HighWord;
                return (high << 16) | low;
            }
            set
            {
                LowWord = (ushort)(value & 0xFFFF);
                HighWord = (ushort)((value >> 16) & 0xFFFF);
            }
        }

        //以 signed32-bit 解讀/寫入
        public int IntValue
        {
            get => unchecked((int)UIntValue);
            set => UIntValue = unchecked((uint)value);
        }

        public override string ToString()
        {
            return $"0x{UIntValue:X8} (High=0x{HighWord:X4}, Low=0x{LowWord:X4})";
        }
    }
}
