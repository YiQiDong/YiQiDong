using Quick.Fields;
using Quick.Protocol;

namespace YiQiDong.Core;

public abstract class AbstractSessionFunction : AbstractFunction
{
    protected string SessionId { get; private set; }
    protected QpChannel Channel { get; private set; }
    public override bool HasSession => true;
    public abstract AbstractSessionFunction Create(string sessionId, QpChannel channel);
    public virtual void Start() { }
    public virtual void Stop() { }

    public AbstractSessionFunction(string sessionId, QpChannel channel)
    {
        SessionId = sessionId;
        Channel = channel;
    }

    protected void OnSessionChanged(FieldForGet[] fields)
    {
        if (string.IsNullOrEmpty(SessionId))
            throw new ArgumentNullException(nameof(SessionId));
        if (Channel == null)
            throw new ArgumentNullException(nameof(Channel));
        Channel.SendNoticePackage(new YiQiDong.Protocol.V1.QpNotices.FunctionSessionChangedNotice()
        {
            SessionId = SessionId,
            Items = fields
        });
    }
}
