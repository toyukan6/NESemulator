using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NESemulator
{
    /// <summary>
    /// エミュ本体
    /// パーツ同士を繋げたりしてる
    /// </summary>
    class VirtualMachine
    {
        RAM ram;

        Cartridge cartridge;

        const int MAIN_CLOCK = 21477272; //21.28MHz(NTSC)
        const byte CPU_CLOCK_FACTOR = 12;
		const byte VIDEO_CLOCK_FACTOR = 4;

        uint clockDelta = 0;


        public VirtualMachine()
        {
            ram = new RAM(this);
            cartridge = null;
        }

        public byte Read(ushort addr)
        {
            switch (addr & 0xE000)
            {
                case 0x0000:
                    ram.Read(addr);
                    return 0;
                case 0x6000:
                    return cartridge.ReadSram(addr);
                case 0x8000:
                case 0xA000:
                    return cartridge.ReadBankLow(addr);
                case 0xC000:
                case 0xE000:
                    return cartridge.ReadBankHigh(addr);
                default:
                    return 0;
            }
        }

        public void Write(ushort addr, byte value)
        {
            switch (addr & 0xE000)
            {
                case 0x0000:
                    ram.Write(addr, value);
                    break;
                case 0x6000:
                    cartridge.WriteSram(addr, value);
                    break;
                case 0x8000:
                case 0xA000:
                    cartridge.WriteBankLow(addr, value);
                    break;
                case 0xC000:
                case 0xE000:
                    cartridge.WriteBankHigh(addr, value);
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// nesファイルとかを読みこむ
        /// </summary>
        /// <param name="filename">ファイル名</param>
        public void LoadCartridge(string filename)
        {
            var data = new List<byte>();
            using (FileStream stream = new FileStream(filename, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    try
                    {
                        while(true)
                            data.Add(reader.ReadByte());
                    }
                    catch
                    { }
                }
            }


        }

        void ConsumeClock(uint clock)
        {
            this.clockDelta += clock;
        }

        public void ConsumeCPUClock(uint clock)
        {
            ConsumeClock(clock * CPU_CLOCK_FACTOR);
        }
    }
}
