using YiQiDong.Agent;

namespace YiQiDong.ArgsHandlers
{
    public partial class Args_Agent
    {
        internal static void Invoke(string[] args)
        {            
            AgentContext.Run<Agent.Agent>(args).Wait();
        }
    }
}
