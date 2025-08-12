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
    public class Runtime_nodejs : IResource
    {
        public string Id => "nodejs";

        public string Name => "Node.js";

        private string[] getNodeJsVersions(HttpClient client)
        {
            var url = "https://nodejs.org/en/about/previous-releases";
            var html = client.GetStringAsync(url).Result;
            var document = new HtmlAgilityPack.HtmlDocument();
            document.LoadHtml(html);
            var collection = document.DocumentNode.Descendants("tr").ToArray();
            List<string> versionList = new List<string>();
            foreach (var tr in collection)
            {
                var tds = tr.Descendants("td").ToArray();
                if (tds.Length < 5)
                    continue;
                var td = tds[0];
                var version = td.InnerText;
                if (version.StartsWith("v"))
                    version = version.Substring(1);
                versionList.Add(version);
            }
            return versionList.ToArray();
        }

        public void Invoke()
        {
            var displayDownloadProgress = new Action<QbNet.TransferProgress>(t =>
            {
                QbConsole.DisplaySameLineInConsole($"[{t.Current * 100 / t.Total}%]进度：{t.Current.ToString("N0")}/{t.Total.ToString("N0")}，速度：{t.Speed.ToString("N0")}，剩余时间：{t.RemainingTime}");
            });

            HttpClient client = new HttpClient();
            Console.WriteLine($"正在获取{Name}版本列表...");
            var versions = getNodeJsVersions(client);
            Console.WriteLine($"请选择{Name}版本：");
            var nodeJsVersion = QbSelect.ArrowSelect(versions.ToDictionary(t => t, t => t).ToArray(), selectedForegroundColor: ConsoleColor.Green);
            var archDict = new Dictionary<string, string>()
            {
                ["win-x64"] = "windows-x64",
                ["linux-x64"] = "linux-x64",
                ["linux-arm64"] = "linux-arm64",
                ["linux-arm64"] = "darwin-x64",
            };
            Console.WriteLine("请选择打包架构(一个都不勾选代表全选)：");
            var selectArchs = QbSelect.MultiSelect(archDict.ToArray(), selectedForegroundColor: ConsoleColor.Green);
            if (selectArchs == null || selectArchs.Length == 0)
                selectArchs = archDict.Keys.ToArray();

            foreach (var arch in selectArchs)
            {
                Console.WriteLine($"正在打包[{arch}]...");
                
                var url = $"https://nodejs.org/download/release/v{nodeJsVersion}/node-v{nodeJsVersion}-{arch}";
                if (arch.StartsWith("win"))
                    url += ".7z";
                else
                    url += ".tar.gz";
                var tmpFileName = Path.Combine("bin", Path.GetFileName(url));
                var tmpFolder = Path.Combine("bin", $"{Id}-{nodeJsVersion}-{arch}");
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
                        case ".7z":
                            {
                                using (var archive = SharpCompress.Archives.SevenZip.SevenZipArchive.Open(tmpFileName))
                                    archive.ExtractToDirectory(tmpFolder);
                                break;
                            }
                        default:
                            {
                                using (var archive = SharpCompress.Archives.Tar.TarArchive.Open(tmpFileName))
                                    archive.ExtractToDirectory(tmpFolder);
                                break;
                            }
                    }
                    var extraFolder = Directory.GetDirectories(tmpFolder).First();
                    foreach (var item in Directory.GetDirectories(extraFolder))
                        Directory.Move(item, Path.Combine(tmpFolder, Path.GetFileName(item)));
                    foreach (var item in Directory.GetFiles(extraFolder))
                        File.Move(item, Path.Combine(tmpFolder, Path.GetFileName(item)));
                    Directory.Delete(extraFolder);
                    Console.WriteLine($"正在打包易启动运行库文件...");
                    //生成元信息文件
                    var metaObj = new YiQiDong.Core.Protocol.V1.Model.RuntimeInfo()
                    {
                        Id = $"{Name}-{nodeJsVersion}",
                        Name = Id,
                        Version = nodeJsVersion,
                        BuildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Description = "Node.js 是一个开源的、跨平台的 JavaScript 运行时环境。",
                        Platform = new[] { arch },
                        Path = new[] { "$RUNTIME_DIR/bin" },
                        ExecuteFiles = new[] { "$RUNTIME_DIR/bin/node" },
                        TestCommand = new Dictionary<string, string[]>()
                        {
                            ["版本"] = new[] { "node", "--version" }
                        }
                    };
                    if (arch.StartsWith("win-"))
                    {
                        metaObj.Path = new[] { "$RUNTIME_DIR" };
                        metaObj.ExecuteFiles = new[] { "$RUNTIME_DIR/node.exe" };
                    }
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
                        archive2.SaveTo($"bin/{Id}-{nodeJsVersion}-{arch}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.yrt",
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
