using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Model
{
    [JsonSerializable(typeof(YqdContainerInfo[]))]
    [JsonSerializable(typeof(YqdContainerInfo))]
    [JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = true)]
    internal partial class YqdContainerInfoSerializerContext : JsonSerializerContext { }

    public class YqdContainerInfo : ContainerInfo
    {
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enable { get; set; } = true;
        /// <summary>
        /// 启动时执行脚本
        /// </summary>
        public string StartScript { get; set; }
        /// <summary>
        /// 启动警告
        /// </summary>
        public string StartWarning { get; set; }
        /// <summary>
        /// 停止时执行脚本
        /// </summary>
        public string StopScript { get; set; }
        /// <summary>
        /// 停止警告
        /// </summary>
        public string StopWarning { get; set; }
        /// <summary>
        /// 启动记录日志
        /// </summary>
        public bool EnableRecordLog { get; set; }
        /// <summary>
        /// 日志保存天数
        /// </summary>
        public int LogSaveDays { get; set; }
        /// <summary>
        /// 启动定时任务表达式
        /// </summary>
        public string StartCron { get; set; }
        /// <summary>
        /// 停止定时任务表达式
        /// </summary>
        public string StopCron { get; set; }
        /// <summary>
        /// 重启定时任务表达式
        /// </summary>
        public string RestartCron { get; set; }
        /// <summary>
        /// 环境变量
        /// </summary>
        public string EnvironmentVariables { get; set; }

        /// <summary>
        /// 是否手动触发容器初始化完成通知。为了兼容老的镜像增加此属性
        /// </summary>
        [JsonIgnore]
        public bool ManualRaiseContainerInitedNotice { get; set; } = false;

        public string ToJsonString()
        {
            return JsonSerializer.Serialize(this, YqdContainerInfoSerializerContext.Default.YqdContainerInfo);
        }

        public static new YqdContainerInfo Parse(string content)
        {
            try { return JsonSerializer.Deserialize(content, YqdContainerInfoSerializerContext.Default.YqdContainerInfo); }
            catch { return null; }
        }

        public IEnumerable<KeyValuePair<string, string>> GetEnvironmentVariables()
        {
            if (string.IsNullOrWhiteSpace(EnvironmentVariables))
                yield break;
            foreach (var line in EnvironmentVariables.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var i = line.IndexOf("=");
                if (i < 0)
                    continue;
                var key = line.Substring(0, i).Trim();
                if (string.IsNullOrWhiteSpace(key))
                    continue;
                var value = line.Substring(i + 1).Trim();
                yield return KeyValuePair.Create(key, value);
            }
        }
    }
}
