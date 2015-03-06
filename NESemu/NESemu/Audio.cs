using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Audio;

namespace NESemulator
{
    class Audio : IReset
    {
        Rect rectangle1;
        Rect rectangle2;
        Triangle triangle;
        Noise noise;
        Delta delta;

        VirtualMachine vm;

        const int AUDIO_CLOCK = 21477272/12;
		const int SAMPLE_RATE = 44100;
        const int FRAME_IRQ_RATE = 240;

        uint frameCount;
        uint leftClock;
        uint clockCount;

        bool isNTSCmode;
        bool frameIRQenabled;
        byte frameIRQCount;

        DynamicSoundEffectInstance dse;

        public Audio(VirtualMachine vm)
        {
            this.vm = vm;
            rectangle1 = new Rect(true);
            rectangle2 = new Rect(false);
            triangle = new Triangle();
            noise = new Noise();
            delta = new Delta();
            frameCount = 0;
            leftClock = 0;
            clockCount = 0;
            frameIRQenabled = true;
            frameIRQCount = 0;
            isNTSCmode = true;
            dse = new DynamicSoundEffectInstance(22000, AudioChannels.Mono);
        }

        public void OnHardReset()
        {
            rectangle1.OnHardReset();
            rectangle2.OnHardReset();
            triangle.OnHardReset();
            noise.OnHardReset();
            delta.OnHardReset();
            frameCount = 0;
            leftClock = 0;
            clockCount = 0;
            frameIRQenabled = true;
            frameIRQCount = 0;
            isNTSCmode = true;
            dse.Stop();
            dse.Play();
        }

        public void OnReset()
        {
            rectangle1.OnReset();
            rectangle2.OnReset();
            triangle.OnReset();
            noise.OnReset();
            delta.OnReset();
            dse.Stop();
            dse.Play();
        }

        public void Run(ushort clockDelta)
        {
        }

        public byte ReadReg(ushort addr)
        {
            if (addr != 0x4015)
                throw new EmulatorException("[FIXME] Invalid addr: 0x" + addr.ToString("x") + "for APU.");

            byte ret = (byte)(
                (this.rectangle1.IsEnabled() ? 1 : 0)
                | (this.rectangle2.IsEnabled() ? 1 : 0)
                | (this.triangle.IsEnabled() ? 4 : 0)
                | (this.noise.IsEnabled() ? 8 : 0)
                | (this.delta.IsEnabled() ? 16 : 0));

            return ret;
        }

        void AnalyzeStatusRegister(byte val)
        {
            rectangle1.SetEnabled((val & 1) == 1);
            rectangle2.SetEnabled((val & 2) == 2);
            triangle.SetEnabled((val & 4) == 4);
            noise.SetEnabled((val & 8) == 8);
            delta.SetEnabled((val & 16) == 16);
        }
    }
}
