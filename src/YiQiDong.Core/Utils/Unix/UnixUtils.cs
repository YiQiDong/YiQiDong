using Quick.Shell.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace YiQiDong.Core.Utils.Unix
{
    public partial class UnixUtils
    {
        internal const int PR_SET_NAME = 15;

        private static Dictionary<string, string> fileNameReplaceDict = new Dictionary<string, string>()
        {
            [" "] = "\\ ",
            ["\""] = "\\\"",
            ["'"] = "\\'",
            ["`"] = "\\`"
        };

        /// <summary>
        /// 为文件添加可执行权限
        /// </summary>
        /// <param name="fileName"></param>
        public static void AddExecutePermissionToFile(string fileName)
        {
            foreach (var key in fileNameReplaceDict.Keys)
            {
                if (fileName.Contains(key))
                    fileName = fileName.Replace(key, fileNameReplaceDict[key]);
            }
            ProcessUtils.ExecuteShell($"chmod +x {fileName}");
        }

        /// <summary>
        /// 当前是否以root账号运行
        /// </summary>
        /// <returns></returns>
        public static bool IsRuningWithRoot()
        {
            return Environment.UserName == "root";
        }

        [LibraryImport("libc", EntryPoint = nameof(prctl), StringMarshalling = StringMarshalling.Utf8)]
        internal static partial int prctl(int option, string arg2);

        /// <summary>
        /// 设置进程名称
        /// </summary>
        /// <param name="processName"></param>
        public static void SetProcessName(string processName)
        {
            prctl(PR_SET_NAME, processName);
        }
    }
}
