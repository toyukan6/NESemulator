using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NESemulator
{
    /// <summary>
    /// カートリッジのベースクラス
    /// カートリッジには色々な形態があるのよ
    /// </summary>
    class Cartridge
    {
        const int SRAM_SIZE = 8192;

        protected NesFile nesFile;

        VirtualMachine vm;
        /// <summary>
        /// SRAMを使うかどうか
        /// </summary>
        bool hasSram;
        /// <summary>
        /// セーブ用のRAM
        /// </summary>
        byte[] sram = new byte[SRAM_SIZE]; 
        ushort addrMask;

        public Cartridge(VirtualMachine vm, NesFile nes)
        {
            this.vm = vm;
            this.nesFile = nes;
            this.hasSram = nes.SramFlag;
            this.addrMask = (ushort)(nes.PrgPageCnt > 1 ? 0x7fff : 0x3fff);
            if (nesFile == null) throw new EmulatorException("NES FILE CAN'T BE NULL!");
        }

        public byte ReadBankHigh(ushort addr)
        {
            return this.nesFile.PrgRom[addr];
        }
        public void WriteBankHigh(ushort addr, byte val)
        {
            //have no effect
        }
        public byte ReadBankLow(ushort addr)
        {
            return this.nesFile.PrgRom[addr & addrMask];
        }
        public void WriteBankLow(ushort addr, byte val)
        {
            //have no effect
        }
        public byte ReadSram(ushort addr)
        {
            if (hasSram)
                return this.sram[addr & 0x1fff];
            else
                return 0;
        }
        public void WriteSram(ushort addr, byte val)
        {
            if (hasSram)
                this.sram[addr & 0x1fff] = val;
        }

        public static Cartridge LoadCartridge(VirtualMachine vm, byte[] data, string name = "MEMORY")
        {
            NesFile nes = new NesFile(data, name);
            var mapperNo = nes.MapperNo;
            switch (mapperNo)
            {
                case 0: break;
                case 1: break;
                case 2: break;
                case 3: break;
                case 4: break;
                case 21: break;
                case 23: break;
                case 25: break;
                default:
                    throw new EmulatorException("Not Supported Mapper: " + mapperNo + "!");
            }
            return null;
        }
    }
}
