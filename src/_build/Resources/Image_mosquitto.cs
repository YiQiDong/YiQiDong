using _build.Core.Snapcraft;
using Quick.Build;
using SharpCompress.Archives;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using YiQiDong.Agent.AgentTypes.TextConfigs;
using YiQiDong.Protocol.V1.Model;

namespace _build.Resources
{
    public class Image_mosquitto : IResource
    {
        public string Id => "mosquitto";

        public string Name => "mosquitto";

        public void Invoke()
        {
            Console.WriteLine("----------------------------------");
            Console.WriteLine("  欢迎使用[适用于弈启动的mosquitto项目]编译脚本");
            Console.WriteLine("----------------------------------");

            var snapcraftClient = new SnapcraftClient();
            Console.WriteLine("正在获取mosquitto最新版本信息...");
            var snapInfo = snapcraftClient.GetSnapInfoAsync("mosquitto").Result;
            var channelMaps = snapInfo.channel_map.Where(t => t.channel.name == "stable").ToArray();
            var mosquittoVersion = channelMaps[0].version;
            Console.WriteLine("mosquitto最新版本: " + mosquittoVersion);
            Console.WriteLine("请选择运行平台(一个都不选代表全选)：");
            var ridDict = new Dictionary<string, string>();
            if (channelMaps.Any(t => t.channel.architecture == "amd64"))
                ridDict["amd64"] = "linux-x64";
            if (channelMaps.Any(t => t.channel.architecture == "arm64"))
                ridDict["arm64"] = "linux-arm64";
            var ridValues = QbSelect.MultiSelect(ridDict.ToArray(), selectedForegroundColor: ConsoleColor.Green);
            if (ridValues == null || ridValues.Length == 0)
                ridValues = ridDict.Keys.ToArray();
            var tmpFolder = Path.Combine("bin", Id);
            if (Directory.Exists(tmpFolder))
            {
                Directory.Delete(tmpFolder, true);
            }

            var tmpPublishFolder = Path.Combine(tmpFolder, mosquittoVersion);
            foreach (var ridValue in ridValues)
            {
                var rid = ridDict[ridValue];
                var channelMap = channelMaps.FirstOrDefault(t => t.channel.architecture == ridValue);
                Console.WriteLine($"开始打包[{rid}]...");
                if (Directory.Exists(tmpPublishFolder))
                {
                    Console.WriteLine($"正在清理目录...");
                    Directory.Delete(tmpPublishFolder, true);
                }
                Directory.CreateDirectory(tmpPublishFolder);
                try
                {
                    string url = channelMap.download.url;
                    var zipFileName = Path.GetFileName(url);
                    //开始下载
                    var zipFile = Path.Combine(tmpPublishFolder, zipFileName);
                    Console.WriteLine($"正在从[{url}]下载文件...");
                    using (var httpClient = snapcraftClient.GetHttpClient())
                    using (var fs = File.OpenWrite(zipFile))
                    using (var ns = httpClient.GetStreamAsync(url).Result)
                        ns.CopyTo(fs);

                    //解压文件
                    QbCommand.Run("7z", $"x -o{tmpPublishFolder} \"{zipFile}\"", handleExitCode: t => true);
                    File.Delete(zipFile);
                    Directory.Move(Path.Combine(tmpPublishFolder, "usr", "sbin"), Path.Combine(tmpPublishFolder, "sbin"));
                    Directory.Delete(Path.Combine(tmpPublishFolder, "meta"), true);
                    Directory.Delete(Path.Combine(tmpPublishFolder, "snap"), true);
                    Directory.Delete(Path.Combine(tmpPublishFolder, "usr"), true);
                    Directory.CreateDirectory(Path.Combine(tmpPublishFolder, "conf"));
                    File.Delete(Path.Combine(tmpPublishFolder, "default_config.conf"));
                    File.Delete(Path.Combine(tmpPublishFolder, "launcher.sh"));
                    File.Move(Path.Combine(tmpPublishFolder, "mosquitto.conf"), Path.Combine(tmpPublishFolder, "conf", "mosquitto.conf"));

                    var textConfigsConfigModel = new ConfigModel()
                    {
                        ContainerMetaInfos = new[]
                         {
                            new ContainerMetaInfo()
                            {
                                StartFileName="sbin/mosquitto",
                                StartArguments= "-c conf/mosquitto.conf",
                                WorkingDir = "$CONTAINER_DIR",
                                Path = new[] { "$IMAGE_DIR" },
                                ContainerFolders = new Dictionary<string, string>()
                                {
                                    ["$IMAGE_DIR/conf"] = "$CONTAINER_DIR/conf"
                                },
                                ConfigFolders = new []{ "$CONTAINER_DIR/conf" }
                            }
                        }
                    };
                    var imageInfo = new ImageInfo()
                    {
                        DefaultId = Id,
                        Name = Name,
                        Version = mosquittoVersion,
                        Tags = new[] { "消息队列" },
                        BuildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Description = "mosquitto一款实现了消息推送协议MQTT的开源消息代理软件，提供轻量级的、支持可发布/可订阅的的消息推送模式，使设备对设备之间的短消息通信变得简单，比如现在应用广泛的低功耗传感器，手机、嵌入式计算机、微型控制器等移动设备。",
                        Platform = new[] { rid },
                        AgentType = nameof(YiQiDong.Agent.AgentTypes.TextConfigs),
                        AgentConfig = JsonNode.Parse(JsonSerializer.Serialize(textConfigsConfigModel, new JsonSerializerOptions()
                        {
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                            WriteIndented = true
                        })).AsObject()
                    };
                    File.WriteAllText(
                        Path.Combine(tmpPublishFolder, IResource.IMAGE_META_FILE),
                        JsonSerializer.Serialize(imageInfo, new JsonSerializerOptions()
                        {
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                            WriteIndented = true
                        }),
                        Encoding.UTF8);
                    var outFile = $"bin/{Id}-{mosquittoVersion}-{rid}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.ymg";
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
    }
}
