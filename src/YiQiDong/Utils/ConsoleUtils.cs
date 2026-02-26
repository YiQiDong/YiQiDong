using Quick.Shell;
using Quick.Shell.Utils;
using Quick.Utils;
using System.Diagnostics;
using System.Text;

namespace YiQiDong.Utils;

public class ConsoleUtils
{
    public static Dictionary<string, string> ConsoleOutputParse(string output, string sp = ":")
    {
        Dictionary<string, string> dict = new();
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var index = line.IndexOf(sp);
            if (index <= 0)
                continue;
            var key = line.Substring(0, index).Trim();
            var value = line.Substring(index + sp.Length).Trim(' ', '\"');
            dict[key] = value;
        }
        return dict;
    }

    public static void ConsoleWrite(string message, ConsoleColor foregroundColor)
    {
        var preForegroundColor = Console.ForegroundColor;
        Console.ForegroundColor = foregroundColor;
        Console.Write(message);
        Console.ForegroundColor = preForegroundColor;
    }

    public static void ConsoleWriteLine(string message, ConsoleColor foregroundColor)
    {
        var preForegroundColor = Console.ForegroundColor;
        Console.ForegroundColor = foregroundColor;
        Console.WriteLine(message);
        Console.ForegroundColor = preForegroundColor;
    }

    public static void ExecuteAction(string name, Action action)
    {
        Console.Write(name);
        try
        {
            action.Invoke();
            ConsoleWriteLine(" [OK]", ConsoleColor.Green);
        }
        catch (Exception ex)
        {
            ConsoleWriteLine(" [ERROR]", ConsoleColor.Red);
            throw new ApplicationException($"执行[{name}]时出错，原因:{ExceptionUtils.GetExceptionMessage(ex)}");
        }
    }

    public static void ExecuteFunc(string name, Func<ShellProcessResult> func, Func<ShellProcessResult, bool> isSuccessFunc = null)
    {
        Console.Write(name);
        try
        {
            var ret = func.Invoke();
            var isSuccess = isSuccessFunc == null ? ret.ExitCode == 0 : isSuccessFunc(ret);
            if (isSuccess)
            {
                ConsoleWriteLine(" [OK]", ConsoleColor.Green);
                return;
            }
            ConsoleWriteLine(" [ERROR]", ConsoleColor.Red);
            var sb = new StringBuilder();
            sb.Append($"执行[{name}]时出错，进程退出码:{ret.ExitCode}。");
            if (!string.IsNullOrEmpty(ret.Output))
                sb.Append($"进程输出:{ret.Output}。");
            if (!string.IsNullOrEmpty(ret.Error))
                sb.Append($"进程错误:{ret.Error}。");
            throw new ApplicationException(sb.ToString());
        }
        catch (Exception ex)
        {
            ConsoleWriteLine(" [ERROR]", ConsoleColor.Red);
            throw new ApplicationException($"执行[{name}]时出错，原因:{ExceptionUtils.GetExceptionMessage(ex)}");
        }
    }

    public static void ExecuteProcessStartInfo(string name, ProcessStartInfo psi, bool runAsAdmin = false, Func<ShellProcessResult, bool> isSuccessFunc = null)
    {
        ExecuteFunc(name, () => ProcessUtils.ExecuteProcessStartInfo(psi, runAsAdmin), isSuccessFunc);
    }

    public static void ExecuteShell(string name, string command, bool runAsAdmin = false, Func<ShellProcessResult, bool> isSuccessFunc = null)
    {
        ExecuteFunc(name, () => ProcessUtils.ExecuteShell(command, runAsAdmin), isSuccessFunc);
    }
}