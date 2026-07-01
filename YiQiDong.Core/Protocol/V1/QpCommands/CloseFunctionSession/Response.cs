using System.Text.Json.Serialization.Metadata;
using Quick.Protocol;

namespace YiQiDong.Protocol.V1.QpCommands.CloseFunctionSession;

public class Response : AbstractQpSerializer<Response>
{
    protected override JsonTypeInfo<Response> GetTypeInfo() => CloseFunctionSessionSerializerContext.Default.Response;
}
