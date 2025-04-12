using System.Text.Json;
using System.Text.Json.Serialization;

namespace YiQiDong.Protocol.V1.Model
{
    [JsonSerializable(typeof(ContainerInfo))]
    internal partial class ContainerInfoSerializerContext : JsonSerializerContext { }

    public class ContainerInfo
    {
        /// <summary>
        /// 编号
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 标签
        /// </summary>
        public string[] Tags { get; set; }
        /// <summary>
        /// 镜像编号
        /// </summary>
        public string ImageId { get; set; }
        /// <summary>
        /// 关联的镜像
        /// </summary>
        public ImageInfo Image { get; set; }
        /// <summary>
        /// 关联的运行库编号
        /// </summary>
        public string[] RuntimeIds { get; set; }
        /// <summary>
        /// 自动启动
        /// </summary>
        public bool AutoStart { get; set; }
        /// <summary>
        /// 日志忽略列表
        /// </summary>
        public string LogIgnoreList { get; set; }
        /// <summary>
        /// 日志级别
        /// </summary>
        public LogLevel LogLevel { get; set; } = LogLevel.Info;

        public static ContainerInfo Parse(string content)
        {
            try { return (ContainerInfo)JsonSerializer.Deserialize(content, typeof(ContainerInfo), ContainerInfoSerializerContext.Default); }
            catch { return null; }
        }
    }
}
