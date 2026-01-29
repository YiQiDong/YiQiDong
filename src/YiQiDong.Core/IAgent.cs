using Quick.Fields;
using YiQiDong.Core.Protocol.V1.Model;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Core
{
    public interface IAgent
    {
        /// <summary>
        /// 进程名称
        /// </summary>
        string ProcessName { get; }
        /// <summary>
        /// 初始化
        /// </summary>
        void Init();
        /// <summary>
        /// 配置文件功能名称
        /// </summary>
        string ConfigFilesFunctionName { get; }
        /// <summary>
        /// 获取配置文件
        /// </summary>
        /// <returns></returns>
        ConfigFileInfo[] GetConfigFiles();
        /// <summary>
        /// 功能列表已改变事件
        /// </summary>
        event EventHandler FunctionListChanged;
        /// <summary>
        /// 获取功能列表
        /// </summary>
        /// <returns></returns>
        FunctionInfo[] GetFunctionList();
        /// <summary>
        /// 执行功能
        /// </summary>
        /// <returns></returns>
        FieldForGet[] ExecuteFunction(FunctionRequest content);
        /// <summary>
        /// 启动进程
        /// </summary>
        void Start();
        /// <summary>
        /// 停止进程
        /// </summary>
        void Stop();
        /// <summary>
        /// 打开功能Session
        /// </summary>
        /// <param name="functionId">功能编号</param>
        /// <returns></returns>
        string OpenFunctionSession(Quick.Protocol.QpChannel channel, string functionId);
        /// <summary>
        /// 关闭功能Session
        /// </summary>
        /// <param name="sessionId"></param>
        void CloseFunctionSession(string sessionId);
    }
}
