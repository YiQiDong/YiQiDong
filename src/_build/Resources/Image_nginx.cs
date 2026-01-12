using _build.Resources.Core;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using YiQiDong.Agent.AgentTypes.TextConfigs;
using YiQiDong.Protocol.V1.Model;

namespace _build.Resources
{
    public class Image_nginx : IResource
    {
        public string Id => "nginx";

        public string Name => "Nginx";

        public void Invoke()
        {
            var workspaceName = "common-binaries";
            var repositoryName = "nginx";

            GithubCommonBinariesUtils.BuildImage(Id, Name, workspaceName, repositoryName, (version, rid) =>
            {
                string binFileName;
                switch (rid)
                {
                    case "win-x64":
                        binFileName = "bin\\nginx.exe";
                        break;
                    case "linux-x64":
                        binFileName = "bin/nginx";
                        break;
                    case "linux-arm64":
                        binFileName = "bin/nginx";
                        break;
                    default:
                        throw new NotImplementedException();
                }

                var textConfigsConfigModel = new ConfigModel()
                {
                    ContainerMetaInfos = new[]
                     {
                        new ContainerMetaInfo()
                        {
                            StartFileName = binFileName,
                            StartArguments= "-c conf/nginx.conf -g \"daemon off;\"",
                            WorkingDir = "$CONTAINER_DIR",
                            Path = new[] { "$IMAGE_DIR" },
                            ContainerFolders = new Dictionary<string, string>()
                            {
                                ["$IMAGE_DIR/conf"] = "$CONTAINER_DIR/conf",
                                ["$IMAGE_DIR/html"] = "$CONTAINER_DIR/html"
                            },
                            ConfigFolders = new []{ "$CONTAINER_DIR/conf" }
                        }
                     }
                };
                return new ImageInfo()
                {
                    DefaultId = Id,
                    Name = Name,
                    Version = version,
                    Tags = new[] { "Web服务器" },
                    BuildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Description = "Nginx是一个受欢迎的开源网页服务器，可以作为反向代理或 HTTP缓存使用。",
                    Platform = new[] { rid },
                    AgentType = nameof(YiQiDong.Agent.AgentTypes.TextConfigs),
                    AgentConfig = JsonNode.Parse(JsonSerializer.Serialize(textConfigsConfigModel, new JsonSerializerOptions()
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        WriteIndented = true
                    })).AsObject()
                };
            });
        }
    }
}
