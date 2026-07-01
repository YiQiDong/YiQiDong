using System;

namespace YiQiDong.Agent.CommandExecuters;

public class CloseFunctionSession
{
    public static Protocol.V1.QpCommands.CloseFunctionSession.Response Execute(Quick.Protocol.QpChannel channel, Protocol.V1.QpCommands.CloseFunctionSession.Request request)
    {
        AgentContext.Agent.CloseFunctionSession(request.SessionId);
        return new Protocol.V1.QpCommands.CloseFunctionSession.Response();
    }
}
