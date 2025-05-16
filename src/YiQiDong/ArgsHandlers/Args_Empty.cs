using Microsoft.Extensions.Hosting;
using Quick.Build;
using Quick.Shell.PowerShell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using YiQiDong.Core.Utils;
using YiQiDong.Utils;

namespace YiQiDong.ArgsHandlers
{
    public partial class Args_Empty
    {
        private class ServiceStatus
        {
            public bool Installed { get; set; } = false;
            public bool Enabled { get; set; } = false;
            public bool Started { get; set; } = false;
        }

        internal static void Invoke(string[] args)
        {
            while (true)
            {
                Console.WriteLine("-------欢迎使用弈启动--------");
                Console.WriteLine($"版本：{Consts.Version}");
                Console.WriteLine($"架构：{Consts.ARCH}");
                Console.WriteLine("-----------------------------");

                var select1Dict = new Dictionary<string, string>()
                {
                    ["Debug"] = "调试运行",
                    ["ServiceManage"] = "服务管理",
                    ["EditConfig"] = "编辑配置"
                };
                if (OperatingSystem.IsWindows())
                {
                    select1Dict["Shotcut"] = "快捷方式";
                    //select1Dict["Test"] = "测试";
                }
                select1Dict["Exit"] = "退出";
                var select1 = QbSelect.ArrowSelect(select1Dict.ToArray(), selectedForegroundColor: ConsoleColor.Green);
                var selectName = select1Dict[select1];
                Console.WriteLine($"----------{selectName}-----------");
                try
                {
                    switch (select1)
                    {
                        case "Debug":
                            Invoke_Debug();
                            break;
                        case "EditConfig":
                            Invoke_EditConfig();
                            break;
                        case "ServiceManage":
                            Invoke_ServiceManage();
                            break;
                        case "Shotcut":
                            if (OperatingSystem.IsWindows())
                                Invoke_Shotcut();
                            break;
                        case "Test":
                            Invoke_Test();
                            break;
                        case "Exit":
                            return;
                    }
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    ConsoleUtils.ConsoleWriteLine($"执行[{selectName}]时出错", ConsoleColor.Red);
                    ConsoleUtils.ConsoleWriteLine(ExceptionUtils.GetExceptionString(ex), ConsoleColor.Red);
                    Console.WriteLine("按回车键回到主菜单...");
                    Console.ReadLine();
                }
            }
        }

        private static void Invoke_Test()
        {
            
        }

        private static void Invoke_Debug()
        {
            Program.Start();
            new HostBuilder().RunConsoleAsync().Wait();
            Program.Stop();
        }

        private static void Invoke_EditConfig()
        {
            var changed = false;
            string line = null;
            Console.Write($"标题[{Program.Config.Title}]：", ConsoleColor.Green);
            line = Console.ReadLine();
            if (!string.IsNullOrEmpty(line))
            {
                Program.Config.Title = line;
                changed = true;
            }
            Console.Write($"URL[{Program.Config.Urls}]：", ConsoleColor.Green);
            line = Console.ReadLine();
            if (!string.IsNullOrEmpty(line))
            {
                if (!line.StartsWith("http://"))
                    line = $"http://*:{line}";
                Program.Config.Urls = line;
                changed = true;
            }
            Console.Write($"密码[{Program.Config.Password}]：", ConsoleColor.Green);
            line = Console.ReadLine();
            if (!string.IsNullOrEmpty(line))
            {
                Program.Config.Password = line;
                changed = true;
            }
            Console.Write($"数据目录[{Program.Config.DataFolder}]：", ConsoleColor.Green);
            line = Console.ReadLine();
            if (!string.IsNullOrEmpty(line))
            {
                Program.Config.DataFolder = line;
                changed = true;
            }
            if (changed)
            {
                Program.Config.Save();
                ConsoleUtils.ConsoleWriteLine("[已保存修改后的配置]", ConsoleColor.Green);
            }
            else
            {
                Console.WriteLine("[配置未修改]");
            }
        }


        [SupportedOSPlatform("windows")]
        private static void Invoke_Shotcut()
        {
            var lnkFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "弈启动.lnk");
            if (File.Exists(lnkFile))
            {
                ConsoleUtils.ExecuteAction("正在删除旧的桌面快捷方式", () => { File.Delete(lnkFile); });
            }
            var executeFileName = Process.GetCurrentProcess().MainModule.FileName;
            var psFileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.ps1");
            var psFileEncoding = Encoding.GetEncoding(Thread.CurrentThread.CurrentCulture.TextInfo.ANSICodePage);
            var psFileContent = @$"$WshShell = New-Object -comObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut(""{lnkFile}"")
$Shortcut.TargetPath =""{executeFileName}""
$Shortcut.WorkingDirectory = ""{Path.GetDirectoryName(executeFileName)}"";
$Shortcut.Save()
Remove-Item ""{psFileName}""
";
            File.WriteAllText(psFileName, psFileContent, psFileEncoding);
            ConsoleUtils.ExecuteFunc("正在创建桌面快捷方式",
                () => PowerShellProcessContext.ExecutePs1File(psFileName));
            Console.WriteLine("[创建桌面快捷方式成功]");
        }
    }
}
