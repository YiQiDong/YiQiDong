using Quick.Protocol;
using System.Text.Json.Serialization.Metadata;

namespace YiQiDong.Protocol.V1.QpCommands.Using
{
    public class Response : AbstractQpSerializer<Response>
    {
        protected override JsonTypeInfo<Response> GetTypeInfo() => UsingCommandSerializerContext.Default.Response;
    }
}
