using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NESemulator
{
    class Triangle : AudioChannel
    {
        byte[] waveForm = new byte[32]
        {
		  0x0,0x1,0x2,0x3,0x4,0x5,0x6,0x7,0x8,0x9,0xA,0xB,0xC,0xD,0xE,0xF,
		  0xF,0xE,0xD,0xC,0xB,0xA,0x9,0x8,0x7,0x6,0x5,0x4,0x3,0x2,0x1,0x0
        };

        bool haltFlag;

        bool enableLinearCounter;
        ushort frequency;
        ushort linearCounterBuffer;
        //
        ushort linearCounter;
        ushort lengthCounter;
        //
        ushort freqCounter;
        ushort streamCounter;

        public override void OnHardReset()
        {
            haltFlag = false;
            enableLinearCounter = false;
            frequency = 0;
            linearCounterBuffer = 0;
            linearCounter = 0;
            lengthCounter = 0;
            freqCounter = 0;
            streamCounter = 0;
        }

        public override void OnReset()
        {
            OnHardReset();
        }

        public void AnalyzeLinearCounterRegister(byte val)
        {
            enableLinearCounter = ((val & 128) == 128);
            linearCounterBuffer = (ushort)(val & 127);
        }

        public override void SetEnabled(bool enabled)
        {
            if (!enabled)
            {
                lengthCounter = 0;
                linearCounterBuffer = 0;
                linearCounter = linearCounterBuffer;
            }
        }

        public override bool IsEnabled()
        {
            return lengthCounter != 0 && linearCounter != 0;
        }

        public override void OnQuaterFrame()
        {
            if (haltFlag)
                linearCounter = linearCounterBuffer;
            else if (linearCounter != 0)
                linearCounter--;

            if (!enableLinearCounter)
                haltFlag = false;
        }

        public override void OnHalfFrame()
        {
            if (lengthCounter != 0 && !enableLinearCounter)
                lengthCounter--;
        }
    }
}
