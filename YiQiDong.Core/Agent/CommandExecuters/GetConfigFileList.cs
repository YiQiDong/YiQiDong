namespace YiQiDong.Agent.CommandExecuters;

public class GetConfigFileList
{
    public static Protocol.V1.QpCommands.GetConfigFileList.Response Execute(Quick.Protocol.QpChannel channel, Protocol.V1.QpCommands.GetConfigFileList.Request request)
    {
        return new Protocol.V1.QpCommands.GetConfigFileList.Response()
        {
            FunctionName = AgentContext.Agent.ConfigFilesFunctionName,
            Items = AgentContext.Agent.GetConfigFiles()
        };
    }
}
