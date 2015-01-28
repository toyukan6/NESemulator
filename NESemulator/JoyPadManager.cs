using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace NESemulator
{
    /* winmm.dllを利用する版。
     * ラッパークラスは分離。 */
    class JoyPadManager
    {
        /// <summary>
        /// パッド最大数
        /// </summary>
        const int padNum = 2;
        static JoyWrapper.JOYINFOEX[] padstate = new JoyWrapper.JOYINFOEX[padNum];
        static JoyWrapper.JOYINFOEX[] padstateold = new JoyWrapper.JOYINFOEX[padNum];
        static int[] padid = new int[padNum];
        static JoyWrapper.JOYCAPS[] padcaps = new JoyWrapper.JOYCAPS[padNum];
        /// <summary>
        /// 遊びの大きさ。これ以下の傾きは検知しない。単位は％。
        /// </summary>
        public static int JoyPlay = 40;
        /// <summary>
        /// キーコンフィグ。コントローラーごとのセット不可、ハードコード済みなど実装がまだ甘い。
        /// （JoyButtons[(int)KeyID]番目のボタンが対応）
        /// </summary>
        public static byte[] JoyButtons = new byte[] { 255, 255, 255, 255, 0, 1, 2, 3, 4, 5, 6, 7, 8 };
        static JoyPadManager()
        {
            GetPad();
        }
        /// <summary>
        /// index番目のパッドは有効かどうか
        /// </summary>
        public static bool Enable(int index) { return padid[index] >= 0; }
        /// <summary>
        /// index番目のパッドのJOYINFOEXの取得
        /// </summary>
        public static JoyWrapper.JOYINFOEX PadState(int index) { return padstate[index]; }
        /// <summary>
        /// index番目のパッドのJOYCAPSの取得
        /// </summary>
        public static JoyWrapper.JOYCAPS PadCaps(int index) { return padcaps[index]; }
        /// <summary>
        /// パッド再取得（joyGetDevCapsやjoyGetPosExは失敗すると重いので
        /// タイトル画面に戻るときなど気にならないところで行う）
        /// </summary>
        public static void GetPad()
        {
            int l = JoyWrapper.joyGetNumDevs();
            for (int k = 0; k < padNum; k++)
            {
                padid[k] = -1;
                padcaps[k] = new JoyWrapper.JOYCAPS();
                for (int i = k == 0 ? 0 : padid[k - 1] + 1; i < l; i++)
                {
                    JoyWrapper.JoyError j = JoyWrapper.joyGetDevCaps(i, ref padcaps[k], JoyWrapper.JoyCapsSize);
                    if (j == JoyWrapper.JoyError.NoError)
                    {
                        padid[k] = i;
                        break;
                    }
                }
                if (padid[k] == -1) return;
            }
        }
        /// <summary>
        /// パッド情報の更新
        /// </summary>
        public static void Update()
        {
            for (int i = 0; i < padNum; i++)
            {
                if (padid[i] < 0) return;
                padstateold[i] = padstate[i];
                //取得するボタンの情報をセット
                padstate[i] = new JoyWrapper.JOYINFOEX();
                padstate[i].dwFlags = JoyWrapper.JoyReturns.All;
                padstate[i].dwSize = JoyWrapper.JoyInfoExSize;
                if (JoyWrapper.joyGetPosEx(padid[i], ref padstate[i]) == JoyWrapper.JoyError.NoError)
                {
                    continue;
                }
                //失敗したらパッドを無効化
                padid[i] = -1;
                padstateold[i] = new JoyWrapper.JOYINFOEX();
            }
        }
        static bool IsKeyPressed(int index, JoyWrapper.JOYINFOEX state, KeyID id, byte[] buttons)
        {
            double play = JoyPlay * 0.005;
            if (padid[index] < 0) return false;
            uint pov = state.dwPOV;
            JoyWrapper.JOYCAPS caps = padcaps[index];
            double x;
            switch (id)
            {
                case KeyID.Left:
                    x = (double)(state.dwXpos - caps.wXmin) / (caps.wXmax - caps.wXmin);    //入力を0～1に変換　ただし大抵はここまでせずに0～65535としてよい
                    return x < 0.5 - play
                    || ((caps.wCaps & JoyWrapper.JoyCaps.HasPov) != 0 && 22500 <= pov && pov <= 31500); //POVでも動かせるようにする　スティック非使用なら便利
                case KeyID.Right:
                    x = (double)(state.dwXpos - caps.wXmin) / (caps.wXmax - caps.wXmin);
                    return x > 0.5 + play
                    || ((caps.wCaps & JoyWrapper.JoyCaps.HasPov) != 0 && 4500 <= pov && pov <= 13500);
                case KeyID.Up:
                    x = (double)(state.dwYpos - caps.wYmin) / (caps.wYmax - caps.wYmin);
                    return x < 0.5 - play
                    || ((caps.wCaps & JoyWrapper.JoyCaps.HasPov) != 0 && 0 <= pov && pov <= 4500) || (31500 <= pov && pov <= 36000);
                case KeyID.Down:
                    x = (double)(state.dwYpos - caps.wYmin) / (caps.wYmax - caps.wYmin);
                    return x > 0.5 + play
                    || ((caps.wCaps & JoyWrapper.JoyCaps.HasPov) != 0 && 13500 <= pov && pov <= 22500);
                default:
                    if (buttons.Length <= (int)id || 32 <= buttons[(int)id]) return false;
                    return (state.dwButtons & Pow2(buttons[(int)id])) != 0;
            }
        }
        /// <summary>
        /// index番目のパッドのidに対応するボタンは押されているか
        /// </summary>
        public static bool IsKeyPressed(int index, KeyID id)
        {
            return IsKeyPressed(index, padstate[index], id, JoyButtons);
        }
        /// <summary>
        /// index番目のパッドのidに対応するボタンは押されていたか
        /// </summary>
        public static bool IsKeyPressedOld(int index, KeyID id)
        {
            return IsKeyPressed(index, padstateold[index], id, JoyButtons);
        }
        /// <summary>
        /// index番目のパッドのidに対応するボタンは新たに押されたか
        /// </summary>
        public static bool IsKeyNewPressed(int index, KeyID id)
        {
            return IsKeyPressed(index, id) && !IsKeyPressedOld(index, id);
        }
        /// <summary>
        /// 押されているボタンの中で最小のものを返す
        /// </summary>
        public static int FirstPressedKey(int index)
        {
            if (padid[index] < 0) return -1;
            for (int i = 0, j = 1; i < 32; i++, j <<= 1)
                if ((padstate[index].dwButtons & j) != 0 && (padstateold[index].dwButtons & j) == 0) return i;
            return -1;
        }
        static uint Pow2(int n)
        {
            return 1u << n;
        }
    }
    /// <summary>
    /// 関数ラッパー用クラス
    /// </summary>
    class JoyWrapper
    {
        [DllImport("winmm.dll")]
        public static extern JoyError joyGetPosEx(int uJoyID, ref JOYINFOEX pji);
        [DllImport("winmm.dll")]
        public static extern JoyError joyGetDevCaps(int uJoyID, ref JOYCAPS pjc, int cbjc);
        [DllImport("winmm.dll")]
        public static extern int joyGetNumDevs();

        public static readonly int JoyInfoExSize = Marshal.SizeOf(typeof(JoyWrapper.JOYINFOEX));
        public static readonly int JoyCapsSize = Marshal.SizeOf(typeof(JoyWrapper.JOYCAPS));

        [StructLayout(LayoutKind.Sequential)]
        public struct JOYINFOEX
        {
            public int dwSize;
            public JoyReturns dwFlags;
            public uint dwXpos;
            public uint dwYpos;
            public uint dwZpos;
            public uint dwRpos;
            public uint dwUpos;
            public uint dwVpos;
            public uint dwButtons;
            public uint dwButtonNumber;
            public uint dwPOV;
            public uint dwReserved1;
            public uint dwReserved2;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct JOYCAPS
        {
            public ushort wMid;
            public ushort wPid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szPname;
            public uint wXmin;
            public uint wXmax;
            public uint wYmin;
            public uint wYmax;
            public uint wZmin;
            public uint wZmax;
            public uint wNumButtons;
            public uint wPeriodMin;
            public uint wPeriodMax;
            public uint wRmin;
            public uint wRmax;
            public uint wUmin;
            public uint wUmax;
            public uint wVmin;
            public uint wVmax;
            public JoyCaps wCaps;
            public uint wMaxAxes;
            public uint wNumAxes;
            public uint wMaxButtons;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szRegKey;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szOEMVxD;
        }
        public enum JoyError : ushort
        {
            NoError = 0,
            BadDeviceID = 2,
            NoDriver = 6,
            InvalParam = 11,
            Parms = 165,
            NoCanDo = 166,
            UnPluggeed = 167,
        }
        [Flags]
        public enum JoyCaps : uint
        {
            HasZ = 1,
            HasR = 2,
            HasU = 4,
            HasV = 8,
            HasPov = 0x10,
            Pov4Dir = 0x20,
            PovCts = 0x40,
        }
        [Flags]
        public enum JoyReturns : uint
        {
            X = 1,
            Y = 2,
            Z = 4,
            R = 8,
            U = 0x10,
            V = 0x20,
            Pov = 0x40,
            Buttons = 0x80,
            RawData = 0x100,
            PovCts = 0x200,
            Centered = 0x400,
            All = X | Y | Z | R | U | V | Pov | Buttons | PovCts | Centered,
        }
    }

    enum KeyID { A, B, START, SELECT, Up, Down, Right, Left }
}
