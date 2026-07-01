using Quick.Protocol;
using System.Text.Json.Serialization.Metadata;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Protocol.V1.QpCommands.GetFunctionList
{
    public class Response : AbstractQpSerializer<Response>
    {
        protected override JsonTypeInfo<Response> GetTypeInfo() => GetFunctionListCommandSerializerContext.Default.Response;
        public FunctionInfo[] Items { get; set; }
    }
}
