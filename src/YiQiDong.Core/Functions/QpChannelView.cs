using Quick.Fields;
using Quick.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YiQiDong.Core.Utils;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Core.Functions
{
    public class QpChannelView : AbstractFunction
    {
        private string functionName;

        public override string Name => functionName;
        private Func<QpChannel> getChannelFunc;

        public QpChannelView(Func<QpChannel> getChannelFunc, string functionName = "通道情况")
        {
            this.functionName = functionName;
            this.getChannelFunc = getChannelFunc;
        }

        public override FieldForGet[] Get()
        {
            List<FieldForGet> list = [new FieldForGet() { Id = "Refresh", Type = FieldType.Button, Name = "刷新" }];
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

        public override FieldForGet[] Post(FunctionRequest request)
        {
            return Get();
        }
    }
}
