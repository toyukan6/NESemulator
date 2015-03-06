using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NESemulator
{
    class IOPort
    {
        VirtualMachine vm;

        KeyID[] padIdxs;

        public IOPort(VirtualMachine vm)
        {
            this.vm = vm;
            JoyPadManager.GetPad();
            padIdxs = new KeyID [] { KeyID.A, KeyID.A };
        }

        public void OnVBlank()
        {
            JoyPadManager.Update();
            KeyManager.Update();
        }

        public void WriteOutReg(byte val)
        {
            if ((val & 1) == 1)
            {
                for (int i = 0; i < padIdxs.Length; i++)
                {
                    padIdxs[i] = KeyID.A;
                }
            }
        }

        public byte ReadInputReg1()
        {
            return ReadInputReg(0);
        }

        public byte ReadInputReg2()
        {
            return ReadInputReg(1);
        }

        byte ReadInputReg(int index)
        {
            byte result;
            if (JoyPadManager.Enable(index))
            {
                if (JoyPadManager.IsKeyPressed(index, padIdxs[index]))
                    result = 1;
                else
                    result = 0;
            }
            else
            {
                if (KeyManager.IsPressed(index, padIdxs[index]))
                    result = 1;
                else 
                    result = 0;
            }
            padIdxs[index] = (KeyID)(((int)padIdxs[index] + 1) % 8);
            return result;
        }
    }
}
