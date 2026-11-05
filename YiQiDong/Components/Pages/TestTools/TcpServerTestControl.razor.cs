using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Quick.Blazor.Bootstrap;
using Quick.Utils;

namespace YiQiDong.Components.Pages.TestTools;

public partial class TcpServerTestControl : ComponentBase, IDisposable
{
    private string ipAddress = "0.0.0.0";
    private int port = 80;
    private bool isListening = false;
    private CancellationTokenSource cts;
    private TcpListener tcpListener;
    private LogViewControl logViewControl;
    private bool includeRaw = true;
    private bool includeUTF8Text = false;
    private bool includeANSIText = false;

    private async Task start()
    {
        try
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();
            tcpListener = new TcpListener(IPAddress.Parse(ipAddress), port);
            tcpListener.Start();
            pushLog($"正在监听: {tcpListener.LocalEndpoint}");
            isListening = true;
            beginAccept(cts.Token);
        }
        catch (Exception ex)
        {
            pushLog($"监听[{ipAddress}:{port}]时出错，原因：{ExceptionUtils.GetExceptionMessage(ex)}");
        }
        _ = InvokeAsync(StateHasChanged);
    }

    private void stop()
    {
        cts?.Cancel();
        cts = null;
        isListening = false;
        tcpListener?.Dispose();
        InvokeAsync(StateHasChanged);
    }

    private void beginAccept(CancellationToken cancellationToken)
    {
        Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var tcpClient = await tcpListener.AcceptTcpClientAsync(cancellationToken);
                var connectionName = tcpClient.Client.RemoteEndPoint.ToString();
                pushLog($"[{connectionName}] 已连接.");
                _ = Task.Run(async () =>
                {
                    var buffer = new byte[10 * 1024];
                    try
                    {
                        using (var stream = tcpClient.GetStream())
                        {
                            var sb = new StringBuilder();
                            while (!cancellationToken.IsCancellationRequested)
                            {
                                var ret = await stream.ReadAtLeastAsync(buffer, 1, true, cancellationToken);

                                sb.AppendLine($"[{connectionName}] 接收到数据[{ret}字节]");
                                if (includeRaw)
                                    sb.AppendLine($"原始数据: {BitConverter.ToString(buffer, 0, ret)}");
                                if (includeUTF8Text)
                                    sb.AppendLine($"UTF8文本: {Encoding.UTF8.GetString(buffer, 0, ret)}");
                                if (includeANSIText)
                                    sb.AppendLine($"本地文本: {Encoding.GetEncoding(0).GetString(buffer, 0, ret)}");
                                pushLog(sb.ToString().Trim());
                                sb.Clear();
                            }
                        }
                    }
                    catch
                    {
                        pushLog($"[{connectionName}] 已断开.");
                    }
                });
            }
        });
    }

    private void pushLog(string line)
    {
        logViewControl.AddLine($"{DateTime.Now:T}: {line}");
    }

    public void Dispose()
    {
        stop();
    }
}
