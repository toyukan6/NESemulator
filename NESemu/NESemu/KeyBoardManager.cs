using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace NESemulator
{
    //キーボードを管理
    static class KeyManager
    {
        private static KeyboardState keyboardState;			//キーボードの状態

        private static Keys[,] keyCodes = new Keys[,]
        {
            { Keys.Z, Keys.Q },
            { Keys.X, Keys.W },
            { Keys.LeftShift, Keys.Tab },
            { Keys.Enter, Keys.P },
            { Keys.Up, Keys.K },
            { Keys.Down, Keys.J },
            { Keys.Left, Keys.H },
            { Keys.Right, Keys.L }
        };

        //キーの状態を更新
        public static void Update()
        {
            keyboardState = Keyboard.GetState();
        }

        //キーの状態を取得
        static bool GetKeyButtonState(Keys keys)
        {
            return keyboardState.IsKeyDown(keys);
        }

        public static bool IsPressed(int index, KeyID id)
        {
            return GetKeyButtonState(keyCodes[(int)id, index]);
        }
    }
}
