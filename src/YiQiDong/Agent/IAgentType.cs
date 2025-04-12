using System;
using YiQiDong.Core;
using YiQiDong.Core.Protocol.V1.Model;

namespace YiQiDong.Agent
{
    public interface IAgentType
    {
        /// <summary>
        /// 初始化
        /// </summary>
        void Init(Action<AbstractFunction> addFunction);
        /// <summary>
        /// 获取配置文件
        /// </summary>
        /// <returns></returns>
        ConfigFileInfo[] GetConfigFiles();
        /// <summary>
        /// 启动
        /// </summary>
        void Start();
        /// <summary>
        /// 停止
        /// </summary>
        void Stop();
    }
}
