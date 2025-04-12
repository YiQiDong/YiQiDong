using Quick.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using YiQiDong.Core.Protocol.V1.Model;

namespace YiQiDong.Protocol.V1.QpCommands.GetConfigFileList
{
    public class Response : AbstractQpSerializer<Response>
    {
        protected override JsonTypeInfo<Response> GetTypeInfo() => GetConfigFileListCommandSerializerContext.Default.Response;
        public ConfigFileInfo[] Items { get; set; }
    }
}
