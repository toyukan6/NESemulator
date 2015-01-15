using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NESemulator
{
    /// <summary>
    /// リセットのあるもの
    /// OnHardResetとOnResetの挙動は 
    /// http://wiki.nesdev.com/w/index.php/CPU_power_up_state や
    /// http://pgate1.at-ninja.jp/NES_on_FPGA/ とかを参照
    /// </summary>
    interface IReset
    {
        /// <summary>
        /// 電源オン時のリセット
        /// </summary>
        void OnHardReset();
        /// <summary>
        /// リセットボタンでのリセット
        /// </summary>
        void OnReset();
    }
}
