using Quick.Fields;
using Quick.Protocol;
using System.Text.Json.Serialization.Metadata;

namespace YiQiDong.Protocol.V1.QpCommands.ExecuteFunction
{
    public class Response: AbstractQpSerializer<Response>
    {
        protected override JsonTypeInfo<Response> GetTypeInfo() => ExecuteFunctionCommandSerializerContext.Default.Response;
        public FieldForGet[] Items { get; set; }
    }
}
