using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace YiQiDong.Utils
{
    public static class FolderUtils
    {
        public static string DataFolder = null;

        private static string programDir = null;
        /// <summary>
        /// 获取程序目录
        /// </summary>
        /// <returns></returns>
        public static string GetProgramDir()
        {
            if (programDir == null)
            programDir = AppContext.BaseDirectory;
            return programDir;
        }

        public static string GetPathUnderDataDir(params string[] paths)
        {
            return Path.Combine(new[] { GetDataDir() }.Concat(paths).ToArray());
        }

        /// <summary>
        /// 获取程序目录下的路径
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        public static string GetPathUnderProgramDir(params string[] paths)
        {
            return Path.Combine(new[] { GetProgramDir() }.Concat(paths).ToArray());
        }

        /// <summary>
        /// 获取数据目录
        /// </summary>
        /// <returns></returns>
        public static string GetDataDir()
        {
            if (string.IsNullOrEmpty(DataFolder))
                throw new ArgumentNullException(nameof(DataFolder));
            return DataFolder;
        }

        /// <summary>
        /// 获取备份目录
        /// </summary>
        /// <returns></returns>
        public static string GetBackupDir()
        {
            return GetPathUnderDataDir("Backup");
        }
    }
}
