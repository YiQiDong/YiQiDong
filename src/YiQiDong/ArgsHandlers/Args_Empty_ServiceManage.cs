using Quick.Build;
using System.ServiceProcess;
using YiQiDong.Core.Utils;
using System.Runtime.Versioning;
using System.Diagnostics;
using YiQiDong.Utils;
using Quick.Shell.Utils;
using YiQiDong.Core.Utils.Unix;
using Quick.Localize;

namespace YiQiDong.ArgsHandlers
{
    public partial class Args_Empty
    {
        //Systemd的sys目录
        private static string[] UnixSystemdSystemFolders = new[]
        {
            "/lib/systemd/system",
            "/usr/lib/systemd/system",
            "/etc/systemd/system"
        };
        private static string systemdSystemFolder = null;
        private static string GetSystemdSystemFolder()
        {
            if (string.IsNullOrEmpty(systemdSystemFolder))
            {
                if (UnixUtils.IsRuningWithRoot())
                {
                    foreach (var folder in UnixSystemdSystemFolders)
                    {
                        var dirInfo = new DirectoryInfo(folder);
                        if (dirInfo.Exists)
                        {
                            //如果目录是只读，则跳过
                            if ((dirInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                                continue;
                            systemdSystemFolder = folder;
                            break;
                        }
                    }
                }
                else
                {
                    systemdSystemFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config/systemd/user");
                    if (!Directory.Exists(systemdSystemFolder))
                        Directory.CreateDirectory(systemdSystemFolder);
                }
            }
            if (string.IsNullOrEmpty(systemdSystemFolder))
                throw new IOException(Locale.GetString("System configuration directory for systemd not found. Searched directories: {0}",string.Join(",", UnixSystemdSystemFolders)));
            return systemdSystemFolder;
        }

        private static string GetSystemdAddonParameter()
        {
            if (UnixUtils.IsRuningWithRoot())
                return null;
            return "--user";
        }

        [SupportedOSPlatform("windows")]
        public static ServiceController GetWin32Service()
        {
            return ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == Consts.SERVICE_NAME_WIN32);
        }

        [SupportedOSPlatform("windows")]
        public static void UseWin32Service(Action<ServiceController> action)
        {
            var serviceController = GetWin32Service();
            if (serviceController == null)
                throw new FileNotFoundException(Locale.GetString("Service does not exist"));
            action.Invoke(serviceController);
        }

        //获取服务状态
        private static ServiceStatus GetServiceStatus()
        {
            var status = new ServiceStatus();
            if (OperatingSystem.IsWindows())
            {
                var serviceController = GetWin32Service();
                if (serviceController == null)
                {
                    status.Installed = false;
                }
                else
                {
                    status.Installed = true;
                    status.Enabled = serviceController.StartType != ServiceStartMode.Disabled;
                    switch (serviceController.Status)
                    {
                        case ServiceControllerStatus.StartPending:
                        case ServiceControllerStatus.Running:
                            status.Started = true;
                            break;
                        default:
                            status.Started = false;
                            break;
                    }
                }
            }
            else if(OperatingSystem.IsMacOS())
            {
                var ret = ProcessUtils.ExecuteShell($"launchctl list {Consts.SERVICE_NAME_UNIX}");
                status.Installed = ret.ExitCode==0;
                if (status.Installed)
                {
                    status.Enabled=true;
                    status.Started = ret.Output.Contains("\"PID\"");
                }
            }
            else
            {
                status.Installed = File.Exists($"{GetSystemdSystemFolder()}/{Consts.SERVICE_NAME_UNIX}.service");
                if (status.Installed)
                {
                    var ret = ProcessUtils.ExecuteShell($"systemctl {GetSystemdAddonParameter()} is-enabled {Consts.SERVICE_NAME_UNIX}.service");
                    status.Enabled = ret.Output == "enabled";
                    if (status.Enabled)
                    {
                        ret = ProcessUtils.ExecuteShell($"systemctl {GetSystemdAddonParameter()} is-active {Consts.SERVICE_NAME_UNIX}.service");
                        status.Started = ret.Output == "active";
                    }
                }
            }
            return status;
        }

        private static void Invoke_Install()
        {
            var status = GetServiceStatus();
            if (status.Installed)
            {
                //检测到已经安装，无法重复安装。
                Console.WriteLine(Locale.GetString("Installation detected, cannot install again."));
                return;
            }

            var executeFileName = Process.GetCurrentProcess().MainModule.FileName;
            if (OperatingSystem.IsWindows())
            {
                var psi = ProcessUtils.CreateProcessStartInfo("sc.exe", "create", Consts.SERVICE_NAME_WIN32, "binPath=", $"{executeFileName} -service", "start=", "delayed-auto", "DisplayName=", "易启动");
                ConsoleUtils.ExecuteProcessStartInfo(Locale.GetString("Installing service"), psi, true);
            }
            else if (OperatingSystem.IsMacOS())
            {
                //正在修改服务文件中的安装目录...
                Console.WriteLine(Locale.GetString("Modifying the installation directory in the service file..."));
                var serviceDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "LaunchAgents");
                var serviceFile = $"{Consts.SERVICE_NAME_UNIX}.plist";
                QbFile.WriteLine(serviceFile, 10, $"      <string>{Environment.CurrentDirectory}/YiQiDong</string>");
                if (!Directory.Exists(serviceDir))
                    Directory.CreateDirectory(serviceDir);
                //正在将服务文件安装到系统服务目录
                ConsoleUtils.ExecuteShell(Locale.GetString("Installing service files to the system service directory"), $"cp {serviceFile} {serviceDir}/{serviceFile}");
                //正在启用服务
                ConsoleUtils.ExecuteShell(Locale.GetString("Enabling service"), $"launchctl load -w {serviceDir}/{serviceFile}");
            }
            else
            {
                var username = Environment.UserName;
                //如果不是root用户登录，检查当前用户有没有设置
                if (!UnixUtils.IsRuningWithRoot())
                {
                    ConsoleUtils.ExecuteShell($"正在允许用户[{username}]逗留", $"loginctl enable-linger {username}");
                }
                Console.WriteLine("正在修改服务文件中的安装目录...");
                var serviceFile = Consts.SERVICE_NAME_UNIX +".service";
                QbFile.WriteLine(serviceFile, 5, $"ExecStart=/bin/sh {Environment.CurrentDirectory}/YiQiDong.sh start");
                QbFile.WriteLine(serviceFile, 6, $"ExecStop=/bin/sh {Environment.CurrentDirectory}/YiQiDong.sh stop");
                ConsoleUtils.ExecuteShell("正在将服务文件安装到系统服务目录", $"cp {serviceFile} {GetSystemdSystemFolder()}");
                ConsoleUtils.ExecuteShell("正在重新加载服务列表", $"systemctl {GetSystemdAddonParameter()} daemon-reload");
                ConsoleUtils.ExecuteShell("正在检查服务是否已经加载", $"systemctl {GetSystemdAddonParameter()} list-unit-files {serviceFile}");
                ConsoleUtils.ExecuteShell("正在启用服务", $"systemctl {GetSystemdAddonParameter()} enable {Consts.SERVICE_NAME_UNIX}");
            }
            Console.WriteLine("-----------------------------");
            Console.WriteLine("安装完成");
            Console.WriteLine("-----------------------------");

            if (!OperatingSystem.IsMacOS())
            {
                Console.WriteLine("是否启动服务？");
                var isStartService = QbSelect.ArrowSelect(new Dictionary<string, string>()
                {
                    ["True"] = "是",
                    ["False"] = "否"
                }.ToArray(), selectedForegroundColor: ConsoleColor.Green);
                if (isStartService == "True")
                {
                    Invoke_Start();
                }
            }
        }

        private static void Invoke_Start()
        {
            if (OperatingSystem.IsWindows())
            {
                var psi = ProcessUtils.CreateProcessStartInfo("sc.exe", "start", Consts.SERVICE_NAME_WIN32);
                ConsoleUtils.ExecuteProcessStartInfo("正在启动服务", psi, true);
            }
            else if(OperatingSystem.IsMacOS())
            {
                ConsoleUtils.ExecuteShell("正在启动服务", $"launchctl start {Consts.SERVICE_NAME_UNIX}");
            }
            else
            {
                ConsoleUtils.ExecuteShell("正在启动服务", $"systemctl {GetSystemdAddonParameter()} start {Consts.SERVICE_NAME_UNIX}");
            }
        }

        private static void Invoke_Stop()
        {
            if (OperatingSystem.IsWindows())
            {
                var psi = ProcessUtils.CreateProcessStartInfo("sc.exe", "stop", Consts.SERVICE_NAME_WIN32);
                ConsoleUtils.ExecuteProcessStartInfo("正在停止服务", psi, true);
            }
            else if(OperatingSystem.IsMacOS())
            {
                ConsoleUtils.ExecuteShell("正在启动服务", $"launchctl stop {Consts.SERVICE_NAME_UNIX}");
                Thread.Sleep(1000);
            }
            else
            {
                ConsoleUtils.ExecuteShell("正在停止服务", $"systemctl {GetSystemdAddonParameter()} stop {Consts.SERVICE_NAME_UNIX}");
            }
        }

        private static void Invoke_Uninstall()
        {
            var status = GetServiceStatus();
            if (!status.Installed)
            {
                Console.WriteLine("检测到已经卸载，无法重复卸载！");
                return;
            }

            //如果服务运行中
            if (status.Started)
            {
                Invoke_Stop();
            }
            if (OperatingSystem.IsWindows())
            {
                var psi = ProcessUtils.CreateProcessStartInfo("sc.exe", "delete", Consts.SERVICE_NAME_WIN32);
                ConsoleUtils.ExecuteProcessStartInfo("正在删除服务", psi, true);
            }
            else if (OperatingSystem.IsMacOS())
            {
                var serviceFile = $"{Consts.SERVICE_NAME_UNIX}.plist";
                ConsoleUtils.ExecuteShell("正在删除服务", $"launchctl remove {Consts.SERVICE_NAME_UNIX}");
                ConsoleUtils.ExecuteShell("正在删除服务文件", $"rm ~/Library/LaunchAgents/{serviceFile}");
            }
            else
            {
                var serviceFile = $"{Consts.SERVICE_NAME_UNIX}.service";
                //如果服务已启用
                if (status.Enabled)
                    ConsoleUtils.ExecuteShell("正在禁用服务", $"systemctl {GetSystemdAddonParameter()} disable {Consts.SERVICE_NAME_UNIX}");
                ConsoleUtils.ExecuteShell("正在删除服务", $"rm {GetSystemdSystemFolder()}/{serviceFile}");
                ConsoleUtils.ExecuteShell("正在重新加载服务列表...", $"systemctl {GetSystemdAddonParameter()} daemon-reload");
                if (UnixUtils.IsRuningWithRoot())
                    ConsoleUtils.ExecuteShell("正在检查《易启动》服务是否删除完成", $"systemctl {GetSystemdAddonParameter()} list-unit-files {serviceFile}", isSuccessFunc: t => t.ExitCode != 0);
            }
            Console.WriteLine("-----------------------------");
            Console.WriteLine("卸载完成");
            Console.WriteLine("-----------------------------");
        }

        private static void Invoke_ServiceManage()
        {
            while (true)
            {
                var status = GetServiceStatus();
                //服务状态
                Console.Write(Locale.GetString("Service Status: "));
                //已安装
                Console.Write("Installed");
                if (status.Installed)
                {
                    ConsoleUtils.ConsoleWrite($"[{Locale.GetString("Yes")}]", ConsoleColor.Green);
                    Console.Write(" ");
                    Console.Write(Locale.GetString("Enabled"));
                    if (status.Enabled)
                    {
                        ConsoleUtils.ConsoleWrite($"[{Locale.GetString("Yes")}]", ConsoleColor.Green);
                        Console.Write(" ");
                        Console.Write(Locale.GetString("Started"));
                        if (status.Started)
                            ConsoleUtils.ConsoleWrite($"[{Locale.GetString("Yes")}]", ConsoleColor.Green);
                        else
                            ConsoleUtils.ConsoleWrite($"[{Locale.GetString("No")}]", ConsoleColor.Red);
                    }
                    else
                    {
                        ConsoleUtils.ConsoleWrite($"[{Locale.GetString("No")}]", ConsoleColor.Red);
                    }
                }
                else
                {
                    ConsoleUtils.ConsoleWrite($"[{Locale.GetString("No")}]", ConsoleColor.Red);
                }
                Console.WriteLine();
                var select1Dict = new Dictionary<string, string>();
                if (status.Installed)
                {
                    if (status.Enabled)
                    {
                        if (status.Started)
                        {
                            select1Dict["Stop"] = Locale.GetString("Stop Service");
                        }
                        else
                        {
                            select1Dict["Start"] = Locale.GetString("Start Service");
                            select1Dict["Uninstall"] = Locale.GetString("Uninstall Service");
                        }
                    }
                    else
                    {
                        select1Dict["Uninstall"] = Locale.GetString("Uninstall Service");
                    }
                }
                else
                {
                    select1Dict["Install"] = Locale.GetString("Install Service");
                }
                select1Dict["Exit"] = "Return to Main Menu";
                var select1 = QbSelect.ArrowSelect(select1Dict.ToArray(), selectedForegroundColor: ConsoleColor.Green);
                var selectName = select1Dict[select1];
                Console.WriteLine($"----------{selectName}-----------");
                try
                {
                    switch (select1)
                    {
                        case "Install":
                            Invoke_Install();
                            break;
                        case "Uninstall":
                            Invoke_Uninstall();
                            break;
                        case "Start":
                            Invoke_Start();
                            break;
                        case "Stop":
                            Invoke_Stop();
                            break;
                        case "Exit":
                            return;
                    }
                }
                catch (Exception ex)
                {
                    ConsoleUtils.ConsoleWriteLine(Locale.GetString("Error when execute [{0}]", selectName), ConsoleColor.Red);
                    ConsoleUtils.ConsoleWriteLine(ExceptionUtils.GetExceptionMessage(ex), ConsoleColor.Red);
                }
            }
        }
    }
}
