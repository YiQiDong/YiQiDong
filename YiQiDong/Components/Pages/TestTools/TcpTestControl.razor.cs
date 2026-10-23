using System.Diagnostics;
using System.Net.Sockets;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Quick.Blazor.Bootstrap;
using Quick.Utils;

namespace YiQiDong.Components.Pages.TestTools;

public partial class TcpTestControl : ComponentBase, IDisposable
{
    private string host;
    private int port = 80;
    private string connectionType = "TEXT";
    private string data;
    private bool isConnecting = false;
    private bool isSending = false;
    private bool isConnected = false;
    private TcpClient tcpClient;
    private Stream stream;
    private StreamWriter writer;
    private LogViewControl logViewControl;

    private async Task start()
    {
        isConnecting = true;
        try
        {
            tcpClient = new()
            {
                SendTimeout = 10 * 1000,
                ReceiveTimeout = 30 * 1000
            };
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await tcpClient.ConnectAsync(host, port);
            stopwatch.Stop();
            stream = tcpClient.GetStream();
            if (connectionType == "TEXT")
                writer = new StreamWriter(stream);
            pushLog($"已连接到[{host}:{port}],用时: {stopwatch.ElapsedMilliseconds}ms");
            beginRead(tcpClient);
            isConnected = true;
        }
        catch (Exception ex)
        {
            pushLog($"连接到[{host}:{port}]时出错，原因：{ExceptionUtils.GetExceptionMessage(ex)}");
        }
        isConnecting = false;
        _ = InvokeAsync(StateHasChanged);
    }

    private void stop()
    {
        writer?.Dispose();
        writer = null;
        stream?.Dispose();
        stream = null;
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
                switch (connectionType)
                {
                    case "TEXT":
                        {
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
                            break;
                        }
                    case "HEX":
                        {
                            var buffer = new byte[10 * 1024];
                            while (client.Connected)
                            {
                                var ret = stream.ReadAtLeast(buffer, 1, true);
                                if (ret <= 0)
                                    throw new IOException("读取数据为0");
                                pushLog($"[RX] {BitConverter.ToString(buffer, 0, ret)}".Replace('-',' '));
                            }
                            break;
                        }
                }
            }
            catch
            {
                pushLog($"到[{host}:{port}]的连接已断开");
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
                switch (connectionType)
                {
                    case "TEXT":
                        writer.WriteLine(data);
                        writer.Flush();
                        pushLog($"[TX] {data}");
                        break;
                    case "HEX":
                        var sendBuffer = Convert.FromHexString(data
                            .Replace(" ", string.Empty)
                            .Replace("-", string.Empty)
                            .Replace(",", string.Empty)
                            .Replace(":", string.Empty)
                            );
                        stream.Write(sendBuffer);
                        pushLog($"[TX] {BitConverter.ToString(sendBuffer).Replace('-',' ')}");
                        break;
                }

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
        logViewControl.AddLine($"{DateTime.Now:T}: {line}");
    }

    public void Dispose()
    {
        stop();
    }
}
