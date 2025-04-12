using Quick.Protocol;
using System.Text.Json.Serialization.Metadata;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Cluster.Protocol.QpCommands.GetContainerList
{
    public class Response : AbstractQpSerializer<Response>
    {
        protected override JsonTypeInfo<Response> GetTypeInfo() => GetContainerListCommandSerializerContext.Default.Response;
        public ContainerInfo[] Items { get; set; }
    }
}
