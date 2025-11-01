namespace YiQiDong.Agent.CommandExecuters;

public class OpenFunctionSession
{
    public static Protocol.V1.QpCommands.OpenFunctionSession.Response Execute(Quick.Protocol.QpChannel channel, Protocol.V1.QpCommands.OpenFunctionSession.Request request)
    {
        var sessionId = AgentContext.Agent.OpenFunctionSession(channel, request.FunctionId);
        return new Protocol.V1.QpCommands.OpenFunctionSession.Response()
        {
            SessionId = sessionId
        };
    }
}
