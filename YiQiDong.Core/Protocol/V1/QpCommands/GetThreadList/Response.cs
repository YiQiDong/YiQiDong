using Quick.Protocol;
using System.Text.Json.Serialization.Metadata;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Protocol.V1.QpCommands.GetThreadList
{
    public class Response : AbstractQpSerializer<Response>
    {
        protected override JsonTypeInfo<Response> GetTypeInfo() => UsingCommandSerializerContext.Default.Response;
        public ThreadInfo[] Threads { get; set; }
    }
}
