using Quick.Fields;
using System.Text;
using YiQiDong.Core;
using YiQiDong.Core.Utils;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Agent.AgentTypes.AppSettings;

public class ConfigFunction : AbstractFunction
{
    public const string APPSETTINGS_FILE = "appsettings.json";
    private string containerFolder;
    public static ConfigFunction Instance { get; private set; }
    public override string Name => "配置";
    public Model AppSettings { get; private set; }

    private Dictionary<string, string> oldVersionJsonFileReplaceDict = new Dictionary<string, string>()
    {
        ["\"GroupBox\""] = "\"ContainerGroup\"",
        ["\"ReadOnly\""] = "\"Input_ReadOnly\"",
        ["\"AllowBlank\""] = "\"Input_AllowBlank\"",
        ["\"RegularExpression\""] = "\"Input_RegularExpression\"",
        ["\"ValidationMessage\""] = "\"Input_ValidationMessage\"",
        ["\"Options\""] = "\"InputSelect_Options\"",
    };

    /// <summary>
    /// 升级老版本JSON文件内容
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    private string handleUpgradeOldVersionJsonFile(string content)
    {
        var sb = new StringBuilder(content);
        foreach (var key in oldVersionJsonFileReplaceDict.Keys)
        {
            var value = oldVersionJsonFileReplaceDict[key];
            sb = sb.Replace(key, value);
        }
        return sb.ToString();
    }

    private string handleDowngradeNewVersionJsonFile(string content)
    {
        var sb = new StringBuilder(content);
        foreach (var key in oldVersionJsonFileReplaceDict.Keys)
        {
            var value = oldVersionJsonFileReplaceDict[key];
            sb = sb.Replace(value, key);
        }
        return sb.ToString();
    }

    public ConfigFunction(string imageFolder, string containerFolder)
    {
        Instance = this;
        this.containerFolder = containerFolder;

        var imageConfigFile = Path.Combine(imageFolder, APPSETTINGS_FILE);
        var containerConfigFile = Path.Combine(containerFolder, APPSETTINGS_FILE);
        //容器配置文件如果存在
        if (File.Exists(containerConfigFile))
        {
            //检查配置项有没有变更
            var containerAppSettingsJson = handleUpgradeOldVersionJsonFile(File.ReadAllText(containerConfigFile));
            var containerAppSettings = Model.FromJsonString(containerAppSettingsJson);
            var imageAppSettingsJson = handleUpgradeOldVersionJsonFile(File.ReadAllText(imageConfigFile));
            var imageAppSettings = Model.FromJsonString(imageAppSettingsJson);

            var fieldsChanged = false;

            //如果字段数量不匹配，则配置项已改变
            if (containerAppSettings.Fields.Length != imageAppSettings.Fields.Length)
            {
                fieldsChanged = true;
            }
            else
            {
                foreach (var field in imageAppSettings.Fields)
                {
                    //如果编号、名称、描述、类型都没有变化，则检查下一个字段
                    if (containerAppSettings.Fields.Any(
                        t =>
                            (string.IsNullOrEmpty(t.Id) || t.Id == field.Id)
                            && (string.IsNullOrEmpty(t.Name) || t.Name == field.Name)
                            && (string.IsNullOrEmpty(t.Description) || t.Description == field.Description)
                            && t.Type == field.Type)
                            )
                        continue;
                    //已经改变
                    fieldsChanged = true;
                    break;
                }
            }
            //如果字段已改变
            if (fieldsChanged)
            {
                //复制字段的值
                foreach (var field in imageAppSettings.Fields)
                {
                    if (string.IsNullOrEmpty(field.Id))
                        continue;
                    var containerField = containerAppSettings.Fields.FirstOrDefault(t => !string.IsNullOrEmpty(t.Id) && t.Id == field.Id);
                    if (containerField == null)
                        continue;
                    field.Value = containerField.Value;
                }
                AppSettings = imageAppSettings;
                //先写入到临时文件
                var tmpFile = Path.GetTempFileName();
                try
                {
                    File.WriteAllText(tmpFile, AppSettings.ToJsonString(), Encoding.UTF8);
                    var fileInfo = new FileInfo(tmpFile);
                    if (!fileInfo.Exists)
                        throw new IOException($"临时配置文件[{tmpFile}]丢失");
                    if (fileInfo.Length <= 0)
                        throw new IOException($"写入的临时配置文件[{tmpFile}]大小为0");
                }
                catch (Exception ex)
                {
                    AgentContext.LogError($"写入临时配置文件[{tmpFile}]时出错，原因：{ExceptionUtils.GetExceptionMessage(ex)}");
                    throw;
                }
                //尝试3次替换配置文件
                for (var i = 0; i < 3; i++)
                {
                    try
                    {
                        if (File.Exists(containerConfigFile))
                            File.Delete(containerConfigFile);
                        if (File.Exists(tmpFile))
                            File.Move(tmpFile, containerConfigFile);
                        break;
                    }
                    catch { }
                }
                if (File.Exists(tmpFile))
                {
                    AgentContext.LogError($"更新配置文件[{containerConfigFile}]时出错，请尝试手动替换配置文件。临时配置文件路径：{tmpFile}");
                }
            }
            //如果字段没有改变
            else
            {
                AppSettings = containerAppSettings;
            }
        }
        //容器配置文件如果不存在，则从镜像目录复制一份
        else
        {
            var folder = Path.GetDirectoryName(containerConfigFile);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            if (File.Exists(imageConfigFile))
                File.Copy(imageConfigFile, containerConfigFile, true);
            AppSettings = Model.FromJsonString(handleUpgradeOldVersionJsonFile(File.ReadAllText(containerConfigFile)));
        }
    }

    private List<FieldForGet> innerGet(FunctionRequest request, bool isReadOnly = false)
    {
        var list = Model.FromJsonString(AppSettings.ToJsonString()).Fields;
        if (request != null)
            foreach (var fieldForPost in request.Fields)
            {
                var field = list.FirstOrDefault(t => t.Id == fieldForPost.Id);
                if (field == null)
                    continue;
                field.Value = fieldForPost.Value;
            }
        foreach (var field in list)
            field.Input_ReadOnly = isReadOnly;
        return list.ToList();
    }

    public override FieldForGet[] Execute(FunctionRequest request)
    {
        var isReadOnly = AgentContext.Container.AutoStart;
        var list = innerGet(request, isReadOnly);
        if (request != null)
        {
            if (request.IsFieldIdsMatch("Save"))
            {
                AppSettings.Fields = list.ToArray();
                var containerConfigFile = Path.Combine(containerFolder, APPSETTINGS_FILE);
                File.WriteAllText(containerConfigFile, handleDowngradeNewVersionJsonFile(AppSettings.ToJsonString()));
                list.Add(new FieldForGet()
                {
                    Name = "保存成功！",
                    Description = $"配置文件[{APPSETTINGS_FILE}]保存成功！",
                    Type = FieldType.MessageBox
                });
            }
        }
        if (!isReadOnly)
            addSaveButton(list);
        return list.ToArray();
    }

    private void addSaveButton(List<FieldForGet> list)
    {
        list.Add(new FieldForGet() { Id = "Save", Name = "保存", Type = FieldType.Button });
    }
}