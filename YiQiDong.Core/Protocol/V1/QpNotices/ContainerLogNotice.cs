using Quick.Protocol;
using System.Text.Json.Serialization.Metadata;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Protocol.V1.QpNotices
{
    /// <summary>
    /// 容器日志通知
    /// </summary>
    public class ContainerLogNotice : AbstractQpSerializer<ContainerLogNotice>
    {
        protected override JsonTypeInfo<ContainerLogNotice> GetTypeInfo() => NoticesSerializerContext.Default.ContainerLogNotice;
        /// <summary>
        /// 日志级别
        /// </summary>
        public LogLevel Level { get; set; }
        /// <summary>
        /// 日志内容
        /// </summary>
        public string Content { get; set; }
    }
}
