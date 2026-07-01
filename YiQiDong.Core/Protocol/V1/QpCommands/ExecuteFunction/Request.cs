using Quick.Protocol;
using System.ComponentModel;
using System.Text.Json.Serialization.Metadata;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Protocol.V1.QpCommands.ExecuteFunction
{
    [DisplayName("执行功能")]
    public class Request : AbstractQpSerializer<Request>, IQpCommandRequest<Request, Response>
    {
        protected override JsonTypeInfo<Request> GetTypeInfo() => ExecuteFunctionCommandSerializerContext.Default.Request;

        /// <summary>
        /// 请求内容
        /// </summary>
        public FunctionRequest Data { get; set; }
    }
}
