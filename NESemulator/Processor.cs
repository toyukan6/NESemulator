using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NESemulator
{
    /// <summary>
    /// CPU (MOS 6502 改)
    /// </summary>
    class Processor : IReset
    {
        const byte FLAG_C = 1;
        const byte FLAG_Z = 2;
        const byte FLAG_I = 4;
        const byte FLAG_D = 8;
        const byte FLAG_B = 16; //not used in NES
        const byte FLAG_ALWAYS_SET = 32;
        const byte FLAG_V = 64;
        const byte FLAG_N = 128;

        byte A;
        byte X;
        byte Y;
        ushort PC;
        byte SP;
        byte P;

        private VirtualMachine vm;

        public Processor(VirtualMachine vm)
        {
            this.vm = vm;
            A = 0;
            X = 0;
            Y = 0;
            PC = 0;
            SP = 0;
            P = 0;
        }

        private byte Read(ushort addr)
        {
            return vm.Read(addr);
        }

        private void Write(ushort addr, byte val)
        {
            vm.Write(addr, val);
        }

        public void OnHardReset()
        {
            this.P = 0x24;
            this.A = 0x0;
            this.X = 0x0;
            this.Y = 0x0;
            this.SP = 0xfd;
            this.Write(0x4017, 0x00);
            this.Write(0x4015, 0x00);
            for (ushort i = 0x4000; i <= 0x4000; i++)
            {
                this.Write(i, 0x00);
            }
            this.PC = (ushort)(Read(0xFFFC) | (Read(0xFFFD) << 8));
        }

        public void OnReset()
        {
            this.SP -= 0x03;
            this.P |= FLAG_I;
            this.Write(0x4015, 0x0);
            this.PC = (ushort)(Read(0xFFFC) | (Read(0xFFFD) << 8));
        }

        public void Run(ushort clockDelta)
        {
            this.P |= FLAG_ALWAYS_SET;

            byte opcode = Read(this.PC);

            switch (opcode)
            {
                default:
                    throw new EmulatorException("[FIXME] Invalid opcode!");
            }

            ConsumeClock(CycleTable[opcode]);
        }

        private void ConsumeClock(byte clock)
        {
            vm.ConsumeCPUClock(clock);
        }
        private void Push(byte val)
        {
            Write((ushort)(0x0100 | this.SP), val);
            this.SP--;
        }
        private byte Pop()
        {
            this.SP++;
            return Read((ushort)(0x0100 | this.SP));
        }
        private void UpdateFlagZN(byte val)
        {
            this.P = (byte)((this.P & 0x7D) | ZNFlagCache[val]);
        }

        #region アドレッシング
        ushort Immediate()
        {
            ushort addr = this.PC;
            this.PC++;
            return addr;
        }
        ushort Absolute()
        {
            ushort addr = Read(this.PC);
            this.PC++;
            addr |= (ushort)(Read(this.PC) << 8);
            this.PC++;
            return addr;
        }
        ushort ZeroPage()
        {
            ushort addr = (ushort)Read(this.PC);
            this.PC++;
            return addr;
        }
        ushort ZeroPageIndexX()
        {
            byte addr = (byte)(Read(this.PC) + this.X);
            this.PC++;
            return addr;
        }
        ushort ZeroPageIndexY()
        {
            byte addr = (byte)(Read(this.PC) + this.Y);
            this.PC++;
            return addr;
        }
        ushort AbsoluteIndexX()
        {
            ushort orig = this.Read(this.PC);
            this.PC++;
            orig |= (ushort)(Read(this.PC) << 8);
            this.PC++;
            ushort addr = (ushort)(orig + this.X);
            if (((addr ^ orig) & 0x0100) != 0) ConsumeClock(1);

            return addr;
        }
        ushort AbsoluteIndexY()
        {
            ushort orig = this.Read(this.PC);
            this.PC++;
            orig |= (ushort)(Read(this.PC) << 8);
            this.PC++;
            ushort addr = (ushort)(orig + this.Y);
            if (((addr ^ orig) & 0x0100) != 0) ConsumeClock(1);

            return addr;
        }
        ushort Relative()
        {
            sbyte addr = (sbyte)Read(this.PC);
            this.PC++;
            return (ushort)(this.PC + addr);
        }
        ushort IndirectX()
        {
            byte idx = (byte)(Read(this.PC) + this.X);
            this.PC++;
            ushort addr = Read(idx);
            idx++;
            addr |= (ushort)(Read(idx) << 8);
            return addr;
        }
        ushort IndirectY()
        {
            byte idx = Read(this.PC);
            this.PC++;
            ushort orig = Read(idx);
            idx++;
            orig |= (ushort)(Read(idx) << 8);
            ushort addr = (ushort)(Read(orig) + this.Y);
            if (((addr ^ orig) & 0x0100) != 0) ConsumeClock(1);
            return addr;
        }
        ushort AbsoluteIndirect()
        {
            ushort srcAddr = Read(this.PC);
            this.PC++;
            srcAddr |= (ushort)(Read(this.PC) << 8);
            this.PC++;
            return (ushort)(Read(srcAddr) | (Read((ushort)((srcAddr & 0xff00) | ((srcAddr + 1) & 0x00ff))) << 8)); //bug of NES
        }
        #endregion

        #region CPU命令
        void LDA(ushort addr)
        {
            this.A = this.Read(addr);
            UpdateFlagZN(this.A);
        }
        void LDY(ushort addr)
        {
            this.Y = this.Read(addr);
            UpdateFlagZN(this.Y);
        }
        void LDX(ushort addr)
        {
            this.X = this.Read(addr);
            UpdateFlagZN(this.X);
        }
        void STA(ushort addr)
        {
            this.Write(addr, this.A);
        }
        void STX(ushort addr)
        {
            this.Write(addr, this.X);
        }
        void STY(ushort addr)
        {
            this.Write(addr, this.Y);
        }
        void TXA()
        {
            this.A = this.X;
            UpdateFlagZN(this.A);
        }
        void TYA()
        {
            this.A = this.Y;
            UpdateFlagZN(this.A);
        }
        void TXS()
        {
            this.SP = this.X;
        }
        void TAY()
        {
            this.Y = this.A;
            UpdateFlagZN(this.Y);
        }
        void TAX()
        {
            this.X = this.A;
            UpdateFlagZN(this.X);
        }
        void TSX()
        {
            this.X = this.SP;
            UpdateFlagZN(this.X);
        }
        void PHP()
        {
            Push((byte)(this.P | FLAG_B)); // bug of 6502! from http://pgate1.at-ninja.jp/NES_on_FPGA/nes_cpu.htm
        }
        void PLP()
        {
            byte newP = Pop();
            if ((this.P & FLAG_I) == FLAG_I && (newP & FLAG_I) == 0)
            {
                //this->needStatusRewrite = true;
                //this->newStatus = newP;
            }
            else
            {
                this.P = newP;
            }
        }
        #endregion

        const byte[] ZNFlagCache = new byte[] 
        { 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
          0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
          0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
          0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
          0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
          0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
          0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
          0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
          0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80 };

        const byte[] CycleTable = new byte[] 
        { 7, 6, 2, 8, 3, 3, 5, 5, 3, 2, 2, 2, 4, 4, 6, 6,
          2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 6, 7,
          6, 6, 2, 8, 3, 3, 5, 5, 4, 2, 2, 2, 4, 4, 6, 6,
          2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 6, 7,
          6, 6, 2, 8, 3, 3, 5, 5, 3, 2, 2, 2, 3, 4, 6, 6,
          2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 6, 7,
          6, 6, 2, 8, 3, 3, 5, 5, 4, 2, 2, 2, 5, 4, 6, 6,
          2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 6, 7,
          2, 6, 2, 6, 3, 3, 3, 3, 2, 2, 2, 2, 4, 4, 4, 4,
          2, 5, 2, 6, 4, 4, 4, 4, 2, 4, 2, 5, 5, 4, 5, 5,
          2, 6, 2, 6, 3, 3, 3, 3, 2, 2, 2, 2, 4, 4, 4, 4,
          2, 5, 2, 5, 4, 4, 4, 4, 2, 4, 2, 4, 4, 4, 4, 4,
          2, 6, 2, 8, 3, 3, 5, 5, 2, 2, 2, 2, 4, 4, 6, 6,
          2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 6, 7,
          2, 6, 3, 8, 3, 3, 5, 5, 2, 2, 2, 2, 4, 4, 6, 6,
          2, 5, 2, 8, 4, 4, 6, 6, 2, 4, 2, 7, 4, 4, 6, 7 };
    }
}
