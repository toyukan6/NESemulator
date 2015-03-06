using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NESemulator
{
    /// <summary>
    /// 例外
    /// </summary>
    class EmulatorException : Exception
    {
        public string Msg { get; private set; }

        public EmulatorException()
        { }

        public EmulatorException(string msg)
        {
            Msg = msg;
        }

        public EmulatorException(EmulatorException e)
        {
            Msg = e.Msg;
        }

        public EmulatorException(string format, params object[] args)
        {
            Msg = string.Format(format, args);
        }
    }
}
