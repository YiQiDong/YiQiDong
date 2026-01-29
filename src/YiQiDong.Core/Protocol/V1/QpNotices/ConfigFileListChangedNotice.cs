using System.Text.Json.Serialization.Metadata;
using Quick.Protocol;

namespace YiQiDong.Protocol.V1.QpNotices;

public class ConfigFileListChangedNotice : AbstractQpSerializer<ConfigFileListChangedNotice>
{
    protected override JsonTypeInfo<ConfigFileListChangedNotice> GetTypeInfo() => NoticesSerializerContext.Default.ConfigFileListChangedNotice;
}
