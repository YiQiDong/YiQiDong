using System.Diagnostics;
using Quick.Protocol;

namespace YiQiDong.Core;

public class ContainerInterfaceManager
{
    public static ContainerInterfaceManager Instance { get; } = new ContainerInterfaceManager();
    private ContainerInterfaceManager() { }
    public string InterfaceUrl { get; private set; }
    

    private QpServer qpServer;

    public void Start()
    {
        var processId = Process.GetCurrentProcess().Id;
        var pipeName = $"{nameof(YiQiDong)}.{processId}.ContainerInterface";
        InterfaceUrl = $"{Quick.Protocol.Pipeline.QpPipelineClientOptions.URI_SCHEMA}://./{pipeName}";

        var options = new Quick.Protocol.Pipeline.QpPipelineServerOptions()
        {
            PipeName = pipeName,
            Password = nameof(YiQiDong),
            ServerProgram = "易启动容器接口管理器",
            InstructionSet = [YiQiDong.Protocol.V1.Instruction.Instance]
        };
        var commandExecuterManager = new CommandExecuterManager();
        commandExecuterManager.Register<YiQiDong.Protocol.V1.QpCommands.Register.Request, YiQiDong.Protocol.V1.QpCommands.Register.Response>(Register);
        options.RegisterCommandExecuterManager(commandExecuterManager);
        qpServer = options.CreateServer();
        qpServer.Start();
    }

    private YiQiDong.Protocol.V1.QpCommands.Register.Response Register(QpChannel channel, YiQiDong.Protocol.V1.QpCommands.Register.Request request)
    {
        var containerContext = ContainerManager.Instance.Get(request.ContainerId);
        if (containerContext == null)
            throw new KeyNotFoundException($"未找到编号为[{request.ContainerId}]的容器");
        containerContext.SetChannel((QpServerChannel)channel);
        return containerContext.Register(channel, request);
    }

    public void Stop()
    {
        qpServer.Stop();
    }
}