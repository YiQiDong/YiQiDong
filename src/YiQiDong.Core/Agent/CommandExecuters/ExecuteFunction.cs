using Quick.Protocol.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using YiQiDong.Protocol.V1.QpCommands;

namespace YiQiDong.Agent.CommandExecuters
{
    public class ExecuteFunction
    {
        public static Protocol.V1.QpCommands.ExecuteFunction.Response Execute(Quick.Protocol.QpChannel handler, Protocol.V1.QpCommands.ExecuteFunction.Request request)
        {
            return new Protocol.V1.QpCommands.ExecuteFunction.Response()
            {
                Items = AgentContext.Agent.ExecuteFunction(request.Data)
            };
        }
    }
}
