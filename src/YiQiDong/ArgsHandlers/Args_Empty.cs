using Quick.Build;
using Quick.Localize;
using Quick.Shell.PowerShell;
using Quick.Utils;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Versioning;
using System.Text;
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

        internal static void Invoke()
        {
            Console.WriteLine("-----------------------------");
            Console.WriteLine("   语言 / Language");
            Console.WriteLine("-----------------------------");
            var allLanguages = new string[] { "zh-CN", "en-US" };
            var selectLanguageDict = allLanguages.ToDictionary(t => t, t => CultureInfo.GetCultureInfo(t).NativeName);
            var selectedLanguage = QbSelect.ArrowSelect(selectLanguageDict.ToArray(), selectedForegroundColor: ConsoleColor.Green);
            GettextResourceManager.ChangeCurrentCulture(CultureInfo.GetCultureInfo(selectedLanguage));

            while (true)
            {
                Console.WriteLine("-----------------------------");
                Console.WriteLine(Locale.GetString("Welcome to use YiQiDong"));
                Console.WriteLine(Locale.GetString("Version: {0}", Consts.Version));
                Console.WriteLine(Locale.GetString("Architecture: {0}", Consts.ARCH));
                Console.WriteLine("-----------------------------");

                var select1Dict = new Dictionary<string, string>()
                {
                    ["Debug"] = Locale.GetString("Debug Run"),
                    ["ServiceManage"] = Locale.GetString("Service Manage"),
                    ["EditConfig"] = Locale.GetString("Edit Config")
                };
                if (OperatingSystem.IsWindows())
                {
                    select1Dict["Shotcut"] = Locale.GetString("Shotcut");
                    //select1Dict["Test"] = Locale.GetString("Test");
                }
                select1Dict["Exit"] = Locale.GetString("Exit");
                var select1 = QbSelect.ArrowSelect(select1Dict.ToArray(), selectedForegroundColor: ConsoleColor.Green);
                var selectName = select1Dict[select1];
                Console.WriteLine($"----------{selectName}-----------");
                try
                {
                    switch (select1)
                    {
                        case "Debug":
                            Program.IsDebugRuning = true;
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
                    ConsoleUtils.ConsoleWriteLine(Locale.GetString("Error when execute [{0}]", selectName), ConsoleColor.Red);
                    ConsoleUtils.ConsoleWriteLine(ExceptionUtils.GetExceptionString(ex), ConsoleColor.Red);
                    Console.WriteLine(Locale.GetString("Press Enter to return to the main menu..."));
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
            Console.Write($"{Locale.GetString("Title")}[{Program.Config.Title}]: ", ConsoleColor.Green);
            line = Console.ReadLine();
            if (!string.IsNullOrEmpty(line))
            {
                Program.Config.Title = line;
                changed = true;
            }
            Console.Write($"URL[{Program.Config.Urls}]: ", ConsoleColor.Green);
            line = Console.ReadLine();
            if (!string.IsNullOrEmpty(line))
            {
                if (!line.StartsWith("http://"))
                    line = $"http://*:{line}";
                Program.Config.Urls = line;
                changed = true;
            }
            Console.Write($"{Locale.GetString("Password")}[{Program.Config.Password}]: ", ConsoleColor.Green);
            line = Console.ReadLine();
            if (!string.IsNullOrEmpty(line))
            {
                Program.Config.Password = line;
                changed = true;
            }
            Console.Write($"{Locale.GetString("Data Folder")}[{Program.Config.DataFolder}]: ", ConsoleColor.Green);
            line = Console.ReadLine();
            if (!string.IsNullOrEmpty(line))
            {
                Program.Config.DataFolder = line;
                changed = true;
            }
            if (changed)
            {
                Program.Config.Save();
                ConsoleUtils.ConsoleWriteLine($"{Locale.GetString("[The modified configuration has been saved]")}", ConsoleColor.Green);
            }
            else
            {
                Console.WriteLine(Locale.GetString("[Configuration not modified]"));
            }
        }


        [SupportedOSPlatform("windows")]
        private static void Invoke_Shotcut()
        {
            var lnkFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), Locale.GetString("YiQiDong") + ".lnk");
            if (File.Exists(lnkFile))
            {
                ConsoleUtils.ExecuteAction(Locale.GetString("Deleting old desktop shortcuts"), () => { File.Delete(lnkFile); });
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
            ConsoleUtils.ExecuteFunc(Locale.GetString("Creating desktop shortcut"),
                () => PowerShellProcessContext.ExecutePs1File(psFileName));
            Console.WriteLine(Locale.GetString("[Desktop shortcut created successfully]"));
        }
    }
}
