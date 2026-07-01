using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace YiQiDong.Utils;

public class Win32Utils
{
    public static long GetFileTimeLong(FILETIME ft)
    {
        long time = ft.dwHighDateTime;
        time = time << 32;
        time = time | (uint)ft.dwLowDateTime;
        return time;
    }

    public static bool GetSystemTimes(ref long lIdleTime, ref long lKernelTime, ref long lUserTime)
    {
        unsafe
        {
            FILETIME ftIdleTime = new();
            FILETIME ftKernelTime = new();
            FILETIME ftUserTime = new();

#pragma warning disable CA1416 // 验证平台兼容性
            var ret = Windows.Win32.PInvoke.GetSystemTimes(&ftIdleTime, &ftKernelTime, &ftUserTime);
#pragma warning restore CA1416 // 验证平台兼容性
            if (ret.Value == 0)
                return false;
            if (ret.Value != 0)
            {
                lIdleTime = GetFileTimeLong(ftIdleTime);
                lKernelTime = GetFileTimeLong(ftKernelTime);
                lUserTime = GetFileTimeLong(ftUserTime);
            }
            return true;
        }
    }
}
