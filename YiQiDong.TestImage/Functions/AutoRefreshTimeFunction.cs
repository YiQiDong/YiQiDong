using Quick.Fields;
using Quick.Protocol;
using YiQiDong.Core;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.TestImage.Functions;

public class AutoRefreshTimeFunction : AbstractAutoRefreshFunction
{
    public override string Name => "自动刷新时间功能";
    public AutoRefreshTimeFunction() : base(null, null) { }
    private AutoRefreshTimeFunction(string sessionId, QpChannel channel) : base(sessionId, channel) { }
    public override AbstractSessionFunction Create(string sessionId, QpChannel channel) => new AutoRefreshTimeFunction(sessionId, channel);

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
}
