using Quick.Protocol;
using System.ComponentModel;
using System.Text.Json.Serialization.Metadata;
using YiQiDong.Cluster.Model;

namespace YiQiDong.Cluster.Protocol.QpCommands.CreateCluster
{
    [DisplayName("创建集群")]
    public class Request : AbstractQpSerializer<Request>, IQpCommandRequest<Request, Response>
    {
        protected override JsonTypeInfo<Request> GetTypeInfo() => CreateClusterCommandSerializerContext.Default.Request;
        public ClusterConfig Config { get; set; }
    }
}
