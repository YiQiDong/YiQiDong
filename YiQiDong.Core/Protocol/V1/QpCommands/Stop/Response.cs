using Quick.Protocol;
using System.Text.Json.Serialization.Metadata;

namespace YiQiDong.Protocol.V1.QpCommands.Stop
{
    public class Response : AbstractQpSerializer<Response>
    {
        protected override JsonTypeInfo<Response> GetTypeInfo() => StopCommandSerializerContext.Default.Response;
        /// <summary>
        /// 退出码
        /// </summary>
        public int ExitCode { get; set; }
    }
}
