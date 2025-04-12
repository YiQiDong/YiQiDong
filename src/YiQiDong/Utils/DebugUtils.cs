using System;
using System.Collections.Generic;
using System.Text;

namespace YiQiDong.Utils
{
    internal class DebugUtils
    {
        public static bool IsDebug()
        {
#if (DEBUG)
            return true;
#else
            return false;
#endif
        }
    }
}
