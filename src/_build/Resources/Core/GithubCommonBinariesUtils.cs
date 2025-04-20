using Octokit;
using Quick.Build;
using SharpCompress.Archives;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using YiQiDong.Core.Protocol.V1.Model;
using YiQiDong.Protocol.V1.Model;

namespace _build.Resources.Core
{
    internal class GithubCommonBinariesUtils
    {
        public delegate ImageInfo GetImageInfoDelegate(string version, string rid);
        public delegate RuntimeInfo GetRuntimeInfoDelegate(string version, string rid);
        public delegate void BeforeZipHandleDelegate(string version, string rid, string folder);

        private static Dictionary<string, string> githubMirrorDict = new Dictionary<string, string>()
        {
            [""] = "直接连接",
            ["https://mirror.ghproxy.com/"] = "GhProxy"
        };

        public static void BuildImage(string imageId, string imageName, string workspaceName, string repositoryName,
            GetImageInfoDelegate getImageInfoDelegate,
            BeforeZipHandleDelegate beforeZipHandleDelegate = null
            )
        {
            var displayDownloadProgress = new Action<QbNet.TransferProgress>(t =>
            {
                QbConsole.DisplaySameLineInConsole($"[{t.Current * 100 / t.Total}%]进度：{t.Current.ToString("N0")}/{t.Total.ToString("N0")}，速度：{t.Speed.ToString("N0")}，剩余时间：{t.RemainingTime}");
            });

            Console.WriteLine("----------------------------------");
            Console.WriteLine($"  欢迎使用[适用于弈启动的{imageName}项目]编译脚本");
            Console.WriteLine("----------------------------------");

            HttpClient httpClient = new HttpClient();
            Console.WriteLine($"正在获取{imageName}文件信息...");
            var github = new GitHubClient(new ProductHeaderValue(repositoryName));
            var contents = github.Repository.Content.GetAllContentsByRef(workspaceName, repositoryName, "binaries").Result;
            var contentDict = contents.ToDictionary(t => t.Name.Replace(".tar.gz", string.Empty), t => t);

            Console.WriteLine($"请选择Github镜像站：");
            var githubMirror = QbSelect.ArrowSelect(githubMirrorDict.ToDictionary(t => t.Key, t => t.Value).ToArray(), selectedForegroundColor: ConsoleColor.Green);

            Console.WriteLine($"请选择要打包的{imageName}版本(一个都不选代表全选)：");
            var contentNames = QbSelect.MultiSelect(contentDict.ToDictionary(t => t.Key, t => t.Key).ToArray(), selectedForegroundColor: ConsoleColor.Green);
            if (contentNames == null || contentNames.Length == 0)
                contentNames = contentDict.Keys.ToArray();

            var tmpFolder = Path.Combine("bin", imageId);
            if (Directory.Exists(tmpFolder))
            {
                Directory.Delete(tmpFolder, true);
            }

            foreach (var contentName in contentNames)
            {
                var content = contentDict[contentName];
                var tmpPublishFolder = Path.Combine(tmpFolder, content.Sha);
                var strs = contentName.Split("-");
                var version = strs[1];
                var platform = strs[2];
                switch (platform)
                {
                    case "win32":
                        platform = "win";
                        break;
                }
                var rawRid = strs[3];
                var rid = platform;
                switch (rawRid)
                {
                    case "x86_64":
                        rid += "-x64";
                        break;
                    case "aarch64":
                        rid += "-arm64";
                        break;
                    default:
                        throw new NotImplementedException();
                }

                Console.WriteLine($"开始打包[{contentName}]...");
                if (Directory.Exists(tmpPublishFolder))
                {
                    Console.WriteLine($"正在清理目录...");
                    Directory.Delete(tmpPublishFolder, true);
                }
                Directory.CreateDirectory(tmpPublishFolder);
                try
                {
                    string url = content.DownloadUrl;
                    url = githubMirror + url;
                    //开始下载
                    var file = Path.Combine(tmpPublishFolder, contentName);
                    Console.WriteLine($"正在从[{url}]下载文件...");
                    QbNet.DownloadFile(url, file, CancellationToken.None, displayDownloadProgress).Wait();
                    //解压文件
                    Console.WriteLine($"文件[{file}]下载完成，正在解压...");
                    using (var tarArchive = SharpCompress.Archives.Tar.TarArchive.Open(file))
                        tarArchive.ExtractToDirectory(tmpPublishFolder);
                    //删除文件
                    File.Delete(file);

                    File.WriteAllText(
                        Path.Combine(tmpPublishFolder, IResource.IMAGE_META_FILE),
                        JsonSerializer.Serialize(getImageInfoDelegate(version, rid), new JsonSerializerOptions()
                        {
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                            WriteIndented = true
                        }),
                        Encoding.UTF8);
                    //压缩前处理
                    beforeZipHandleDelegate?.Invoke(version, rid, tmpPublishFolder);

                    var outFile = $"bin/{imageId}-{version}-{rid}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.ymg";
                    Console.WriteLine($"正在制作弈启动镜像[{rid}]...");
                    using (var archive = SharpCompress.Archives.Zip.ZipArchive.Create())
                    {
                        archive.AddAllFromDirectory(tmpPublishFolder);
                        archive.SaveTo(outFile, CompressionType.LZMA);
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    if (Directory.Exists(tmpPublishFolder))
                        Directory.Delete(tmpPublishFolder, true);
                }
            }
            if (Directory.Exists(tmpFolder))
                Directory.Delete(tmpFolder, true);
        }

        public static void BuildRuntime(string runtimeId, string runtimeName, string workspaceName, string repositoryName,
            GetRuntimeInfoDelegate getRuntimeInfoDelegate,
            BeforeZipHandleDelegate beforeZipHandleDelegate = null)
        {

            Console.WriteLine("----------------------------------");
            Console.WriteLine($"  欢迎使用[适用于弈启动的{runtimeName}项目]编译脚本");
            Console.WriteLine("----------------------------------");

            HttpClient httpClient = new HttpClient();
            Console.WriteLine($"正在获取{runtimeName}文件信息...");
            var github = new GitHubClient(new ProductHeaderValue(repositoryName));
            var contents = github.Repository.Content.GetAllContentsByRef(workspaceName, repositoryName, "binaries").Result;
            var contentDict = contents.ToDictionary(t => t.Name.Replace(".tar.gz", string.Empty), t => t);

            Console.WriteLine($"请选择Github镜像站：");
            var githubMirror = QbSelect.ArrowSelect(githubMirrorDict.ToDictionary(t => t.Key, t => t.Value).ToArray(), selectedForegroundColor: ConsoleColor.Green);

            Console.WriteLine($"请选择要打包的{runtimeName}版本(一个都不选代表全选)：");
            var contentNames = QbSelect.MultiSelect(contentDict.ToDictionary(t => t.Key, t => t.Key).ToArray(), selectedForegroundColor: ConsoleColor.Green);
            if (contentNames == null || contentNames.Length == 0)
                contentNames = contentDict.Keys.ToArray();

            var tmpFolder = Path.Combine("bin", runtimeId);
            if (Directory.Exists(tmpFolder))
            {
                Directory.Delete(tmpFolder, true);
            }

            foreach (var contentName in contentNames)
            {
                var content = contentDict[contentName];
                var tmpPublishFolder = Path.Combine(tmpFolder, content.Sha);
                var strs = contentName.Split("-");
                var version = strs[1];
                var platform = strs[2];
                switch (platform)
                {
                    case "win32":
                        platform = "win";
                        break;
                }
                var rawRid = strs[3];
                var rid = platform;
                switch (rawRid)
                {
                    case "x86_64":
                        rid += "-x64";
                        break;
                    case "aarch64":
                        rid += "-arm64";
                        break;
                    default:
                        throw new NotImplementedException();
                }

                Console.WriteLine($"开始打包[{contentName}]...");
                if (Directory.Exists(tmpPublishFolder))
                {
                    Console.WriteLine($"正在清理目录...");
                    Directory.Delete(tmpPublishFolder, true);
                }
                Directory.CreateDirectory(tmpPublishFolder);
                try
                {
                    string url = content.DownloadUrl;
                    url = githubMirror + url;
                    //开始下载
                    var file = Path.Combine(tmpPublishFolder, contentName);
                    Console.WriteLine($"正在从[{url}]下载文件...");
                    using (var fs = File.OpenWrite(file))
                    using (var ns = httpClient.GetStreamAsync(url).Result)
                        ns.CopyTo(fs);
                    //解压文件
                    Console.WriteLine($"文件[{file}]下载完成，正在解压...");
                    using (var tarArchive = SharpCompress.Archives.Tar.TarArchive.Open(file))
                        tarArchive.ExtractToDirectory(tmpPublishFolder);
                    //删除文件
                    File.Delete(file);

                    File.WriteAllText(
                        Path.Combine(tmpPublishFolder, IResource.RUNTIME_META_FILE),
                        JsonSerializer.Serialize(getRuntimeInfoDelegate(version, rid), new JsonSerializerOptions()
                        {
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                            WriteIndented = true
                        }),
                        Encoding.UTF8);
                    //压缩前处理
                    beforeZipHandleDelegate?.Invoke(version, rid, tmpPublishFolder);

                    var outFile = $"bin/{runtimeId}-{version}-{rid}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.yrt";
                    Console.WriteLine($"正在制作弈启动运行库[{rid}]...");
                    using (var archive = SharpCompress.Archives.Zip.ZipArchive.Create())
                    {
                        archive.AddAllFromDirectory(tmpPublishFolder);
                        archive.SaveTo(outFile, CompressionType.LZMA);
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    if (Directory.Exists(tmpPublishFolder))
                        Directory.Delete(tmpPublishFolder, true);
                }
            }
            if (Directory.Exists(tmpFolder))
                Directory.Delete(tmpFolder, true);
        }
    }
}
