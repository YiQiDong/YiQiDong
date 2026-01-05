using Quick.Fields;
using Quick.Protocol;
using YiQiDong.Core;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.TestImage.Functions;

public class AutoRefreshTimeFunction : AbstractSessionFunction
{
    public override string Name => "自动刷新时间功能";
    public AutoRefreshTimeFunction() : base(null, null) { }
    private AutoRefreshTimeFunction(string sessionId, QpChannel channel) : base(sessionId, channel) { }
    public override AbstractSessionFunction Create(string sessionId, QpChannel channel) => new AutoRefreshTimeFunction(sessionId, channel);

    private CancellationTokenSource cts;

    public override FieldForGet[] Execute(FunctionRequest request)
    {
        return
        [
            new ()
            {
                Id="txtCurrentTime",
                Name = "当前时间",
                Type = FieldType.InputText,
                Input_ReadOnly = true,
                Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            }
        ];
    }

    public override void Start()
    {
        cts = new();
        _ = beginShowTime(cts.Token);
    }

    private async Task beginShowTime(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);
            OnSessionChanged(Execute(null));
        }
    }

    public override void Stop()
    {
        cts.Cancel();
    }
}
