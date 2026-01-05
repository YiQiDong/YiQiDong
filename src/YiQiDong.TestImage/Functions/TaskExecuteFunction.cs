using Quick.Fields;
using Quick.Protocol;
using YiQiDong.Agent;
using YiQiDong.Core;
using YiQiDong.Core.Utils;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.TestImage.Functions;

public class TaskExecuteFunction : AbstractSessionFunction
{
    public override string Name => "任务执行功能";
    public TaskExecuteFunction() : base(null, null) { }
    private TaskExecuteFunction(string sessionId, QpChannel channel) : base(sessionId, channel) { }
    public override AbstractSessionFunction Create(string sessionId, QpChannel channel) => new TaskExecuteFunction(sessionId, channel);

    private CancellationTokenSource cts;

    public override void Start() { }

    public override void Stop()
    {
        cts?.Cancel();
    }

    public override FieldForGet[] Execute(FunctionRequest request)
    {
        if (request != null)
        {
            if (request.IsFieldIdsMatch("btnStart"))
            {
                AgentContext.LogInfo("开始执行任务...");
                cts?.Cancel();
                cts = new();
                var token = cts.Token;
                Task.Run(async () =>
                {
                    try
                    {
                        for (var i = 0; i < 10; i++)
                        {
                            AgentContext.LogInfo($"任务进度：{i + 1}/10");
                            if (token.IsCancellationRequested)
                                return;
                            OnSessionChanged(
                            [
                                new ()
                            {
                                Type = FieldType.Alert,
                                Name = "正在执行任务中。。。",
                                Description = $"进度：{i+1}/10"
                            },
                            new ()
                            {
                                Id="btnCancel",
                                Name = "取消",
                                Type = FieldType.Button
                            }
                            ]);
                            await Task.Delay(1000, token);
                        }
                        AgentContext.LogInfo("任务执行完成.");
                        OnSessionChanged(Execute(null));
                    }
                    catch(OperationCanceledException)
                    {
                        AgentContext.LogInfo("任务执行已取消");
                    }
                    catch (Exception ex)
                    {
                        AgentContext.LogError("任务执行时出错，原因：" + ExceptionUtils.GetExceptionString(ex));
                    }
                });
                return [];
            }
            else if (request.IsFieldIdsMatch("btnCancel"))
            {
                cts?.Cancel();
                AgentContext.LogInfo("任务已取消.");
            }
        }
        return [
            new ()
            {
                Id="btnStart",
                Name = "开始",
                Type = FieldType.Button
            }
        ];
    }
}
