using Quick.Protocol;
using System.Text.Json.Serialization.Metadata;
using YiQiDong.Core.Protocol.V1.Model;

namespace YiQiDong.Protocol.V1.QpCommands.GetConfigFileList
{
    public class Response : AbstractQpSerializer<Response>
    {
        protected override JsonTypeInfo<Response> GetTypeInfo() => GetConfigFileListCommandSerializerContext.Default.Response;
        public string FunctionName { get; set; }
        public ConfigFileInfo[] Items { get; set; }
    }
}
