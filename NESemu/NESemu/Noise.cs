using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NESemulator
{
    class Noise : AudioChannel
    {
        ushort[] frequencyTable = new ushort[]
        {
            4, 8, 16, 32, 64, 96, 128, 160, 202, 254, 380, 508, 762, 1016, 2034, 4068
        };

        //rand
        ushort shiftRegister;
        bool modeFlag;

        //decay
        byte volumeOrDecayRate;
        bool decayReloaded;
        bool decayEnabled;

        byte decayCounter;
        byte decayVolume;
        //
        bool loopEnabled;
        ushort frequency;
        //
        ushort lengthCounter;
        //
        ushort freqCounter;

        public override void OnHardReset()
        {
            //rand
            shiftRegister = 1 << 14;
            modeFlag = false;

            //decay
            volumeOrDecayRate = 0;
            decayReloaded = false;
            decayEnabled = false;

            decayCounter = 0;
            decayVolume = 0;
            //
            loopEnabled = false;
            frequency = 0;
            //
            lengthCounter = 0;
            //
            freqCounter = 0;
        }

        public override void OnReset()
        {
            OnHardReset();
        }

        public override void SetEnabled(bool enabled)
        {
            if (!enabled) lengthCounter = 0;
        }

        public override bool IsEnabled()
        {
            return lengthCounter != 0;
        }

        public void AnalyzeVolumeRegister(byte val)
        {
            volumeOrDecayRate = (byte)(val & 15);
            decayCounter = volumeOrDecayRate;
            decayEnabled = (val & 16) == 0;
            loopEnabled = (val & 32) == 32;
        }

        public void AnalyzeFrequencyRegister(byte val)
        {
            modeFlag = (val & 128) == 128;
            frequency = frequencyTable[val & 15];
        }

        public void AnalyzeLengthRegister(byte val)
        {
            //Writing to the length registers restarts the length (obviously),
            lengthCounter = LengthCounterConst[val >> 3];
            //and restarts the decay volume (channel 1,2,4 only).
            decayReloaded = true;
        }

        public override void OnQuaterFrame()
        {
            if (decayCounter == 0)
            {
                decayCounter = this.volumeOrDecayRate;
                if (decayVolume == 0)
                {
                    if (loopEnabled)
                        decayVolume = 0xf;
                }
                else
                {
                    decayVolume--;
                }
            }
            else
            {
                this.decayCounter--;
            }

            if (decayReloaded)
            {
                decayReloaded = false;
                decayVolume = 0xf;
            }
        }

        public override void OnHalfFrame()
        {
            if (lengthCounter != 0 && !loopEnabled)
                lengthCounter--;
        }
    }
}
