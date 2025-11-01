using System.Text.Json.Serialization.Metadata;
using Quick.Fields;
using Quick.Protocol;

namespace YiQiDong.Protocol.V1.QpCommands.OpenFunctionSession;

public class Response : AbstractQpSerializer<Response>
{
    protected override JsonTypeInfo<Response> GetTypeInfo() => OpenFunctionSessionSerializerContext.Default.Response;
    /// <summary>
    /// Session编号
    /// </summary>
    public string SessionId { get; set; }
    public FieldForGet[] Items { get; set; }
}
