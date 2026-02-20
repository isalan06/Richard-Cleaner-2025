using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.MitsubishiPLC.Models
{
    // 以 ushort Data 為基底，提供各 bit 的 get/set 屬性與 indexer
    public struct PLC_Bit_Union
    {
        // 儲存整個16-bit 值
        public ushort Data { get; set; }

        // 索引器：以0..15 表示 bit位置
        public bool this[int bitIndex]
        {
            get => GetBit(bitIndex);
            set => SetBit(bitIndex, value);
        }

        //取得指定 bit
        public bool GetBit(int bitIndex)
        {
            if (bitIndex < 0 || bitIndex > 15) throw new ArgumentOutOfRangeException(nameof(bitIndex));
            return (Data & (1u << bitIndex)) != 0;
        }

        // 設定指定 bit
        public void SetBit(int bitIndex, bool value)
        {
            if (bitIndex < 0 || bitIndex > 15) throw new ArgumentOutOfRangeException(nameof(bitIndex));
            var mask = (ushort)(1u << bitIndex);
            if (value)
            {
                Data = (ushort)(Data | mask);
            }
            else
            {
                Data = (ushort)(Data & (ushort)~mask);
            }
        }

        // 個別位元屬性，方便呼叫
        public bool Bit0 { get => GetBit(0); set => SetBit(0, value); }
        public bool Bit1 { get => GetBit(1); set => SetBit(1, value); }
        public bool Bit2 { get => GetBit(2); set => SetBit(2, value); }
        public bool Bit3 { get => GetBit(3); set => SetBit(3, value); }
        public bool Bit4 { get => GetBit(4); set => SetBit(4, value); }
        public bool Bit5 { get => GetBit(5); set => SetBit(5, value); }
        public bool Bit6 { get => GetBit(6); set => SetBit(6, value); }
        public bool Bit7 { get => GetBit(7); set => SetBit(7, value); }
        public bool Bit8 { get => GetBit(8); set => SetBit(8, value); }
        public bool Bit9 { get => GetBit(9); set => SetBit(9, value); }
        public bool Bit10 { get => GetBit(10); set => SetBit(10, value); }
        public bool Bit11 { get => GetBit(11); set => SetBit(11, value); }
        public bool Bit12 { get => GetBit(12); set => SetBit(12, value); }
        public bool Bit13 { get => GetBit(13); set => SetBit(13, value); }
        public bool Bit14 { get => GetBit(14); set => SetBit(14, value); }
        public bool Bit15 { get => GetBit(15); set => SetBit(15, value); }

        // 建構子
        public PLC_Bit_Union(ushort data)
        {
            Data = data;
        }

        //方便顯示
        public override string ToString()
        {
            return Convert.ToString(Data, 2).PadLeft(16, '0');
        }
    }
}
