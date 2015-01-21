using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

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
        Processor processor;
        Video video;

        const int MAIN_CLOCK = 21477272; //21.28MHz(NTSC)
        const byte CPU_CLOCK_FACTOR = 12;
		const byte VIDEO_CLOCK_FACTOR = 4;

        uint clockDelta = 0;

        private bool resetFlag;
        private bool hardResetFlag;

        public VirtualMachine()
        {
            ram = new RAM(this);
            processor = new Processor(this);
            video = new Video(this);
            cartridge = null;
            resetFlag = false;
            hardResetFlag = false;
        }

        public byte Read(ushort addr)
        {
#if DEBUG
            return TestData[addr];
#else
            switch (addr & 0xE000)
            {
                case 0x0000:
                    return ram.Read(addr);
                case 0x2000:
                    return video.ReadReg(addr);
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
#endif
        }

        public void Write(ushort addr, byte value)
        {
#if DEBUG
            TestData[addr] = value; return;
#else
            switch (addr & 0xE000)
            {
                case 0x0000:
                    ram.Write(addr, value);
                    break;
                case 0x2000:
                    video.WriteReg(addr, value);
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
#endif
        }
#if DEBUG
        public byte[] TestData = new byte[65536];

        List<ushort> testAddr = new List<ushort>();

        public bool AddrExist(ushort addr)
        {
            return testAddr.Contains(addr);
        }

        public void Test()
        {
            var data = new byte[65536];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = 0xFF;
            }
            using (StreamReader stream = new StreamReader("op.txt"))
            {
                string str = stream.ReadLine();
                while (str != null)
                {
                    var bytes = str.Split(new string[] { ":", " " }, StringSplitOptions.RemoveEmptyEntries);
                    var addr = ushort.Parse(bytes[0], NumberStyles.HexNumber);
                    for (int i = 1; i < bytes.Length; i++)
                    {
                        testAddr.Add(addr);
                        data[addr] = byte.Parse(bytes[i], NumberStyles.HexNumber);
                        addr++;
                    }
                    str = stream.ReadLine();
                }
            }
            TestData = data;
        }
#endif
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
                        while (reader.PeekChar() >= 0)
                            data.Add(reader.ReadByte());
                    }
                    catch
                    { }
                }
            }

            this.cartridge = Cartridge.LoadCartridge(this, data.ToArray(), filename);
        }

        void ConsumeClock(uint clock)
        {
            this.clockDelta += clock;
        }

        public void ConsumeCPUClock(uint clock)
        {
            ConsumeClock(clock * CPU_CLOCK_FACTOR);
        }

        public void SendReset()
        {
            this.resetFlag = true;
        }

        public void SendHardReset()
        {
            this.hardResetFlag = true;
        }

        public void Run()
        {
            if (this.hardResetFlag)
            {
                this.clockDelta = 0;
                this.hardResetFlag = false;
                this.processor.OnHardReset();
                //this.cartridge.OnHardReset();
                return;
            }
            else if (this.resetFlag)
            {
                this.clockDelta = 0;
                this.resetFlag = false;
                this.processor.OnReset();
                //this.cartridge.OnReset();
                return;
            }

            ushort cpuClockDelta = (ushort)(this.clockDelta / CPU_CLOCK_FACTOR);
            this.clockDelta = 0;

            this.processor.Run(cpuClockDelta);

            //this.cartridge.Run(cpuClockDelta);
        }
    }
}
