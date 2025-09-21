using System;
using System.IO;
using System.Text;
using System.ComponentModel.DataAnnotations;
using YiQiDong.Utils;
using YiQiDong.Core.Utils.Unix;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace YiQiDong.Model;

[JsonSerializable(typeof(ConfigModel))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class ConfigModelSerializerContext : JsonSerializerContext { }

/// <summary>
/// 易启动配置模型类
/// </summary>
public class ConfigModel
{
    /// <summary>
    /// 标题
    /// </summary>
    [Required(ErrorMessage = "请输入标题")]
    public string Title { get; set; }
    /// <summary>
    /// URL
    /// </summary>
    [Required(ErrorMessage = "请输入URL")]
    [RegularExpression("^(http://((\\d{1,2}|1\\d\\d|2[0-4]\\d|25[0-5])\\.(\\d{1,2}|1\\d\\d|2[0-4]\\d|25[0-5])\\.(\\d{1,2}|1\\d\\d|2[0-4]\\d|25[0-5])\\.(\\d{1,2}|1\\d\\d|2[0-4]\\d|25[0-5])|\\*)(\\:([0-9]|[1-9]\\d{1,3}|[1-5]\\d{4}|6[0-5]{2}[0-3][0-5])[,;]?)?)+$", ErrorMessage = "URL格式不正确")]
    public string Urls { get; set; }

    /// <summary>
    /// 默认HTML
    /// </summary>
    public string DefaultHtml { get; set; }

    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; }
    /// <summary>
    /// 数据目录
    /// </summary>
    [Required(ErrorMessage = "请输入数据目录")]
    public string DataFolder { get; set; }
    /// <summary>
    /// 容器初始化间隔时间
    /// </summary>
    public int AgentInitInterval { get; set; } = 1000;
    /// <summary>
    /// 容器传输超时时间
    /// </summary>
    public int AgentTransportTimeout { get; set; } = 60000;
    /// <summary>
    /// 环境变量
    /// </summary>
    public string EnvironmentVariables { get; set; }
    /// <summary>
    /// 启动脚本
    /// </summary>
    public string StartScript { get; set; }
    /// <summary>
    /// 停止脚本
    /// </summary>
    public string StopScript { get; set; }

    public static string GetDefaultDataFolder()
    {
        if (OperatingSystem.IsWindows())
        {
            var commonApplicationDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (string.IsNullOrEmpty(commonApplicationDataFolder))
                commonApplicationDataFolder = @"C:\ProgramData";
            return Path.Combine(commonApplicationDataFolder, nameof(YiQiDong), "Data");
        }
        if (UnixUtils.IsRuningWithRoot())
            return $"/var/lib/{nameof(YiQiDong)}";
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), $".var/lib/{nameof(YiQiDong)}");
    }

    public static string GetConfigFile()
    {
        if (OperatingSystem.IsWindows())
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), nameof(YiQiDong));
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return Path.Combine(folder, Consts.CONFIG_JSON_FILENAME);
        }
        else
        {
            string folder;
            if (UnixUtils.IsRuningWithRoot())
                folder = "/etc";
            else
                folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".etc");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return $"{folder}/{nameof(YiQiDong)}.conf";
        }
    }

    /// <summary>
    /// 加载
    /// </summary>
    /// <returns></returns>
    public static ConfigModel Load()
    {
        var templateConfigFile = FolderUtils.GetPathUnderProgramDir(Consts.CONFIG_JSON_FILENAME);
        var templateContent = File.ReadAllText(templateConfigFile);
        var templateModel = JsonSerializer.Deserialize(templateContent, ConfigModelSerializerContext.Default.ConfigModel);
        if (DebugUtils.IsDebug())
        {
            if (string.IsNullOrEmpty(templateModel.DataFolder))
                templateModel.DataFolder = "Data";
            return templateModel;
        }
        var configFile = GetConfigFile();
        if (!File.Exists(configFile))
            File.Copy(templateConfigFile, configFile);
        var content = File.ReadAllText(configFile);
        var model = JsonSerializer.Deserialize(content, ConfigModelSerializerContext.Default.ConfigModel);
        var isModelChanged = false;
        if (string.IsNullOrEmpty(model.DataFolder))
        {
            model.DataFolder = GetDefaultDataFolder();
            isModelChanged = true;
        }
        if (isModelChanged)
            model.Save();
        return model;
    }

    /// <summary>
    /// 保存
    /// </summary>
    public void Save()
    {
        var content = JsonSerializer.Serialize(this, ConfigModelSerializerContext.Default.ConfigModel);
        var configFile = GetConfigFile();
        if (DebugUtils.IsDebug())
            configFile = FolderUtils.GetPathUnderProgramDir(Consts.CONFIG_JSON_FILENAME);
        File.WriteAllText(configFile, content, Encoding.UTF8);
    }

    public ConfigModel Clone()
    {
        var content = JsonSerializer.Serialize(this, ConfigModelSerializerContext.Default.ConfigModel);
        return JsonSerializer.Deserialize(content, ConfigModelSerializerContext.Default.ConfigModel);
    }
}
