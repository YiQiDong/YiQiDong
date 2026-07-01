using System.ComponentModel;
using System.Text.Json.Serialization.Metadata;
using Quick.Protocol;

namespace YiQiDong.Protocol.V1.QpCommands.CloseFunctionSession;


[DisplayName("关闭功能Session")]
public class Request : AbstractQpSerializer<Request>, IQpCommandRequest<Request, Response>
{
    protected override JsonTypeInfo<Request> GetTypeInfo() => CloseFunctionSessionSerializerContext.Default.Request;
    /// <summary>
    /// Session编号
    /// </summary>
    public string SessionId { get; set; }
}
