using Quick.Protocol;
using System.ComponentModel;
using System.Text.Json.Serialization.Metadata;

namespace YiQiDong.Protocol.V1.QpCommands.Using
{
    [DisplayName("使用容器")]
    public class Request : AbstractQpSerializer<Request>, IQpCommandRequest<Request, Response>
    {
        protected override JsonTypeInfo<Request> GetTypeInfo() => UsingCommandSerializerContext.Default.Request;
        /// <summary>
        /// 容器编号
        /// </summary>
        public string ContainerId { get; set; }
    }
}
