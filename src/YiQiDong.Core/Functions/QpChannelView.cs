using Quick.Fields;
using Quick.Protocol;
using YiQiDong.Core.Utils;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Core.Functions
{
    public class QpChannelView : AbstractSessionFunction, IDisposable
    {
        private string functionName;

        public override string Name => functionName;
        private Func<QpChannel> getChannelFunc;

        public QpChannelView(Func<QpChannel> getChannelFunc, string functionName = "通道情况")
        {
            this.functionName = functionName;
            this.getChannelFunc = getChannelFunc;
        }

        public QpChannelView(Func<QpChannel> getChannelFunc, string sessionId, QpChannel channel) : base(sessionId, channel)
        {
            this.getChannelFunc = getChannelFunc;
        }

        public override AbstractSessionFunction Create(string sessionId, QpChannel channel)
        {
            return new QpChannelView(getChannelFunc, sessionId, channel);
        }

        private CancellationTokenSource cts;

        
        public override FieldForGet[] Execute(FunctionRequest request)
        {
            List<FieldForGet> list = new();
            try
            {
                var channel = getChannelFunc();
                if (channel == null)
                    throw new IOException("当前未连接");
                list.Add(new FieldForGet()
                {
                    Type = FieldType.ContainerTable,
                    Children =
                    [
                        new ()
                       {
                           Type = FieldType.ContainerTableTr,
                           Children =
                           [
                               new (){ Type =  FieldType.ContainerTableTh,Value="时间"},
                               new (){ Type =  FieldType.ContainerTableTd,Value=DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}
                           ]
                       },
                       new ()
                       {
                           Type = FieldType.ContainerTableTr,
                           Children =
                           [
                               new (){ Type =  FieldType.ContainerTableTh,Value="通道名称"},
                               new (){ Type =  FieldType.ContainerTableTd,Value=channel.ChannelName}
                           ]
                       },
                       new ()
                       {
                           Type = FieldType.ContainerTableTr,
                           Children =
                           [
                               new (){ Type =  FieldType.ContainerTableTh,Value="连接时间"},
                               new (){ Type =  FieldType.ContainerTableTd,Value=channel.LastConnectedTime?.ToString("yyyy-MM-dd HH:mm:ss")}
                           ]
                       },
                       new ()
                       {
                           Type = FieldType.ContainerTableTr,
                           Children =
                           [
                               new (){ Type =  FieldType.ContainerTableTh,Value="已发送"},
                               new (){ Type =  FieldType.ContainerTableTd,Value=channel.BytesSent.ToString("N0")}
                           ]
                       },
                       new ()
                       {
                           Type = FieldType.ContainerTableTr,
                           Children =
                           [
                               new (){ Type =  FieldType.ContainerTableTh,Value="已接收"},
                               new (){ Type =  FieldType.ContainerTableTd,Value=channel.BytesReceived.ToString("N0")}
                           ]
                       },
                       new ()
                       {
                           Type = FieldType.ContainerTableTr,
                           Children =
                           [
                               new (){ Type =  FieldType.ContainerTableTh,Value="每秒发送"},
                               new (){ Type =  FieldType.ContainerTableTd,Value=channel.BytesSentPerSec.ToString("N0")}
                           ]
                       },
                       new ()
                       {
                           Type = FieldType.ContainerTableTr,
                           Children =
                           [
                               new (){ Type =  FieldType.ContainerTableTh,Value="每秒接收"},
                               new (){ Type =  FieldType.ContainerTableTd,Value=channel.BytesReceivedPerSec.ToString("N0")}
                           ]
                       },
                       new ()
                       {
                           Type = FieldType.ContainerTableTr,
                           Children =
                           [
                               new (){ Type =  FieldType.ContainerTableTh,Value="包发送队列数量"},
                               new (){ Type =  FieldType.ContainerTableTd,Value=channel.PackageSendQueueCount.ToString()}
                           ]
                       }
                    ]
                });
            }
            catch (Exception ex)
            {
                list.Add(new FieldForGet() { Type = FieldType.HtmlPre, Value = ExceptionUtils.GetExceptionString(ex) });
            }
            return list.ToArray();
        }

        private async Task beginRefresh(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);
                var fields = Execute(null);
                OnSessionChanged(fields);
            }
        }

        public override void Start()
        {
            cts?.Cancel();
            cts = new();
            _ = beginRefresh(cts.Token);
        }

        public override void Stop()
        {
            cts?.Cancel();
            cts = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
