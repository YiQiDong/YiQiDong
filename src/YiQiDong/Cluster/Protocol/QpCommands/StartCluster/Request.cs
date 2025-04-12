using Quick.Protocol;
using System.ComponentModel;
using System.Text.Json.Serialization.Metadata;

namespace YiQiDong.Cluster.Protocol.QpCommands.StartCluster
{
    [DisplayName("启动集群")]
    public class Request : AbstractQpSerializer<Request>, IQpCommandRequest<Request, Response>
    {
        protected override JsonTypeInfo<Request> GetTypeInfo() => StartClusterCommandSerializerContext.Default.Request;
    }
}
