using Quick.Utils;

namespace YiQiDong.Agent.CommandExecuters
{
    public class Exit
    {
        public static Protocol.V1.QpCommands.Exit.Response Execute(Quick.Protocol.QpChannel channel, Protocol.V1.QpCommands.Exit.Request request)
        {
            //停止代理上下文
            try
            {
                AgentContext.Stop();
            }
            catch (Exception ex)
            {
                AgentContext.LogError($"停止容器上下文时出错，原因：{ExceptionUtils.GetExceptionString(ex)}");
            }
            //等待1秒，退出容器进程
            Task.Delay(1000).ContinueWith(t => AgentContext.Dispose());
            return new Protocol.V1.QpCommands.Exit.Response();
        }
    }
}
