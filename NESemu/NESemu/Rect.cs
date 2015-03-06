using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NESemulator
{
    class Rect : AudioChannel
    {
        private bool isFirst;

        byte volumeOrDecayRate;

        bool decayReloaded;
        bool decayEnabled;
        byte dutyRatio;

        byte decayCounter;

        byte decayVolume;

        //sweep
        bool sweepEnabled;
        byte sweepShiftAmount;
        bool sweepIncreased;
        byte sweepUpdateRatio;

        byte sweepCounter;

        //
        ushort frequency;
        bool loopEnabled;
        byte lengthCounter;

        //
        ushort freqCounter;
        ushort dutyCounter;


        public Rect(bool isFirst)
            : base()
        {
            this.isFirst = isFirst;
        }

        public override void OnHardReset()
        {
            volumeOrDecayRate = 0;
            decayReloaded = false;
            decayEnabled = false;
            decayVolume = 0;
            dutyRatio = 0;
            dutyCounter = 0;
            decayCounter = 0;
            sweepEnabled = false;
            sweepShiftAmount = 0;
            sweepIncreased = false;
            sweepUpdateRatio = 0;
            sweepCounter = 0;
            frequency = 0;
            freqCounter = 0;
            loopEnabled = false;
            lengthCounter = 0;
        }

        public void AnalyzeVolumeRegister(byte val)
        {
            volumeOrDecayRate = (byte)(val & 15);
            decayCounter = volumeOrDecayRate;
            decayEnabled = (val & 16) == 0;
            loopEnabled = (val & 32) == 32;
            var duty = val >> 6;
            if (duty == 0) dutyRatio = 2;
            else if (duty == 1) dutyRatio = 4;
            else if (duty == 2) dutyRatio = 8;
            else if (duty == 3) dutyRatio = 12;
        }

        public void AnalyzeSweepRegister(byte val)
        {
            sweepShiftAmount = (byte)(val & 7);
            sweepIncreased = (val & 0x8) == 0x0;
            sweepUpdateRatio = (byte)((val >> 4) & 3);
            sweepCounter = sweepUpdateRatio;
            sweepEnabled = (val & 0x80) == 0x80;
        }

        public void AnayzeFrequencyRegister(byte val)
        {
            frequency = (ushort)((frequency & 0x0700) | val);
        }

        public void AnalyzeLengthRegister(byte val)
        {
            frequency = (ushort)((frequency & 0x00ff) | ((val & 7) << 8));
            lengthCounter = LengthCounterConst[val >> 3];
            //Writing to the length registers restarts the length (obviously),
            //and also restarts the duty cycle (channel 1,2 only),
            dutyCounter = 0;
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

            if (sweepEnabled)
            {
                if (sweepCounter == 0)
                {
                    this.sweepCounter = sweepUpdateRatio;
                    if (lengthCounter != 0 && this.sweepShiftAmount != 0)
                    {
                        ushort shift = (ushort)(this.frequency >> this.sweepShiftAmount);
                        if (this.sweepIncreased)
                        {
                            this.frequency += shift;
                        }
                        else
                        {
                            this.frequency -= shift;
                            if (this.isFirst)
                                this.frequency--;
                        }
                    }
                }
                else
                {
                    this.sweepCounter--;
                }
            }
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
            return lengthCounter != 0 && this.frequency >= 0x8 && this.frequency < 0x800;
        }
    }
}
