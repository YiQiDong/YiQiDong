using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace YiQiDong.Cluster.Model
{

    [JsonSerializable(typeof(ClusterConfig))]
    [JsonSourceGenerationOptions(WriteIndented = true)]
    internal partial class ClusterConfigSerializerContext : JsonSerializerContext { }
    
    public class ClusterConfig
    {
        /// <summary>
        /// 自身节点URL
        /// </summary>
        public string SelfNodeUrl { get; set; }
        /// <summary>
        /// 对方节点URL
        /// </summary>
        public string OppositeNodeUrl { get; set; }
        /// <summary>
        /// 对方节点密码
        /// </summary>
        public string OppositeNodePassword { get; set; }
        /// <summary>
        /// 传输超时时间
        /// </summary>
        public int TransportTimeout { get; set; } = 15000;
        /// <summary>
        /// 自动启动
        /// </summary>
        public bool AutoStart { get; set; }
        /// <summary>
        /// 集群容器列表
        /// </summary>
        public List<ClusterContainerInfo> ClusterContainerList { get; set; } = new List<ClusterContainerInfo>();
    }
}
