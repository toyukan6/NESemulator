using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NESemulator
{
    abstract class AudioChannel : IReset
    {
        public static byte[] LengthCounterConst = new byte[]
        {
            0x0A,0xFE,0x14,0x02,0x28,0x04,0x50,0x06,
            0xA0,0x08,0x3C,0x0A,0x0E,0x0C,0x1A,0x0E,
            0x0C,0x10,0x18,0x12,0x30,0x14,0x60,0x16,
            0xC0,0x18,0x48,0x1A,0x10,0x1C,0x20,0x1E
        };

        public abstract void OnHardReset();
        public abstract void OnReset();
        public abstract bool IsEnabled();
        public abstract void SetEnabled(bool enabled);
        public abstract void OnQuaterFrame();
        public abstract void OnHalfFrame();
    }
}
