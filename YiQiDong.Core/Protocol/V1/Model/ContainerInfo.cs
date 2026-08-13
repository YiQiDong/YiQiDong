using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YiQiDong.Protocol.V1.Model
{
    [JsonSerializable(typeof(ContainerInfo))]
    public partial class ContainerInfoSerializerContext : JsonSerializerContext
    {
        public static ContainerInfoSerializerContext Default2 { get; } = new ContainerInfoSerializerContext(new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    public class ContainerInfo
    {
        /// <summary>
        /// 编号
        /// </summary>
        public virtual string Id { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public virtual string Name { get; set; }
        /// <summary>
        /// 描述
        /// </summary>
        public virtual string Description { get; set; }
        /// <summary>
        /// 标签
        /// </summary>
        public virtual string[] Tags { get; set; }
        /// <summary>
        /// 镜像编号
        /// </summary>
        public virtual string ImageId { get; set; }
        /// <summary>
        /// 关联的镜像
        /// </summary>
        public virtual ImageInfo Image { get; set; }
        /// <summary>
        /// 关联的运行库编号
        /// </summary>
        public virtual string[] RuntimeIds { get; set; }
        /// <summary>
        /// 自动启动
        /// </summary>
        public virtual bool AutoStart { get; set; }
        /// <summary>
        /// 启用压缩
        /// </summary>
        public virtual bool EnableCompress { get; set; }
        /// <summary>
        /// 启用加密
        /// </summary>
        public virtual bool EnableEncrypt { get; set; }
        /// <summary>
        /// 加密算法
        /// </summary>
        public virtual string EncryptAlgorithm { get; set; } = "DES";
        /// <summary>
        /// 加密模式
        /// </summary>
        public virtual string EncryptMode { get; set; } = "ECB";
        /// <summary>
        /// 加密填充
        /// </summary>
        public virtual string EncryptPadding { get; set; } = "PKCS7";
        /// 传输超时时间
        /// </summary>
        public virtual int TransportTimeout { get; set; } = 60000;
        /// <summary>
        /// 日志级别
        /// </summary>
        public virtual LogLevel LogLevel { get; set; } = LogLevel.Info;

        public static ContainerInfo Parse(string content)
        {
            try { return (ContainerInfo)JsonSerializer.Deserialize(content, typeof(ContainerInfo), ContainerInfoSerializerContext.Default2); }
            catch { return null; }
        }
        
        public string ToJson()
        {
            return JsonSerializer.Serialize(this, ContainerInfoSerializerContext.Default2.ContainerInfo);
        }
    }
}
