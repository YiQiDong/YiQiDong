using Quick.Fields;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Core;

public abstract class AbstractFunction
{
    public virtual string Id => Name;
    public abstract string Name { get; }
    public virtual bool HasSession { get; } = false;
    /// <summary>
    /// 执行超时时间（单位：毫秒）
    /// </summary>
    public virtual int ExecuteTimeout => 30000;

    public virtual FunctionInfo Info => new FunctionInfo() { Id = Id, Name = Name, HasSession = HasSession, ExecuteTimeout = ExecuteTimeout };

    public virtual FieldForGet[] Execute(FunctionRequest request) => [];
}