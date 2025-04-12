using Quick.Protocol.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using YiQiDong.Protocol.V1.QpCommands;

namespace YiQiDong.Agent.CommandExecuters
{
    public class GetFunctionList
    {
        public static Protocol.V1.QpCommands.GetFunctionList.Response Execute(Quick.Protocol.QpChannel channel, Protocol.V1.QpCommands.GetFunctionList.Request request)
        {
            return new Protocol.V1.QpCommands.GetFunctionList.Response()
            {
                Items = AgentContext.Agent.GetFunctionList()
            };
        }
    }
}
