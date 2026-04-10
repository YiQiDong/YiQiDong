using Quick.Blazor.Bootstrap;
using Quick.Utils;

namespace YiQiDong.Components.Pages.SysTools;

public partial class CommandExecute
{
    public int ConsoleRows = 20;
    private string command;
    private LogViewControl logViewControl;

    private void ConsoleSetRows(int rows)
    {
        ConsoleRows = rows;
    }

    private void Ok()
    {
        var currentCommand = command;
        command = null;
        Task.Run(() =>
        {
            if (!string.IsNullOrEmpty(currentCommand))
            {
                logViewControl.AddLine($"> {currentCommand}");

                try
                {
                    var ret = Quick.Shell.Utils.ProcessUtils.ExecuteShell(currentCommand);
                    if (!string.IsNullOrEmpty(ret.Output))
                        logViewControl.AddLine(ret.Output);
                    if (!string.IsNullOrEmpty(ret.Error))
                        logViewControl.AddLine(ret.Error);
                }
                catch (Exception ex)
                {
                    logViewControl.AddLine(ExceptionUtils.GetExceptionString(ex));
                }
            }
            logViewControl.AddLine(">");
        });
    }
}