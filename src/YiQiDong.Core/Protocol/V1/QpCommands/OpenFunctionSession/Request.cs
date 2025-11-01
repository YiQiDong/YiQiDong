using System.ComponentModel;
using System.Text.Json.Serialization.Metadata;
using Quick.Protocol;

namespace YiQiDong.Protocol.V1.QpCommands.OpenFunctionSession;

[DisplayName("打开功能Session")]
public class Request : AbstractQpSerializer<Request>, IQpCommandRequest<Request, Response>
{
    protected override JsonTypeInfo<Request> GetTypeInfo() => OpenFunctionSessionSerializerContext.Default.Request;

    /// <summary>
    /// 功能编号
    /// </summary>
    public string FunctionId { get; set; }
}