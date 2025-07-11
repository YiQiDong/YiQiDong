using System.Diagnostics;
using System.Net.NetworkInformation;
using Microsoft.AspNetCore.Components;
using Quick.Blazor.Bootstrap;
using Quick.Blazor.Bootstrap.Utils;

namespace YiQiDong.Components.Controls.TestTools;

public partial class PingTestControl : ComponentBase,IDisposable
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
        pushLog($"开始测试Ping[{host}]...");
        try
        {
            var stopwatch = new Stopwatch();
            using (var ping = new Ping())
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    stopwatch.Restart();
                    var rep = await ping.SendPingAsync(host, TimeSpan.FromSeconds(2), cancellationToken: cancellationToken);
                    stopwatch.Stop();
                    switch (rep.Status)
                    {
                        case IPStatus.Success:
                            pushLog($"来自 {rep.Address} 的回复: 字节={rep.Buffer?.Length} 时间={stopwatch.ElapsedMilliseconds}ms TTL={rep.RoundtripTime}");
                            break;
                        case IPStatus.DestinationHostUnreachable:
                            pushLog("无法访问目标主机。");
                            break;
                        case IPStatus.TimedOut:
                            pushLog("请求超时");
                            break;
                        default:
                            pushLog($"来自 {rep.Address} 的回复: {rep.Status}");
                            break;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            stop();
            pushLog("测试时出错，原因：" + ExceptionUtils.GetExceptionString(ex));
        }
        pushLog("测试结束.");
    }

    public void Dispose()
    {
        stop();
    }
}
