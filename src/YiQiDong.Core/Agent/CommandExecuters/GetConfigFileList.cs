using Quick.Protocol.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using YiQiDong.Protocol.V1.QpCommands;

namespace YiQiDong.Agent.CommandExecuters
{
    public class GetConfigFileList
    {
        public static Protocol.V1.QpCommands.GetConfigFileList.Response Execute(Quick.Protocol.QpChannel channel, Protocol.V1.QpCommands.GetConfigFileList.Request request)
        {
            return new Protocol.V1.QpCommands.GetConfigFileList.Response()
            {
                Items = AgentContext.Agent.GetConfigFiles()
            };
        }
    }
}
