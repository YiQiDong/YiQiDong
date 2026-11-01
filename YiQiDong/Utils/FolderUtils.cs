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

        //重试最大次数
        private const int RETRY_MAX_TIMES = 10;
        //重试间隔
        private const int RETRY_INTERVAL = 5000;

        public static void PrepareToDeleteFolder(string folder, List<string> toDeleteFolderList, Action<int, int, string> progressHandler = null)
        {
            if (Directory.Exists(folder))
            {
                var tmpFolder = $"{folder}_{Guid.NewGuid():N}";
                for (var i = 0; i < RETRY_MAX_TIMES; i++)
                {
                    progressHandler?.Invoke(RETRY_MAX_TIMES, i + 1, $"正在移动目录[{folder}]...");
                    try 
                    {
                         Directory.Move(folder, tmpFolder);
                         break;
                    }
                    catch (Exception ex)
                    {
                        if (i + 1 >= RETRY_MAX_TIMES)
                            throw new IOException($"目录[{folder}]无法移动", ex);
                        Thread.Sleep(RETRY_INTERVAL);
                    }
                }
                toDeleteFolderList.Add(tmpFolder);
            }
        }

        public static void MoveFolder(string srcFolder, string desFolder, Action<int, int, string> progressHandler = null)
        {
            if (!Directory.Exists(srcFolder))
                return;
            for (var i = 0; i < RETRY_MAX_TIMES; i++)
            {
                progressHandler?.Invoke(RETRY_MAX_TIMES, i + 1, $"正在移动目录[{srcFolder}]到[{desFolder}]...");
                try 
                {
                     Directory.Move(srcFolder, desFolder);
                     break;
                }
                catch (Exception ex)
                {
                    if (i + 1 >= RETRY_MAX_TIMES)
                        throw new IOException($"无法移动目录[{srcFolder}]到[{desFolder}]", ex);
                    Thread.Sleep(RETRY_INTERVAL);
                }
            }
        }

        public static void DeleteFolder(List<string> toDeleteFolderList, Action<int, int, string> progressHandler = null)
        {
            foreach (var folder in toDeleteFolderList)
            {
                if (!Directory.Exists(folder))
                    continue;
                for (var i = 0; i < RETRY_MAX_TIMES; i++)
                {
                    progressHandler?.Invoke(RETRY_MAX_TIMES, i + 1, $"正在删除目录[{folder}]...");
                    try 
                    {
                         Directory.Delete(folder, true);
                         break;
                    }
                    catch (Exception ex)
                    {
                        if (i + 1 >= RETRY_MAX_TIMES)
                            throw new IOException($"目录[{folder}]无法删除", ex);
                        Thread.Sleep(RETRY_INTERVAL);
                    }
                }
            }
        }
    }
}
