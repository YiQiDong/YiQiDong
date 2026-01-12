using System.Text.Json;
using System.Text.Json.Serialization;

namespace YiQiDong.Agent.AgentTypes.TextConfigs
{
    [JsonSerializable(typeof(ContainerConfigModel))]
    internal partial class ContainerConfigModelSerializerContext : JsonSerializerContext { }

    public class ContainerConfigModel
    {
        /// <summary>
        /// 环境变量
        /// </summary>
        public Dictionary<string, string> Environment { get; set; }
        /// <summary>
        /// 克隆
        /// </summary>
        /// <returns></returns>
        public ContainerConfigModel Clone()
        {
            var json = JsonSerializer.Serialize(this, ContainerConfigModelSerializerContext.Default.ContainerConfigModel);
            return JsonSerializer.Deserialize(json, ContainerConfigModelSerializerContext.Default.ContainerConfigModel);
        }
    }
}
