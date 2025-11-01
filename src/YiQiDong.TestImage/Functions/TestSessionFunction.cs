using System;
using Quick.Fields;
using Quick.Protocol;
using YiQiDong.Agent;
using YiQiDong.Core;

namespace YiQiDong.TestImage.Functions;

public class TestSessionFunction : AbstractSessionFunction
{
    public override string Name => "测试Session功能";
    public TestSessionFunction() { }
    public TestSessionFunction(string sessionId, QpChannel channel) : base(sessionId, channel) { }
    public override AbstractSessionFunction Create(string sessionId,QpChannel channel) => new TestSessionFunction(sessionId,channel);

    private CancellationTokenSource cts;

    public override void Start()
    {
        AgentContext.LogInfo($"Session[{SessionId}]已启动");
        cts = new();
        _ = beginShowTime(cts.Token);
    }
    
    

    private async Task beginShowTime(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            OnSessionChanged([
                new ()
                {
                    Id="txtCurrentTime",
                    Name = "当前时间",
                    Type = FieldType.InputText,
                    Input_ReadOnly = true,
                    Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                },
                new ()
                {
                    Id="btnRefresh",
                    Name = "刷新",
                    Type = FieldType.Button
                }
            ]);
            await Task.Delay(1000, cancellationToken);
        }
    }

    public override void Stop()
    {
        cts.Cancel();
        AgentContext.LogInfo($"Session[{SessionId}]已停止");
    }
}
