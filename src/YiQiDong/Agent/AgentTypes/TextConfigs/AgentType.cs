using Quick.Shell.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using YiQiDong.Agent.AgentTypes.AppSettings;
using YiQiDong.Agent.AgentTypes.TextConfigs.Functions;
using YiQiDong.Core;
using YiQiDong.Core.Protocol.V1.Model;
using YiQiDong.Core.Utils;
using YiQiDong.Core.Utils.Unix;

namespace YiQiDong.Agent.AgentTypes.TextConfigs
{
    internal class AgentType : IAgentType
    {
        internal ContainerMetaInfo metaInfo;
        internal ContainerConfigModel configModel;
        internal Dictionary<string, string> processEnviromentsDictionary;
        private Dictionary<string, string> withDollarEnviromentsDictionary;

        private Process Process { get; set; }
        private StreamWriter Writer;
        private string[] logIgnoreList;

        internal EnvironmentVariableInfo GetEnvironmentVariableInfo(string key)
        {
            return metaInfo.Environments?.FirstOrDefault(t => t.Key == key);
        }

        private void refreshWithDollarEnviromentsDictionary()
        {
            withDollarEnviromentsDictionary = new Dictionary<string, string>();
            foreach (var t in processEnviromentsDictionary)
                withDollarEnviromentsDictionary["$" + t.Key] = t.Value;
            foreach (var t in configModel.Environment)
                withDollarEnviromentsDictionary["$" + t.Key] = t.Value;
        }

        internal string processPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            bool isChanged;
            while (true)
            {
                isChanged = false;
                foreach (var item in withDollarEnviromentsDictionary)
                {
                    if (path.Contains(item.Key))
                    {
                        path = path.Replace(item.Key, item.Value);
                        isChanged = true;
                    }
                }
                if (!isChanged)
                    break;
            }
            return path;
        }

        private string[] getFolderFiles(string folder, string[] fileFilters, bool includeSubFolder)
        {
            if (fileFilters == null || fileFilters.Length == 0)
                return Directory.GetFiles(folder, null, includeSubFolder ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories);
            var fileList = new List<string>();
            foreach (var fileFilter in fileFilters)
            {
                var files = Directory.GetFiles(folder, fileFilter, includeSubFolder ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories);
                fileList.AddRange(files);
            }
            return fileList.ToArray();
        }

        public void Init(Action<AbstractFunction,bool?> addFunction)
        {
            if (!string.IsNullOrEmpty(AgentContext.Container.LogIgnoreList))
                logIgnoreList = AgentContext.Container.LogIgnoreList.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var metaConfig = ConfigModel.Parse(AgentContext.Container.Image.AgentConfig);
            metaInfo = metaConfig.GetContainerMetaInfo();
            if (metaInfo == null)
                throw new ArgumentException("配置文件中未找到容器元信息！");
            configModel = ConfigFileProcessor.Default.Load(ContainerConfigModelSerializerContext.Default.ContainerConfigModel);
            if (configModel == null)
                configModel = new ContainerConfigModel();
            if (configModel.Environment == null)
                configModel.Environment = new Dictionary<string, string>();
            var metaEnvironment = metaInfo?.Environments;
            if (metaEnvironment != null)
            {
                foreach (var imageEnvironmentItem in metaEnvironment)
                {
                    if (!configModel.Environment.ContainsKey(imageEnvironmentItem.Key))
                    {
                        configModel.Environment[imageEnvironmentItem.Key] = imageEnvironmentItem.Value;
                    }
                }
            }
            processEnviromentsDictionary = new Dictionary<string, string>();
            foreach (string key in Environment.GetEnvironmentVariables().Keys)
            {
                processEnviromentsDictionary[key] = Environment.GetEnvironmentVariable(key);
            }
            refreshWithDollarEnviromentsDictionary();
            var afterEnvironmentChanged = () =>
            {
                //复制文件到容器
                if (metaInfo.ContainerFiles != null)
                {
                    foreach (var item in metaInfo.ContainerFiles)
                    {
                        var sourceFile = processPath(item.Key);
                        var desFile = processPath(item.Value);
                        if (!File.Exists(sourceFile))
                            continue;
                        if (File.Exists(desFile))
                            continue;
                        var desFolder = Path.GetDirectoryName(desFile);
                        if (!Directory.Exists(desFolder))
                            Directory.CreateDirectory(desFolder);
                        File.Copy(sourceFile, desFile);
                    }
                }
                //复制目录到容器
                if (metaInfo.ContainerFolders != null)
                {
                    foreach (var item in metaInfo.ContainerFolders)
                    {
                        var sourceDir = processPath(item.Key);
                        var desDir = processPath(item.Value);
                        FileSystemUtils.CopyFolder(sourceDir, desDir);
                    }
                }
                if (metaInfo.ContainerFolderInfos != null)
                {
                    foreach (var item in metaInfo.ContainerFolderInfos)
                    {
                        var sourceDir = processPath(item.Key);
                        var containerFolderInfo = item.Value;
                        var desDir = processPath(containerFolderInfo.Path);
                        FileSystemUtils.CopyFolder(sourceDir, desDir, containerFolderInfo.FileFilters, containerFolderInfo.IncludeSubFolder);
                    }
                }
                refreshWithDollarEnviromentsDictionary();
            };
            addFunction(new EnvironmentConfigFunction(this, afterEnvironmentChanged), null);
            if (metaInfo.HelpDict != null)
                addFunction(new Core.Functions.HelpFunction(metaInfo.HelpDict), null);
            addFunction(new SendCommandFunction(this), true);
            afterEnvironmentChanged();
        }

        public ConfigFileInfo[] GetConfigFiles()
        {
            var list = new List<ConfigFileInfo>();
            if (metaInfo.ConfigFolders != null)
                foreach (var item in metaInfo.ConfigFolders)
                {
                    var folder = processPath(item);
                    var folderLastName = Path.GetFileName(folder);
                    foreach (var file in Directory.GetFiles(folder))
                    {
                        var name = $"{folderLastName}/{file.Substring(folder.Length + 1)}";
                        list.Add(new ConfigFileInfo()
                        {
                            Name = name,
                            FilePath = file,
                            FileEncoding = metaInfo.ConfigFileEncoding
                        });
                    }
                }
            if (metaInfo.ConfigFolderInfos != null)
                foreach (var item in metaInfo.ConfigFolderInfos)
                {
                    var folder = processPath(item.Path);
                    var files = getFolderFiles(folder, item.FileFilters, item.IncludeSubFolder);
                    var folderLastName = Path.GetFileName(folder);
                    foreach (var file in files)
                    {
                        var name = $"{folderLastName}/{file.Substring(folder.Length + 1)}";
                        list.Add(new ConfigFileInfo()
                        {
                            Name = name,
                            FilePath = file,
                            FileEncoding = metaInfo.ConfigFileEncoding
                        });
                    }
                }
            if (metaInfo.ConfigFiles != null)
                foreach (var item in metaInfo.ConfigFiles)
                {
                    var file = processPath(item.Key);
                    list.Add(new ConfigFileInfo()
                    {
                        Name = item.Value,
                        FilePath = file,
                        FileEncoding = metaInfo.ConfigFileEncoding
                    });
                }
            return list.ToArray();
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            var line = e.Data;
            if (line == null)
                return;
            if (logIgnoreList != null)
                if (logIgnoreList.Any(t => line.Contains(t)))
                    return;
            AgentContext.LogInfo(line);
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            var line = e.Data;
            if (line == null)
                return;
            if (logIgnoreList != null)
                if (logIgnoreList.Any(t => line.Contains(t)))
                    return;
            AgentContext.LogError(e.Data);
        }

        private void delayStart(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;
            Task.Delay(5000, cancellationToken).ContinueWith(t =>
            {
                if (t.IsCanceled)
                    return;
                try
                {
                    innerStart(cancellationToken);
                }
                catch
                {
                    delayStart(cancellationToken);
                }
            });
        }

        private void UseProcessStartInfo(ProcessStartInfo psi, ContainerMetaInfo appSettingPSI, Action<ProcessStartInfo> psiHandler)
        {
            ProcessUtils.ProcessProcessStartInfo(psi);
            //如果指定了编码，则设置输入输出编码
            if (!string.IsNullOrEmpty(appSettingPSI.Encoding))
            {
                var encoding = Encoding.GetEncoding(appSettingPSI.Encoding);
                psi.StandardOutputEncoding = encoding;
                psi.StandardErrorEncoding = encoding;
                psi.StandardInputEncoding = encoding;
            }
            //设置工作目录
            if (appSettingPSI.WorkingDir != null)
                psi.WorkingDirectory = processPath(appSettingPSI.WorkingDir);
            //添加环境变量
            if (configModel.Environment != null)
                foreach (var item in configModel.Environment)
                    psi.Environment[item.Key] = processPath(item.Value);

            var prePath = (string)null;
            //添加PATH
            if (appSettingPSI.Path != null && appSettingPSI.Path.Length > 0)
            {
                prePath = Environment.GetEnvironmentVariable("PATH");
                List<string> pathList = new List<string>();
                foreach (var path in appSettingPSI.Path)
                {
                    pathList.Add(processPath(path));
                }
                //将原PATH添加到最后
                if (!string.IsNullOrEmpty(prePath))
                    pathList.Add(prePath);
                var paths = string.Join(OperatingSystem.IsWindows() ? ";" : ":", pathList);
                Environment.SetEnvironmentVariable("PATH", paths);
                psi.EnvironmentVariables["PATH"] = paths;
            }
            psiHandler(psi);
            //还原PATH变量
            if (prePath != null)
                Environment.SetEnvironmentVariable("PATH", prePath);
        }

        private void checkAndAddExecutePermission(string filename)
        {
            //如果是在非Windows上运行，则检查添加文件可执行权限
            if (!OperatingSystem.IsWindows())
            {
                var tmpFile = filename;
                if (File.Exists(tmpFile))
                {
                    UnixUtils.AddExecutePermissionToFile(tmpFile);
                }
                else
                {
                    tmpFile = Path.Combine(AgentContext.Container.ImageFolder, filename);
                    if (File.Exists(tmpFile))
                        UnixUtils.AddExecutePermissionToFile(tmpFile);
                }
            }
        }

        private void innerStart(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;
            if (Process != null)
                return;
            if (!AgentContext.Container.AutoStart)
                return;
            var filename = processPath(metaInfo.GetStartFileName());
            checkAndAddExecutePermission(filename);
            var arguments = processPath(metaInfo.StartArguments);
            ProcessStartInfo psi = new ProcessStartInfo(filename, arguments);
            AgentContext.LogInfo($"正在启动工作进程[文件名:{filename},参数:{arguments}]...");
            UseProcessStartInfo(psi, metaInfo, psi => Process = Process.Start(psi));
            int processId;
            string processName = null;
            try
            {
                processId = Process.Id;
                processName = Process.ProcessName;
                AgentContext.LogInfo($"进程[Id:{processId},Name:{processName}]已经启动。");
                Writer = Process.StandardInput;
                Writer.NewLine = Environment.NewLine;
                Process.EnableRaisingEvents = true;
                Process.OutputDataReceived += Process_OutputDataReceived;
                Process.ErrorDataReceived += Process_ErrorDataReceived;
                Process.BeginOutputReadLine();
                Process.BeginErrorReadLine();
                Task.Run(async () =>
                {
                    await Process.WaitForExitAsync();
                    AgentContext.LogInfo($"进程[Id:{processId},Name:{processName}]已经退出，退出码：{Process.ExitCode}。");
                    Process = null;
                    if (cancellationToken.IsCancellationRequested)
                        return;
                    delayStart(cancellationToken);
                });
            }
            catch (Exception ex)
            {
                AgentContext.LogInfo(Process.StandardOutput.ReadToEnd());
                AgentContext.LogError(Process.StandardError.ReadToEnd());
                throw new IOException(ex.Message);
            }
        }
        private CancellationTokenSource cts;
        public void Start()
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            try
            {
                innerStart(cancellationToken);
            }
            catch (Exception ex)
            {
                Process = null;
                AgentContext.LogError($"启动进程时出错，原因：{ExceptionUtils.GetExceptionString(ex)}");
                delayStart(cancellationToken);
            }
        }

        public void Stop()
        {
            cts?.Cancel();
            cts = null;

            var process = Process;
            if (process == null)
                return;

            //如果配置了退出命令
            if (!string.IsNullOrEmpty(metaInfo.ExitCommand))
            {
                SendCommand(metaInfo.ExitCommand);
            }
            else
            {
                var filename = processPath(metaInfo.GetStopFileName());
                if (string.IsNullOrEmpty(filename))
                {
                    process.Kill(true);
                }
                else
                {
                    checkAndAddExecutePermission(filename);
                    var arguments = processPath(metaInfo.StopArguments);
                    AgentContext.LogInfo($"正在启动结束进程[文件名:{filename},参数:{arguments}]...");
                    ProcessStartInfo psi = new ProcessStartInfo(filename, arguments);
                    UseProcessStartInfo(psi, metaInfo, psi =>
                    {
                        var ret = ProcessUtils.ExecuteProcessStartInfo(psi);
                        if (!string.IsNullOrEmpty(ret.Output))
                            AgentContext.LogInfo(ret.Output);
                        if (!string.IsNullOrEmpty(ret.Error))
                            AgentContext.LogInfo(ret.Error);
                        AgentContext.LogInfo($"结束进程执行完成，退出码：{ret.ExitCode}");
                    });
                }
            }
            if (!process.HasExited && !process.WaitForExit(metaInfo.ExitTimeout))
            {
                AgentContext.LogInfo($"等待{metaInfo.ExitTimeout}毫秒后，进程还未结束。强制结束进程树。");
                process.Kill(true);
            }
        }

        public void SendCommand(string cmd)
        {
            var process = Process;
            if (process == null)
                throw new IOException("当前未启动工作进程");
            Writer.WriteLine(cmd);
            Writer.Flush();
            AgentContext.LogInfo($"已向进程[Id:{Process.Id},Name:{process.ProcessName}]发送命令[{cmd}].");
        }
    }
}
