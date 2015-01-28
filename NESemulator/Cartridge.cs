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
    abstract class Cartridge : IReset
    {
        const int SRAM_SIZE = 8192;

        protected NesFile nesFile;

        MirrorType mirrorType;

        byte[,] vramMirroring;

        VirtualMachine vm;
        /// <summary>
        /// SRAMを使うかどうか
        /// </summary>
        bool hasSram;
        /// <summary>
        /// セーブ用のRAM
        /// </summary>
        byte[] sram = new byte[SRAM_SIZE];

        byte[] internalVram;

        byte[] fourScreenVram;

        public Cartridge(VirtualMachine vm, NesFile nes)
        {
            this.vm = vm;
            this.nesFile = nes;
            this.hasSram = nes.SramFlag;
            mirrorType = nes.MirrorType;
            if (nesFile == null) throw new EmulatorException("NES FILE CAN'T BE NULL!");
        }

        public virtual byte ReadPatternTableLow(ushort addr);

        public virtual void WritePatternTableLow(ushort addr, byte val);

        public virtual byte ReadPatternTableHigh(ushort addr);

        public virtual void WritePatternTableHigh(ushort addr, byte val);

        public virtual byte ReadNameTable(ushort addr);

        public virtual void WriteNameTable(ushort addr, byte val);

        public virtual byte ReadRegisterArea(ushort addr)
        {
            return (byte)(addr >> 8);
        }

        public virtual void WriteRegisterArea(ushort addr, byte val);

        public virtual byte ReadSaveArea(ushort addr)
        {
            return ReadSram(addr);
        }

        public virtual void WriteSaveArea(ushort addr, byte val)
        {
            WriteSram(addr, val);
        }

        public virtual byte ReadBankHigh(ushort addr);

        public virtual void WriteBankHigh(ushort addr, byte val);

        public virtual byte ReadBankLow(ushort addr);

        public virtual void WriteBankLow(ushort addr, byte val);

        public void ConnectInternalVram(byte[] internalVram)
        {
            this.internalVram = internalVram;
            ChangeMirrorType(this.mirrorType);
        }

        public void ChangeMirrorType(MirrorType mirrorType)
        {
        }

        protected byte ReadSram(ushort addr)
        {
            if (hasSram)
                return this.sram[addr & 0x1fff];
            else
                return 0;
        }

        protected void WriteSram(ushort addr, byte val)
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
                case 0:
                    return new Mapper0(vm, nes);
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

        public virtual void OnHardReset();

        public virtual void OnReset();

        public virtual void Run(ushort clockDelta);
    }
}
