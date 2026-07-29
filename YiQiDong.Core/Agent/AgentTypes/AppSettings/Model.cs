using Quick.Fields;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YiQiDong.Agent.AgentTypes.AppSettings
{
    [JsonSerializable(typeof(Model))]
    public partial class ModelSerializerContext : JsonSerializerContext
    {
        public static ModelSerializerContext Default2 { get; } = new ModelSerializerContext(new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    public class Model
    {
        public PSI ProcessStartInfo { get; set; }
        public FieldForGet[] Fields { get; set; }

        public string ToJsonString()
        {
            return JsonSerializer.Serialize(this, typeof(Model), ModelSerializerContext.Default2);
        }

        public static Model FromJsonString(string json)
        {
            return (Model)JsonSerializer.Deserialize(json, typeof(Model), ModelSerializerContext.Default2);
        }
    }
}