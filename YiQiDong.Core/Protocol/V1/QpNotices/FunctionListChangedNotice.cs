using Quick.Protocol;
using System.ComponentModel;
using System.Text.Json.Serialization.Metadata;

namespace YiQiDong.Protocol.V1.QpNotices
{
    [DisplayName("功能列表已改变通知")]
    public class FunctionListChangedNotice : AbstractQpSerializer<FunctionListChangedNotice>
    {
        protected override JsonTypeInfo<FunctionListChangedNotice> GetTypeInfo() => NoticesSerializerContext.Default.FunctionListChangedNotice;
    }
}
