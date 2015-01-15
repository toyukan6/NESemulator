using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NESemulator
{
    /// <summary>
    /// RAM
    /// </summary>
    class RAM : IReset
    {
        byte[] wram = new byte[WRAM_LENGTH];

        public const int WRAM_LENGTH = 2048;

        readonly VirtualMachine vm;

        public RAM(VirtualMachine vm)
        {
            this.vm = vm;
        }
        /// <summary>
        /// 電源オン時のリセット
        /// </summary>
        public void OnHardReset()
        {
            for (int i = 0; i < wram.Length; i++)
                wram[i] = 0xff;
            wram[0x8] = 0xf7;
            wram[0x9] = 0xef;
            wram[0xa] = 0xdf;
            wram[0xf] = 0xbf;
        }
        /// <summary>
        /// リセットボタンでのリセット
        /// </summary>
        public void OnReset()
        {
        }
        public byte Read(ushort addr)
        {
            return wram[addr & 0x7ff];
        }
        public void Write(ushort addr, byte value)
        {
            wram[addr & 0x7ff] = value;
        }
    }
}
