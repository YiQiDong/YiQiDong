using Octokit;
using Quick.Build;
using SharpCompress.Archives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace _build.Resources
{
    public class Runtime_java : IResource
    {
        public string Id => "java";

        public string Name => "Java运行库";

        private string[] getJavaVersions()
        {
            return new[] { "8", "11", "17", "21" };
        }

        private ReleaseAsset getJavaRuntimeAsset(HttpClient client, string javaVersion, string arch,out string jreVersion)
        {
            var workspaceName = "adoptium";
            var repositoryName = $"temurin{javaVersion}-binaries";
            
            var github = new GitHubClient(new ProductHeaderValue(repositoryName));
            var releaseResult = github.Repository.Release.GetLatest(workspaceName, repositoryName).Result;

            if (int.Parse(javaVersion) > 9)
            {
                //jdk-11.0.21+9
                //jdk-11.0.20.1+1
                //jdk-11.0.18+10
                jreVersion = releaseResult.Name.Replace("jdk-", string.Empty);
                switch (jreVersion.Count(t => t == '.'))
                {
                    case 1:
                    case 2:
                        jreVersion = jreVersion.Replace("+", ".");
                        break;
                    case 3:
                    default:
                        jreVersion = jreVersion.Replace("+", string.Empty);
                        break;

                }
            }
            else
            {
                //jdk8u392-b08
                var vRegex = new Regex("u(?<v>.*)?-");
                var v = vRegex.Match(releaseResult.Name).Groups["v"].Value;
                jreVersion = $"{javaVersion}.0.{v}";
            }

            var fileExt = "tar.gz";
            if (arch.EndsWith("windows"))
                fileExt = ".zip";
            var release = releaseResult.Assets
                .FirstOrDefault(t => t.Name.Contains("jre_" + arch) && t.Name.EndsWith(fileExt));
            return release;
        }

        public void Invoke()
        {
            var displayDownloadProgress = new Action<QbNet.TransferProgress>(t =>
            {
                QbConsole.DisplaySameLineInConsole($"[{t.Current * 100 / t.Total}%]进度：{t.Current.ToString("N0")}/{t.Total.ToString("N0")}，速度：{t.Speed.ToString("N0")}，剩余时间：{t.RemainingTime}");
            });

            HttpClient client = new HttpClient();
            Console.WriteLine("正在获取Java版本列表...");
            var versions = getJavaVersions();
            Console.WriteLine("请选择Java版本：");
            var javaVersion = QbSelect.ArrowSelect(versions.ToDictionary(t => t, t => t).ToArray(), selectedForegroundColor: ConsoleColor.Green);
            var archDict = new Dictionary<string, string>()
            {
                ["win-x64"] = "x64_windows",
                ["linux-x64"] = "x64_linux",
                ["linux-arm64"] = "aarch64_linux",
                //["osx-x64"] = "x64_mac",
            };
            Console.WriteLine("请选择打包架构(一个都不勾选代表全选)：");
            var selectArchs = QbSelect.MultiSelect(archDict.ToArray(), selectedForegroundColor: ConsoleColor.Green);
            if (selectArchs == null || selectArchs.Length == 0)
                selectArchs = archDict.Keys.ToArray();
            
            foreach (var arch in selectArchs)
            {
                Console.WriteLine($"正在打包[{arch}]...");
                var releaseAsset = getJavaRuntimeAsset(client, javaVersion, archDict[arch], out var jreVersion);
                var tmpFileName = Path.Combine("bin", releaseAsset.Name);
                var tmpFolder = Path.Combine("bin", $"{Id}-{jreVersion}-{arch}");
                if (Directory.Exists(tmpFolder))
                    Directory.Delete(tmpFolder, true);
                var fileExt = Path.GetExtension(tmpFileName);
                try
                {
                    if (!File.Exists(tmpFileName))
                    {
                        Console.WriteLine($"正在从[{releaseAsset.BrowserDownloadUrl}]下载...");
                        QbNet.DownloadFile(releaseAsset.BrowserDownloadUrl, tmpFileName, CancellationToken.None, displayDownloadProgress).Wait();
                    }
                    Console.WriteLine($"文件[{tmpFileName}]下载完成，正在解压...");
                    switch (fileExt)
                    {
                        case ".zip":
                            {
                                using (var archive = SharpCompress.Archives.Zip.ZipArchive.Open(tmpFileName))
                                    archive.ExtractToDirectory(tmpFolder);
                                break;
                            }
                        case ".gz":
                            {
                                using (var archive = SharpCompress.Archives.Tar.TarArchive.Open(tmpFileName))
                                    archive.ExtractToDirectory(tmpFolder);
                                break;
                            }
                    }
                    var tmpJreFolder = Directory.GetDirectories(tmpFolder).FirstOrDefault();

                    Console.WriteLine($"正在打包易启动运行库文件...");
                    //生成元信息文件
                    var metaObj = new YiQiDong.Core.Protocol.V1.Model.RuntimeInfo()
                    {
                        Id = $"{Name}-{jreVersion}",
                        Name = Id,
                        Version = jreVersion,
                        BuildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Description = "Java™ 是世界领先的编程语言和平台。Adoptium 工作推进和支持高质量、TCK 认证的运行时和其相关技术，使其在 Java 生态系统中应用。Eclipse Temurin 是 Adoptium OpenJDK 发行版的名称。",
                        Platform = new[] { arch },
                        Environment = new Dictionary<string, string>()
                        {
                            ["JAVA_HOME"] = "$RUNTIME_DIR",
                            ["JRE_HOME"] = "$RUNTIME_DIR"
                        },
                        Path = new[] { "$RUNTIME_DIR/bin" },
                        ExecuteFiles = new[] { "$RUNTIME_DIR/bin/java" },
                        TestCommand = new Dictionary<string, string[]>()
                        {
                            ["版本"] = new[] { "java", "-version" }
                        }
                    };
                    if (arch.StartsWith("win-"))
                        metaObj.ExecuteFiles = metaObj.ExecuteFiles.Select(t => t + ".exe").ToArray();
                    var metaFile = Path.Combine(tmpJreFolder, IResource.RUNTIME_META_FILE);
                    File.WriteAllText(metaFile, JsonSerializer.Serialize(metaObj, new JsonSerializerOptions()
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        WriteIndented = true
                    }), Encoding.UTF8);
                    //打包
                    using (var archive2 = SharpCompress.Archives.Zip.ZipArchive.Create())
                    {
                        archive2.AddAllFromDirectory(tmpJreFolder);
                        archive2.SaveTo($"bin/{Id}-{jreVersion}-{arch}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.yrt",
                            new SharpCompress.Writers.WriterOptions(SharpCompress.Common.CompressionType.LZMA));
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    if (Directory.Exists(tmpFolder))
                        Directory.Delete(tmpFolder, true);
                    if (File.Exists(tmpFileName))
                        File.Delete(tmpFileName);
                }
            }
        }
    }
}
