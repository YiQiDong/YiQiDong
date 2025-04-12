using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace YiQiDong.Protocol.V1.Model
{
    [JsonSerializable(typeof(ImageInfo))]
    internal partial class ImageInfoSerializerContext : JsonSerializerContext { }
    /// <summary>
    /// 镜像信息
    /// </summary>
    public class ImageInfo
    {
        /// <summary>
        /// 编号
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 默认编号
        /// </summary>
        public string DefaultId { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 标签
        /// </summary>
        public string[] Tags { get; set; }
        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 版本
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// 平台
        /// </summary>
        public string[] Platform { get; set; }
        /// <summary>
        /// 运行库
        /// </summary>
        public string[] Runtime { get; set; }
        /// <summary>
        /// 构建时间
        /// </summary>
        public string BuildTime { get; set; }
        /// <summary>
        /// Agent执行文件，默认是dotnet
        /// </summary>
        public string AgentExecute { get; set; }
        /// <summary>
        /// Agent启动文件，作为Agent执行文件的参数
        /// </summary>
        public string AgentStartup { get; set; }
        /// <summary>
        /// Agent类型，仅当使用YiQiDong.Agent时有效
        /// </summary>
        public string AgentType { get; set; }
        /// <summary>
        /// Agent类型配置
        /// </summary>
        public JsonObject AgentConfig { get; set; }
        /// <summary>
        /// 环境变量
        /// </summary>
        public Dictionary<string, string> Environment { get; set; }
        /// <summary>
        /// 路径列表
        /// </summary>
        public string[] Path { get; set; }
        /// <summary>
        /// 可执行文件，用于在非Windows系统上添加可执行权限
        /// </summary>
        public string[] ExecuteFiles { get; set; }
        /// <summary>
        /// 测试命令
        /// </summary>
        public Dictionary<string, string[]> TestCommand { get; set; }

        public static ImageInfo Parse(string content)
        {
            return (ImageInfo)JsonSerializer.Deserialize(content, typeof(ImageInfo), ImageInfoSerializerContext.Default);
        }
    }
}
