namespace YiQiDong.Agent.CommandExecuters;

public class GetStackTrace
{
    public static Protocol.V1.QpCommands.GetStackTrace.Response Execute(Quick.Protocol.QpChannel channel, Protocol.V1.QpCommands.GetStackTrace.Request request)
    {
        return new Protocol.V1.QpCommands.GetStackTrace.Response()
        {
            StackTrace = Environment.StackTrace
        };
    }
}