using Quick.Protocol;
using System.Text.Json.Serialization.Metadata;

namespace YiQiDong.Protocol.V1.QpNotices
{
    /// <summary>
    /// 容器已启动通知
    /// </summary>
    public class ContainerStartedNotice : AbstractQpSerializer<ContainerStartedNotice>
    {
        protected override JsonTypeInfo<ContainerStartedNotice> GetTypeInfo() => NoticesSerializerContext.Default.ContainerStartedNotice;
    }
}
