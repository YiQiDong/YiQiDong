using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YiQiDong.Protocol.V1.Model
{
    /// <summary>
    /// 反向代理规则链接信息
    /// </summary>
    public class ReverseProxyRuleLinkInfo
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// URL
        /// </summary>
        public string Url { get; set; }
    }
}
