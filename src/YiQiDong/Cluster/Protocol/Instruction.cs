using Quick.Protocol;

namespace YiQiDong.Cluster.Protocol
{
    public class Instruction
    {
        public static QpInstruction Instance = new QpInstruction()
        {
            Id = typeof(Instruction).Namespace,
            Name = "易启动集群协议V1",
            CommandInfos = new QpCommandInfo[]
            {
                QpCommandInfo.Create(new QpCommands.CreateCluster.Request()),
                QpCommandInfo.Create(new QpCommands.DeleteCluster.Request()),
                QpCommandInfo.Create(new QpCommands.UpdateConfig.Request()),
                QpCommandInfo.Create(new QpCommands.GetContainerList.Request()),
                QpCommandInfo.Create(new QpCommands.StartCluster.Request()),
                QpCommandInfo.Create(new QpCommands.StopCluster.Request())
            }
        };
    }
}
