using System.Net.Sockets;
using Microsoft.AspNetCore.Components;
using Quick.Blazor.Bootstrap;
using Quick.Blazor.Bootstrap.Utils;

namespace YiQiDong.Components.Pages.TestTools;

public partial class TcpPortTestControl : ComponentBase, IDisposable
{
    private string host;
    private bool isTesting = false;
    private CancellationTokenSource cts;
    private LogViewControl logViewControl;

    private void start()
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();
        isTesting = true;
        _ = beginTest(cts.Token);
    }

    private void stop()
    {
        cts?.Cancel();
        cts = null;
        isTesting = false;
    }

    private void pushLog(string line)
    {
        logViewControl.AddLine($"{DateTime.Now.ToLongTimeString()}: {line}");
    }

    private async Task beginTest(CancellationToken cancellationToken)
    {
        isTesting = true;
        pushLog($"开始对[{host}]进行TCP端口扫描...");
        try
        {
            for (var i = 1; i < 65535; i++)
            {
                using (var client = new TcpClient())
                {
                    try
                    {
                        await client.ConnectAsync(host, i, cancellationToken);
                        pushLog($"检测到端口[{i}]开放");
                        client.Close();
                    }
                    catch { }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            stop();
            pushLog("测试时出错，原因：" + ExceptionUtils.GetExceptionString(ex));
        }
        isTesting = false;
        pushLog("测试结束.");
    }

    public void Dispose()
    {
        stop();
    }
}
