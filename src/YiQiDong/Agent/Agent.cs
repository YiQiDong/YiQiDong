using Quick.Utils;
using YiQiDong.Core;
using YiQiDong.Core.Protocol.V1.Model;

namespace YiQiDong.Agent
{
    public class Agent : AbstractAgent
    {
        private IAgentType agentType;
        public override void Init()
        {
            var agentTypeName = AgentContext.Container.Image.AgentType;
            if (agentTypeName == null)
                agentTypeName = nameof(AgentTypes.AppSettings);
            switch (agentTypeName)
            {
                case nameof(AgentTypes.TextConfigs):
                    agentType = new AgentTypes.TextConfigs.AgentType();
                    break;
                case nameof(AgentTypes.AppSettings):
                default:
                    agentType = new AgentTypes.AppSettings.AgentType();
                    break;
            }
            base.Init();
            if (AgentContext.IsContainerRuning)
            {
                agentType.Init(AddFunction);
            }
        }

        public override ConfigFileInfo[] GetConfigFiles()
        {
            if (agentType == null)
                throw new ApplicationException("Agent尚未初始化！");
            return agentType.GetConfigFiles();
        }

        public override void Start()
        {
            if (agentType == null)
                throw new ApplicationException("Agent尚未初始化！");
            base.Start();            
            agentType.Start();
        }

        public override void Stop()
        {
            if (agentType == null)
                throw new ApplicationException($"agentType is null,Image.AgentType: {AgentContext.Container.Image.AgentType}");
            try
            {
                agentType.Stop();
            }
            catch(Exception ex)
            {
                AgentContext.LogError("停止容器时出错，原因："+ExceptionUtils.GetExceptionString(ex));
            }
            base.Stop();
        }
    }
}