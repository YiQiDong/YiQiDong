using Quick.Fields;
using YiQiDong.Agent;
using YiQiDong.Core.Protocol.V1.Model;
using YiQiDong.Core.Utils;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Core;

public abstract class AbstractAgent : IAgent
{
    public const string DEFAULT_PROCESS_NAME = "YiQiDong:Agent";
    private Dictionary<string, AbstractFunction> agentStartFunctionDict = new Dictionary<string, AbstractFunction>();
    private Dictionary<string, AbstractFunction> agentStopFunctionDict = new Dictionary<string, AbstractFunction>();
    private Dictionary<string, AbstractSessionFunction> sessionFunctionDict = new Dictionary<string, AbstractSessionFunction>();

    public virtual string ProcessName => DEFAULT_PROCESS_NAME;
    public virtual ConfigFileInfo[] GetConfigFiles() => null;
    public event EventHandler FunctionListChanged;
    protected void RaiseEvent_FunctionListChanged()
    {
        FunctionListChanged?.Invoke(this, EventArgs.Empty);
    }

    public virtual void Init()
    {
        agentStartFunctionDict.Clear();
        agentStopFunctionDict.Clear();
    }

    protected void AddFunction(AbstractFunction function)
    {
        AddFunction(function, null);
    }

    protected void AddFunction(AbstractFunction function, bool? agentStartVisiable)
    {
        if (agentStartVisiable == null)
        {
            agentStartFunctionDict[function.Id] = function;
            agentStopFunctionDict[function.Id] = function;
        }
        else if (agentStartVisiable.Value)
        {
            agentStartFunctionDict[function.Id] = function;
        }
        else
        {
            agentStopFunctionDict[function.Id] = function;
        }
    }

    public virtual void Start()
    {
        RaiseEvent_FunctionListChanged();
    }

    public virtual void Stop()
    {
        RaiseEvent_FunctionListChanged();
        AbstractSessionFunction[] sessionFunctions = null;
        lock (sessionFunctionDict)
        {
            sessionFunctions = sessionFunctionDict.Values.ToArray();
            sessionFunctionDict.Clear();
        }
        foreach (var function in sessionFunctions)
            function.Stop();
    }

    public virtual FunctionInfo[] GetFunctionList()
    {
        if (AgentContext.Container.AutoStart)
            return agentStartFunctionDict.Values.Select(t => t.Info).ToArray();
        else
            return agentStopFunctionDict.Values.Select(t => t.Info).ToArray();
    }

    private AbstractFunction GetFunction(string functionId)
    {
        if (string.IsNullOrEmpty(functionId))
            throw new ArgumentNullException(nameof(functionId));

        if (AgentContext.Container.AutoStart)
        {
            if (!agentStartFunctionDict.TryGetValue(functionId, out var function))
                throw new ApplicationException($"未找到编号为[{functionId}]的功能。");
            return function;
        }
        else
        {
            if (!agentStopFunctionDict.TryGetValue(functionId, out var function))
                throw new ApplicationException($"未找到编号为[{functionId}]的功能。");
            return function;
        }
    }

    private AbstractSessionFunction GetSessionFunction(string functionId)
    {
        var session = GetFunction(functionId) as AbstractSessionFunction;
        if (session == null)
            throw new ApplicationException($"未找到编号为[{functionId}]的功能。");
        return session;
    }

    public virtual FieldForGet[] ExecuteFunction(FunctionRequest request)
    {
        AbstractFunction function = null;
        if (string.IsNullOrEmpty(request.SessionId))
            function = GetFunction(request.FunctionId);
        else
            lock (sessionFunctionDict)
            {
                if (!sessionFunctionDict.TryGetValue(request.SessionId, out var tmpFunction))
                    throw new ApplicationException($"未找到SessionId编号为[{request.SessionId}]的功能。");
                function = tmpFunction;
            }
        try
        {
            return function.Execute(request);
        }
        catch (Exception ex)
        {
            return
            [
                new FieldForGet(){ Name="错误",Description=ExceptionUtils.GetExceptionString(ex), Type = FieldType.Alert }
            ];
        }
    }

    public string OpenFunctionSession(Quick.Protocol.QpChannel channel, string functionId)
    {
        var function = GetSessionFunction(functionId);
        var sessionId = Guid.NewGuid().ToString("N");
        var functionInstance = function.Create(sessionId, channel);

        lock (sessionFunctionDict)
            sessionFunctionDict[sessionId] = functionInstance;

        Task.Delay(100).ContinueWith(t => functionInstance.Start());
        return sessionId;
    }

    public void CloseFunctionSession(string sessionId)
    {
        AbstractSessionFunction functionInstance;
        lock (sessionFunctionDict)
        {
            if (!sessionFunctionDict.TryGetValue(sessionId, out functionInstance))
                return;
            sessionFunctionDict.Remove(sessionId);
        }
        functionInstance.Stop();
    }
}
