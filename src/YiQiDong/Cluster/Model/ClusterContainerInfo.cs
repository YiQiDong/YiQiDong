using System.ComponentModel.DataAnnotations;

namespace YiQiDong.Cluster.Model
{
    public class ClusterContainerInfo
    {
        /// <summary>
        /// 容器名称
        /// </summary>
        public string ContainerName { get; set; }
        /// <summary>
        /// 是否自身节点激活
        /// </summary>
        public bool IsSelfNodeActive { get; set; }
    }
}
