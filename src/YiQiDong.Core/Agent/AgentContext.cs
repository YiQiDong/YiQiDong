using Quick.Protocol;
using Quick.Protocol.Streams;
using System.Text;
using YiQiDong.Core;
using YiQiDong.Core.Agent;
using YiQiDong.Core.Utils;
using YiQiDong.Core.Utils.Unix;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Agent;

public class AgentContext
{
    private static CancellationTokenSource cts = null;
    public static bool IsContainerRuning { get; private set; }
    public static QpClient Client { get; private set; }
    public static ContainerContext Container { get; private set; }
    public static IAgent Agent { get; private set; }
    private static string[] logIgnoreList;

    private class LogTextWriter : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
        private LogLevel logLevel;

        public LogTextWriter(LogLevel logLevel)
        {
            this.logLevel = logLevel;
        }

        public override void WriteLine(string value)
        {
            Log(logLevel, value);
        }
    }

    public static void Log(LogLevel level, string content)
    {
        if (logIgnoreList != null)
            if (logIgnoreList.Any(t => content.Contains(t)))
                return;

        if (!IsContainerRuning)
        {
            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: [{level}] {content}");
            return;
        }
        try
        {
            if (level < Container.LogLevel)
                return;
            Client?.SendNoticePackage(new Protocol.V1.QpNotices.ContainerLogNotice()
            {
                Level = level,
                Content = content
            });
        }
        catch { }
    }

    public static void LogTrace(string content) => Log(LogLevel.Trace, content);
    public static void LogDebug(string content) => Log(LogLevel.Debug, content);
    public static void LogInfo(string content) => Log(LogLevel.Info, content);
    public static void LogWarn(string content) => Log(LogLevel.Warn, content);
    public static void LogError(string content) => Log(LogLevel.Error, content);

    public static void Dispose()
    {
        try
        {
            if (Agent != null)
            {
                if (Container != null && Container.AutoStart)
                    Agent.Stop();
                Agent = null;
            }
        }
        catch { }
        try
        {
            Client?.Close();
            Client = null;
        }
        catch { }
        cts?.Cancel();
    }

    public static async Task Run<TAgent>(string[] args)
        where TAgent : IAgent, new()
    {
        IsContainerRuning = args != null && args.Length > 0;
        var commandExecuterManager = new CommandExecuterManager();
        commandExecuterManager.Register(new Protocol.V1.QpCommands.GetFunctionList.Request(), CommandExecuters.GetFunctionList.Execute);
        commandExecuterManager.Register(new Protocol.V1.QpCommands.ExecuteFunction.Request(), CommandExecuters.ExecuteFunction.Execute);
        commandExecuterManager.Register(new Protocol.V1.QpCommands.GetConfigFileList.Request(), CommandExecuters.GetConfigFileList.Execute);
        commandExecuterManager.Register(new Protocol.V1.QpCommands.Start.Request(), CommandExecuters.Start.Execute);
        commandExecuterManager.Register(new Protocol.V1.QpCommands.Stop.Request(), CommandExecuters.Stop.Execute);
        commandExecuterManager.Register(new Protocol.V1.QpCommands.Exit.Request(), CommandExecuters.Exit.Execute);
        commandExecuterManager.Register(new Protocol.V1.QpCommands.OpenFunctionSession.Request(), CommandExecuters.OpenFunctionSession.Execute);
        commandExecuterManager.Register(new Protocol.V1.QpCommands.CloseFunctionSession.Request(), CommandExecuters.CloseFunctionSession.Execute);

        Agent = new TAgent();
        //如果是Linux系统，则设置进程名
        if (OperatingSystem.IsLinux())
            UnixUtils.SetProcessName(Agent.ProcessName);

        Agent.FunctionListChanged += (sender, e) =>
        {
            Client?.SendNoticePackage(new Protocol.V1.QpNotices.FunctionListChangedNotice());
        };

        if (!IsContainerRuning)
        {
            Agent.Init();
            Agent.Start();
            Console.WriteLine("按回车键退出.");
            Console.ReadLine();
            //清理
            Dispose();
            return;
        }
        cts = new CancellationTokenSource();
        LogInfo("正在连接到易启动...");
        var options = new QpStreamClientOptions()
        {
            BaseStream = new InputOutputStream(Console.OpenStandardInput(), Console.OpenStandardOutput()),
            Password = nameof(YiQiDong),
            TransportTimeout = 60000,
            InstructionSet = new[] { Protocol.V1.Instruction.Instance }
        };
        options.RegisterCommandExecuterManager(commandExecuterManager);
        Client = new QpStreamClient(options);
        Client.Disconnected += (sender, e) =>
        {
            Console.Error.WriteLine("与易启动南向接口的连接已断开，容器进程正在退出...");
            Dispose();
        };
        try
        {
            await Client.ConnectAsync();
        }
        catch
        {
            Console.Error.WriteLine("连接到易启动失败，容器进程正在退出...");
            return;
        }
        try
        {
            //注册容器
            var rep = await Client.SendCommand(new Protocol.V1.QpCommands.Register.Request());
            Container = new ContainerContext(rep.ContainerInfo)
            {
                ContainerFolder = rep.ContainerFolder,
                ImageFolder = rep.ImageFolder
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("向易启动注册容器失败，原因：" + ExceptionUtils.GetExceptionString(ex));
            return;
        }
        if (!string.IsNullOrEmpty(Container.LogIgnoreList))
            logIgnoreList = Container.LogIgnoreList.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Console.SetOut(new LogTextWriter(LogLevel.Info));
        Console.SetError(new LogTextWriter(LogLevel.Error));
        //设置当前目录到容器目录
        Environment.CurrentDirectory = Container.ContainerFolder;
        //代理初始化
        Agent.Init();
        //发送容器已初始化通知
        Client?.SendNoticePackage(new Protocol.V1.QpNotices.ContainerInitedNotice());
        //等待进程退出
        try
        {
            await Task.Delay(-1, cts.Token);
        }
        catch (OperationCanceledException)
        { }
        //清理
        Dispose();
    }

    public static void Start()
    {
        if (Container != null)
            Container.AutoStart = true;
        Task.Run(() =>
        {
            try
            {
                Agent.Start();
            }
            catch (Exception ex)
            {
                LogError($"启动时出错，原因：{ExceptionUtils.GetExceptionString(ex)}");
            }
            //发送容器已启动通知
            Client?.SendNoticePackage(new Protocol.V1.QpNotices.ContainerStartedNotice());
        });
    }

    public static void Stop()
    {
        if (Container == null)
            return;
        if (Container.AutoStart)
        {
            Container.AutoStart = false;
            try
            {
                Agent.Stop();
            }
            catch (Exception ex)
            {
                LogError($"停止时出错，原因：{ExceptionUtils.GetExceptionString(ex)}");
            }
            //发送容器已停止通知
            Client?.SendNoticePackage(new Protocol.V1.QpNotices.ContainerStopedNotice());
        }
    }
}