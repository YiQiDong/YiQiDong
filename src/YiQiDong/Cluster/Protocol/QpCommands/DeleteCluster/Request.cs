using Quick.Protocol;
using System.ComponentModel;
using System.Text.Json.Serialization.Metadata;

namespace YiQiDong.Cluster.Protocol.QpCommands.DeleteCluster
{
    [DisplayName("删除集群")]
    public class Request : AbstractQpSerializer<Request>, IQpCommandRequest<Request, Response>
    {
        protected override JsonTypeInfo<Request> GetTypeInfo() => DeleteClusterCommandSerializerContext.Default.Request;
    }
}
