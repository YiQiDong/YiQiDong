namespace YiQiDong.Agent.CommandExecuters
{
    public class Stop
    {
        public static Protocol.V1.QpCommands.Stop.Response Execute(Quick.Protocol.QpChannel handler, Protocol.V1.QpCommands.Stop.Request request)
        {            
            AgentContext.Stop();
            return new Protocol.V1.QpCommands.Stop.Response();
        }
    }
}
