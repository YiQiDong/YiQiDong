using System.ComponentModel;
using System.Text.Json.Serialization.Metadata;
using Quick.Fields;
using Quick.Protocol;

namespace YiQiDong.Protocol.V1.QpNotices;

[DisplayName("功能Session已改变通知")]
public class FunctionSessionChangedNotice : AbstractQpSerializer<FunctionSessionChangedNotice>
{
    protected override JsonTypeInfo<FunctionSessionChangedNotice> GetTypeInfo() => NoticesSerializerContext.Default.FunctionSessionChangedNotice;
    public string SessionId { get; set; }
    public FieldForGet[] Items { get; set; }
}