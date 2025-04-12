using Quick.Protocol;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Protocol.V1.QpCommands.AddReverseProxyRule
{
    [DisplayName("添加反向代理规则")]
    public class Request : AbstractQpSerializer<Request>, IQpCommandRequest<Request, Response>
    {
        protected override JsonTypeInfo<Request> GetTypeInfo() => AddReverseProxyRuleCommandSerializerContext.Default.Request;

        /// <summary>
        /// 路径
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// URL
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// 链接
        /// </summary>
        public ReverseProxyRuleLinkInfo[] Links { get; set; }
    }
}
