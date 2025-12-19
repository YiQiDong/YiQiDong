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
using System.Threading;

namespace _build.Resources
{
    public class Runtime_dotnet_sdk : IResource
    {
        public string Id => "dotnet-sdk";

        public string Name => ".NET SDK";

        private string[] getDotnetVersions(HttpClient client)
        {
            var url = "https://dotnet.microsoft.com/zh-cn/download/dotnet";
            var html = client.GetStringAsync(url).Result;
            var document = new HtmlAgilityPack.HtmlDocument();
            document.LoadHtml(html);
            var collection = document.DocumentNode.Descendants("tr").ToArray();
            List<string> versionList = new List<string>();
            foreach (var tr in collection)
            {
                var tds = tr.Descendants("td").ToArray();
                if (tds.Length < 3)
                    continue;
                var td = tds[tds.Length - 3];
                versionList.Add(td.InnerText);
            }
            return versionList.ToArray();
        }

        private string getDotNetSdkVersion(HttpClient client, string dotnetVersion)
        {
            var mainVersion = Version.Parse(dotnetVersion);
            var manVersionString = $"{mainVersion.Major}.{mainVersion.Minor}";
            var url = $"https://dotnet.microsoft.com/zh-cn/download/dotnet/{manVersionString}";
            var html = client.GetStringAsync(url).Result;
            var document = new HtmlAgilityPack.HtmlDocument();
            document.LoadHtml(html);
            var collection = document.DocumentNode.SelectNodes("//div[@class='download-panel']").ToArray();
            var node = collection.First();
            var versionNode = node.SelectNodes("//h3[@id]").ToArray()[0];
            var versionNodeIdStr = versionNode.GetAttributeValue("id", null);
            var sdkVersion = versionNodeIdStr.Split('-')[1];
            return sdkVersion;
        }

        private string getDotNetSdkDownloadUrl(HttpClient client, string dotnetSdkVersion, string arch)
        {
            var url = $"https://dotnet.microsoft.com/zh-cn/download/dotnet/thank-you/sdk-{dotnetSdkVersion}-{arch}-binaries";
            var html = client.GetStringAsync(url).Result;
            var document = new HtmlAgilityPack.HtmlDocument();
            document.LoadHtml(html);
            var aNode = document.DocumentNode.SelectSingleNode("//a[@id='directLink']");
            var href = aNode.GetAttributeValue("href", null);
            return href;
        }

        public void Invoke()
        {
            var displayDownloadProgress = new Action<QbNet.TransferProgress>(t =>
            {
                QbConsole.DisplaySameLineInConsole($"[{t.Current * 100 / t.Total}%]进度：{t.Current.ToString("N0")}/{t.Total.ToString("N0")}，速度：{t.Speed.ToString("N0")}，剩余时间：{t.RemainingTime}");
            });
            
            HttpClient client = new HttpClient();
            Console.WriteLine("正在获取.NET版本列表...");
            var versions = getDotnetVersions(client);
            Console.WriteLine("请选择.NET版本：");
            var dotnetVersion = QbSelect.ArrowSelect(versions.ToDictionary(t => t, t => t).ToArray(), selectedForegroundColor: ConsoleColor.Green);
            Console.WriteLine($"正在获取.NET {dotnetVersion}的SDK版本号...");
            var sdkVersion = getDotNetSdkVersion(client, dotnetVersion);
            var archDict = new Dictionary<string, string>()
            {
                ["win-x64"] = "windows-x64",
                ["linux-x64"] = "linux-x64",
                ["linux-arm64"] = "linux-arm64",
                ["linux-arm"] = "linux-arm32",
                //["osx-x64"] = "macos-x64"
            };
            Console.WriteLine("请选择打包架构(一个都不勾选代表全选)：");
            var selectArchs = QbSelect.MultiSelect(archDict.ToArray(), selectedForegroundColor: ConsoleColor.Green);
            if (selectArchs == null || selectArchs.Length == 0)
                selectArchs = archDict.Keys.ToArray();

            foreach (var arch in selectArchs)
            {
                Console.WriteLine($"正在打包[{arch}]...");
                var url = getDotNetSdkDownloadUrl(client, sdkVersion, archDict[arch]);
                var tmpFileName = Path.Combine("bin", Path.GetFileName(url));
                var tmpFolder = Path.Combine("bin", $"{Id}-{dotnetVersion}-{arch}");
                if (Directory.Exists(tmpFolder))
                    Directory.Delete(tmpFolder, true);
                var fileExt = Path.GetExtension(url);
                try
                {
                    if (!File.Exists(tmpFileName))
                    {
                        Console.WriteLine($"正在从[{url}]下载...");
                        QbNet.DownloadFile(url, tmpFileName, CancellationToken.None, displayDownloadProgress).Wait();
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
                                var tarFile = Path.GetFileNameWithoutExtension(tmpFileName);
                                if (File.Exists(tarFile))
                                    File.Delete(tarFile);
                                using (var archive = SharpCompress.Archives.GZip.GZipArchive.Open(tmpFileName))
                                using (var gzStream = archive.Entries.First().OpenEntryStream())
                                using (var tarFs = new FileStream(tarFile, FileMode.Create))
                                    gzStream.CopyTo(tarFs);

                                using (var archive = SharpCompress.Archives.Tar.TarArchive.Open(tarFile))
                                    archive.ExtractToDirectory(tmpFolder);

                                File.Delete(tarFile);
                                break;
                            }
                    }
                    Console.WriteLine($"正在打包易启动运行库文件...");
                    //生成元信息文件
                    var metaObj = new YiQiDong.Core.Protocol.V1.Model.RuntimeInfo()
                    {
                        Id = $"{Name}-{dotnetVersion}",
                        Name = Id,
                        Version = dotnetVersion,
                        BuildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Description = ".NET 是一个免费的跨平台开源开发人员平台，用于生成许多不同类型的应用。使用 .NET，可以使用多种语言、编辑器和库来构建 Web、移动、桌面、游戏和 IoT 等。",
                        Platform = new[] { arch },
                        Environment = new Dictionary<string, string>()
                        {
                            ["DOTNET_ROOT"] = "$RUNTIME_DIR",
                            ["DOTNET_CLI_HOME"] = "$RUNTIME_DIR"
                        },
                        Path = new[] { "$RUNTIME_DIR" },
                        ExecuteFiles = new[] { "$RUNTIME_DIR/dotnet" },
                        TestCommand = new Dictionary<string, string[]>()
                        {
                            ["信息"] = new[] { "dotnet", "--info" }
                        }
                    };
                    if (arch.StartsWith("win-"))
                        metaObj.ExecuteFiles = metaObj.ExecuteFiles.Select(t => t + ".exe").ToArray();
                    var metaFile = Path.Combine(tmpFolder, IResource.RUNTIME_META_FILE);
                    File.WriteAllText(metaFile, JsonSerializer.Serialize(metaObj, new JsonSerializerOptions()
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        WriteIndented = true
                    }), Encoding.UTF8);
                    //打包
                    using (var archive2 = SharpCompress.Archives.Zip.ZipArchive.Create())
                    {
                        archive2.AddAllFromDirectory(tmpFolder);
                        archive2.SaveTo($"bin/{Id}-{dotnetVersion}-{arch}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.yrt",
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
