using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using YiQiDong.Utils;

namespace YiQiDong.ArgsHandlers
{
    public class ArgsHandler
    {
        internal static void Invoke(string[] args)
        {
            var firstArg = args?.FirstOrDefault() ?? string.Empty;
            if (firstArg == "-agent")
            {
                Args_Agent.Invoke(args);
                return;
            }
            Program.LoadConfig();
            switch (firstArg)
            {
                case "":
                    Args_Empty.Invoke();
                    break;
                case "-debug":
                    Program.Start();
                    new HostBuilder().RunConsoleAsync().Wait();
                    Program.Stop();
                    break;
                case "-prepare":
                    Args_Prepare.Invoke(args);
                    break;
                case "-service":
                    Args_Service.Invoke(args);
                    break;
                default:
                    ConsoleUtils.ConsoleWriteLine("Unknown argument: " + firstArg);
                    break;
            }
        }
    }
}
