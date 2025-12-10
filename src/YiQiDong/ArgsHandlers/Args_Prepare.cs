using Quick.Shell.Utils;
using YiQiDong.Utils;

namespace YiQiDong.ArgsHandlers
{
    public class Args_Prepare
    {
        internal static void Invoke(string[] args)
        {
            if (!string.IsNullOrEmpty(Program.Config.EnvironmentVariables))
            {
                var dict = ConsoleUtils.ConsoleOutputParse(Program.Config.EnvironmentVariables, "=");
                foreach (var item in dict)
                {
                    var cmdLine = $"export {item.Key}=\"{item.Value}\"";
                    Console.WriteLine(cmdLine);
                }
            }
        }
    }
}
