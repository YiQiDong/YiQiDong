using Quick.Protocol;
using System.ComponentModel;
using System.Text.Json.Serialization.Metadata;
using YiQiDong.Cluster.Model;

namespace YiQiDong.Cluster.Protocol.QpCommands.UpdateConfig
{
    [DisplayName("更新集群配置")]
    public class Request : AbstractQpSerializer<Request>, IQpCommandRequest<Request, Response>
    {
        protected override JsonTypeInfo<Request> GetTypeInfo() => UpdateConfigCommandSerializerContext.Default.Request;
        public ClusterConfig Config { get; set; }
    }
}
