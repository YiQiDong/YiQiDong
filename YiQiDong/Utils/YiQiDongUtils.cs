using System;
using System.Diagnostics;
using System.Text;
using Quick.Build;
using Quick.Shell.PowerShell;
using Quick.Shell.Utils;
using YiQiDong.Core.Utils.Unix;

namespace YiQiDong.Utils;

public class YiQiDongUtils
{
    public static async Task RestartService()
    {
        await Task.Delay(100);
        Program.StopContainers();
        
        if (OperatingSystem.IsWindows())
        {
            var psFileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.ps1");
            var psFileEncoding = Encoding.GetEncoding(Thread.CurrentThread.CurrentCulture.TextInfo.ANSICodePage);
            var psFileContent = @$"sc.exe stop {Consts.SERVICE_NAME_WIN32}
Wait-Process -Id {Process.GetCurrentProcess().Id}
sc.exe start {Consts.SERVICE_NAME_WIN32}
Remove-Item ""{psFileName}""
";
            File.WriteAllText(psFileName, psFileContent, psFileEncoding);
            PowerShellProcessContext.ExecutePs1File(psFileName, true);
        }
        else
        {
            var shFile = $"{nameof(YiQiDong)}.sh";
            UnixUtils.AddExecutePermissionToFile(shFile);
            //如果是在chroot环境中运行
            if (UnixUtils.IsRuningInChroot())
            {
                ConsoleUtils.ConsoleWriteLine($"检测到chroot环境");
                Process.Start(new ProcessStartInfo(shFile, "run chroot")
                {
                    WorkingDirectory = Environment.CurrentDirectory
                });
                Environment.Exit(0);
            }
            //如果是在docker环境中运行
            else if (UnixUtils.IsRuningInDocker())
            {
                ConsoleUtils.ConsoleWriteLine("检测到docker环境");
                Environment.Exit(0);
            }
            //否则重启服务
            else
            {
                if (OperatingSystem.IsMacOS())
                {
                    var serviceDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "LaunchAgents");
                    var serviceFile = $"YiQiDong.Updater.plist";
                    QbFile.WriteLine(serviceFile, 11, $"      <string>{Environment.CurrentDirectory}/YiQiDong.Updater.sh</string>");
                    if (!Directory.Exists(serviceDir))
                        Directory.CreateDirectory(serviceDir);
                    //正在将服务文件安装到系统服务目录
                    ProcessUtils.ExecuteShell($"cp {serviceFile} {serviceDir}/{serviceFile}");
                    //正在启用服务
                    ProcessUtils.ExecuteShell($"launchctl load -w {serviceDir}/{serviceFile}");
                }
                else
                {
                    if (UnixUtils.IsRuningWithRoot())
                        ProcessUtils.ExecuteShell("systemctl restart YiQiDong");
                    else
                        ProcessUtils.ExecuteShell("systemctl --user restart YiQiDong");
                }
            }
        }
    }
}
