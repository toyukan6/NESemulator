using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace NESemulator
{
    /// <summary>
    /// PPU
    /// 画像出力関係
    /// </summary>
    class Video : IReset
    {
        VirtualMachine vm;

        public const int ScreenWidth = 256;
        public const int ScreenHeight = 240;
        public const int EmptyBit = 0x00;
        public const int BackSpriteBit = 0x40;
        public const int BackgroundBit = 0x80;
        public const int FrontSpriteBit = 0xc0;
        public const int SpriteLayerBit = 0x40;
        public const int LayerBitMask = 0xc0;

        bool isEven;
        ushort nowY;
        ushort nowX;
        byte[] spRam;
        byte[] internalVram;
        byte[,] palette;
        byte[,] screenBuffer;

        /* PPU Control Register 1 */
        bool executeNMIonVBlank;
        byte spriteHeight;
        ushort patternTableAddressBackground;
        ushort patternTableAddress8x8Sprites;
        byte vramIncrementSize;

        /* PPU Control Register 2 */
        byte colorEmphasis;
        bool spriteVisibility;
        bool backgroundVisibility;
        bool spriteClipping;
        bool backgroundClipping;
        byte paletteMask;

        /* PPU Status Register */
        bool nowOnVBnank;
        bool sprite0Hit;
        bool lostSprites;

        /* addressControl */
        byte vramBuffer;
        byte spriteAddr;
        ushort vramAddrRegister;
        ushort vramAddrReloadRegister;
        byte horizontalScrollBits;
        bool scrollRegisterWritten;
        bool vramAddrRegisterWritten;

        public Video(VirtualMachine vm)
        {
            this.vm = vm;
            executeNMIonVBlank = false;
            spriteHeight = 8; patternTableAddressBackground = 0;
            patternTableAddress8x8Sprites = 0;
            vramIncrementSize = 1;
            colorEmphasis = 0;
            spriteVisibility = false;
            backgroundVisibility = false;
            spriteClipping = false;
            backgroundClipping = false;
            paletteMask = 0;
            nowOnVBnank = false;
            sprite0Hit = false;
            lostSprites = false;
            vramBuffer = 0;
            spriteAddr = 0;
            vramAddrRegister = 0;
            vramAddrReloadRegister = 0;
            horizontalScrollBits = 0;
            scrollRegisterWritten = false;
            vramAddrRegisterWritten = false;
            spRam = new byte[256];
            internalVram = new byte[2048];
            palette = new byte[9, 4];
            screenBuffer = new byte[ScreenHeight, ScreenWidth];
            for (int i = 0; i < screenBuffer.GetLength(0); i++)
                for (int j = 0; j < screenBuffer.GetLength(1); j++)
                    screenBuffer[i, j] = 0;
        }

        public byte ReadReg(ushort addr)
        {
        }

        public void WriteReg(ushort addr, byte val)
        {
        }
    }
}
