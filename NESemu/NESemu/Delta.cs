using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NESemulator
{
    class Delta : AudioChannel
    {
        ushort[] frequencyTable = new ushort[]
        {
            428, 380, 340, 320, 286, 254, 226, 214, 190, 160, 142, 128, 106,  84,  72,  54 //NTSC
        };

        VirtualMachine vm;
        //
        bool loopEnabled;
        ushort frequency;
        byte deltaCounter;
        ushort sampleAddr;
        ushort sampleLength;
        ushort sampleLengthBuffer;
        //
        byte sampleBuffer;
        byte sampleBufferLeft;
        //
        ushort freqCounter;

        public override void OnHardReset()
        {
        }

        public override void OnReset()
        {
        }

        public void AnalyzeFrequencyRegister(byte val)
        {
            loopEnabled = (val & 64) == 64;
            frequency = frequencyTable[val & 0xf];
        }

        public void AnalyzeDeltaCounterRegister(byte val)
        {
            deltaCounter = (byte)(val & 0x7f);
        }

        public void AnalyzeSampleAddressRegister(byte val)
        {
            sampleAddr = (ushort)(0xc000 | (val << 6));
        }

        public void AbalyzeSampleLengthRegister(byte val)
        {
            sampleLength = sampleLengthBuffer = (ushort)((val << 4) | 1);
        }

        public override void SetEnabled(bool enabled)
        {
            if (!enabled)
                sampleLength = 0;
            else if (sampleLength == 0)
                sampleLength = sampleLengthBuffer;
        }

        public override bool IsEnabled()
        {
            return sampleLength != 0;
        }

        public override void OnQuaterFrame() { }
        public override void OnHalfFrame() { }
    }
}
