using Quick.Fields;
using System;
using System.Collections.Generic;
using System.Linq;
using YiQiDong.Agent;
using YiQiDong.Core.Protocol.V1.Model;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Core
{
    public abstract class AbstractAgent : IAgent
    {
        public const string DEFAULT_PROCESS_NAME = "YiQiDong:Agent";
        private Dictionary<string, AbstractFunction> agentStartFunctionDict = new Dictionary<string, AbstractFunction>();
        private Dictionary<string, AbstractFunction> agentStopFunctionDict = new Dictionary<string, AbstractFunction>();
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
        }

        public virtual FunctionInfo[] GetFunctionList()
        {
            if (AgentContext.Container.AutoStart)
                return agentStartFunctionDict.Values.Select(t => t.Info).ToArray();
            else
                return agentStopFunctionDict.Values.Select(t => t.Info).ToArray();
        }

        public virtual FieldForGet[] ExecuteFunction(FunctionRequest request)
        {
            var functionId = request.FunctionId;

            if (string.IsNullOrEmpty(functionId))
                throw new ArgumentNullException(nameof(request.FunctionId));

            AbstractFunction function = null;
            if (AgentContext.Container.AutoStart)
            {
                if (!agentStartFunctionDict.ContainsKey(functionId))
                    throw new ApplicationException($"未找到编号为[{functionId}]的功能。");
                function = agentStartFunctionDict[functionId];
            }
            else
            {
                if (!agentStopFunctionDict.ContainsKey(functionId))
                    throw new ApplicationException($"未找到编号为[{functionId}]的功能。");
                function = agentStopFunctionDict[functionId];
            }

            try
            {
                return function.Execute(request);
            }
            catch (Exception ex)
            {
                return new FieldForGet[]
                {
                    new FieldForGet(){ Name="未处理错误",Description=ex.ToString(), Type = FieldType.Alert }
                };
            }
        }
    }
}
