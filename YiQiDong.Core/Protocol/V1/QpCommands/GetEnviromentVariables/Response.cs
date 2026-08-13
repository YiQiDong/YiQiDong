using Quick.Protocol;
using System.Text.Json.Serialization.Metadata;

namespace YiQiDong.Protocol.V1.QpCommands.GetEnviromentVariables
{
    public class Response : AbstractQpSerializer<Response>
    {
        protected override JsonTypeInfo<Response> GetTypeInfo() => GetEnviromentVariablesCommandSerializerContext.Default.Response;
        /// <summary>
        /// 环境变量
        /// </summary>
        public Dictionary<string, string> EnviromentVariables { get; set; }
    }
}
