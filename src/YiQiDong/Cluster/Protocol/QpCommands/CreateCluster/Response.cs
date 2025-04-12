using Quick.Protocol;
using System.Text.Json.Serialization.Metadata;

namespace YiQiDong.Cluster.Protocol.QpCommands.CreateCluster
{
    public class Response : AbstractQpSerializer<Response>
    {
        protected override JsonTypeInfo<Response> GetTypeInfo() => CreateClusterCommandSerializerContext.Default.Response;
    }
}
