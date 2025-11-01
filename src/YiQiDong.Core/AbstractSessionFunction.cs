using Quick.Fields;
using Quick.Protocol;
using YiQiDong.Agent;
using YiQiDong.Core.Utils;

namespace YiQiDong.Core;

public abstract class AbstractSessionFunction : AbstractFunction
{
    protected string SessionId { get; private set; }
    protected QpChannel Channel { get; private set; }
    public override bool HasSession => true;
    public abstract AbstractSessionFunction Create(string sessionId, QpChannel channel);
    public abstract void Start();
    public abstract void Stop();

    public AbstractSessionFunction() { }
    public AbstractSessionFunction(string sessionId, QpChannel channel)
    {
        SessionId = sessionId;
        Channel = channel;
    }

    protected void OnSessionChanged(FieldForGet[] fields)
    {
        Channel.SendNoticePackage(new YiQiDong.Protocol.V1.QpNotices.FunctionSessionChangedNotice()
        {
            SessionId = SessionId,
            Items = fields
        });
    }
}
