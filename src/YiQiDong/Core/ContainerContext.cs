using Quick.Blazor.Bootstrap.ReverseProxy;
using Quick.Blazor.Bootstrap.ReverseProxy.Model;
using Quick.Fields;
using Quick.Protocol;
using Quick.Protocol.Exceptions;
using Quick.Shell.Utils;
using System.Buffers;
using System.Diagnostics;
using System.Text;
using YiQiDong.Core.Protocol.V1.Model;
using YiQiDong.Core.Utils;
using YiQiDong.Model;
using YiQiDong.Protocol.V1.Model;
using YiQiDong.Protocol.V1.QpNotices;
using YiQiDong.Utils;
using LogLevel = YiQiDong.Protocol.V1.Model.LogLevel;

namespace YiQiDong.Core;

public class ContainerContext : IDisposable
{
    public const string EXECUTE_FILE_AGENT = nameof(YiQiDong);

    //日志最多1000行
    public const int MAX_CONSOLE_OUTPUT_LINES = 1000;
    //日志最多50K个字符
    public const int MAX_CONSOLE_OUTPUT_CHARS = 50 * 1024;

    private Timer clearLogFilesTimer;
    private string logFolder;
    private NLog.LogFactory logFactory;
    private NLog.ILogger logger;
    private static Encoding ansiEncoding = Encoding.GetEncoding(Thread.CurrentThread.CurrentCulture.TextInfo.ANSICodePage);

    public YqdContainerInfo ContainerInfo { get; private set; }
    public Process Process { get; private set; }
    public QpServerChannel ProcessChannel { get; private set; }
    private List<ReverseProxyRule> reverseProxyRuleList = new List<ReverseProxyRule>();

    //是否显示容器日志
    public bool ShowConsoleHistory { get; set; } = true;
    private int consoleOutputCharCount = 0;
    public Queue<string> ConsoleOutputQueue { get; private set; } = new Queue<string>();
    private CommandExecuterManager commandExecuterManager;
    private NoticeHandlerManager noticeHandlerManager;

    public event EventHandler FunctionListChanged;
    public event EventHandler ReverseProxyRuleListChanged;

    private void RaiseEvent_FunctionListChanged()
    {
        FunctionListChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RaiseEvent_ReverseProxyRuleListChanged()
    {
        ReverseProxyRuleListChanged?.Invoke(this, EventArgs.Empty);
    }

    public ReverseProxyRule[] GetReverseProxyRuleList()
    {
        lock (reverseProxyRuleList)
            return reverseProxyRuleList.ToArray();
    }

    private YiQiDong.Protocol.V1.QpCommands.Register.Response Register(QpChannel channel, YiQiDong.Protocol.V1.QpCommands.Register.Request request)
    {
        channel.Disconnected += Channel_Disconnected;
        addConsoleHistory($"[平台]容器连接成功.");
        //如果需要手动触发容器初始化完成通知，则延迟1秒后触发
        if (ContainerInfo.ManualRaiseContainerInitedNotice)
        {
            Task.Delay(1000).ContinueWith(t => handleContainerInitedNotice(channel, new ContainerInitedNotice()));
        }
        return new YiQiDong.Protocol.V1.QpCommands.Register.Response()
        {
            ContainerInfo = ContainerInfo,
            ContainerFolder = ContainerPathUtils.GetContainerFolder(ContainerInfo.Id),
            ImageFolder = ImagePathUtils.GetImageFolder(ContainerInfo.ImageId)
        };
    }

    private YiQiDong.Protocol.V1.QpCommands.AddReverseProxyRule.Response AddReverseProxyRule(QpChannel channel, YiQiDong.Protocol.V1.QpCommands.AddReverseProxyRule.Request request)
    {
        if (!request.Path.StartsWith("/"))
            throw new CommandException(255, "Path必须以'/'开头！");
        var path = $"/{ContainerInfo.Id}{request.Path}";
        ReverseProxyManager.Instance.AddRule(path, request.Url);
        lock (reverseProxyRuleList)
            reverseProxyRuleList.Add(new ReverseProxyRule()
            {
                Path = path,
                Url = request.Url,
                Links = request.Links.Select(t => new Quick.Blazor.Bootstrap.ReverseProxy.Model.ReverseProxyRuleLinkInfo()
                {
                    Name = t.Name,
                    Url = t.Url
                }).ToArray()
            });
        RaiseEvent_ReverseProxyRuleListChanged();
        return new YiQiDong.Protocol.V1.QpCommands.AddReverseProxyRule.Response();
    }

    public string ConsoleHistory
    {
        get
        {
            lock (ConsoleOutputQueue)
                return string.Join(Environment.NewLine, ConsoleOutputQueue);
        }
    }
    public event EventHandler ConsoleHistoryChanged;

    public ConfigFileInfo[] ConfigFiles { get; set; }

    private bool _IsConnected = false;

    public bool IsConnected
    {
        get { return _IsConnected; }
        private set
        {
            _IsConnected = value;
            ContainerManager.Instance.RaiseEvent_ContainerChanged();
        }
    }

    private void InitLogFactory()
    {
        var config = new NLog.Config.LoggingConfiguration();
        var layout = NLog.Layouts.Layout.FromString("${date:format=HH\\:mm\\:ss.ffff}: ${message}");
        logFolder = Path.Combine(ContainerPathUtils.GetContainerFolder(ContainerInfo.Id), "YiQiDong.Container.Logs");
        var logFile = Path.Combine(logFolder, "${shortdate}.log");
        var fileTarget = new NLog.Targets.FileTarget(string.Empty)
        {
            Layout = layout,
            FileName = logFile
        };
        config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, fileTarget);

        logFactory = new NLog.LogFactory() { Configuration = config };
        logger = logFactory.GetLogger(ContainerInfo.Name);
    }

    public ContainerContext(YqdContainerInfo containerInfo)
    {
        ContainerInfo = containerInfo;

        commandExecuterManager = new CommandExecuterManager();
        commandExecuterManager.Register<YiQiDong.Protocol.V1.QpCommands.Register.Request, YiQiDong.Protocol.V1.QpCommands.Register.Response>(Register);
        commandExecuterManager.Register<YiQiDong.Protocol.V1.QpCommands.AddReverseProxyRule.Request, YiQiDong.Protocol.V1.QpCommands.AddReverseProxyRule.Response>(AddReverseProxyRule);

        noticeHandlerManager = new NoticeHandlerManager();
        noticeHandlerManager.Register<ContainerLogNotice>(handleContainerLogNotice);
        noticeHandlerManager.Register<FunctionListChangedNotice>(handleFunctionListChangedNotice);
        noticeHandlerManager.Register<ContainerInitedNotice>(handleContainerInitedNotice);
        noticeHandlerManager.Register<ContainerStartedNotice>(handleContainerStartedNotice);
        noticeHandlerManager.Register<ContainerStopedNotice>(handleContainerStopedNotice);
        noticeHandlerManager.Register<FunctionSessionChangedNotice>(handleFunctionSessionChangedNotice);
        InitLogFactory();
    }

    private void addConsoleHistory(string line)
    {
        line = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: {line}";
        lock (ConsoleOutputQueue)
        {
            while (ConsoleOutputQueue.Count > MAX_CONSOLE_OUTPUT_LINES
                || consoleOutputCharCount > MAX_CONSOLE_OUTPUT_CHARS)
            {
                var delLine = ConsoleOutputQueue.Dequeue();
                consoleOutputCharCount -= delLine.Length;
            }
            ConsoleOutputQueue.Enqueue(line);
            consoleOutputCharCount += line.Length;
        }
        ConsoleHistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    private void pushLog(LogLevel level, string message)
    {
        addConsoleHistory($"[{level}] {message}");
        //如果启用了记录日志到文件
        if (ContainerInfo.EnableRecordLog)
        {
            try
            {
                switch (level)
                {
                    case LogLevel.Trace:
                        logger.Trace(message);
                        break;
                    case LogLevel.Debug:
                        logger.Debug(message);
                        break;
                    case LogLevel.Info:
                        logger.Info(message);
                        break;
                    case LogLevel.Warn:
                        logger.Warn(message);
                        break;
                    case LogLevel.Error:
                        logger.Error(message);
                        break;
                }
            }
            catch (Exception ex)
            {
                addConsoleHistory("[平台]记录日志到文件时出错，原因：" + ExceptionUtils.GetExceptionMessage(ex));
            }
        }
    }

    //处理容器日志通知
    private void handleContainerLogNotice(QpChannel channel, YiQiDong.Protocol.V1.QpNotices.ContainerLogNotice notice)
    {
        pushLog(notice.Level, notice.Content);
    }

    //处理功能列表已改变通知
    private void handleFunctionListChangedNotice(QpChannel channel, YiQiDong.Protocol.V1.QpNotices.FunctionListChangedNotice notice)
    {
        RaiseEvent_FunctionListChanged();
    }

    //处理容器初始化完成通知
    private void handleContainerInitedNotice(QpChannel channel, ContainerInitedNotice notice)
    {
        Process?.Refresh();
        addConsoleHistory($"[平台]容器启用完成.");
        Task.Run(async () =>
        {
            //获取容器配置文件
            try
            {
                var rep = await ProcessChannel.SendCommand(new YiQiDong.Protocol.V1.QpCommands.GetConfigFileList.Request());
                ConfigFiles = rep.Items;
            }
            catch
            { }
            RaiseEvent_FunctionListChanged();
            //如果设置了自动启动
            if (ContainerInfo.AutoStart)
                await Start();
            IsConnected = true;
            //开始处理定时启动、停止和重启功能
            if (!string.IsNullOrEmpty(ContainerInfo.StartCron))
            {
                startCrontabSchedule = NCrontab.CrontabSchedule.TryParse(ContainerInfo.StartCron);
                if (startCrontabSchedule == null)
                {
                    addConsoleHistory($"[平台]设置的定时启动表示式[{ContainerInfo.StartCron}]无效！");
                }
                else
                {
                    startCronNextExecuteTime = startCrontabSchedule.GetNextOccurrence(DateTime.Now);
                    addConsoleHistory($"[平台]容器已设置定时启动表示式[{ContainerInfo.StartCron}]，下次执行时间：{startCronNextExecuteTime}");
                }
            }
            if (!string.IsNullOrEmpty(ContainerInfo.StopCron))
            {
                stopCrontabSchedule = NCrontab.CrontabSchedule.TryParse(ContainerInfo.StopCron);
                if (stopCrontabSchedule == null)
                {
                    addConsoleHistory($"[平台]设置的定时停止表示式[{ContainerInfo.StopCron}]无效！");
                }
                else
                {
                    stopCronNextExecuteTime = stopCrontabSchedule.GetNextOccurrence(DateTime.Now);
                    addConsoleHistory($"[平台]容器已设置定时停止表示式[{ContainerInfo.StopCron}]，下次执行时间：{stopCronNextExecuteTime}");
                }
            }
            if (!string.IsNullOrEmpty(ContainerInfo.RestartCron))
            {
                restartCrontabSchedule = NCrontab.CrontabSchedule.TryParse(ContainerInfo.RestartCron);
                if (restartCrontabSchedule == null)
                {
                    addConsoleHistory($"[平台]设置的定时重启表示式[{ContainerInfo.RestartCron}]无效！");
                }
                else
                {
                    restartCronNextExecuteTime = restartCrontabSchedule.GetNextOccurrence(DateTime.Now);
                    addConsoleHistory($"[平台]容器已设置定时重启表示式[{ContainerInfo.RestartCron}]，下次执行时间：{restartCronNextExecuteTime}");
                }
            }

            if (startCrontabSchedule != null
                || stopCrontabSchedule != null
                || restartCrontabSchedule != null)
            {
                ctsCron?.Cancel();
                ctsCron = new CancellationTokenSource();
                beginCheckCron(ctsCron.Token);
            }
        });
    }

    private void handleContainerStartedNotice(QpChannel channel, ContainerStartedNotice notice)
    {
        addConsoleHistory("[平台]容器启动完成.");
    }

    private void handleContainerStopedNotice(QpChannel channel, ContainerStopedNotice notice)
    {
        addConsoleHistory("[平台]容器停止完成.");
    }

    private Dictionary<string, Action<FunctionSessionChangedNotice>> functionSessionChangedNoticeHandlerDict = new();

    public void AddFunctionSessionChangedNoticeHandler(string sessionId, Action<FunctionSessionChangedNotice> handler)
    {
        lock (functionSessionChangedNoticeHandlerDict)
            functionSessionChangedNoticeHandlerDict[sessionId] = handler;
    }

    public void RemoveFunctionSessionChangedNoticeHandler(string sessionId)
    {
        lock (functionSessionChangedNoticeHandlerDict)
            if (functionSessionChangedNoticeHandlerDict.ContainsKey(sessionId))
                functionSessionChangedNoticeHandlerDict.Remove(sessionId);
    }

    private void handleFunctionSessionChangedNotice(QpChannel channel, FunctionSessionChangedNotice notice)
    {
        if (functionSessionChangedNoticeHandlerDict.TryGetValue(notice.SessionId, out var handler))
            handler.Invoke(notice);
    }

    public void BeginEnable()
    {
        if (!ContainerInfo.Enable)
            return;
        var containerInfo = ContainerInfo;
        if (containerInfo == null)
            return;

        if (Process == null)
        {
            addConsoleHistory("[平台]正在启用容器...");
            var imageInfo = containerInfo.Image;
            if (imageInfo == null)
            {
                addConsoleHistory($"[错误]容器关联的镜像[{containerInfo.ImageId}]不存在！");
                return;
            }
            var imageFolder = ImagePathUtils.GetImageFolder(imageInfo.Id);
            var containerFolder = ContainerPathUtils.GetContainerFolder(containerInfo.Id);
            RuntimeInfo[] runtimes = null;
            if (ContainerInfo.RuntimeIds != null && ContainerInfo.RuntimeIds.Length > 0)
            {
                List<RuntimeInfo> runtimeList = new List<RuntimeInfo>();
                foreach (var runtimeId in ContainerInfo.RuntimeIds)
                {
                    var runtimeInfo = RuntimeManager.Instance.Get(runtimeId);
                    if (runtimeInfo == null)
                    {
                        addConsoleHistory($"[平台][警告]未找到编号为[{runtimeId}]的运行库。");
                        continue;
                    }
                    runtimeList.Add(runtimeInfo);
                }
                runtimes = runtimeList.ToArray();
            }
            if (imageInfo.Runtime != null && imageInfo.Runtime.Length > 0)
            {
                if (runtimes == null)
                {
                    addConsoleHistory($"[平台][错误]容器未配置镜像要求的运行库[{string.Join(",", imageInfo.Runtime)}]");
                    return;
                }
                foreach (var line in imageInfo.Runtime)
                {
                    var nameAndVersion = NameAndVersion.Parse(line);
                    var runtime = runtimes.FirstOrDefault(t => t.Name == nameAndVersion.Name);
                    if (runtime == null)
                    {
                        addConsoleHistory($"[平台][错误]容器未配置镜像要求的运行库[{line}]");
                        return;
                    }
                    var containerRuntimeVersion = Version.Parse(runtime.Version);
                    var imageRuntimeVersionString = nameAndVersion.Version;
                    if (!imageRuntimeVersionString.Contains("."))
                        imageRuntimeVersionString += ".0";
                    var imageRuntimeVersion = Version.Parse(imageRuntimeVersionString);
                    if (containerRuntimeVersion < imageRuntimeVersion)
                    {
                        addConsoleHistory($"[平台][错误]容器配置运行库[{nameAndVersion.Name}]的版本[{runtime.Version}]小于镜像中要求的运行库版本[{nameAndVersion.Version}]");
                        return;
                    }
                }
            }
            ProcessStartInfo psi = null;
            var processFileName = imageInfo.AgentExecute;
            if (string.IsNullOrEmpty(processFileName))
                processFileName = "dotnet";

            var tmpFileName = Path.Combine(imageFolder, processFileName);
            if (File.Exists(tmpFileName))
            {
                processFileName = tmpFileName;
                //如果是在非Windows上运行，则检查添加文件可执行权限
                if (!OperatingSystem.IsWindows())
                    Utils.Unix.UnixUtils.AddExecutePermissionToFile(processFileName);
            }
            psi = new ProcessStartInfo(processFileName);
            //如果是最老的镜像
            if (string.IsNullOrEmpty(containerInfo.Image.AgentStartup) && string.IsNullOrEmpty(containerInfo.Image.AgentExecute))
            {
                psi.WorkingDirectory = containerFolder;
#if DEBUG
                var executeFileAgentDir = Path.GetFullPath(FolderUtils.GetPathUnderProgramDir("../../../YiQiDong/bin/Debug"));
                psi.ArgumentList.Add(Path.Combine(executeFileAgentDir, $"{EXECUTE_FILE_AGENT}.dll"));
#else
                var executeFileName = FolderUtils.GetPathUnderProgramDir(EXECUTE_FILE_AGENT);
                if (OperatingSystem.IsWindows())
                    executeFileName += ".exe";
                psi.FileName = executeFileName;
#endif
                psi.ArgumentList.Add("-agent");
                psi.ArgumentList.Add(containerInfo.Id);
                containerInfo.ManualRaiseContainerInitedNotice = false;
            }
            //如果是老镜像
            else if (File.Exists(Path.Combine(imageFolder, "YiQiDong.Protocol.dll")))
            {
                psi.WorkingDirectory = containerFolder;
                if (!string.IsNullOrEmpty(imageInfo.AgentStartup))
                {
                    var agentStartup = imageInfo.AgentStartup;
                    var agentStartupFullPath = Path.Combine(imageFolder, agentStartup);
                    if (File.Exists(agentStartupFullPath))
                        agentStartup = agentStartupFullPath;
                    psi.ArgumentList.Add(agentStartup);
                }
                psi.ArgumentList.Add($"YiQiDong.ContainerId=\"{containerInfo.Id}\"");
                psi.ArgumentList.Add($"YiQiDong.DataFolder=\"{FolderUtils.GetDataDir()}\"");
                psi.ArgumentList.Add($"YiQiDong.TransportTimeout=\"{Program.Config.AgentTransportTimeout}\"");
                containerInfo.ManualRaiseContainerInitedNotice = true;
            }
            //否则是新镜像
            else
            {
                psi.WorkingDirectory = imageFolder;
                if (!string.IsNullOrEmpty(imageInfo.AgentStartup))
                    psi.ArgumentList.Add(imageInfo.AgentStartup);
                psi.ArgumentList.Add(containerInfo.Name);
                psi.ArgumentList.Add(containerInfo.Id);
                containerInfo.ManualRaiseContainerInitedNotice = false;
            }
            try
            {
                addConsoleHistory("[平台]容器进程文件名：" + psi.FileName);
                addConsoleHistory("[平台]容器进程参数：" + string.Join(" ", psi.ArgumentList));
                //添加镜像目录、容器目录环境变量
                psi.Environment["IMAGE_DIR"] = imageFolder;
                psi.Environment["CONTAINER_DIR"] = containerFolder;

                //添加运行库的其他环境变量
                foreach (var item in RuntimeManager.Instance.GetRuntimesEnvironment(runtimes))
                    psi.Environment[item.Key] = item.Value;
                //添加镜像的其他环境变量
                foreach (var item in ImageManager.Instance.GetImageEnvironment(imageInfo))
                    psi.Environment[item.Key] = item.Value;                
                //添加容器配置的环境变量
                foreach (var item in ContainerInfo.GetEnvironmentVariables())
                    psi.Environment[item.Key] = item.Value;

                //添加PATH路径
                var pathList = new List<string>();
                pathList.AddRange(ImageManager.Instance.GetImagePath(imageInfo));
                pathList.AddRange(RuntimeManager.Instance.GetRuntimesPath(runtimes));

                //启动进程
                var process = RuntimeUtils.StartProcess(psi, pathList);
                process.EnableRaisingEvents = true;
                process.ErrorDataReceived += Process_ErrorDataReceived;
                process.BeginErrorReadLine();
                addConsoleHistory($"[平台]容器进程已创建，进程ID:{process.Id}");

                var options = new Quick.Protocol.Streams.QpStreamServerOptions()
                {
                    BaseStream = new Quick.Protocol.Streams.InputOutputStream(process.StandardOutput.BaseStream, process.StandardInput.BaseStream),
                    Password = nameof(YiQiDong),
                    ServerProgram = "易启动容器接口管理器",
                    InstructionSet = new[] { YiQiDong.Protocol.V1.Instruction.Instance }
                };
                options.RegisterCommandExecuterManager(commandExecuterManager);
                options.RegisterNoticeHandlerManager(noticeHandlerManager);
                options.ProtocolErrorHandler = Process_OnProtocolError;
                Process = process;
                ProcessChannel = new Quick.Protocol.Streams.QpStreamServerChannel(options);
                process.WaitForExitAsync().ContinueWith(task =>
                {
                    if (process.HasExited)
                    {
                        addConsoleHistory($"[平台]容器进程已退出，退出码：{process.ExitCode}");
                        if (ContainerInfo.Enable)
                        {
                            Task.Delay(5000).ContinueWith(t =>
                            {
                                if (Process == null)
                                    BeginEnable();
                            });
                        }
                        Process = null;
                        ProcessChannel = null;
                    }
                    else
                    {
                        if (!ContainerInfo.Enable)
                        {
                            try { process.Kill(true); }
                            catch { }
                            Process = null;
                            ProcessChannel = null;
                            return;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                addConsoleHistory($"[平台]启动容器进程失败，文件：{psi.FileName}，参数：{psi.Arguments}，原因：{ExceptionUtils.GetExceptionMessage(ex)}...");
            }
        }
    }

    private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        addConsoleHistory($"[错误]" + e.Data);
    }

    private void Process_OnProtocolError(Stream stream, ReadOnlySequence<byte> bytes)
    {
        var isFirstLine = true;
        var encoding = Encoding.UTF8;
        using (var reader = new StreamReader(stream, encoding))
        {
            var line = reader.ReadLine();
            if (isFirstLine)
            {
                line = encoding.GetString(bytes) + line;
                isFirstLine = false;
            }
            addConsoleHistory($"[错误]" + line);
        }
        addConsoleHistory($"[平台]容器进程输出数据格式错误，正在退出进程。。。");
        try
        {
            Process?.Kill(true);
        }
        catch { }
    }

    public void BeginDisable()
    {
        addConsoleHistory("[平台]正在禁用容器...");
        ctsCron?.Cancel();
        if (ProcessChannel != null)
        {
            ProcessChannel.Disconnected -= Channel_Disconnected;
            try
            {
                //先尝试发送退出指令。最多等待1秒
                ProcessChannel.SendCommand(
                    new YiQiDong.Protocol.V1.QpCommands.Exit.Request())
                    .Wait(1000);
                //如果发送退出指令成功，则等待进程退出。最多等待10秒
                if (Process != null)
                {
                    Process.WaitForExit(10 * 1000);
                    if (Process.HasExited)
                        Process = null;
                }
            }
            catch (Exception ex)
            {
                addConsoleHistory("[平台]向容器发送退出指令出错。原因：" + ExceptionUtils.GetExceptionMessage(ex));
            }
            ProcessChannel?.Stop();
            ProcessChannel = null;
        }

        IsConnected = false;
        //如果发了退出指令后，进程还没有退出。则强制结束进程
        if (Process != null)
        {
            try
            {
                Process.Kill(true);
            }
            catch (Exception ex)
            {
                addConsoleHistory("[平台]禁用结束进程时出错，原因：" + ExceptionUtils.GetExceptionString(ex));
            }
            Process = null;
            ProcessChannel = null;
        }
        addConsoleHistory("[平台]容器禁用完成.");
    }

    private void Channel_Disconnected(object sender, EventArgs e)
    {
        addConsoleHistory($"[平台]到容器的连接已经断开.");
        IsConnected = false;

        lock (reverseProxyRuleList)
            reverseProxyRuleList.Clear();
        RaiseEvent_ReverseProxyRuleListChanged();
        ReverseProxyManager.Instance.RemoveRules($"/{ContainerInfo.Id}/");
        if (ProcessChannel != null)
        {
            ProcessChannel.Disconnected -= Channel_Disconnected;
            ProcessChannel.Stop();
            ProcessChannel = null;
        }
        if (Process != null)
        {
            try { Process.Kill(true); }
            catch { }
            Process = null;
            ProcessChannel = null;
        }
        if (ContainerInfo.Enable)
            Task.Delay(1000).ContinueWith(t => BeginEnable());
    }

    private CancellationTokenSource ctsCron;
    private DateTime startCronNextExecuteTime;
    private DateTime stopCronNextExecuteTime;
    private DateTime restartCronNextExecuteTime;
    private NCrontab.CrontabSchedule startCrontabSchedule;
    private NCrontab.CrontabSchedule stopCrontabSchedule;
    private NCrontab.CrontabSchedule restartCrontabSchedule;

    public void Enable()
    {
        ContainerInfo.Enable = true;
        ContainerManager.Instance.SaveContainerFile(ContainerInfo);
        ContainerManager.Instance.RaiseEvent_ContainerChanged();
        BeginEnable();
    }

    public void Disable()
    {
        ContainerInfo.Enable = false;
        BeginDisable();
        ContainerManager.Instance.SaveContainerFile(ContainerInfo);
        ContainerManager.Instance.RaiseEvent_ContainerChanged();
    }

    private void beginCheckCron(CancellationToken cancellationToken)
    {
        Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(t =>
        {
            if (t.IsCanceled)
                return;
            checkCron();
            beginCheckCron(cancellationToken);
        });
    }

    private void checkCron()
    {
        var currentTime = DateTime.Now;
        if (startCrontabSchedule != null && currentTime >= startCronNextExecuteTime)
        {
            addConsoleHistory("[平台]到达设置的自动启动时间，开始启动容器...");
            try { Start(); }
            catch { }
            startCronNextExecuteTime = startCrontabSchedule.GetNextOccurrence(currentTime);
        }
        if (stopCrontabSchedule != null && currentTime >= stopCronNextExecuteTime)
        {
            addConsoleHistory("[平台]到达设置的自动停止时间，开始停止容器...");
            try { Stop().Wait(); }
            catch { }
            stopCronNextExecuteTime = stopCrontabSchedule.GetNextOccurrence(currentTime);
        }
        if (restartCrontabSchedule != null && currentTime >= restartCronNextExecuteTime)
        {
            addConsoleHistory("[平台]到达设置的自动重启时间，开始重启容器...");
            try { Restart().Wait(); }
            catch { }
            restartCronNextExecuteTime = restartCrontabSchedule.GetNextOccurrence(currentTime);
        }
    }


    public Task Start()
    {
        if (ProcessChannel == null)
            return null;
        ContainerInfo.AutoStart = true;
        lock (reverseProxyRuleList)
            reverseProxyRuleList.Clear();
        RaiseEvent_ReverseProxyRuleListChanged();
        ReverseProxyManager.Instance.RemoveRules($"/{ContainerInfo.Id}/");
        ContainerManager.Instance.SaveContainerFile(ContainerInfo);
        ContainerManager.Instance.RaiseEvent_ContainerChanged();

        //如果有启动脚本，则先执行
        if (!string.IsNullOrEmpty(ContainerInfo.StartScript))
        {
            pushLog(LogLevel.Info, "开始执行启动脚本...");
            executeScripts(ContainerInfo.StartScript);
        }

        return ProcessChannel.SendCommand(
            new YiQiDong.Protocol.V1.QpCommands.Start.Request()).ContinueWith(t =>
        {
            if (t.IsFaulted)
                addConsoleHistory("[平台]发送启动指令出错，原因：" + ExceptionUtils.GetExceptionString(t.Exception.InnerException));
            else
                addConsoleHistory("[平台]发送启动指令成功.");
        });
    }
    private void executeScripts(string scripts)
    {
        var lines = scripts.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            try
            {
                var ret = ProcessUtils.ExecuteShell(line);
                if (!string.IsNullOrEmpty(ret.Output))
                    pushLog(LogLevel.Info, ret.Output);
                if (!string.IsNullOrEmpty(ret.Error))
                    pushLog(LogLevel.Error, ret.Error);
            }
            catch (Exception ex)
            {
                pushLog(LogLevel.Error, $"执行脚本[{line}]时出错，原因：{ExceptionUtils.GetExceptionMessage(ex)}");
            }
        }
    }

    public async Task Stop(bool saveContainerInfoFile = true)
    {
        if (ProcessChannel == null)
            return;

        ContainerInfo.AutoStart = false;
        lock (reverseProxyRuleList)
            reverseProxyRuleList.Clear();
        RaiseEvent_ReverseProxyRuleListChanged();
        ReverseProxyManager.Instance.RemoveRules($"/{ContainerInfo.Id}/");
        if (saveContainerInfoFile)
            ContainerManager.Instance.SaveContainerFile(ContainerInfo);
        ContainerManager.Instance.RaiseEvent_ContainerChanged();
        var isAutoInitBeforeSendCommand = ContainerInfo.Enable;
        try
        {
            await ProcessChannel.SendCommand
                (new YiQiDong.Protocol.V1.QpCommands.Stop.Request()).ConfigureAwait(false);
            addConsoleHistory("[平台]发送停止指令成功.");
        }
        catch (Exception ex)
        {
            addConsoleHistory("[平台]发送停止指令出错，原因：" + ExceptionUtils.GetExceptionString(ex));
        }
        //如果有停止脚本，则执行
        if (!string.IsNullOrEmpty(ContainerInfo.StopScript))
        {
            pushLog(LogLevel.Info, "开始执行停止脚本...");
            executeScripts(ContainerInfo.StopScript);
        }
    }

    public async Task Restart()
    {
        await Stop();
        await Task.Delay(1000);
        await Start();
    }

    public void ClearContainerHistory()
    {
        lock (ConsoleOutputQueue)
        {
            ConsoleOutputQueue.Clear();
            consoleOutputCharCount = 0;
        }
        ConsoleHistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public FunctionInfo[] GetFunctionList()
    {
        var ret = ProcessChannel?.SendCommand(
            new YiQiDong.Protocol.V1.QpCommands.GetFunctionList.Request()).Result;
        return ret?.Items;
    }

    public string OpenFunctionSession(FunctionInfo function)
    {
        var ret = ProcessChannel?.SendCommand(
            new YiQiDong.Protocol.V1.QpCommands.OpenFunctionSession.Request() { FunctionId = function.Id }, function.ExecuteTimeout).Result;
        return ret?.SessionId;
    }

    public void CloseFunctionSession(FunctionInfo function, string sessionId)
    {
        _ = ProcessChannel?.SendCommand(
            new YiQiDong.Protocol.V1.QpCommands.CloseFunctionSession.Request() { SessionId = sessionId }, function.ExecuteTimeout).Result;
    }

    public FieldForGet[] ExecuteFunction(FunctionInfo function, string[] fieldIds = null, FieldForPost[] fields = null, string sessionId = null)
    {
        var request = new FunctionRequest()
        {
            FunctionId = function.Id,
            FieldIds = fieldIds,
            Fields = fields,
            SessionId = sessionId
        };
        var ret = ProcessChannel?.SendCommand(
            new YiQiDong.Protocol.V1.QpCommands.ExecuteFunction.Request() { Data = request }, function.ExecuteTimeout).Result;
        return ret?.Items;
    }

    private void checkLogFiles(object _)
    {
        var dir = logFolder;
        var logSaveDays = ContainerInfo.LogSaveDays;
        //如果日志目录不存在，则跳过此次检查
        if (!Directory.Exists(dir))
            return;
        var files = Directory.GetFiles(dir, "*.log");
        //如果日志文件的数量小于等于设置的保存天数，则跳过此次检查
        if (files.Length <= logSaveDays)
            return;

        var toDelFiles = files.OrderBy(t => t).Take(files.Length - logSaveDays).ToArray();
        foreach (var file in toDelFiles)
        {
            try
            {
                File.Delete(file);
                pushLog(LogLevel.Info, $"[平台]删除日志文件[{file}]成功");
            }
            catch (Exception ex)
            {
                pushLog(LogLevel.Error, $"[平台]删除日志文件[{file}]失败，原因：{ExceptionUtils.GetExceptionMessage(ex)}");
            }
        }
    }

    public void Dispose()
    {
        clearLogFilesTimer?.Dispose();
        clearLogFilesTimer = null;
        logger = null;
        logFactory?.Dispose();
        logFactory = null;
    }
}
