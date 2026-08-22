namespace YiQiDong.Agent.CommandExecuters
{
    public class GetFunctionList
    {
        public static async ValueTask<Protocol.V1.QpCommands.GetFunctionList.Response> Execute(Quick.Protocol.QpChannel channel, Protocol.V1.QpCommands.GetFunctionList.Request request)
        {
            return new Protocol.V1.QpCommands.GetFunctionList.Response()
            {
                Items = AgentContext.Agent.GetFunctionList()
            };
        }
    }
}
