using Quick.Fields;
using Quick.Utils;
using System.Text;
using YiQiDong.Agent;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Core.Functions
{
    public class AppSettingsConfig : AbstractFunction
    {
        private string containerFolder;
        private Func<bool> isReadonlyFunc;

        public override string Name => "配置";
        public Quick.Fields.AppSettings.Model AppSettings { get; private set; }

        private bool isValueChanged<T>(T aValue, T bValue)
        {
            if (aValue == null && bValue == null)
                return false;
            if (aValue == null || bValue == null)
                return true;
            return aValue.Equals(bValue);
        }

        private bool isDictChanged(IDictionary<string, string> aDict, IDictionary<string, string> bDict)
        {
            if (aDict == null && bDict == null)
                return false;
            if (aDict == null || bDict == null)
                return true;
            if (aDict.Count != bDict.Count)
                return true;
            foreach (var aKey in aDict.Keys)
            {
                if (!bDict.ContainsKey(aKey))
                    return true;
                if (isValueChanged(aDict[aKey], bDict[aKey]))
                    return true;
            }
            return false;
        }

        private bool isFieldChanged(FieldForGet aItem, FieldForGet bItem)
        {
            if (aItem.Id == null && bItem.Id == null)
                return false;
            if (isValueChanged(aItem.Description, bItem.Description))
                return true;
            if (isValueChanged(aItem.Id, bItem.Id))
                return true;
            if (isValueChanged(aItem.InputFile_FileFilter, bItem.InputFile_FileFilter))
                return true;
            if (isDictChanged(aItem.InputSelect_Options, bItem.InputSelect_Options))
                return true;
            if (isValueChanged(aItem.InputTextArea_Rows, bItem.InputTextArea_Rows))
                return true;
            if (isValueChanged(aItem.Input_AllowBlank, bItem.Input_AllowBlank))
                return true;
            if (isValueChanged(aItem.Input_ReadOnly, bItem.Input_ReadOnly))
                return true;
            if (isValueChanged(aItem.Input_RegularExpression, bItem.Input_RegularExpression))
                return true;
            if (isValueChanged(aItem.Input_ValidationMessage, bItem.Input_ValidationMessage))
                return true;
            if (isValueChanged(aItem.Name, bItem.Name))
                return true;
            if (isValueChanged(aItem.Pager_PageSize, bItem.Pager_PageSize))
                return true;
            if (isValueChanged(aItem.Pager_RecordCount, bItem.Pager_RecordCount))
                return true;
            if (isValueChanged(aItem.PostOnChanged, bItem.PostOnChanged))
                return true;
            if (isValueChanged(aItem.Type, bItem.Type))
                return true;
            if (isValueChanged(aItem.ColumnWidth, bItem.ColumnWidth))
                return true;
            return false;
        }

        public bool isFieldsChanged(FieldForGet[] aItems, FieldForGet[] bItems)
        {
            if (aItems == null && bItems == null)
                return false;
            if (aItems == null || bItems == null)
                return true;
            if (aItems.Length != bItems.Length)
                return true;
            for (int i = 0; i < aItems.Length; i++)
            {
                var aItem = aItems[i];
                var bItem = bItems[i];
                if (isFieldChanged(aItem, bItem))
                    return true;
                if (isFieldsChanged(aItem.Children, bItem.Children))
                    return true;
            }
            return false;
        }

        //aItems->bItems
        private void copyFieldsValue(FieldForGet[] aItems, FieldForGet[] bItems)
        {
            if (aItems == null || bItems == null)
                return;
            foreach (var aItem in aItems)
            {
                var id = aItem.Id;
                var bItem = bItems.FirstOrDefault(b => b.Id == id);
                if (bItem == null)
                    continue;
                bItem.Value = aItem.Value;
                copyFieldsValue(aItem.Children, bItem.Children);
            }
        }

        private void copyValue(FieldForPost[] newFields, FieldForGet[] oldFields)
        {
            if (newFields == null || oldFields == null)
                return;
            foreach (var newField in newFields)
            {
                var oldField = oldFields.FirstOrDefault(t => t.Id == newField.Id);
                if (oldField == null)
                    continue;
                oldField.Value = newField.Value;
                copyValue(newField.Children, oldField.Children);
            }
        }

        private void travelFieldForGetItems(IEnumerable<FieldForGet> fields, Action<FieldForGet> handler)
        {
            if (fields == null)
                return;
            foreach (var field in fields)
            {
                handler.Invoke(field);
                travelFieldForGetItems(field.Children, handler);
            }
        }

        public AppSettingsConfig(string imageFolder, string containerFolder, Func<bool> isReadonlyFunc)
        {
            this.containerFolder = containerFolder;
            this.isReadonlyFunc = isReadonlyFunc;

            var imageConfigFile = Path.Combine(imageFolder, Quick.Fields.AppSettings.Model.APPSETTINGS_JSON_FILENAME);
            var containerConfigFile = Path.Combine(containerFolder, Quick.Fields.AppSettings.Model.APPSETTINGS_JSON_FILENAME);
            //容器配置文件如果存在
            if (File.Exists(containerConfigFile))
            {
                //检查配置项有没有变更
                var containerAppSettings = Quick.Fields.AppSettings.Model.FromJsonString(File.ReadAllText(containerConfigFile));
                var imageAppSettings = Quick.Fields.AppSettings.Model.FromJsonString(File.ReadAllText(imageConfigFile));

                var fieldsChanged = isFieldsChanged(containerAppSettings.Fields, imageAppSettings.Fields);
                //如果字段已改变
                if (fieldsChanged)
                {
                    copyFieldsValue(containerAppSettings.Fields, imageAppSettings.Fields);
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
                AppSettings = Quick.Fields.AppSettings.Model.FromJsonString(File.ReadAllText(containerConfigFile));
            }
        }

        private List<FieldForGet> innerGet(FunctionRequest request, bool isReadOnly = false)
        {
            var list = Quick.Fields.AppSettings.Model.FromJsonString(AppSettings.ToJsonString()).Fields;
            if (request != null)
                copyValue(request.Fields, list);

            travelFieldForGetItems(list, field => field.Input_ReadOnly = isReadOnly);
            return list.ToList();
        }

        public override FieldForGet[] Execute(FunctionRequest request)
        {
            var isReadOnly = false;
            if (isReadonlyFunc != null)
                isReadOnly = isReadonlyFunc.Invoke();
            var list = innerGet(request, isReadOnly);
            if (request != null)
            {
                if (request.IsFieldIdsMatch("Save"))
                {
                    AppSettings.Fields = list.ToArray();
                    var containerConfigFile = Path.Combine(containerFolder, Quick.Fields.AppSettings.Model.APPSETTINGS_JSON_FILENAME);
                    File.WriteAllText(containerConfigFile, AppSettings.ToJsonString());
                    list.Add(new FieldForGet()
                    {
                        Name = "保存成功！",
                        Description = $"配置文件[{Quick.Fields.AppSettings.Model.APPSETTINGS_JSON_FILENAME}]保存成功！",
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
}
