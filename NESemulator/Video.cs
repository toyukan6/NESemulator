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
        Cartridge cartridge;

        public const int ScreenWidth = 256;
        public const int ScreenHeight = 240;
        public const byte EmptyBit = 0x00;
        public const byte BackSpriteBit = 0x40;
        public const byte BackgroundBit = 0x80;
        public const byte FrontSpriteBit = 0xc0;
        public const byte SpriteLayerBit = 0x40;
        public const byte LayerBitMask = 0xc0;

        const int clockPerScanline = 341;
		const int scanlinePerScreen = 262;
		const int defaultSpriteCnt = 8;

        bool isEven;
        ushort nowY;
        ushort nowX;
        byte[] spRam;
        byte[] internalVram;
        byte[,] palette;
        byte[,] screenBuffer;

        SpriteSlot[] spriteTable;
        ushort spriteHitCnt;

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
            isEven = false;
            nowY = 0;
            nowX = 0;
            spriteTable = new SpriteSlot[defaultSpriteCnt];
            spriteHitCnt = 0;
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

        public void OnHardReset()
        {
            for (int i = 0; i < internalVram.Length; i++)
                internalVram[i] = 0;
            for (int i = 0; i < spRam.Length; i++)
                spRam[i] = 0;
            for (int i = 0; i < palette.GetLength(0) - 1; i++)
                for (int j = 0; j < palette.GetLength(1); j++)
                    palette[i, j] = 0;
            nowY = 0;
            nowX = 0;
            executeNMIonVBlank = false;
            spriteHeight = 8;
            patternTableAddressBackground = 0x0000;
            patternTableAddress8x8Sprites = 0x0000;
            vramIncrementSize = 1;
            vramAddrReloadRegister = 0x0000;
            horizontalScrollBits = 0;
            colorEmphasis = 0;
            spriteVisibility = false;
            backgroundVisibility = false;
            spriteClipping = true;
            backgroundClipping = true;
            paletteMask = 0x3f;
            spriteAddr = 0;
            vramAddrRegisterWritten = false;
            scrollRegisterWritten = false;
            vramAddrRegister = 0;
        }

        public void OnReset()
        {
            executeNMIonVBlank = false;
            spriteHeight = 8;
            patternTableAddressBackground = 0x0000;
            patternTableAddress8x8Sprites = 0x0000;
            vramIncrementSize = 1;
            vramAddrReloadRegister = 0x0000;
            horizontalScrollBits = 0;
            colorEmphasis = 0;
            spriteVisibility = false;
            backgroundVisibility = false;
            spriteClipping = true;
            backgroundClipping = true;
            paletteMask = 0x3f;
            vramAddrRegisterWritten = false;
            scrollRegisterWritten = false;
            vramBuffer = 0;
        }

        public void Run(ushort clockDelta)
        {
            this.nowX += clockDelta;
            while (this.nowX >= 341)
            {
                this.nowY++;
                this.nowX -= 341;
                if (this.nowY <= 240)
                {
                }
                else if (this.nowY == 241)
                {
                }
                else if (this.nowY == 242)
                {
                }
                else if (this.nowY <= 261)
                {
                }
                else if (this.nowY == 262)
                {
                }
                else
                {
                    throw new EmulatorException("Invalid scanline " + this.nowY);
                }
            }
        }

        public void ConnectCartridge(Cartridge cartridge)
        {
            this.cartridge = cartridge;
            this.cartridge.ConnectInternalVram(this.internalVram);
        }

        void SpriteEval()
        {
            var y = this.nowY - 1;
            byte _spriteHitCnt = 0;
            this.lostSprites = false;
            bool bigSprite = this.spriteHeight == 16;
            ushort spriteTileAddrBase = this.patternTableAddress8x8Sprites;
            for (ushort i = 0; i < 256; i += 4)
            {
                byte spY = (byte)(ReadSprite(i) + 1);
                ushort spYend = (ushort)(spY + this.spriteHeight);
                bool hit = false;
                if (spY <= y && y < spYend)
                {//Hit!
                    if (_spriteHitCnt < defaultSpriteCnt)
                    {
                        hit = true;
                        SpriteSlot slot = spriteTable[_spriteHitCnt];
                        slot.Idx = (byte)(i >> 2);
                        slot.Y = spY;
                        slot.X = ReadSprite((ushort)(i + 3));
                        if (bigSprite)
                        {
                            //8x16
                            byte val = ReadSprite((ushort)(i + 1));
                            slot.TileAddr = (ushort)((val & 1) << 12 | (val & 0xfe) << 4);
                        }
                        else
                        {
                            //8x8
                            slot.TileAddr = (ushort)((ReadSprite((ushort)(i + 1)) << 4) | spriteTileAddrBase);
                        }
                        byte attr = ReadSprite((ushort)(i + 2));
                        slot.PaletteNo = (byte)(4 | (attr & 3));
                        slot.IsForeground = (attr & (1 << 5)) == 0;
                        slot.FlipHorizontal = (attr & (1 << 6)) != 0;
                        slot.FlipVertical = (attr & (1 << 7)) != 0;
                        _spriteHitCnt++;
                    }
                    else
                    {
                        //本当はもっと複雑な仕様みたいなものの、省略。
                        //http://wiki.nesdev.com/w/index.php/PPU_sprite_evaluation のSprite overflow bugの項目を参照
                        this.lostSprites = true;
                        break;
                    }
                }
            }
            //残りは無効化
            this.spriteHitCnt = _spriteHitCnt;
            for (ushort i = _spriteHitCnt; i < defaultSpriteCnt; i++)
            {
                spriteTable[i].Y = 255;
            }
        }

        void BuildSpriteLine()
        {
            if (!this.spriteVisibility) return;

            var y = this.nowY - 1;
            bool searchSprite0Hit = !this.sprite0Hit;
            ReadVram(this.spriteTable[0].TileAddr); //読み込まないと、MMC4が動かない。
            for (byte i = 0; i < this.spriteHitCnt; i++)
            {
                SpriteSlot slot = this.spriteTable[i];
                searchSprite0Hit &= (slot.Idx == 0);
                ushort offY = 0;

                if (slot.FlipVertical)
                    offY = (ushort)(this.spriteHeight + slot.Y - y - 1);
                else
                    offY = (ushort)(y - slot.Y);
                ushort off = (ushort)(slot.TileAddr | ((offY & 0x8) << 1) | (offY & 7));
                byte firstPlane = ReadVram(off);
                byte secondPlane = ReadVram((ushort)(off + 8));
                ushort endX = (ushort)Math.Min(ScreenWidth - slot.X, 8);
                byte layerMask = slot.IsForeground ? FrontSpriteBit : BackSpriteBit;
                for (ushort x = 0; x < endX; x++)
                {
                    byte color;
                    if (slot.FlipHorizontal)
                        color = (byte)(((firstPlane >> x) & 1) | (((secondPlane >> x) & 1) << 1));
                    else
                        color = (byte)(((firstPlane >> (7 - x)) & 1) | (((secondPlane >> (7 - x)) & 1) << 1));
                    byte target = screenBuffer[y, slot.X + x];
                    bool isEmpty = (target & LayerBitMask) == EmptyBit;
                    bool isBackgroundDrawn = (target & LayerBitMask) == BackgroundBit;
                    bool isSpriteNotDrawn = (target & SpriteLayerBit) == 0;
                    if (searchSprite0Hit && (color != 0 && isBackgroundDrawn))
                    {
                        this.sprite0Hit = true;
                        searchSprite0Hit = false;
                    }
                    if (color != 0 && ((!slot.IsForeground && isEmpty) || (slot.IsForeground && isSpriteNotDrawn)))
                        screenBuffer[y, slot.X + x] = (byte)(this.palette[slot.PaletteNo, color] | layerMask);
                }
            }
        }

        void BuildBgLine()
        {
            if (!this.backgroundVisibility) return;
            ushort nameTableAddr = (ushort)(0x2000 | (vramAddrRegister & 0xfff));
            byte offY = (byte)(vramAddrRegister >> 12);
            byte offX = this.horizontalScrollBits;

            ushort bgTileAddrBase = this.patternTableAddressBackground;

            ushort renderX = 0;
            while (true)
            {
                ushort tileNo = ReadVram(nameTableAddr);
                ushort tileYofScreen = (ushort)((nameTableAddr & 0x03e0) >> 5);
                byte palNo =
                        (byte)((
                            ReadVram((ushort)((nameTableAddr & 0x2f00) | 0x3c0 | ((tileYofScreen & 0x1C) << 1) | ((nameTableAddr >> 2) & 7)))
                                        >> (((tileYofScreen & 2) << 1) | (nameTableAddr & 2))
                                ) & 0x3);
                //タイルのサーフェイスデータを取得
                ushort off = (ushort)(bgTileAddrBase | (tileNo << 4) | offY);
                byte firstPlane = ReadVram(off);
                byte secondPlane = ReadVram((ushort)(off + 8));
                //書く！
                for (byte x = offX; x < 8; x++)
                {
                    byte color = (byte)(((firstPlane >> (7 - x)) & 1) | (((secondPlane >> (7 - x)) & 1) << 1));
                    if (color != 0)
                        screenBuffer[nowY - 1, renderX] = (byte)(this.palette[palNo, color] | BackgroundBit);
                    renderX++;
                    if (renderX >= ScreenWidth)
                        return;
                }
                if ((nameTableAddr & 0x001f) == 0x001f)
                {
                    nameTableAddr &= 0xFFE0;
                    nameTableAddr ^= 0x400;
                }
                else
                {
                    nameTableAddr++;
                }
                offX = 0;//次からは最初のピクセルから書ける            
            }
        }

        public byte ReadReg(ushort addr)
        {
            //http://wiki.nesdev.com/w/index.php/PPU_registers#OAM_data_.28.242004.29_.3C.3E_read.2Fwrite
            switch (addr & 0x07)
            {
                case 0x02:
                    return BuildPPUStatusRegister();
                case 0x04:
                    return ReadSpriteDataRegister();
                default: return 0;
            }
        }

        public void WriteReg(ushort addr, byte value)
        {
            //http://wiki.nesdev.com/w/index.php/PPU_registers#OAM_data_.28.242004.29_.3C.3E_read.2Fwrite
            switch (addr & 0x07)
            {
                case 0x00:
                    AnalyzePPUControlRegister1(value);
                    break;
                case 0x01:
                    AnalyzePPUControlRegister2(value);
                    break;
                case 0x03:
                    AnalyzeSpriteAddrRegister(value);
                    break;
                case 0x04:
                    WriteSpriteDataRegister(value);
                    break;
                case 0x05:
                    AnalyzePPUBackgroundScrollingOffset(value);
                    break;
                case 0x06:
                    AnalyzeVramAddrRegister(value);
                    break;
                case 0x07:
                    WriteVramDataRegister(value);
                    break;
                default:
                    throw new EmulatorException("Invalid addr: 0x" + addr.ToString("x2"));
            }
        }

        byte BuildPPUStatusRegister()
        {
            vramAddrRegisterWritten = false;
            scrollRegisterWritten = false;
            byte result = (byte)((this.nowOnVBnank ? 128 : 0)
                                 | (this.sprite0Hit ? 64 : 0)
                                 | (this.lostSprites ? 32 : 0));
            this.nowOnVBnank = false;
            return result;
        }

        void AnalyzePPUControlRegister1(byte val)
        {
            executeNMIonVBlank = ((val & 0x80) == 0x80) ? true : false;
            spriteHeight = (byte)(((val & 0x20) == 0x20) ? 16 : 8);
            patternTableAddressBackground = (byte)((val & 0x10) << 8);
            patternTableAddress8x8Sprites = (byte)((val & 0x8) << 9);
            vramIncrementSize = (byte)(((val & 0x4) == 0x4) ? 32 : 1);
            vramAddrReloadRegister = (ushort)((vramAddrReloadRegister & 0x73ff) | ((val & 0x3) << 10));
        }

        void AnalyzePPUControlRegister2(byte val)
        {
            colorEmphasis = (byte)(val >> 5);
            spriteVisibility = ((val & 0x10) == 0x10) ? true : false;
            backgroundVisibility = ((val & 0x08) == 0x08) ? true : false;
            spriteClipping = ((val & 0x04) == 0x04) ? false : true;
            backgroundClipping = ((val & 0x2) == 0x02) ? false : true;
            paletteMask = (byte)(((val & 0x1) == 0x01) ? 0x30 : 0x3f);
        }

        void AnalyzePPUBackgroundScrollingOffset(byte val)
        {
            if (scrollRegisterWritten)
            {
                vramAddrReloadRegister = (ushort)((vramAddrReloadRegister & 0x8C1F) | ((val & 0xf8) << 2) | ((val & 7) << 12));
            }
            else
            {
                vramAddrReloadRegister = (ushort)((vramAddrReloadRegister & 0xFFE0) | val >> 3);
                horizontalScrollBits = (byte)(val & 7);
            }
            scrollRegisterWritten = !scrollRegisterWritten;
        }

        void AnalyzeVramAddrRegister(byte val)
        {
            if (vramAddrRegisterWritten)
            {
                vramAddrReloadRegister = (ushort)((vramAddrReloadRegister & 0x7f00) | val);
                vramAddrRegister = (ushort)(vramAddrReloadRegister & 0x3fff);
            }
            else
            {
                vramAddrReloadRegister = (ushort)((vramAddrReloadRegister & 0x00ff) | ((val & 0x7f) << 8));
            }
            vramAddrRegisterWritten = !vramAddrRegisterWritten;
        }

        void AnalyzeSpriteAddrRegister(byte val)
        {
            spriteAddr = val;
        }

        byte ReadSpriteDataRegister()
        {
            return ReadSprite(spriteAddr);
        }

        void WriteSpriteDataRegister(byte val)
        {
            WriteVram(vramAddrRegister, val);
            vramAddrRegister = (ushort)((vramAddrRegister + vramIncrementSize) & 0x3fff);
        }

        byte ReadSprite(ushort addr)
        {
            return this.spRam[addr];
        }

        byte ReadVramDataRegister(ushort addr)
        {
            if ((vramAddrRegister & 0x3f00) == 0x3f00)
            {
                byte ret = ReadPalette(vramAddrRegister);
                vramBuffer = ReadVramExternal(vramAddrRegister); //ミラーされてるVRAMにも同時にアクセスしなければならない。
                vramAddrRegister = (ushort)((vramAddrRegister + vramIncrementSize) & 0x3fff);
                return ret;
            }
            else
            {
                byte ret = vramBuffer;
                vramBuffer = ReadVramExternal(vramAddrRegister);
                vramAddrRegister = (ushort)((vramAddrRegister + vramIncrementSize) & 0x3fff);
                return ret;
            }
        }

        void WriteVramDataRegister(byte value)
        {
            WriteVram(vramAddrRegister, value);
            vramAddrRegister = (ushort)((vramAddrRegister + vramIncrementSize) & 0x3fff);
        }

        byte ReadVram(ushort addr)
        {
            if ((addr & 0x3f00) == 0x3f00)
                return ReadPalette(addr);
            else
                return ReadVramExternal(addr);
        }

        void WriteVram(ushort addr, byte val)
        {
            if ((addr & 0x3f00) == 0x3f00)
                WritePalette(addr, val);
            else
                WriteVramExternal(addr, val);
        }

        byte ReadVramExternal(ushort addr)
        {
            switch (addr & 0x3000)
            {
                case 0x0000:
                    return this.cartridge.ReadPatternTableLow(addr);
                case 0x1000:
                    return this.cartridge.ReadPatternTableHigh(addr);
                case 0x2000:
                    return this.cartridge.ReadNameTable(addr);
                case 0x3000:
                    return this.cartridge.ReadNameTable(addr);
                default:
                    throw new EmulatorException("Invalid vram access");
            }
        }

        void WriteVramExternal(ushort addr, byte val)
        {
            switch (addr & 0x3000)
            {
                case 0x0000:
                    this.cartridge.WritePatternTableLow(addr, val);
                    break;
                case 0x1000:
                    this.cartridge.WritePatternTableHigh(addr, val);
                    break;
                case 0x2000:
                    this.cartridge.WriteNameTable(addr, val);
                    break;
                case 0x3000:
                    this.cartridge.WriteNameTable(addr, val);
                    break;
                default:
                    throw new EmulatorException("Invalid vram access");
            }
        }

        byte ReadPalette(ushort addr)
        {
            if ((addr & 0x03) == 0)
                return this.palette[8, (addr >> 2) & 3];
            else
                return this.palette[(addr >> 2) & 7, addr & 3];
        }

        void WritePalette(ushort addr, byte val)
        {
            if ((addr & 0x03) == 0)
                this.palette[8, (addr >> 2) & 3] = (byte)(val & 0x3f);
            else
                this.palette[(addr >> 2) & 7, addr & 3] = (byte)(val & 0x3f);
        }
    }

    class SpriteSlot
    {
        public byte Idx;
        public byte Y;
        public byte X;
        public byte PaletteNo;
        public ushort TileAddr;
        public bool IsForeground;
        public bool FlipHorizontal;
        public bool FlipVertical;

        public SpriteSlot(byte idx, byte y, byte x, byte paletteNo,  ushort tileAddr, bool isForeground, bool flipHorizontal, bool flipVertical)
        {
            this.Idx = idx;
            this.Y = y;
            this.X = x;
            this.PaletteNo = paletteNo;
            this.TileAddr = tileAddr;
            this.IsForeground = isForeground;
            this.FlipHorizontal = flipHorizontal;
            this.FlipVertical = flipVertical;
        }
    }
}
