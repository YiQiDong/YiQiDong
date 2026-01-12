using Quick.Shell.Utils;
using System.Diagnostics;
using YiQiDong.Core.Protocol.V1.Model;
using YiQiDong.Core.Utils;

namespace YiQiDong.Agent.AgentTypes.AppSettings
{
    public class AgentType : IAgentType
    {
        private Process Process { get; set; }
        private StreamWriter Writer;
        public ConfigFileInfo[] GetConfigFiles() => null;
        private string[] logIgnoreList;

        public void Init(Action<Core.AbstractFunction, bool?> addFunction)
        {
            if (!string.IsNullOrEmpty(AgentContext.Container.LogIgnoreList))
                logIgnoreList = AgentContext.Container.LogIgnoreList.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

            var imageFolder = AgentContext.Container.ImageFolder;
            var containerFolder = AgentContext.Container.ContainerFolder;

            addFunction(new ConfigFunction(imageFolder, containerFolder), null);
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
            AgentContext.LogError(line);
        }

        private void delayStart()
        {
            Task.Delay(5000).ContinueWith(t =>
            {
                try { innerStart(); }
                catch
                {
                    delayStart();
                }
            });
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            AgentContext.LogInfo(
                $"进程[Id:{Process.Id},Name:{Process.ProcessName}]已经退出，退出码：{Process.ExitCode}。");
            Process = null;
            delayStart();
        }

        private void innerStart()
        {
            if (Process != null)
                return;
            if (!AgentContext.Container.AutoStart)
                return;

            var imageFolder = AgentContext.Container.ImageFolder;
            var containerFolder = AgentContext.Container.ContainerFolder;

            var appSettingPSI = ConfigFunction.Instance.AppSettings.ProcessStartInfo;
            if (appSettingPSI == null)
                throw new ArgumentException("配置文件中未找到进程启动相关信息！");

            var arguments = appSettingPSI.Arguments;
            arguments = arguments.Replace("{IMAGE_FOLDER}", imageFolder);
            ProcessStartInfo psi = new ProcessStartInfo(appSettingPSI.FileName, arguments);

            AgentContext.LogInfo("进程文件名：" + psi.FileName);
            AgentContext.LogInfo("进程参数：" + psi.Arguments);
            ProcessUtils.ProcessProcessStartInfo(psi);
            psi.WorkingDirectory = containerFolder;

            Process = Process.Start(psi);
            Writer = Process.StandardInput;

            Process.EnableRaisingEvents = true;
            Process.OutputDataReceived += Process_OutputDataReceived;
            Process.ErrorDataReceived += Process_ErrorDataReceived;
            Process.BeginOutputReadLine();
            Process.BeginErrorReadLine();
            AgentContext.LogInfo($"进程[Id:{Process.Id},Name:{Process.ProcessName}]已经启动。");
            Process.Exited += Process_Exited;
        }

        public void Start()
        {
            try
            {
                innerStart();
            }
            catch (Exception ex)
            {
                AgentContext.LogError($"启动进程时出错，原因：{ExceptionUtils.GetExceptionString(ex)}");
                delayStart();
            }
        }

        public void Stop()
        {
            var process = Process;
            if (process == null)
                return;
            var appSettingPSI = ConfigFunction.Instance.AppSettings.ProcessStartInfo;
            if (appSettingPSI == null || string.IsNullOrEmpty(appSettingPSI.ExitCommand))
                process.Kill(true);
            else
            {
                AgentContext.LogInfo(
                    $"向进程[Id:{Process.Id},Name:{process.ProcessName}]发送结束命令:{appSettingPSI.ExitCommand}。");

                Writer.NewLine = Environment.NewLine;
                Writer.WriteLine(appSettingPSI.ExitCommand);
                Writer.Flush();

                if (!process.WaitForExit(10 * 1000))
                {
                    AgentContext.LogInfo(
                        $"向进程[Id:{Process.Id},Name:{process.ProcessName}]发送结束命令10秒后，进程还未结束。强制结束进程树。");
                    process.Kill(true);
                }
            }
        }
    }
}
