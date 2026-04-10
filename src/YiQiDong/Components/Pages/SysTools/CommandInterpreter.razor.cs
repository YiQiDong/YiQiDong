using System.Diagnostics;
using Quick.Blazor.Bootstrap;
using Quick.Shell.Utils;
using Quick.Utils;

namespace YiQiDong.Components.Pages.SysTools;

public partial class CommandInterpreter : IDisposable
{
    public int ConsoleRows = 20;
    private string command;
    private LogViewControl logViewControl;
    private string propmt = string.Empty;
    private Process process;
    private StreamReader reader;
    private StreamWriter writer;
    private ModalAlert modalAlert;

    private void ConsoleSetRows(int rows)
    {
        ConsoleRows = rows;
    }

    private Dictionary<string, string> commandInterpreterDict;
    private CancellationTokenSource cts;

    protected override void OnInitialized()
    {
        if (OperatingSystem.IsWindows())
        {
            commandInterpreterDict = new()
            {
                ["cmd"] = "cmd",
                ["powershell"] = "PowerShell"
            };
        }
        else
        {
            commandInterpreterDict = new()
            {
                ["sh"] = "sh",
                ["bash"] = "bash",
                ["zsh"] = "zsh",
                ["fish"] = "fish"
            };
        }
    }

    private async Task Start(string cmd)
    {
        try
        {
            cts?.Cancel();
            cts = new();
            var cancellationToken = cts.Token;

            process = Process.Start(ProcessUtils.CreateProcessStartInfo(cmd));
            reader = process.StandardOutput;
            writer = process.StandardInput;
            var errReader = process.StandardError;
            _ = Task.Run(async () =>
            {
                await process.WaitForExitAsync(cancellationToken);
                process = null;
                cts?.Cancel();
                cts = null;
                _ = InvokeAsync(StateHasChanged);
            });
            _ = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var line = await errReader.ReadLineAsync(cancellationToken);
                    logViewControl.AddLine(line);
                }
            });
            _ = Task.Run(async () =>
            {
                var charBuffer = new char[1024];
                while (!cancellationToken.IsCancellationRequested)
                {
                    var ret = await reader.ReadAsync(charBuffer, cancellationToken);
                    if (ret == 0)
                        continue;
                    var charSpan = new Span<char>(charBuffer, 0, ret);
                    var str = new string(charSpan);
                    if (!string.IsNullOrEmpty(propmt))
                        str = propmt + str;
                    var isEnd = str.EndsWith(Environment.NewLine);
                    string[] strs;
                    if (isEnd)
                    {
                        propmt = string.Empty;
                        strs = str.Substring(0, str.Length - Environment.NewLine.Length).Split(Environment.NewLine);
                    }
                    else
                    {
                        strs = str.Split(Environment.NewLine);
                        propmt = strs.Last();
                        strs = strs.Take(strs.Length - 1).ToArray();
                    }
                    foreach (var line in strs)
                        logViewControl.AddLine(line);
                    _ = InvokeAsync(StateHasChanged);
                }
            });
            _ = InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            modalAlert.Show("错误", ExceptionUtils.GetExceptionString(ex));
        }
    }

    private void Stop()
    {
        if (process != null)
            process.Kill(true);
    }

    private void Clear()
    {
        logViewControl.Clear();
        logViewControl.SetContent(null);
    }

    private async Task Ok()
    {
        var currentCommand = command;
        command = null;
        await InvokeAsync(StateHasChanged);
        await writer.WriteLineAsync(currentCommand);
    }

    public void Dispose()
    {
        Stop();
    }
}