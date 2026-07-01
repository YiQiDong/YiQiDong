using System.Diagnostics;
using System.Net.Sockets;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Quick.Blazor.Bootstrap;
using Quick.Utils;

namespace YiQiDong.Components.Pages.TestTools;

public partial class TcpTestControl : ComponentBase, IDisposable
{
    private string address;
    private string data;
    private bool isConnecting = false;
    private bool isSending = false;
    private bool isConnected = false;
    private TcpClient tcpClient;
    private StreamWriter writer;
    private LogViewControl logViewControl;

    private async Task start()
    {
        string host;
        int port;

        try
        {
            var strs = address.Split(":");
            host = strs[0].Trim();
            port = int.Parse(strs[1].Trim());
        }
        catch
        {
            pushLog($"地址[{address}]无效!");
            return;
        }
        isConnecting = true;
        try
        {
            tcpClient = new();
            tcpClient.SendTimeout = 10 * 1000;
            tcpClient.ReceiveTimeout = 30 * 1000;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await tcpClient.ConnectAsync(host, port);
            stopwatch.Stop();
            writer = new StreamWriter(tcpClient.GetStream());
            pushLog($"已连接到[{address}],用时: {stopwatch.ElapsedMilliseconds}ms");
            beginRead(tcpClient);
            isConnected = true;
        }
        catch (Exception ex)
        {
            pushLog($"连接到[{address}]时出错，原因：{ExceptionUtils.GetExceptionMessage(ex)}");
        }
        isConnecting = false;
        _ = InvokeAsync(StateHasChanged);
    }

    private void stop()
    {
        writer?.Dispose();
        writer = null;
        tcpClient?.Close();
        tcpClient?.Dispose();
        isConnected = false;
        InvokeAsync(StateHasChanged);
    }

    private void beginRead(TcpClient client)
    {
        Task.Run(() =>
        {
            try
            {
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream))
                {
                    while (client.Connected)
                    {
                        var line = reader.ReadLine();
                        if (line == null)
                            throw new IOException();
                        pushLog($"[RX] {line}");
                    }
                }
            }
            catch
            {
                pushLog($"到[{address}]的连接已断开");
                stop();
            }
        });
    }

    private void onDataKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !isSending)
            send();
    }

    private void send()
    {
        isSending = true;
        InvokeAsync(StateHasChanged);
        _ = Task.Run(() =>
        {
            try
            {
                writer.WriteLine(data);
                writer.Flush();
                pushLog($"[TX] {data}");
                data = null;
            }
            catch (Exception ex)
            {
                pushLog($"发送数据时出错，原因：{ExceptionUtils.GetExceptionMessage(ex)}");
            }
            isSending = false;
            InvokeAsync(StateHasChanged);
        });
    }

    private void pushLog(string line)
    {
        logViewControl.AddLine($"{DateTime.Now.ToLongTimeString()}: {line}");
    }

    public void Dispose()
    {
        stop();
    }
}
