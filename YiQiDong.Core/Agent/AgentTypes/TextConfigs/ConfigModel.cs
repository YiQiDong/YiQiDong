using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace YiQiDong.Agent.AgentTypes.TextConfigs
{
    [JsonSerializable(typeof(ConfigModel))]
    public partial class AgentConfigModelSerializerContext : JsonSerializerContext
    {
        public static AgentConfigModelSerializerContext Default2 { get; } = new AgentConfigModelSerializerContext(new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    public class ConfigModel
    {
        /// <summary>
        /// 容器元信息
        /// </summary>
        public ContainerMetaInfo[] ContainerMetaInfos { get; set; }

        public static ConfigModel Parse(JsonObject agentConfig)
        {
            return (ConfigModel)JsonSerializer.Deserialize(
                agentConfig.ToJsonString(),
                typeof(ConfigModel),
                AgentConfigModelSerializerContext.Default2);
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, AgentConfigModelSerializerContext.Default2.ConfigModel);
        }

        internal ContainerMetaInfo GetContainerMetaInfo()
        {
            if (ContainerMetaInfos == null)
                return null;
            foreach (var cmi in ContainerMetaInfos)
            {
                var platforms = cmi.Platform;
                if (platforms == null || platforms.Length == 0)
                    return cmi;
                foreach (var platform in platforms)
                {
                    if (string.IsNullOrEmpty(platform)
                        || platform == "any")
                        return cmi;
                    var strs = platform.Split('-');
                    var os = strs.FirstOrDefault();
                    //先检查操作系统
                    if (string.IsNullOrEmpty(os))
                        return cmi;
                    var isOsMatch = false;
                    switch (os)
                    {
                        case "win":
                            isOsMatch = OperatingSystem.IsWindows();
                            break;
                        case "linux":
                            isOsMatch = OperatingSystem.IsLinux();
                            break;
                        case "osx":
                            isOsMatch = OperatingSystem.IsMacOS();
                            break;
                    }
                    if (!isOsMatch)
                        continue;
                    //再检查架构
                    var arch = strs.Skip(1).FirstOrDefault();
                    if (string.IsNullOrEmpty(arch))
                        return cmi;
                    if (RuntimeInformation.ProcessArchitecture.ToString().ToLower() == arch)
                        return cmi;
                }
            }
            return null;
        }
    }
}