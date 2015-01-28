using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NESemulator
{
    class Mapper0 : Cartridge
    {
        ushort addrMask;

        bool hasChrRam;

        byte[] chrRam;

        public Mapper0(VirtualMachine vm, NesFile nes)
            : base(vm, nes)
        {
            this.addrMask = (ushort)(nes.PrgPageCnt > 1 ? 0x7fff : 0x3fff);
            this.hasChrRam = nesFile.ChrPageCnt == 0;
            if (this.hasChrRam)
            {
                chrRam = new byte[8192];
                for (int i = 0; i < chrRam.Length; i++)
                    chrRam[i] = 0;
            }
        }

        public override byte ReadPatternTableHigh(ushort addr)
        {
            if (this.hasChrRam)
                return this.chrRam[addr & 0x1fff];
            else
                return this.nesFile.ChrRom[addr & 0x1fff];
        }

        public override void WritePatternTableHigh(ushort addr, byte val)
        {
            if (this.hasChrRam)
                this.chrRam[addr & 0x1fff] = val;
        }

        public override byte ReadPatternTableLow(ushort addr)
        {
            if (this.hasChrRam)
                return this.chrRam[addr & 0x1fff];
            else
                return this.nesFile.ChrRom[addr & 0x1fff];
        }

        public override void WritePatternTableLow(ushort addr, byte val)
        {
            if (this.hasChrRam)
                this.chrRam[addr & 0x1fff] = val;
        }

        public override byte ReadBankHigh(ushort addr)
        {
            return this.nesFile.PrgRom[addr & addrMask];
        }

        public override void WriteBankHigh(ushort addr, byte val) { }

        public override byte ReadBankLow(ushort addr)
        {
            return this.nesFile.PrgRom[addr & addrMask];
        }

        public override void WriteBankLow(ushort addr, byte val) { }
    }
}
