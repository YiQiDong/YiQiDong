using Microsoft.AspNetCore.Builder;
using Quick.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YiQiDong.Cluster;

namespace Microsoft.AspNetCore.Builder
{
    public static class ClusterInterfaceMiddlewareExtensions
    {
        private static Quick.Protocol.WebSocket.Server.AspNetCore.QpWebSocketServer server;
        public static IApplicationBuilder UseClusterInterface(this IApplicationBuilder app)
        {
            var serverOptions = new Quick.Protocol.WebSocket.Server.AspNetCore.QpWebSocketServerOptions()
            {
                Path = "/cluster",
                Password = YiQiDong.Program.Config.Password,
                ServerProgram = nameof(YiQiDong)
            };
            ClusterManager.Instance.HandleServerOptions(serverOptions);
            app.UseQuickProtocol(serverOptions, out server);

            server.ChannelConnected += Server_ChannelConnected;
            server.ChannelDisconnected += Server_ChannelDisconnected;
            server.Start();
            return app;
        }

        private static void Server_ChannelConnected(object sender, QpServerChannel e)
        {
            Console.WriteLine($"{DateTime.Now.ToLongTimeString()}: 通道[{e.ChannelName}]已连接!");
        }

        private static void Server_ChannelDisconnected(object sender, QpServerChannel e)
        {
            Console.WriteLine($"{DateTime.Now.ToLongTimeString()}: 通道[{e.ChannelName}]已断开!");
        }
    }
}
