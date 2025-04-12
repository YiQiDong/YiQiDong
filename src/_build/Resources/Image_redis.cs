using _build.Resources.Core;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using YiQiDong.Agent.AgentTypes.TextConfigs;
using YiQiDong.Protocol.V1.Model;

namespace _build.Resources
{
    public class Image_redis : IResource
    {
        public string Id => "redis";
        public string Name => "Redis";

        public void Invoke()
        {
            var workspaceName = "common-binaries";
            var repositoryName = "redis";

            GithubCommonBinariesUtils.BuildImage(Id, Name, workspaceName, repositoryName, (version, rid) =>
            {
                var textConfigsConfigModel = new ConfigModel()
                {
                    ContainerMetaInfos = new[]
                    {
                        new ContainerMetaInfo()
                        {
                            StartFileName="bin/redis-server",
                            StartArguments= "conf/redis.conf",
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
                return new ImageInfo()
                {
                    DefaultId = Id,
                    Name = Name,
                    Version = version,
                    Tags = new[] { "缓存" },
                    BuildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Description = "Redis 是一个强大的内存数据结构存储，用作数据库、缓存和消息代理。",
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
