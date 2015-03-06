using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace NESemulator
{
    /// <summary>
    /// エミュ本体
    /// パーツ同士を繋げたりしてる
    /// </summary>
    class VirtualMachine
    {
        public RAM Ram { get; private set; }
        public Cartridge Cartridge { get; private set; }
        public Processor Processor { get; private set; }
        public Video Video { get; private set; }
        public GraphicsDevice Device { get; private set; }
        public IOPort IOPort { get; private set; }

        const int MAIN_CLOCK = 21477272; //21.28MHz(NTSC)
        const byte CPU_CLOCK_FACTOR = 12;
		const byte VIDEO_CLOCK_FACTOR = 4;

        uint clockDelta = 0;

        private bool resetFlag;
        private bool hardResetFlag;

        bool oneFrame;

        Stopwatch sw;

        public VirtualMachine()
        {
            Ram = new RAM(this);
            Processor = new Processor(this);
            Video = new Video(this);
            Cartridge = null;
            IOPort = new IOPort(this);
            resetFlag = false;
            hardResetFlag = false;
            oneFrame = true;
            sw = new Stopwatch();
        }

        public byte Read(ushort addr)
        {
            switch (addr & 0xE000)
            {
                case 0x0000:
                    return Ram.Read(addr);
                case 0x2000:
                    return Video.ReadReg(addr);
                case 0x4000:
                    if (addr == 0x4015)
                        //return audio.readReg(addr);
                        return 0;
                    else if (addr == 0x4016)
                        return IOPort.ReadInputReg1();
                    else if (addr == 0x4017)
                        return IOPort.ReadInputReg2();
                    else if (addr < 0x4018)
                        throw new EmulatorException("[FIXME] Invalid addr: 0x" + addr.ToString("x2"));
                    else
                        return Cartridge.ReadRegisterArea(addr);
                case 0x6000:
                    return Cartridge.ReadSaveArea(addr);
                case 0x8000:
                case 0xA000:
                    return Cartridge.ReadBankLow(addr);
                case 0xC000:
                case 0xE000:
                    return Cartridge.ReadBankHigh(addr);
                default:
                    return 0;
            }
        }

        public void Write(ushort addr, byte val)
        {
            switch (addr & 0xE000)
            {
                case 0x0000:
                    Ram.Write(addr, val);
                    break;
                case 0x2000:
                    Video.WriteReg(addr, val);
                    break;
                case 0x4000:
                    if (addr == 0x4014)
                        Video.ExecuteDMA(val);
                    else if (addr == 0x4016)
                        IOPort.WriteOutReg(val);
                    else if (addr < 0x4018) { }
                    //audio.writeReg(addr, value);
                    else
                        Cartridge.WriteRegisterArea(addr, val);
                    break;
                case 0x6000:
                    Cartridge.WriteSaveArea(addr, val);
                    break;
                case 0x8000:
                case 0xA000:
                    Cartridge.WriteBankLow(addr, val);
                    break;
                case 0xC000:
                case 0xE000:
                    Cartridge.WriteBankHigh(addr, val);
                    break;
                default:
                    break;
            }
        }

        public void SendNMI()
        {
            this.Processor.SendNMI();
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
                        while (true)
                            data.Add(reader.ReadByte());
                    }
                    catch (EndOfStreamException e)
                    { }
                }
            }

            this.Cartridge = Cartridge.LoadCartridge(this, data.ToArray(), filename);
            this.Video.ConnectCartridge(this.Cartridge);
        }

        public void InitVideoTexture(GraphicsDevice graphics)
        {
            this.Video.InitTexture(graphics);
        }

        void ConsumeClock(uint clock)
        {
            this.clockDelta += clock;
        }

        public void FrameEnd()
        {
            this.oneFrame = false;
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

        public void SendVBlank()
        {
            this.IOPort.OnVBlank();
        }

        public void Run()
        {
            if (this.hardResetFlag)
            {
                this.clockDelta = 0;
                this.hardResetFlag = false;
                this.oneFrame = true;
                this.Ram.OnHardReset();
                this.Processor.OnHardReset();
                this.Video.OnHardReset();
                //this.cartridge.OnHardReset();
                return;
            }
            else if (this.resetFlag)
            {
                this.clockDelta = 0;
                this.resetFlag = false;
                this.oneFrame = true;
                this.Ram.OnReset();
                this.Processor.OnReset();
                this.Video.OnReset();
                //this.cartridge.OnReset();
                return;
            }

            this.oneFrame = true;

            while (oneFrame)
            {
                ushort cpuClockDelta = (ushort)(this.clockDelta / CPU_CLOCK_FACTOR);
                ushort videoClockDelta = (ushort)(this.clockDelta / VIDEO_CLOCK_FACTOR);
                this.clockDelta = 0;

                sw.Start();
                this.Processor.Run(cpuClockDelta);
                sw.Stop();
                var delta = sw.ElapsedMilliseconds;
                sw.Reset();

                this.Video.Run(videoClockDelta);
            }

            //this.cartridge.Run(cpuClockDelta);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            this.Video.Draw(spriteBatch);
        }
    }
}
