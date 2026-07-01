using Quick.Protocol;
using System.Text.Json.Serialization.Metadata;

namespace YiQiDong.Protocol.V1.QpNotices
{
    /// <summary>
    /// 容器已停止通知
    /// </summary>
    public class ContainerStopedNotice : AbstractQpSerializer<ContainerStopedNotice>
    {
        protected override JsonTypeInfo<ContainerStopedNotice> GetTypeInfo() => NoticesSerializerContext.Default.ContainerStopedNotice;
    }
}
