using Quick.Protocol;
using System.ComponentModel;
using System.Text.Json.Serialization.Metadata;

namespace YiQiDong.Protocol.V1.QpCommands.Register
{
    [DisplayName("注册容器")]
    public class Request : AbstractQpSerializer<Request>, IQpCommandRequest<Request, Response>
    {
        protected override JsonTypeInfo<Request> GetTypeInfo() => RegisterCommandSerializerContext.Default.Request;
        /// <summary>
        /// 环境变量
        /// </summary>
        public Dictionary<string, string> EnviromentVariables { get; set; }
    }
}
