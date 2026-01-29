using Quick.Fields;
using YiQiDong.Core;
using YiQiDong.Core.Utils;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Agent.AgentTypes.TextConfigs.Functions;

internal class SendCommandFunction : AbstractFunction
{
    private const string TXT_COMMAND = nameof(TXT_COMMAND);
    private const string BTN_SEND = nameof(BTN_SEND);
    private AgentType agentType;

    public SendCommandFunction(AgentType agentType)
    {
        this.agentType = agentType;
    }

    public override string Name => "发送命令";
    public override bool IsVisiable() => AgentContext.Container.AutoStart;

    public override FieldForGet[] Execute(FunctionRequest request)
    {
        var isReadOnly = !AgentContext.Container.AutoStart;
        var cmd = request!=null?request.GetFieldValue(TXT_COMMAND):null;
        var list = new List<FieldForGet>()
        {
            new ()
            {
                Id=TXT_COMMAND,
                Name="命令",
                Type = FieldType.InputText,
                Input_ReadOnly = isReadOnly,
                Value = cmd
            }
        };
        if (!isReadOnly)
            list.Add(new()
            {
                Id = BTN_SEND,
                Name = "发送",
                Type = FieldType.Button,
                Input_ReadOnly = isReadOnly
            });
        if (request != null && request.IsFieldIdsMatch(BTN_SEND))
        {
            try
            {
                if (string.IsNullOrEmpty(cmd))
                    throw new ArgumentNullException("未输入要发送的命令！");
                agentType.SendCommand(cmd);
                list.Add(new()
                {
                    Name = "信息",
                    Description = $"发送命令[{cmd}]成功！",
                    Type = FieldType.MessageBox,
                    Input_ReadOnly = true
                });
            }
            catch (Exception ex)
            {
                list.Add(new()
                {
                    Name = "错误",
                    Description = $"发送命令时出错，原因：{ExceptionUtils.GetExceptionMessage(ex)}",
                    Type = FieldType.MessageBox,
                    Input_ReadOnly = true
                });
            }
        }
        return list.ToArray();
    }
}
