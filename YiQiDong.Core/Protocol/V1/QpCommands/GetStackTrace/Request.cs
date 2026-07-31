using Quick.Protocol;
using System.ComponentModel;
using System.Text.Json.Serialization.Metadata;

namespace YiQiDong.Protocol.V1.QpCommands.GetStackTrace
{
    [DisplayName("获取堆栈跟踪信息")]
    public class Request : AbstractQpSerializer<Request>, IQpCommandRequest<Request, Response>
    {
        protected override JsonTypeInfo<Request> GetTypeInfo() => UsingCommandSerializerContext.Default.Request;
    }
}
