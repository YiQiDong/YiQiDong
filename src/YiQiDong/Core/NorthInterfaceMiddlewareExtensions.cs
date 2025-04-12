using Microsoft.AspNetCore.Builder;
using Quick.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Builder
{
    public static class NorthInterfaceMiddlewareExtensions
    {
        private static Quick.Protocol.WebSocket.Server.AspNetCore.QpWebSocketServer server;
        public static IApplicationBuilder UseNorthInterface(this IApplicationBuilder app)
        {
            app.UseQuickProtocol(new Quick.Protocol.WebSocket.Server.AspNetCore.QpWebSocketServerOptions()
            {
                Path = "/north",
                Password = YiQiDong.Program.Config.Password,
                ServerProgram = nameof(YiQiDong)
            }, out server);

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
