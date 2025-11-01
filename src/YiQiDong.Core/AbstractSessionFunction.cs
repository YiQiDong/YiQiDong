namespace YiQiDong.Core;

public abstract class AbstractSessionFunction : AbstractFunction
{
    public override bool HasSession => true;
    public abstract AbstractSessionFunction Create(Quick.Protocol.QpChannel channel);
    public abstract void Start();
    public abstract void Stop();
}
