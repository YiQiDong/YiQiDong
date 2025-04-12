using HtmlAgilityPack;
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
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using YiQiDong.Agent.AgentTypes.TextConfigs;
using YiQiDong.Protocol.V1.Model;

namespace _build.Resources
{
    public class Image_consul : IResource
    {
        public string Id => "consul";

        public string Name => "Consul";

        public void Invoke()
        {
            var baseUrl = "https://releases.hashicorp.com/consul";
            //全部文件夹信息
            //https://releases.hashicorp.com/consul
            //指定版本文件夹信息
            //https://releases.hashicorp.com/consul/1.17.2/
            //下载地址
            //https://releases.hashicorp.com/consul/1.17.2/consul_1.17.2_windows_amd64.zip
            //https://releases.hashicorp.com/consul/1.17.2/consul_1.17.2_linux_amd64.zip
            //https://releases.hashicorp.com/consul/1.17.2/consul_1.17.2_linux_arm64.zip
            string URL_TEMPLATE = "{0}/{1}/consul_{1}_{2}.zip";

            Console.WriteLine("----------------------------------");
            Console.WriteLine("  欢迎使用[适用于弈启动的Consul项目]编译脚本");
            Console.WriteLine("----------------------------------");

            HttpClient httpClient = new HttpClient();
            Console.WriteLine("正在获取Consul最新版本信息...");
            var versionIndexHtml = httpClient.GetStringAsync(baseUrl).Result;
            var document = new HtmlDocument();
            document.LoadHtml(versionIndexHtml);
            var ulNode = document.DocumentNode.SelectSingleNode("//ul");
            List<Version> versionList = new List<Version>();
            foreach (var liNode in ulNode.SelectNodes("li").ToArray())
            {
                var aNode = liNode.SelectSingleNode("a");
                var line = aNode.InnerText;
                if (!line.StartsWith("consul_")
                    || line.Contains("+"))
                    continue;
                var version = line.Replace("consul_", string.Empty);
                if (Version.TryParse(version, out var ver))
                    versionList.Add(ver);
            }
            var consulVersion = versionList.Max().ToString();
            Console.WriteLine("请选择运行平台(一个都不选代表全选)：");
            var ridDict = new Dictionary<string, string>()
            {
                ["windows_amd64"] = "win-x64",
                ["linux_amd64"] = "linux-x64",
                ["linux_arm64"] = "linux-arm64"
            };
            var ridValues = QbSelect.MultiSelect(ridDict.ToArray(), selectedForegroundColor: ConsoleColor.Green);
            if (ridValues == null || ridValues.Length == 0)
                ridValues = ridDict.Keys.ToArray();
            var tmpFolder = Path.Combine("bin", Id);
            if (Directory.Exists(tmpFolder))
            {
                Directory.Delete(tmpFolder, true);
            }

            var tmpPublishFolder = Path.Combine(tmpFolder, consulVersion);
            //准备资源文件            
            var resourceFolder = Path.Combine(tmpFolder, "resource");
            if (!Directory.Exists(resourceFolder))
                Directory.CreateDirectory(resourceFolder);
            {
                var confFolder = Path.Combine(resourceFolder, "conf");
                Directory.CreateDirectory(confFolder);
                var templateConfFile = Path.Combine(confFolder, "conf.hcl");
                File.WriteAllText(templateConfFile, @"datacenter = ""dc1""
node_name = ""node1""
data_dir = ""data""
encrypt = ""7yvaWFQg6r6vcmGAw/TspLVmge+gw3V4+nPZi+32iyQ=""
server = true
ui = true
bootstrap_expect = 2
advertise_addr = ""192.168.1.71""
retry_join = [""192.168.1.72""]
");
            }

            foreach (var ridValue in ridValues)
            {
                var rid = ridDict[ridValue];
                Console.WriteLine($"开始打包[{rid}]...");
                if (Directory.Exists(tmpPublishFolder))
                {
                    Console.WriteLine($"正在清理目录...");
                    Directory.Delete(tmpPublishFolder, true);
                }
                Directory.CreateDirectory(tmpPublishFolder);
                try
                {
                    string url = string.Format(URL_TEMPLATE, baseUrl, consulVersion, ridValue);
                    string binFileName;
                    
                    switch (rid)
                    {
                        case "win-x64":
                            binFileName = "consul.exe";
                            break;
                        case "linux-x64":
                            binFileName = "consul";
                            break;
                        case "linux-arm64":
                            binFileName = "consul";
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    var zipFileName = Path.GetFileName(url);
                    //开始下载
                    var zipFile = Path.Combine(tmpPublishFolder, zipFileName);
                    Console.WriteLine($"正在从[{url}]下载文件...");
                    using (var fs = File.OpenWrite(zipFile))
                    using (var ns = httpClient.GetStreamAsync(url).Result)
                        ns.CopyTo(fs);
                    //解压文件
                    using (var zipArchive = SharpCompress.Archives.Zip.ZipArchive.Open(zipFile))
                        zipArchive.ExtractToDirectory(tmpPublishFolder);
                    File.Delete(zipFile);

                    //复制资源文件
                    QbFolder.Copy(resourceFolder, tmpPublishFolder);

                    var textConfigsConfigModel = new ConfigModel()
                    {
                        ContainerMetaInfos = new[]
                         {
                        new ContainerMetaInfo()
                        {
                            FileName = binFileName,
                            StartArguments= "agent -config-file=\"conf/conf.hcl\"",
                            StopArguments = "leave",
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
                        Version = consulVersion,
                        Tags = new[] { "微服务" },
                        BuildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Description = "Consul是微服务架构中，解决服务发现、配置中心的分布式中间件。",
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
                    var outFile = $"bin/{Id}-{consulVersion}-{rid}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.ymg";
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
