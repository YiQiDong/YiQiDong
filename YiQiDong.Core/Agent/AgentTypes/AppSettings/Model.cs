using Quick.Fields;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YiQiDong.Agent.AgentTypes.AppSettings
{
    [JsonSerializable(typeof(Model))]
    [JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    internal partial class ModelSerializerContext : JsonSerializerContext { }

    public class Model
    {
        public PSI ProcessStartInfo { get; set; }
        public FieldForGet[] Fields { get; set; }

        public string ToJsonString()
        {
            return JsonSerializer.Serialize(this, typeof(Model), ModelSerializerContext.Default);
        }

        public static Model FromJsonString(string json)
        {
            return (Model)JsonSerializer.Deserialize(json, typeof(Model), ModelSerializerContext.Default);
        }
    }
}