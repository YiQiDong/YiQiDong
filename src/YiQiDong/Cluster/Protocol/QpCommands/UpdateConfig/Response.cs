using Quick.Protocol;
using System.Text.Json.Serialization.Metadata;

namespace YiQiDong.Cluster.Protocol.QpCommands.UpdateConfig
{
    public class Response : AbstractQpSerializer<Response>
    {
        protected override JsonTypeInfo<Response> GetTypeInfo() => UpdateConfigCommandSerializerContext.Default.Response;
    }
}
