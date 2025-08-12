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
using System.Threading;
using YiQiDong.Agent.AgentTypes.TextConfigs;
using YiQiDong.Protocol.V1.Model;

namespace _build.Resources
{
    public class Image_Gitea : IResource
    {
        public string Id => "gitea";

        public string Name => "Gitea";
        private Dictionary<string, string> helpDict = new Dictionary<string, string>()
        {
            ["从外部迁入报错"] = @"Gitea迁入外部仓库时，提示“您不能从不允许的主机导入，请询问管理员以检查ALLOWED_DOMAINSALLOW_LOCALNETWORKSBLOCKED_DOMAINS 设置。”

编辑配置文件[$GITEA_WORK_DIR/custom/conf/app.ini]，在最下方加入：

[migrations]
ALLOW_LOCALNETWORKS = true
ALLOWED_DOMAINS = 127.0.0.1,xxx.xxx.com"
        };

        public void Invoke()
        {
            var displayDownloadProgress = new Action<QbNet.TransferProgress>(t =>
            {
                QbConsole.DisplaySameLineInConsole($"[{t.Current * 100 / t.Total}%]进度：{t.Current.ToString("N0")}/{t.Total.ToString("N0")}，速度：{t.Speed.ToString("N0")}，剩余时间：{t.RemainingTime}");
            });

            HttpClient client = new HttpClient();
            Console.WriteLine("正在获取Gitea版本列表...");
            var giteaVersionDict = new Dictionary<string, string>();
            {
                var html = client.GetStringAsync("https://dl.gitea.com/gitea/").Result;
                var document = new HtmlDocument();
                document.LoadHtml(html);
                var tableNode = document.DocumentNode.SelectSingleNode("//table");
                foreach (var trNode in tableNode.SelectSingleNode("tbody").SelectNodes("tr").Skip(1).ToArray())
                {
                    var nodes = trNode.SelectNodes("td").ToArray();
                    var version = nodes[1].InnerText;
                    if (version.Contains("-"))
                        continue;
                    if (!Version.TryParse(version, out var fullVersion))
                        continue;
                    var bigVersion = $"{fullVersion.Major}.{fullVersion.Minor}";
                    if (giteaVersionDict.ContainsKey(bigVersion))
                        continue;
                    giteaVersionDict[bigVersion] = version;
                }
            }
            Console.WriteLine("请选择Gitea版本：");
            var giteaVersion = QbSelect.ArrowSelect(
                giteaVersionDict.Values.ToDictionary(t => t, t => t).ToArray(),
                selectedForegroundColor: ConsoleColor.Green);

            var archDict = new Dictionary<string, string>()
            {
                ["win-x64"] = "windows-4.0-amd64.exe.xz",
                ["linux-x64"] = "linux-amd64.xz",
                ["linux-arm64"] = "linux-arm64.xz",
                ["linux-arm"] = "linux-arm-6.xz",
                ["osx-x64"] = "darwin-10.12-amd64.xz"
            };

            Console.WriteLine("请选择打包架构(一个都不勾选代表全选)：");
            var selectArchs = QbSelect.MultiSelect(archDict.Keys.ToDictionary(t => t, t => t).ToArray(), selectedForegroundColor: ConsoleColor.Green);
            if (selectArchs == null || selectArchs.Length == 0)
                selectArchs = archDict.Keys.ToArray();

            foreach (var arch in selectArchs)
            {
                Console.WriteLine($"正在打包[{arch}]...");
                var url = $"https://dl.gitea.com/gitea/{giteaVersion}/gitea-{giteaVersion}-{archDict[arch]}";
                var tmpFileName = Path.Combine("bin", Path.GetFileName(url));
                var tmpFolder = Path.Combine("bin", $"{Id}-{giteaVersion}-{arch}");
                if (Directory.Exists(tmpFolder))
                    Directory.Delete(tmpFolder, true);
                Directory.CreateDirectory(tmpFolder);

                try
                {
                    if (!File.Exists(tmpFileName))
                    {
                        Console.WriteLine($"正在从[{url}]下载...");
                        QbNet.DownloadFile(url, tmpFileName, CancellationToken.None, displayDownloadProgress).Wait();
                    }
                    Console.WriteLine($"文件[{tmpFileName}]下载完成，正在解压...");

                    var exeFile = Path.GetFileNameWithoutExtension(tmpFileName);
                    var exeFileFullPath = Path.Combine(tmpFolder, exeFile);
                    if (File.Exists(exeFileFullPath))
                        File.Delete(exeFileFullPath);

                    using (Stream stream = File.OpenRead(tmpFileName))
                    using (var xzStream = new SharpCompress.Compressors.Xz.XZStream(stream))
                    using (var exeFileStream = File.OpenWrite(exeFileFullPath))
                        xzStream.CopyTo(exeFileStream);

                    var startFileName = exeFile;
                    var startArguments = "web";
                    var enviromentList = new List<EnvironmentVariableInfo>()
                    {
                        new ()
                        {
                            Key="GITEA_WORK_DIR",
                            Description="Gitea工作目录",
                            Value="$CONTAINER_DIR"
                        }
                    };
                    //非Windows平台，不能以root账号运行
                    if (!arch.StartsWith("win-"))
                    {
                        enviromentList.AddRange(
                        [
                            new()
                            {
                                Key = "HOME",
                                Description = "HOME目录",
                                Value = "$CONTAINER_DIR"
                            },
                            new()
                            {
                                Key = "GITEA_I_AM_BEING_UNSAFE_RUNNING_AS_ROOT",
                                Description = "是否允许Gitea以root用户运行",
                                Value = "true"
                            }
                        ]);
                    }

                    var textConfigsConfigModel = new ConfigModel()
                    {
                        ContainerMetaInfos =
                        [
                            new ContainerMetaInfo()
                            {
                                StartFileName = startFileName,
                                StartArguments= startArguments,
                                Encoding="UTF-8",
                                ExitTimeout = 10000,
                                WorkingDir = "$CONTAINER_DIR",
                                Path = ["$IMAGE_DIR" ],
                                Environments = enviromentList.ToArray(),
                                ConfigFolders = ["$GITEA_WORK_DIR/custom/conf"],
                                HelpDict =helpDict
                            }
                        ]
                    };
                    var imageInfo = new ImageInfo()
                    {
                        DefaultId = Id,
                        Name = Name,
                        Version = giteaVersion,
                        Tags = ["代码仓库"],
                        ExecuteFiles = [$"$IMAGE_DIR/{exeFile}"],
                        BuildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Description = "Gitea 是一个轻量级的 DevOps 平台软件。从开发计划到产品成型的整个软件生命周期，他都能够高效而轻松的帮助团队和开发者。包括 Git 托管、代码审查、团队协作、软件包注册和 CI/CD。它与 GitHub、Bitbucket 和 GitLab 等比较类似。",
                        Platform = [arch],
                        Runtime = ["git-2.0"],
                        AgentType = nameof(YiQiDong.Agent.AgentTypes.TextConfigs),
                        AgentConfig = JsonNode.Parse(JsonSerializer.Serialize(textConfigsConfigModel, new JsonSerializerOptions()
                        {
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                            WriteIndented = true
                        })).AsObject()
                    };
                    File.WriteAllText(Path.Combine(tmpFolder, IResource.IMAGE_META_FILE),
                            JsonSerializer.Serialize(imageInfo, new JsonSerializerOptions()
                            {
                                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                                WriteIndented = true
                            }),
                            Encoding.UTF8);
                    var outFile = $"bin/{Name}-{giteaVersion}-{arch}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.ymg";
                    Console.WriteLine($"正在制作易启动镜像[{arch}]...");

                    using (var archive = SharpCompress.Archives.Zip.ZipArchive.Create())
                    {
                        archive.AddAllFromDirectory(tmpFolder);
                        archive.SaveTo(outFile, CompressionType.LZMA);
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
