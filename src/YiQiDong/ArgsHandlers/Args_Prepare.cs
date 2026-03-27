using Quick.Shell.Utils;
using YiQiDong.Utils;

namespace YiQiDong.ArgsHandlers
{
    public class Args_Prepare
    {
        internal static void Invoke(string[] args)
        {
            if (!string.IsNullOrEmpty(Program.Config.LinuxOSLang))
                ConsoleUtils.ConsoleWriteLine($"export LANG=\"{Program.Config.LinuxOSLang}\"");
            if (!string.IsNullOrEmpty(Program.Config.LinuxOSTimeZone))
                ConsoleUtils.ConsoleWriteLine($"export TZ=\"{Program.Config.LinuxOSTimeZone}\"");
            if (!string.IsNullOrEmpty(Program.Config.EnvironmentVariables))
            {
                var dict = ConsoleUtils.ConsoleOutputParse(Program.Config.EnvironmentVariables, "=");
                foreach (var item in dict)
                    ConsoleUtils.ConsoleWriteLine($"export {item.Key}=\"{item.Value}\"");
            }
        }
    }
}
