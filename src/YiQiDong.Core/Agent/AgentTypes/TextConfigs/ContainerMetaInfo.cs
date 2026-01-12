using System.Collections.Generic;

namespace YiQiDong.Agent.AgentTypes.TextConfigs
{
    //元文件夹信息
    public class MetaFolderInfo
    {
        /// <summary>
        /// 路径
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// 文件过滤器
        /// </summary>
        public string[] FileFilters { get; set; }
        /// <summary>
        /// 是否包含子文件夹
        /// </summary>
        public bool IncludeSubFolder { get; set; }
    }


    /// <summary>
    /// 容器元信息
    /// </summary>
    public class ContainerMetaInfo
    {
        /// <summary>
        /// 平台
        /// </summary>
        public string[] Platform { get; set; }
        /// <summary>
        /// 输入输出编码
        /// </summary>
        public string Encoding { get; set; }
        /// <summary>
        /// 进程文件名
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// 启动时进程文件名
        /// </summary>
        public string StartFileName { get; set; }
        /// <summary>
        /// 启动进程参数
        /// </summary>
        public string StartArguments { get; set; }
        /// <summary>
        /// 停止时进程文件名
        /// </summary>
        public string StopFileName { get; set; }
        /// <summary>
        /// 停止进程参数
        /// </summary>
        public string StopArguments { get; set; }
        /// <summary>
        /// 进程退出命令
        /// </summary>
        public string ExitCommand { get; set; }
        /// <summary>
        /// 进程退出超时时间
        /// </summary>
        public int ExitTimeout { get; set; } = 10000;
        /// <summary>
        /// 进程工作目录
        /// </summary>
        public string WorkingDir { get; set; }
        /// <summary>
        /// 环境变量
        /// </summary>
        public EnvironmentVariableInfo[] Environments { get; set; }
        /// <summary>
        /// 路径列表
        /// </summary>
        public string[] Path { get; set; }
        /// <summary>
        /// 配置文件目录
        /// </summary>
        public string[] ConfigFolders { get; set; }
        /// <summary>
        /// 配置文件信息目录
        /// </summary>
        public MetaFolderInfo[] ConfigFolderInfos { get; set; }
        /// <summary>
        /// 配置文件字典
        /// </summary>
        public Dictionary<string, string> ConfigFiles { get; set; }
        /// <summary>
        /// 配置文件编码
        /// </summary>
        public string ConfigFileEncoding { get; set; }
        /// <summary>
        /// 容器文件，从镜像目录复制到容器目录
        /// </summary>
        public Dictionary<string, string> ContainerFiles { get; set; }
        /// <summary>
        /// 容器目录，从镜像目录复制到容器目录
        /// </summary>
        public Dictionary<string, string> ContainerFolders { get; set; }
        /// <summary>
        /// 容器目录信息，从镜像目录复制到容器目录
        /// </summary>
        public Dictionary<string, MetaFolderInfo> ContainerFolderInfos { get; set; }
        /// <summary>
        /// 帮助字典
        /// </summary>
        public Dictionary<string, string> HelpDict { get; set; }

        internal string GetStartFileName() => StartFileName ?? FileName;
        internal string GetStopFileName() => StopFileName ?? FileName;
    }
}