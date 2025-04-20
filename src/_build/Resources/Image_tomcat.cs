using HtmlAgilityPack;
using Quick.Build;
using SharpCompress.Archives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using YiQiDong.Protocol.V1.Model;

namespace _build.Resources
{
    public class Image_tomcat : IResource
    {
        public string Id => "tomcat";

        public string Name => "Tomcat";

        public class TomcatVersionInfo
        {
            public string lastestReleasedVersion { get; set; }
            public string supportedJavaVersion { get; set; }
            public string downloadUrl { get; set; }
        }

        public void Invoke()
        {
            var displayDownloadProgress = new Action<QbNet.TransferProgress>(t =>
            {
                QbConsole.DisplaySameLineInConsole($"[{t.Current * 100 / t.Total}%]进度：{t.Current.ToString("N0")}/{t.Total.ToString("N0")}，速度：{t.Speed.ToString("N0")}，剩余时间：{t.RemainingTime}");
            });
            
            HttpClient client = new HttpClient();
            Console.WriteLine("正在获取Tomcat版本列表...");
            Dictionary<string, TomcatVersionInfo> tomcatVersionInfoDict = new Dictionary<string, TomcatVersionInfo>();
            {
                var html = client.GetStringAsync("https://tomcat.apache.org/whichversion.html").Result;
                var document = new HtmlDocument();
                document.LoadHtml(html);
                var tableNode = document.DocumentNode.SelectSingleNode("//table[@class='detail-table']");
                foreach (var trNode in tableNode.SelectNodes("tr").Skip(1).ToArray())
                {
                    var nodes = trNode.SelectNodes("td").ToArray();
                    var version = nodes[6].InnerText;
                    if (version.Contains(" "))
                        continue;
                    var versionInfo = new Version(version);
                    var javaVersion = nodes[7].InnerText.Split(' ').FirstOrDefault();
                    var item = new TomcatVersionInfo()
                    {
                        lastestReleasedVersion = version,
                        supportedJavaVersion = javaVersion,
                        downloadUrl = $"https://archive.apache.org/dist/tomcat/tomcat-{versionInfo.Major}/v{version}/bin/apache-tomcat-{version}.zip"
                    };
                    tomcatVersionInfoDict[item.lastestReleasedVersion] = item;
                }
            }
            tomcatVersionInfoDict["8.5.100"] = new TomcatVersionInfo()
            {
                    lastestReleasedVersion = "8.5.100",
                    supportedJavaVersion = "7",
                    downloadUrl = $"https://archive.apache.org/dist/tomcat/tomcat-8/v8.5.100/bin/apache-tomcat-8.5.100.zip"
            };
            Console.WriteLine("请选择Tomcat版本：");
            var tomcatVersion = QbSelect.ArrowSelect(
                tomcatVersionInfoDict.Keys.ToDictionary(t => t, t => t).ToArray(),
                selectedForegroundColor: ConsoleColor.Green);
            var tomcatInfo = tomcatVersionInfoDict[tomcatVersion];
            var tmpFolder = Path.Combine("bin", Id);
            var tmpFileName = Path.Combine(tmpFolder, Path.GetFileName(tomcatInfo.downloadUrl));
            var tmpPublishFolder = Path.Combine(tmpFolder, tomcatInfo.lastestReleasedVersion);

            if (Directory.Exists(tmpFolder))
                Directory.Delete(tmpFolder, true);
            Directory.CreateDirectory(tmpPublishFolder);

            try
            {
                if (!File.Exists(tmpFileName))
                {
                    Console.WriteLine($"正在从[{tomcatInfo.downloadUrl}]下载...");
                    QbNet.DownloadFile(tomcatInfo.downloadUrl, tmpFileName, CancellationToken.None, displayDownloadProgress).Wait();
                }
                Console.WriteLine($"文件[{tmpFileName}]下载完成，正在解压...");
                using (var archive = SharpCompress.Archives.Zip.ZipArchive.Open(tmpFileName))
                    archive.ExtractToDirectory(tmpPublishFolder);
                var tmpTomcatFolder = Directory.GetDirectories(tmpPublishFolder).FirstOrDefault();

                Console.WriteLine($"正在打包弈启动运行库文件...");

                var Environments = new YiQiDong.Agent.AgentTypes.TextConfigs.EnvironmentVariableInfo[]
                {
                    new (){ Key="CATALINA_HOME",Value = "$IMAGE_DIR",Description="程序目录，指向镜像中Tomcat程序目录，一般不要修改，默认为镜像目录。" },
                    new (){ Key="CATALINA_BASE",Value = "$CONTAINER_DIR",Description="基础目录，存放conf，webapps的目录，默认为容器目录。" },
                    new (){ Key="CATALINA_TMPDIR",Value = "$CONTAINER_DIR/temp",Description="临时目录，存放Tomcat临时文件的目录，默认为容器目录。" },
                    new (){ Key="CATALINA_OPTS",Value = "" }
                };
                var ContainerFolders = new Dictionary<string, string>()
                {
                    ["$IMAGE_DIR/conf"] = "$CATALINA_BASE/conf",
                    ["$IMAGE_DIR/webapps"] = "$CATALINA_BASE/webapps"
                };
                var ConfigFolders = new[] { "$CATALINA_BASE/conf" };

                var textConfigsConfigModel = new YiQiDong.Agent.AgentTypes.TextConfigs.ConfigModel()
                {
                    ContainerMetaInfos = new[]
                    {
                        new YiQiDong.Agent.AgentTypes.TextConfigs.ContainerMetaInfo()
                        {
                            Platform = new []{ "win"},
                            Encoding = "UTF-8",
                            StartFileName = "$IMAGE_DIR/bin/catalina.bat",
                            StartArguments = "run",
                            WorkingDir = "$CONTAINER_DIR",
                            Environments = Environments,
                            ContainerFolders = ContainerFolders,
                            ConfigFolders = ConfigFolders
                        },
                        new YiQiDong.Agent.AgentTypes.TextConfigs.ContainerMetaInfo()
                        {
                            Platform = new []{ "linux", "osx"},
                            StartFileName = "$IMAGE_DIR/bin/catalina.sh",
                            StartArguments = "run",
                            WorkingDir = "$CONTAINER_DIR",
                            Environments = Environments,
                            ContainerFolders = ContainerFolders,
                            ConfigFolders = ConfigFolders
                        }
                    }
                };

                //生成元信息文件                
                var metaObj = new ImageInfo()
                {
                    DefaultId = Id,
                    Name = Name,
                    Version = tomcatInfo.lastestReleasedVersion,
                    Tags = new[] { "Web服务器" },
                    BuildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Description = "Tomcat 是一个免费的开放源代码的Web 应用服务器。",
                    Platform = new[] { "any" },
                    Runtime = new[] { $"java-{tomcatInfo.supportedJavaVersion}" },
                    AgentType = nameof(YiQiDong.Agent.AgentTypes.TextConfigs),
                    AgentConfig = JsonNode.Parse(JsonSerializer.Serialize(textConfigsConfigModel, new JsonSerializerOptions()
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        WriteIndented = true
                    })).AsObject()
                };
                var metaFile = Path.Combine(tmpTomcatFolder, IResource.IMAGE_META_FILE);
                File.WriteAllText(metaFile, JsonSerializer.Serialize(metaObj, new JsonSerializerOptions()
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                }), Encoding.UTF8);
                //打包
                using (var archive2 = SharpCompress.Archives.Zip.ZipArchive.Create())
                {
                    archive2.AddAllFromDirectory(tmpTomcatFolder);
                    archive2.SaveTo($"bin/{Id}-{tomcatInfo.lastestReleasedVersion}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.ymg",
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
            }
        }
    }
}
