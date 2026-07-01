using Quick.Protocol;
using System.Text.Json.Serialization.Metadata;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Protocol.V1.QpCommands.Register
{
    public class Response : AbstractQpSerializer<Response>
    {
        protected override JsonTypeInfo<Response> GetTypeInfo() => RegisterCommandSerializerContext.Default.Response;
        public ContainerInfo ContainerInfo { get; set; }
        public string ContainerFolder { get; set; }
        public string ImageFolder { get; set; }
    }
}
