using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YiQiDong.Core.Protocol.V1.Model
{
    /// <summary>
    /// 配置文件信息
    /// </summary>
    public class ConfigFileInfo
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; }
        /// <summary>
        /// 文件编码
        /// </summary>
        public string FileEncoding { get; set; }
    }
}
