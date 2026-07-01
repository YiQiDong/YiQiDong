using YiQiDong.Agent;
using YiQiDong.Core;

namespace YiQiDong.TestImage;

public class Agent : AbstractAgent
{
    public override void Init()
    {
        if (AgentContext.IsContainerRuning)
        {
            AddFunction(new Functions.TestFunction());
            AddFunction(new Functions.AutoRefreshTimeFunction());
            AddFunction(new Functions.TaskExecuteFunction());
        }
    }
}
