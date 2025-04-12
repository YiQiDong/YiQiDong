namespace YiQiDong.Agent.CommandExecuters
{
    public class Start
    {
        public static Protocol.V1.QpCommands.Start.Response Execute(Quick.Protocol.QpChannel channel, Protocol.V1.QpCommands.Start.Request request)
        {
            AgentContext.Start();
            return new Protocol.V1.QpCommands.Start.Response();
        }
    }
}
