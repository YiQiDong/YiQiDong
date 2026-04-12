using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using YiQiDong.Core.Utils;
using System.Text.Json.Nodes;
using SharpCompress.Readers;

namespace YiQiDong.Utils
{
    public class UpdateUtils
    {
        public class VersionAndArch
        {
            public string Version { get; set; }
            public string Arch { get; set; }
        }

        private static VersionAndArch GetVersionAndArchFromZipArchive(SharpCompress.Archives.IArchive archive)
        {
            string content = string.Empty;
            //检查压缩包中的版本
            using (var archiveReader = archive.ExtractAllEntries())
                while (archiveReader.MoveToNextEntry())
                {
                    var entry = archiveReader.Entry;
                    if (entry.Key == Consts.CONFIG_JSON_FILENAME)
                    {
                        using (var entryStream = archiveReader.OpenEntryStream())
                        using (var reader = new StreamReader(entryStream))
                            content = reader.ReadToEnd();
                    }
                }

            if (string.IsNullOrEmpty(content))
                throw new ApplicationException("选择的文件不是有效的易启动程序文件！");

            var jObj = JsonNode.Parse(content).AsObject();
            var version = jObj[nameof(Consts.Version)].GetValue<string>();
            var arch = jObj[nameof(Consts.ARCH)].GetValue<string>();

            if (string.IsNullOrEmpty(version))
                throw new ApplicationException("选择的文件中未包含版本信息，不是有效的易启动程序文件！");
            if (string.IsNullOrEmpty(arch))
                throw new ApplicationException("选择的文件中未包含架构信息，不是有效的易启动程序文件！");
            if (!RuntimeUtils.IsMatchRID(arch))
                throw new ApplicationException($"选择的文件中的架构[{arch}]不匹配当前计算机架构[{RuntimeUtils.GetCurrentRID()}]");
            return new VersionAndArch()
            {
                Version = version,
                Arch = arch
            };
        }

        public static VersionAndArch GetVersionAndArchFromUpdateFile(string updateFile)
        {
            using (var archive = SharpCompress.Archives.Zip.ZipArchive.OpenArchive(updateFile))
            {
                return GetVersionAndArchFromZipArchive(archive);
            }
        }

        public static bool IsDataFolderInDanger()
        {
            var currentDir = Environment.CurrentDirectory;
            var dataFolder = Path.GetFullPath(Program.Config.DataFolder);
            //因为Windows系统上不区分大小写，所以全部转为小写
            if (OperatingSystem.IsWindows())
            {
                currentDir = currentDir.ToLower();
                dataFolder = dataFolder.ToLower();
            }
            if (dataFolder == currentDir || dataFolder.StartsWith(currentDir + Path.DirectorySeparatorChar))
                return true;
            return false;
        }

        public static async Task Update(string desDir, string updateFile, Action<string> pushLog = null, Action<int> progressNotifyAction = null)
        {
            await Task.Delay(100);
            using (var archive = SharpCompress.Archives.ArchiveFactory.OpenArchive(updateFile))
            {
                //检查数据目录是否存在风险
                if (IsDataFolderInDanger())
                {
                    throw new ApplicationException($"当前配置的数据目录[{Program.Config.DataFolder}]存在风险，更新时会删除镜像和容器文件。已禁止更新！");
                }
                //检查压缩包中的版本和架构
                var versionAndArch = GetVersionAndArchFromZipArchive(archive);
                pushLog?.Invoke($"正在更新到版本[{versionAndArch.Version}]，架构[{versionAndArch.Arch}]。");

                //删除目录
                if (Directory.Exists(desDir))
                {
                    try
                    {
                        pushLog?.Invoke($"删除目录内容: {desDir}");
                        foreach (var item in Directory.GetDirectories(desDir))
                            Directory.Delete(item, true);
                        foreach (var item in Directory.GetFiles(desDir))
                            File.Delete(item);
                    }
                    catch (Exception ex)
                    {
                        pushLog?.Invoke($"删除目录内容[{desDir}]时出错，原因：" + ex.Message);
                        return;
                    }
                }

                var totalCount = 0;
                using (var archiveReader = archive.ExtractAllEntries())
                    while (archiveReader.MoveToNextEntry())
                    {
                        totalCount++;
                    }

                var currentCount = 0;
                if (!Directory.Exists(desDir))
                    Directory.CreateDirectory(desDir);
                using (var archiveReader = archive.ExtractAllEntries())
                    while (archiveReader.MoveToNextEntry())
                    {
                        pushLog?.Invoke($"抽取: {archiveReader.Entry.Key}");
                        currentCount++;
                        progressNotifyAction?.Invoke(currentCount * 100 / totalCount);
                        archiveReader.WriteEntryToDirectory(desDir);
                    }
            }
        }
    }
}
