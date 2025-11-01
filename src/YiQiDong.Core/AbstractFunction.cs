using Quick.Fields;
using Quick.Protocol.Utils;
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

    public virtual FieldForGet[] Execute(FunctionRequest request)
    {
        try
        {
            if (request.FieldIds == null || request.FieldIds.Length == 0)
                return Get();
            else
                return Post(request);
        }
        catch (Exception ex)
        {
            return new FieldForGet[]
            {
                    new FieldForGet(){ Name="错误",Description = ExceptionUtils.GetExceptionString( ex), Type= FieldType.Alert }
            };
        }
    }

    public virtual FieldForGet[] Get() => new FieldForGet[0];
    public virtual FieldForGet[] Post(FunctionRequest request) => new FieldForGet[0];
}