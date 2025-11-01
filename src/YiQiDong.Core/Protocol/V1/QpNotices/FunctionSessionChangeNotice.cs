
using System.ComponentModel;
using System.Text.Json.Serialization.Metadata;
using Quick.Fields;
using Quick.Protocol;

namespace YiQiDong.Protocol.V1.QpNotices;

[DisplayName("功能列表已改变通知")]
public class FunctionSessionChangeNotice : AbstractQpSerializer<FunctionSessionChangeNotice>
{
    protected override JsonTypeInfo<FunctionSessionChangeNotice> GetTypeInfo() => NoticesSerializerContext.Default.FunctionSessionChangeNotice;   
    public FieldForGet[] Items { get; set; }
}