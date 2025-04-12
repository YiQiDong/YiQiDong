using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace YiQiDong.Core.Utils
{
    public static class FileSystemUtils
    {
        public static void CopyFile(string file, string folder)
        {
            CopyFile(file, folder, false);
        }

        public static void CopyFile(string file, string folder, bool checkLastWriteTime)
        {
            var targetFile = Path.Combine(folder, Path.GetFileName(file));
            if (!File.Exists(file))
                return;
            if (File.Exists(targetFile))
            {
                if (!checkLastWriteTime)
                    return;
                //检查文件最后检查时间
                if (File.GetLastWriteTime(file) == File.GetLastWriteTime(targetFile))
                    return;
                File.Delete(targetFile);
            }
            File.Copy(file, targetFile);
        }

        public static void CopyFolder(string source, string target, string[] fileFilters = null, bool includeSubFolder = true)
        {
            if (!Directory.Exists(source) || Directory.Exists(target))
                return;

            Directory.CreateDirectory(target);
            string[] files = null;
            if (fileFilters == null || fileFilters.Length == 0)
            {
                files = Directory.GetFiles(source);
            }
            else
            {
                var fileList = new List<string>();
                foreach (var fileFilter in fileFilters)
                {
                    fileList.AddRange(Directory.GetFiles(source, fileFilter));
                }
                files = fileList.Distinct().ToArray();
            }
            //先复制文件
            foreach (var file in files)
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
            //如果包含子目录
            if (includeSubFolder)
            {
                //复制目录
                var folders = Directory.GetDirectories(source);
                foreach (var folder in folders)
                    CopyFolder(folder, Path.Combine(target, Path.GetFileName(folder)), fileFilters, includeSubFolder);
            }
        }
    }
}
