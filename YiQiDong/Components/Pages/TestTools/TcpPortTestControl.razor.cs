using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Components;
using Quick.Blazor.Bootstrap;
using Quick.Utils;

namespace YiQiDong.Components.Pages.TestTools;

public partial class TcpPortTestControl : ComponentBase, IDisposable
{
    private Options options = new Options();
    private bool isTesting = false;
    private int scanned;
    private int total;
    private float progressPercent = 0;
    private CancellationTokenSource cts;
    private LogViewControl logViewControl;

    private void start()
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();
        isTesting = true;
        _ = beginTest(options, cts.Token);
    }

    private void stop()
    {
        clean();
        pushLog("已停止");
    }

    private void clean()
    {
        cts?.Cancel();
        cts = null;
        isTesting = false;
    }

    private void pushLog(string line)
    {
        logViewControl.AddLine($"{DateTime.Now.ToLongTimeString()}: {line}");
    }

    internal sealed class Options
    {
        public string Target { get; set; } = "";
        public int StartPort { get; set; } = 1;
        public int EndPort { get; set; } = 65535;
        public int TimeoutMs { get; set; } = 800;
        public int Concurrency { get; set; } = 200;
        public bool Verbose { get; set; } = false;

        public void Check()
        {
            if (string.IsNullOrWhiteSpace(Target))
                throw new IOException("必须指定目标地址");
            if (StartPort < 1 || StartPort > 65535 ||
                EndPort < 1 || EndPort > 65535)
                throw new IOException("端口范围须在 1-65535 之间");
            if (StartPort > EndPort)
                throw new IOException("起始端口须小于等于结束端口");

            if (TimeoutMs < 1)
                throw new IOException("超时须大于 0");
            if (Concurrency < 1)
                throw new IOException("并发数须大于 0");
        }
    }

    private string divider = new string('-', 48);

    private async Task beginTest(Options options, CancellationToken cancellationToken)
    {
        isTesting = true;

        try
        {
            options.Check();
        }
        catch (Exception ex)
        {
            pushLog(ex.Message);
            clean();
            return;
        }
        try
        {
            IPAddress ip = null;
            try
            {
                if (!IPAddress.TryParse(options.Target, out ip))
                {
                    var addresses = await Dns.GetHostAddressesAsync(options.Target);
                    ip = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork || a.AddressFamily == AddressFamily.InterNetworkV6)
                         ?? addresses.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                pushLog($"无法解析目标地址 '{options.Target}': {ex.Message}");
                return;
            }

            if (ip is null)
            {
                pushLog($"无法解析目标地址 '{options.Target}'");
                return;
            }

            pushLog($"开始扫描 {options.Target} ({ip}) 端口 {options.StartPort}-{options.EndPort}");
            pushLog($"超时={options.TimeoutMs}ms  最大并发={options.Concurrency}  {(options.Verbose ? "(详细模式)" : "")}");
            pushLog(divider);

            _ = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(100, cancellationToken);
                    progressPercent = scanned * 100F / total;
                    await InvokeAsync(StateHasChanged);
                }
            });

            
            total = options.EndPort - options.StartPort + 1;            
            scanned = 0;
            progressPercent = 0;

            var stopwatch = Stopwatch.StartNew();
            var openPorts = new ConcurrentBag<int>();
            var ports = Enumerable.Range(options.StartPort, total);
            await Parallel.ForEachAsync(ports,
                new ParallelOptions { MaxDegreeOfParallelism = options.Concurrency, CancellationToken = cancellationToken },
                async (port, ct) =>
                {
                    var open = await IsPortOpen(ip, port, options.TimeoutMs, ct);
                    if (open)
                        openPorts.Add(port);

                    if (options.Verbose)
                    {
                        pushLog(open
                        ? $"  [OPEN]   {port,-6} {ServiceName(port)}"
                        : $"  [closed] {port,-6}");
                    }
                    Interlocked.Increment(ref scanned);
                });
            stopwatch.Stop();

            if (options.Verbose)
                pushLog(divider);

            pushLog($"开放端口: {openPorts.Count} 个");
            foreach (var p in openPorts.OrderBy(x => x))
                pushLog($"  {p,-6} {ServiceName(p)}");
            pushLog(divider);
            pushLog($"完成。开放 {openPorts.Count}/{total} 个端口，耗时 {stopwatch.Elapsed.TotalSeconds:F1}s");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            pushLog("测试时出错，原因：" + ExceptionUtils.GetExceptionString(ex));
        }
        clean();
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        clean();
    }


    private static async Task<bool> IsPortOpen(IPAddress ip, int port, int timeoutMs, CancellationToken ct)
    {
        Socket socket = null;
        try
        {
            socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                SendTimeout = timeoutMs,
                ReceiveTimeout = timeoutMs,
                Blocking = true
            };
            var timeoutCts = new CancellationTokenSource(timeoutMs);
            var cts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
            await socket.ConnectAsync(new IPEndPoint(ip, port), cts.Token);
            return socket.Connected;
        }
        catch (SocketException)
        {
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
        finally
        {
            socket?.Dispose();
        }
    }

    private static readonly Dictionary<int, string> CommonServices = new()
    {
        [7] = "Echo",
        [20] = "FTP-data",
        [21] = "FTP",
        [22] = "SSH",
        [23] = "Telnet",
        [25] = "SMTP",
        [53] = "DNS",
        [79] = "Finger",
        [80] = "HTTP",
        [110] = "POP3",
        [111] = "RPC",
        [113] = "Ident",
        [119] = "NNTP",
        [135] = "MS-RPC",
        [139] = "NetBIOS",
        [143] = "IMAP",
        [389] = "LDAP",
        [443] = "HTTPS",
        [445] = "SMB",
        [465] = "SMTPS",
        [514] = "Syslog",
        [587] = "SMTP-sub",
        [993] = "IMAPS",
        [995] = "POP3S",
        [1433] = "MSSQL",
        [1521] = "Oracle",
        [2049] = "NFS",
        [3306] = "MySQL",
        [3389] = "RDP",
        [5432] = "PostgreSQL",
        [5900] = "VNC",
        [5985] = "WinRM",
        [5986] = "WinRM-S",
        [6379] = "Redis",
        [7001] = "WebLogic",
        [8080] = "HTTP-alt",
        [8443] = "HTTPS-alt",
        [9200] = "Elasticsearch",
        [11211] = "Memcached",
        [27017] = "MongoDB"
    };

    private static string ServiceName(int port) =>
        CommonServices.TryGetValue(port, out var name) ? name : "";
}
