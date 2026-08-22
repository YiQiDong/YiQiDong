using System.Collections;

namespace YiQiDong.Agent.CommandExecuters;

public class GetEnviromentVariables
{
    public static async ValueTask<Protocol.V1.QpCommands.GetEnviromentVariables.Response> Execute(Quick.Protocol.QpChannel channel, Protocol.V1.QpCommands.GetEnviromentVariables.Request request)
    {
        var environmentVariableDict = new Dictionary<string, string>();
        foreach (DictionaryEntry env in Environment.GetEnvironmentVariables())
            environmentVariableDict[env.Key.ToString()] = env.Value?.ToString();

        return new Protocol.V1.QpCommands.GetEnviromentVariables.Response()
        {
            EnviromentVariables = environmentVariableDict
        };
    }
}
