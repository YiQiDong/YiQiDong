using Quick.Protocol;
using System.Text.Json.Serialization.Metadata;

namespace YiQiDong.Cluster.Protocol.QpCommands.DeleteCluster
{
    public class Response : AbstractQpSerializer<Response>
    {
        protected override JsonTypeInfo<Response> GetTypeInfo() => DeleteClusterCommandSerializerContext.Default.Response;
    }
}
