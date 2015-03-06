using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NESemulator
{
    /// <summary>
    /// NESファイルのパースと解析を行う
    /// フォーマットはiNES
    /// http://wiki.nesdev.com/w/index.php/INES
    /// </summary>
    class NesFile
    {
        string filename;
        public byte MapperNo { get; private set; }
        public byte[] PrgRom { get; private set; }
        public byte[] ChrRom { get; private set; }
        public byte[] Trainer { get; private set; }

        public MirrorType MirrorType { get; private set; }
        public bool TrainerFlag { get; private set; }
        public bool SramFlag { get; private set; }
        public uint PrgSize { get; private set; }
        public uint ChrSize { get; private set; }
        public uint PrgPageCnt { get; private set; }
        public uint ChrPageCnt { get; private set; }

        const uint NES_FORMAT_SIZE = 16;
        const uint TRAINER_SIZE = 512;
        const uint PRG_ROM_PAGE_SIZE = 16 * 1024;
        const uint CHR_ROM_PAGE_SIZE = 8 * 1024;

        public NesFile(byte[] data, string name = "MEMORY")
        {
            this.Trainer = new byte[512];
            this.filename = name;
            this.MapperNo = 0;
            this.PrgRom = null;
            this.ChrRom = null;
            this.MirrorType = MirrorType.HORIZONTAL;
            this.TrainerFlag = false;
            this.SramFlag = false;
            this.PrgSize = 0;
            this.ChrSize = 0;
            this.PrgPageCnt = 0;
            this.ChrPageCnt = 0;
            AnalyzeFile(data);
        }

        void AnalyzeFile(byte[] data)
        {
            uint size = (uint)data.Length;

            if (!(data[0] == 'N' && data[1] == 'E' && data[2] == 'S' && data[3] == 0x1A))
                throw new EmulatorException("[FIXME] Invalid header: " + filename);
            this.PrgSize = PRG_ROM_PAGE_SIZE * data[4];
            this.ChrSize = CHR_ROM_PAGE_SIZE * data[5];
            this.PrgPageCnt = data[4];
            this.ChrPageCnt = data[5];
            this.MapperNo = (byte)(((data[6] & 0xf0) >> 4) | (data[7] & 0xf0));
            this.TrainerFlag = (data[6] & 0x4) == 0x4;
            this.SramFlag = (data[6] & 0x2) == 0x2;
            if ((data[6] & 0x8) == 0x8)
                this.MirrorType = MirrorType.FOUR_SCREEN;
            else
                this.MirrorType = (data[6] & 0x1) == 0x1 ? MirrorType.VERTICAL : MirrorType.HORIZONTAL;

            uint fptr = NES_FORMAT_SIZE;
            if (this.TrainerFlag)
            {
                if (fptr + TRAINER_SIZE > size)
                    throw new EmulatorException("[FIXME] Invalid file size; too short!: " + filename);
                for (int i = 0; i < TRAINER_SIZE; i++)
                    this.Trainer[i] = data[i];
                fptr += TRAINER_SIZE;
            }

            this.PrgRom = new byte[this.PrgSize];
            if (fptr + this.PrgSize > size)
                throw new EmulatorException("[FIXME] Invalid file size; too short!: " + filename);
            for (int i = 0; i < this.PrgSize; i++)
                this.PrgRom[i] = data[i + fptr];
            fptr += this.PrgSize;

            this.ChrRom = new byte[this.ChrSize];
            if (fptr + this.ChrSize > size)
                throw new EmulatorException("[FIXME] Invalid file size; too short!: " + filename);
            else if (fptr + this.ChrSize < size)
                throw new EmulatorException("[FIXME] Invalid file size; too long!: " + filename);
            for (int i = 0; i < this.ChrSize; i++)
                this.ChrRom[i] = data[i + fptr];
            fptr += this.ChrSize;
        }
    }

    enum MirrorType { SINGLE0, SINGLE1, HORIZONTAL, VERTICAL, FOUR_SCREEN }
}