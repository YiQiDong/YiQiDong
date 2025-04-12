using Quick.Protocol;
using System.Text.Json.Serialization.Metadata;

namespace YiQiDong.Protocol.V1.QpNotices
{
    /// <summary>
    /// 容器已初始化通知
    /// </summary>
    public class ContainerInitedNotice : AbstractQpSerializer<ContainerInitedNotice>
    {
        protected override JsonTypeInfo<ContainerInitedNotice> GetTypeInfo() => NoticesSerializerContext.Default.ContainerInitedNotice;
    }
}
