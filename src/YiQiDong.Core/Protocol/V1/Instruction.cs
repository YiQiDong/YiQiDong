using Quick.Protocol;

namespace YiQiDong.Protocol.V1
{
    public class Instruction
    {
        public static QpInstruction Instance = new QpInstruction()
        {
            Id = typeof(Instruction).Namespace,
            Name = "弈启动协议V1",
            CommandInfos = new QpCommandInfo[]
            {
                QpCommandInfo.Create(new QpCommands.Register.Request()),
                QpCommandInfo.Create(new QpCommands.Using.Request()),
                QpCommandInfo.Create(new QpCommands.GetFunctionList.Request()),
                QpCommandInfo.Create(new QpCommands.GetConfigFileList.Request()),
                QpCommandInfo.Create(new QpCommands.ExecuteFunction.Request()),
                QpCommandInfo.Create(new QpCommands.Start.Request()),
                QpCommandInfo.Create(new QpCommands.Stop.Request()),
                QpCommandInfo.Create(new QpCommands.AddReverseProxyRule.Request()),
                QpCommandInfo.Create(new QpCommands.Exit.Request())
            },
            NoticeInfos = new QpNoticeInfo[]
            {
                QpNoticeInfo.Create(new QpNotices.ContainerLogNotice()),
                QpNoticeInfo.Create(new QpNotices.FunctionListChangedNotice()),
                QpNoticeInfo.Create(new QpNotices.ContainerInitedNotice()),
                QpNoticeInfo.Create(new QpNotices.ContainerStartedNotice()),
                QpNoticeInfo.Create(new QpNotices.ContainerStopedNotice())
            }
        };
    }
}
