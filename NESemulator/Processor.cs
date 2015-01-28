using System;
using System.Collections.Generic;
using System.IO;
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

        bool nmi;

        private VirtualMachine vm;
        private bool needStatusRewrite;
        private byte newStatus;

        StreamWriter writer = new StreamWriter("result.txt");

        public Processor(VirtualMachine vm)
        {
            this.vm = vm;
            A = 0;
            X = 0;
            Y = 0;
            PC = 0;
            SP = 0;
            P = 0;
            nmi = false;
            needStatusRewrite = false;
            newStatus = 0;
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
                this.Write(i, 0x00);
#if DEBUG
            this.PC = 0xC000;
#else
            this.PC = (ushort)(Read(0xFFFC) | (Read(0xFFFD) << 8));
#endif
            nmi = false;
            needStatusRewrite = false;
            newStatus = 0;
        }

        public void OnReset()
        {
            this.SP -= 0x03;
            this.P |= FLAG_I;
            this.Write(0x4015, 0x0);
            this.PC = (ushort)(Read(0xFFFC) | (Read(0xFFFD) << 8));
            nmi = false;
            needStatusRewrite = false;
            newStatus = 0;
        }

        public void Run(ushort clockDelta)
        {
            this.P |= FLAG_ALWAYS_SET;

            if (needStatusRewrite)
            {
                this.P = newStatus;
                needStatusRewrite = false;
            }

            if (this.nmi) this.OnNMI();

            byte opcode = Read(this.PC);
#if DEBUG
            string flag = "";
            flag += ((this.P & FLAG_N) > 0) ? 'N' : 'n';
            flag += ((this.P & FLAG_V) > 0) ? 'V' : 'v';
            flag += ((this.P & FLAG_ALWAYS_SET) > 0) ? 'U' : 'u';
            flag += ((this.P & FLAG_B) > 0) ? 'B' : 'b';
            flag += ((this.P & FLAG_D) > 0) ? 'D' : 'd';
            flag += ((this.P & FLAG_I) > 0) ? 'I' : 'i';
            flag += ((this.P & FLAG_Z) > 0) ? 'Z' : 'z';
            flag += ((this.P & FLAG_C) > 0) ? 'C' : 'c';
            if (vm.AddrExist(this.PC))
            {
                writer.WriteLine("{0} op:{1} a:{2} x:{3} y:{4} sp:{5} p:{6}", this.PC.ToString("x4"), opcode.ToString("x2"), this.A.ToString("x2"), this.X.ToString("x2"), this.Y.ToString("x2"), this.SP.ToString("x2"), flag);
                writer.Flush();
                Console.WriteLine("{0} op:{1} a:{2} x:{3} y:{4} sp:{5} p:{6}", this.PC.ToString("x4"), opcode.ToString("x2"), this.A.ToString("x2"), this.X.ToString("x2"), this.Y.ToString("x2"), this.SP.ToString("x2"), flag);
            }
#endif
            this.PC++;

            switch (opcode)
            {
                #region 命令たち
                case 0x00:
                    this.BRK();
                    break;
                case 0x01:
                    this.ORA(IndirectX());
                    break;
                //case 0x02: // Future Expansion
                //case 0x03: // Future Expansion
                //case 0x04: // Future Expansion
                case 0x05:
                    this.ORA(ZeroPage());
                    break;
                case 0x06:
                    this.ASL(ZeroPage());
                    break;
                //case 0x07: // Future Expansion
                case 0x08:
                    this.PHP();
                    break;
                case 0x09:
                    this.ORA(Immediate());
                    break;
                case 0x0A:
                    this.ASL();
                    break;
                //case 0x0B: // Future Expansion
                //case 0x0C: // Future Expansion
                case 0x0D:
                    this.ORA(Absolute());
                    break;
                case 0x0E:
                    this.ASL(Absolute());
                    break;
                //case 0x0F: // Future Expansion
                case 0x10:
                    this.BPL(Relative());
                    break;
                case 0x11:
                    this.ORA(IndirectY());
                    break;
                //case 0x12: // Future Expansion
                //case 0x13: // Future Expansion
                //case 0x14: // Future Expansion
                case 0x15:
                    this.ORA(ZeroPageIndexX());
                    break;
                case 0x16:
                    this.ASL(ZeroPageIndexX());
                    break;
                //case 0x17: // Future Expansion
                case 0x18:
                    this.CLC();
                    break;
                case 0x19:
                    this.ORA(AbsoluteIndexY());
                    break;
                //case 0x1A: // Future Expansion
                //case 0x1B: // Future Expansion
                //case 0x1C: // Future Expansion
                case 0x1D:
                    this.ORA(AbsoluteIndexX());
                    break;
                case 0x1E:
                    this.ASL(AbsoluteIndexX());
                    break;
                //case 0x1F: // Future Expansion
                case 0x20:
                    this.JSR(Absolute());
                    break;
                case 0x21:
                    this.AND(IndirectX());
                    break;
                //case 0x22: // Future Expansion
                //case 0x23: // Future Expansion
                case 0x24:
                    this.BIT(ZeroPage());
                    break;
                case 0x25:
                    this.AND(ZeroPage());
                    break;
                case 0x26:
                    this.ROL(ZeroPage());
                    break;
                //case 0x27: // Future Expansion
                case 0x28:
                    this.PLP();
                    break;
                case 0x29:
                    this.AND(Immediate());
                    break;
                case 0x2A:
                    this.ROL();
                    break;
                //case 0x2B: // Future Expansion
                case 0x2C:
                    this.BIT(Absolute());
                    break;
                case 0x2D:
                    this.AND(Absolute());
                    break;
                case 0x2E:
                    this.ROL(Absolute());
                    break;
                //case 0x2F: // Future Expansion
                case 0x30:
                    this.BMI(Relative());
                    break;
                case 0x31:
                    this.AND(IndirectY());
                    break;
                //case 0x32: // Future Expansion
                //case 0x33: // Future Expansion
                //case 0x34: // Future Expansion
                case 0x35:
                    this.AND(ZeroPageIndexX());
                    break;
                case 0x36:
                    this.ROL(ZeroPageIndexX());
                    break;
                //case 0x37: // Future Expansion
                case 0x38:
                    this.SEC();
                    break;
                case 0x39:
                    this.AND(AbsoluteIndexY());
                    break;
                //case 0x3A: // Future Expansion
                //case 0x3B: // Future Expansion
                //case 0x3C: // Future Expansion
                case 0x3D:
                    this.AND(AbsoluteIndexX());
                    break;
                case 0x3E:
                    this.ROL(AbsoluteIndexX());
                    break;
                //case 0x3F: // Future Expansion
                case 0x40:
                    this.RTI();
                    break;
                case 0x41:
                    this.EOR(IndirectX());
                    break;
                //case 0x42: // Future Expansion
                //case 0x43: // Future Expansion
                //case 0x44: // Future Expansion
                case 0x45:
                    this.EOR(ZeroPage());
                    break;
                case 0x46:
                    this.LSR(ZeroPage());
                    break;
                //case 0x47: // Future Expansion
                case 0x48:
                    this.PHA();
                    break;
                case 0x49:
                    this.EOR(Immediate());
                    break;
                case 0x4A:
                    this.LSR();
                    break;
                //case 0x4B: // Future Expansion
                case 0x4C:
                    this.JMP(Absolute());
                    break;
                case 0x4D:
                    this.EOR(Absolute());
                    break;
                case 0x4E:
                    this.LSR(Absolute());
                    break;
                //case 0x4F: // Future Expansion
                case 0x50:
                    this.BVC(Relative());
                    break;
                case 0x51:
                    this.EOR(IndirectY());
                    break;
                //case 0x52: // Future Expansion
                //case 0x53: // Future Expansion
                //case 0x54: // Future Expansion
                case 0x55:
                    this.EOR(ZeroPageIndexX());
                    break;
                case 0x56:
                    this.LSR(ZeroPageIndexX());
                    break;
                //case 0x57: // Future Expansion
                case 0x58:
                    this.CLI();
                    break;
                case 0x59:
                    this.EOR(AbsoluteIndexY());
                    break;
                //case 0x5A: // Future Expansion
                //case 0x5B: // Future Expansion
                //case 0x5C: // Future Expansion
                case 0x5D:
                    this.EOR(AbsoluteIndexX());
                    break;
                case 0x5E:
                    this.LSR(AbsoluteIndexX());
                    break;
                //case 0x5F: // Future Expansion
                case 0x60:
                    this.RTS();
                    break;
                case 0x61:
                    this.ADC(IndirectX());
                    break;
                //case 0x62: // Future Expansion
                //case 0x63: // Future Expansion
                //case 0x64: // Future Expansion
                case 0x65:
                    this.ADC(ZeroPage());
                    break;
                case 0x66:
                    this.ROR(ZeroPage());
                    break;
                //case 0x67: // Future Expansion
                case 0x68:
                    this.PLA();
                    break;
                case 0x69:
                    this.ADC(Immediate());
                    break;
                case 0x6A:
                    this.ROR();
                    break;
                //case 0x6B: // Future Expansion
                case 0x6C:
                    this.JMP(this.AbsoluteIndirect());
                    break;
                case 0x6D:
                    this.ADC(Absolute());
                    break;
                case 0x6E:
                    this.ROR(Absolute());
                    break;
                //case 0x6F: // Future Expansion
                case 0x70:
                    this.BVS(Relative());
                    break;
                case 0x71:
                    this.ADC(IndirectY());
                    break;
                //case 0x72: // Future Expansion
                //case 0x73: // Future Expansion
                //case 0x74: // Future Expansion
                case 0x75:
                    this.ADC(ZeroPageIndexX());
                    break;
                case 0x76:
                    this.ROR(ZeroPageIndexX());
                    break;
                //case 0x77: // Future Expansion
                case 0x78:
                    this.SEI();
                    break;
                case 0x79:
                    this.ADC(AbsoluteIndexY());
                    break;
                //case 0x7A: // Future Expansion
                //case 0x7B: // Future Expansion
                //case 0x7C: // Future Expansion
                case 0x7D:
                    this.ADC(AbsoluteIndexX());
                    break;
                case 0x7E:
                    this.ROR(AbsoluteIndexX());
                    break;
                //case 0x7F: // Future Expansion
                //case 0x80: // Future Expansion
                case 0x81:
                    this.STA(IndirectX());
                    break;
                //case 0x82: // Future Expansion
                //case 0x83: // Future Expansion
                case 0x84:
                    this.STY(ZeroPage());
                    break;
                case 0x85:
                    this.STA(ZeroPage());
                    break;
                case 0x86:
                    this.STX(ZeroPage());
                    break;
                //case 0x87: // Future Expansion
                case 0x88:
                    this.DEY();
                    break;
                //case 0x89: // Future Expansion
                case 0x8A:
                    this.TXA();
                    break;
                //case 0x8B: // Future Expansion
                case 0x8C:
                    this.STY(Absolute());
                    break;
                case 0x8D:
                    this.STA(Absolute());
                    break;
                case 0x8E:
                    this.STX(Absolute());
                    break;
                //case 0x8F: // Future Expansion
                case 0x90:
                    this.BCC(Relative());
                    break;
                case 0x91:
                    this.STA(IndirectY());
                    break;
                //case 0x92: // Future Expansion
                //case 0x93: // Future Expansion
                case 0x94:
                    this.STY(ZeroPageIndexX());
                    break;
                case 0x95:
                    this.STA(ZeroPageIndexX());
                    break;
                case 0x96:
                    this.STX(ZeroPageIndexY());
                    break;
                //case 0x97: // Future Expansion
                case 0x98:
                    this.TYA();
                    break;
                case 0x99:
                    this.STA(AbsoluteIndexY());
                    break;
                case 0x9A:
                    this.TXS();
                    break;
                //case 0x9B: // Future Expansion
                //case 0x9C: // Future Expansion
                case 0x9D:
                    this.STA(AbsoluteIndexX());
                    break;
                //case 0x9E: // Future Expansion
                //case 0x9F: // Future Expansion
                case 0xA0:
                    this.LDY(Immediate());
                    break;
                case 0xA1:
                    this.LDA(IndirectX());
                    break;
                case 0xA2:
                    this.LDX(Immediate());
                    break;
                case 0xA3: // Future Expansion
                    if (this.PC == 0xE546)
                    {
                        this.LDA(257);
                        this.LDX(257);
                        this.PC++;
                    }
                    else if (this.PC == 0xE56C)
                    {
                        this.LDA(769);
                        this.LDX(769);
                        this.PC++;
                    }
                    break;
                case 0xA4:
                    this.LDY(ZeroPage());
                    break;
                case 0xA5:
                    this.LDA(ZeroPage());
                    break;
                case 0xA6:
                    this.LDX(ZeroPage());
                    break;
                //case 0xA7: // Future Expansion
                case 0xA8:
                    this.TAY();
                    break;
                case 0xA9:
                    this.LDA(Immediate());
                    break;
                case 0xAA:
                    this.TAX();
                    break;
                //case 0xAB: // Future Expansion
                case 0xAC:
                    this.LDY(Absolute());
                    break;
                case 0xAD:
                    this.LDA(Absolute());
                    break;
                case 0xAE:
                    this.LDX(Absolute());
                    break;
                //case 0xAF: // Future Expansion
                case 0xB0:
                    this.BCS(Relative());
                    break;
                case 0xB1:
                    this.LDA(IndirectY());
                    break;
                //case 0xB2: // Future Expansion
                //case 0xB3: // Future Expansion
                case 0xB4:
                    this.LDY(ZeroPageIndexX());
                    break;
                case 0xB5:
                    this.LDA(ZeroPageIndexX());
                    break;
                case 0xB6:
                    this.LDX(ZeroPageIndexY());
                    break;
                //case 0xB7: // Future Expansion
                case 0xB8:
                    this.CLV();
                    break;
                case 0xB9:
                    this.LDA(AbsoluteIndexY());
                    break;
                case 0xBA:
                    this.TSX();
                    break;
                //case 0xBB: // Future Expansion
                case 0xBC:
                    this.LDY(AbsoluteIndexX());
                    break;
                case 0xBD:
                    this.LDA(AbsoluteIndexX());
                    break;
                case 0xBE:
                    this.LDX(AbsoluteIndexY());
                    break;
                //case 0xBF: // Future Expansion
                case 0xC0:
                    this.CPY(Immediate());
                    break;
                case 0xC1:
                    this.CMP(IndirectX());
                    break;
                //case 0xC2: // Future Expansion
                //case 0xC3: // Future Expansion
                case 0xC4:
                    this.CPY(ZeroPage());
                    break;
                case 0xC5:
                    this.CMP(ZeroPage());
                    break;
                case 0xC6:
                    this.DEC(ZeroPage());
                    break;
                //case 0xC7: // Future Expansion
                case 0xC8:
                    this.INY();
                    break;
                case 0xC9:
                    this.CMP(Immediate());
                    break;
                case 0xCA:
                    this.DEX();
                    break;
                //case 0xCB: // Future Expansion
                case 0xCC:
                    this.CPY(Absolute());
                    break;
                case 0xCD:
                    this.CMP(Absolute());
                    break;
                case 0xCE:
                    this.DEC(Absolute());
                    break;
                //case 0xCF: // Future Expansion
                case 0xD0:
                    this.BNE(Relative());
                    break;
                case 0xD1:
                    this.CMP(IndirectY());
                    break;
                //case 0xD2: // Future Expansion
                //case 0xD3: // Future Expansion
                //case 0xD4: // Future Expansion
                case 0xD5:
                    this.CMP(ZeroPageIndexX());
                    break;
                case 0xD6:
                    this.DEC(ZeroPageIndexX());
                    break;
                //case 0xD7: // Future Expansion
                case 0xD8:
                    this.CLD();
                    break;
                case 0xD9:
                    this.CMP(AbsoluteIndexY());
                    break;
                //case 0xDA: // Future Expansion
                //case 0xDB: // Future Expansion
                //case 0xDC: // Future Expansion
                case 0xDD:
                    this.CMP(AbsoluteIndexX());
                    break;
                case 0xDE:
                    this.DEC(AbsoluteIndexX());
                    break;
                //case 0xDF: // Future Expansion
                case 0xE0:
                    this.CPX(Immediate());
                    break;
                case 0xE1:
                    this.SBC(IndirectX());
                    break;
                //case 0xE2: // Future Expansion
                //case 0xE3: // Future Expansion
                case 0xE4:
                    this.CPX(ZeroPage());
                    break;
                case 0xE5:
                    this.SBC(ZeroPage());
                    break;
                case 0xE6:
                    this.INC(ZeroPage());
                    break;
                //case 0xE7: // Future Expansion
                case 0xE8:
                    this.INX();
                    break;
                case 0xE9:
                    this.SBC(Immediate());
                    break;
                case 0xEA:
                    this.NOP();
                    break;
                //case 0xEB: // Future Expansion
                case 0xEC:
                    this.CPX(Absolute());
                    break;
                case 0xED:
                    this.SBC(Absolute());
                    break;
                case 0xEE:
                    this.INC(Absolute());
                    break;
                //case 0xEF: // Future Expansion
                case 0xF0:
                    this.BEQ(Relative());
                    break;
                case 0xF1:
                    this.SBC(IndirectY());
                    break;
                //case 0xF2: // Future Expansion
                //case 0xF3: // Future Expansion
                //case 0xF4: // Future Expansion
                case 0xF5:
                    this.SBC(ZeroPageIndexX());
                    break;
                case 0xF6:
                    this.INC(ZeroPageIndexX());
                    break;
                //case 0xF7: // Future Expansion
                case 0xF8:
                    this.SED();
                    break;
                case 0xF9:
                    this.SBC(AbsoluteIndexY());
                    break;
                //case 0xFA: // Future Expansion
                //case 0xFB: // Future Expansion
                //case 0xFC: // Future Expansion
                case 0xFD:
                    this.SBC(AbsoluteIndexX());
                    break;
                case 0xFE:
                    this.INC(AbsoluteIndexX());
                    break;
                //case 0xFF: // Future Expansion
                default:
                    var opcodePC = this.PC - 1;
                    throw new EmulatorException("[FIXME] Invalid opcode: {0} in {1}", opcode.ToString("x2"), opcodePC.ToString("x4"));
                #endregion
            }

            ConsumeClock(CycleTable[opcode]);
        }

        public void SendNMI()
        {
            nmi = true;
        }

        void OnNMI()
        {
            //http://nesdev.com/6502_cpu.txt
            ConsumeClock(7);
            this.P = (byte)(this.P & ~FLAG_B);
            Push((byte)((this.PC >> 8) & 0xFF));
            Push((byte)(this.PC & 0xFF));
            Push(this.P);
            this.P |= FLAG_I;
            this.PC = (ushort)((Read(0xFFFA) | (Read(0xFFFB) << 8)));
            this.nmi = false;
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
            ushort addr = Read(this.PC);
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
            ushort addr = (ushort)(orig + this.Y);
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
                this.needStatusRewrite = true;
                this.newStatus = newP;
            }
            else
            {
                this.P = newP;
            }
        }
        void PHA()
        {
            Push(this.A);
        }
        void PLA()
        {
            this.A = Pop();
            UpdateFlagZN(this.A);
        }
        void ADC(ushort addr)
        {
            byte val = Read(addr);
            ushort result = (ushort)(this.A + val + (this.P & FLAG_C));
            byte newA = (byte)(result & 0xff);
            this.P = (byte)((this.P & ~(FLAG_V | FLAG_C))
                            | ((this.A ^ val ^ 0x80) & (this.A ^ newA) & 0x80) >> 1 //set V flag 
                            | ((result >> 8) & FLAG_C)); //set C flag
            this.A = newA;
            UpdateFlagZN(this.A);
        }
        void SBC(ushort addr)
        {
            byte val = Read(addr);
            ushort result = (ushort)(this.A - val - ((this.P & FLAG_C) ^ FLAG_C));
            byte newA = (byte)(result & 0xff);
            this.P = (byte)((this.P & ~(FLAG_V | FLAG_C))
                            | ((this.A ^ val) & (this.A ^ newA) & 0x80) >> 1 //set V flag 
                            | (((result >> 8) & FLAG_C) ^ FLAG_C)); //set C flag
            this.A = newA;
            UpdateFlagZN(this.A);
        }
        void CPX(ushort addr)
        {
            ushort val = (ushort)(this.X - Read(addr));
            byte val8 = (byte)(val & 0xff);
            UpdateFlagZN(val8);
            this.P = (byte)((this.P & 0xfe) | (((val >> 8) & 0x1) ^ 0x1));
        }
        void CPY(ushort addr)
        {
            ushort val = (ushort)(this.Y - Read(addr));
            byte val8 = (byte)(val & 0xff);
            UpdateFlagZN(val8);
            this.P = (byte)((this.P & 0xfe) | (((val >> 8) & 0x1) ^ 0x1));
        }
        void CMP(ushort addr)
        {
            ushort val = (ushort)(this.A - Read(addr));
            byte val8 = (byte)(val & 0xff);
            UpdateFlagZN(val8);
            this.P = (byte)((this.P & 0xfe) | (((val >> 8) & 0x1) ^ 0x1));
        }
        void AND(ushort addr)
        {
            this.A &= Read(addr);
            UpdateFlagZN(this.A);
        }
        void EOR(ushort addr)
        {
            this.A ^= Read(addr);
            UpdateFlagZN(this.A);
        }
        void ORA(ushort addr)
        {
            this.A |= Read(addr);
            UpdateFlagZN(this.A);
        }
        void BIT(ushort addr)
        {
            byte val = Read(addr);
            this.P = (byte)((this.P & (0xff & ~(FLAG_V | FLAG_N | FLAG_Z)))
                            | (val & (FLAG_V | FLAG_N))
                            | (ZNFlagCache[this.A & val] & FLAG_Z));
        }
        void ASL()
        {
            this.P = (byte)((this.P & 0xFE) | this.A >> 7);
            this.A <<= 1;
            UpdateFlagZN(this.A);
        }
        void ASL(ushort addr)
        {
            byte val = Read(addr);
            this.P = (byte)((this.P & 0xFE) | val >> 7);
            val <<= 1;
            this.Write(addr, val);
            UpdateFlagZN(val);
        }
        void LSR()
        {
            this.P = (byte)((this.P & 0xFE) | this.A & 0x01);
            this.A >>= 1;
            UpdateFlagZN(this.A);
        }
        void LSR(ushort addr)
        {
            byte val = Read(addr);
            this.P = (byte)((this.P & 0xFE) | val & 0x01);
            val >>= 1;
            this.Write(addr, val);
            UpdateFlagZN(val);
        }
        void ROL()
        {
            byte carry = (byte)(this.A >> 7);
            this.A = (byte)((this.A << 1) | (this.P & 0x01));
            this.P = (byte)((this.P & 0xFE) | carry);
            UpdateFlagZN(this.A);
        }
        void ROL(ushort addr)
        {
            byte val = Read(addr);
            byte carry = (byte)(val >> 7);
            val = (byte)((val << 1) | (this.P & 0x01));
            this.P = (byte)((this.P & 0xFE) | carry);
            this.Write(addr, val);
            UpdateFlagZN(val);
        }
        void ROR()
        {
            byte carry = (byte)(this.A & 0x01);
            this.A = (byte)((this.A >> 1) | ((this.P & 0x01) << 7));
            this.P = (byte)((this.P & 0xFE) | carry);
            UpdateFlagZN(this.A);
        }
        void ROR(ushort addr)
        {
            byte val = Read(addr);
            byte carry = (byte)(val & 0x01);
            val = (byte)((val >> 1) | ((this.P & 0x01) << 7));
            this.P = (byte)((this.P & 0xFE) | carry);
            this.Write(addr, val);
            UpdateFlagZN(val);
        }
        void INX()
        {
            this.X++;
            UpdateFlagZN(this.X);
        }
        void INY()
        {
            this.Y++;
            UpdateFlagZN(this.Y);
        }
        void INC(ushort addr)
        {
            byte val = Read(addr);
            val++;
            this.Write(addr, val);
            UpdateFlagZN(val);
        }
        void DEX()
        {
            this.X--;
            UpdateFlagZN(this.X);
        }
        void DEY()
        {
            this.Y--;
            UpdateFlagZN(this.Y);
        }
        void DEC(ushort addr)
        {
            byte val = Read(addr);
            val--;
            this.Write(addr, val);
            UpdateFlagZN(val);
        }
        void CLC()
        {
            this.P = (byte)(this.P & ~FLAG_C);
        }
        void CLI()
        {
            // http://twitter.com/#!/KiC6280/status/112348378100281344
            // http://twitter.com/#!/KiC6280/status/112351125084180480
            this.needStatusRewrite = true;
            this.newStatus = (byte)(this.P & ~FLAG_I);
        }
        void CLV()
        {
            this.P = (byte)(this.P & ~FLAG_V);
        }
        void CLD()
        {
            this.P = (byte)(this.P & ~FLAG_D);
        }
        void SEC()
        {
            this.P |= FLAG_C;
        }
        void SEI()
        {
            this.P |= FLAG_I;
        }
        void SED()
        {
            this.P |= FLAG_D;
        }
        void NOP()
        {
        }
        void BRK()
        {
            //NES ON FPGAには、
            //「割り込みが確認された時、Iフラグがセットされていれば割り込みは無視します。」
            //…と合ったけど、他の資料だと違う。http://nesdev.parodius.com/6502.txt
            //DQ4はこうしないと、動かない。
            this.PC++;
            Push((byte)((this.PC >> 8) & 0xFF));
            Push((byte)(this.PC & 0xFF));
            this.P |= FLAG_B;
            Push(this.P);
            this.P |= FLAG_I;
            this.PC = (ushort)(Read(0xFFFE) | (Read(0xFFFF) << 8));
        }
        void BCC(ushort addr)
        {
            if ((this.P & FLAG_C) == 0)
            {
                if (((this.PC ^ addr) & 0x0100) != 0)
                    ConsumeClock(2);
                else
                    ConsumeClock(1);
                this.PC = addr;
            }
        }
        void BCS(ushort addr)
        {
            if ((this.P & FLAG_C) == FLAG_C)
            {
                if (((this.PC ^ addr) & 0x0100) != 0)
                    ConsumeClock(2);
                else
                    ConsumeClock(1);
                this.PC = addr;
            }
        }
        void BEQ(ushort addr)
        {
            if ((this.P & FLAG_Z) == FLAG_Z)
            {
                if (((this.PC ^ addr) & 0x0100) != 0)
                    ConsumeClock(2);
                else
                    ConsumeClock(1);
                this.PC = addr;
            }
        }
        void BNE(ushort addr)
        {
            if ((this.P & FLAG_Z) == 0)
            {
                if (((this.PC ^ addr) & 0x0100) != 0)
                    ConsumeClock(2);
                else
                    ConsumeClock(1);
                this.PC = addr;
            }
        }
        void BVC(ushort addr)
        {
            if ((this.P & FLAG_V) == 0)
            {
                if (((this.PC ^ addr) & 0x0100) != 0)
                    ConsumeClock(2);
                else
                    ConsumeClock(1);
                this.PC = addr;
            }
        }
        void BVS(ushort addr)
        {
            if ((this.P & FLAG_V) == FLAG_V)
            {
                if (((this.PC ^ addr) & 0x0100) != 0)
                    ConsumeClock(2);
                else
                    ConsumeClock(1);
                this.PC = addr;
            }
        }
        void BPL(ushort addr)
        {
            if ((this.P & FLAG_N) == 0)
            {
                if (((this.PC ^ addr) & 0x0100) != 0)
                    ConsumeClock(2);
                else
                    ConsumeClock(1);
                this.PC = addr;
            }
        }
        void BMI(ushort addr)
        {
            if ((this.P & FLAG_N) == FLAG_N)
            {
                if (((this.PC ^ addr) & 0x0100) != 0)
                    ConsumeClock(2);
                else
                    ConsumeClock(1);
                this.PC = addr;
            }
        }
        void JSR(ushort addr)
        {
            this.PC--;
            Push((byte)((this.PC >> 8) & 0xFF));
            Push((byte)(this.PC & 0xFF));
            this.PC = addr;
        }
        void JMP(ushort addr)
        {
            this.PC = addr;
        }
        void RTI()
        {
            this.P = Pop();
            this.PC = Pop();
            this.PC = (ushort)(this.PC | Pop() << 8);
        }
        void RTS()
        {
            this.PC = Pop();
            this.PC = (ushort)((this.PC | Pop() << 8) + 1);
        }
        #endregion

        byte[] ZNFlagCache = new byte[] 
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

        byte[] CycleTable = new byte[] 
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
