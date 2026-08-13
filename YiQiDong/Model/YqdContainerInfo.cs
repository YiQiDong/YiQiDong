using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Model
{
    [JsonSerializable(typeof(YqdContainerInfo[]))]
    [JsonSerializable(typeof(YqdContainerInfo))]
    public partial class YqdContainerInfoSerializerContext : JsonSerializerContext
    {
        public static YqdContainerInfoSerializerContext Default2 { get; } = new YqdContainerInfoSerializerContext(new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    public class YqdContainerInfo : ContainerInfo
    {
        /// <summary>
        /// 是否启用
        /// </summary>
        public virtual bool Enable { get; set; } = true;
        /// <summary>
        /// 启动时执行脚本
        /// </summary>
        public virtual string StartScript { get; set; }
        /// <summary>
        /// 启动警告
        /// </summary>
        public virtual string StartWarning { get; set; }
        /// <summary>
        /// 停止时执行脚本
        /// </summary>
        public virtual string StopScript { get; set; }
        /// <summary>
        /// 停止警告
        /// </summary>
        public virtual string StopWarning { get; set; }
        /// <summary>
        /// 启动记录日志
        /// </summary>
        public virtual bool EnableRecordLog { get; set; }
        /// <summary>
        /// 日志保存天数
        /// </summary>
        public virtual int LogSaveDays { get; set; }
        /// <summary>
        /// 启动定时任务表达式
        /// </summary>
        public virtual string StartCron { get; set; }
        /// <summary>
        /// 停止定时任务表达式
        /// </summary>
        public virtual string StopCron { get; set; }
        /// <summary>
        /// 重启定时任务表达式
        /// </summary>
        public virtual string RestartCron { get; set; }
        /// <summary>
        /// 环境变量
        /// </summary>
        public virtual string EnvironmentVariables { get; set; }

        public string ToJsonString()
        {
            return JsonSerializer.Serialize(this, YqdContainerInfoSerializerContext.Default2.YqdContainerInfo);
        }

        public YqdContainerInfo Clone() => (YqdContainerInfo)MemberwiseClone();

        public static new YqdContainerInfo Parse(string content)
        {
            try { return JsonSerializer.Deserialize(content, YqdContainerInfoSerializerContext.Default2.YqdContainerInfo); }
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
