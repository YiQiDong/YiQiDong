using Quick.Blazor.Bootstrap;
using Quick.Utils;

namespace YiQiDong.Components.Pages.SysTools;

public partial class CommandExecute : IDisposable
{
    public int ConsoleRows = 20;
    private string command;
    private Quick.Shell.ICommandContext commandContext;
    private LogViewControl logViewControl;

    protected override void OnInitialized()
    {
        if (OperatingSystem.IsWindows())
        {
            commandContext = new Quick.Shell.WinCmd.WinCmdCommandContext();
        }
        else
        {
            commandContext = new Quick.Shell.UnixShell.UnixShellCommandContext();
        }
    }

    public void Dispose()
    {
        commandContext?.Dispose();
    }

    private void ConsoleSetRows(int rows)
    {
        ConsoleRows = rows;
    }

    private async Task Ok()
    {
        var currentCommand = command;
        command = null;
        await Task.Delay(10);
        if (!string.IsNullOrEmpty(currentCommand))
        {
            logViewControl.AddLine($"> {currentCommand}");

            try
            {
                var ret = commandContext.ExecuteCommand(currentCommand, false);
                if (ret.Output != null)
                    foreach (var line in ret.Output)
                        logViewControl.AddLine(line);
            }
            catch (Exception ex)
            {
                logViewControl.AddLine(ExceptionUtils.GetExceptionString(ex));
            }
        }
        logViewControl.AddLine(">");
    }
}