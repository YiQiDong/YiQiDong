using System;
using System.Collections.Generic;
using System.Text;

namespace YiQiDong.Protocol.V1.Model
{
    /// <summary>
    /// 功能信息
    /// </summary>
    public class FunctionInfo
    {
        /// <summary>
        /// 编号
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 执行超时时间(单位：毫秒)
        /// </summary>
        public int ExecuteTimeout { get; set; } = 30000;
    }
}
