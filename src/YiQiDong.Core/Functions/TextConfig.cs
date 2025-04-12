using Quick.Fields;
using System;
using System.Collections.Generic;
using System.IO;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Core.Functions
{
    public class TextConfig : AbstractFunction
    {
        private string name;
        private string file;
        private Func<bool> isReadonlyFunc;

        public override string Id => name;
        public override string Name => name;

        private const string CONTENT_KEY = "Content";

        public TextConfig(string name, string file,Func<bool> isReadonlyFunc)
        {
            this.name = name;
            this.file = file;
            this.isReadonlyFunc = isReadonlyFunc;
        }

        private List<FieldForGet> innerGet(FunctionRequest request, bool isReadOnly = false)
        {
            List<FieldForGet> list = new List<FieldForGet>();
            if (!File.Exists(file))
            {
                list.Add(new FieldForGet() { Name = "失败", Description = $"配置文件[{file}]不存在！", Input_ReadOnly = true, Type = FieldType.Alert });
                return list;
            }
            string tmpKey;
            tmpKey = CONTENT_KEY;
            list.Add(new FieldForGet()
            {
                Id = tmpKey,
                Name = "内容",
                Type = FieldType.InputTextArea,
                Input_ReadOnly = isReadOnly,
                Value = request == null ? File.ReadAllText(file) : request.GetFieldValue(tmpKey),
                Input_AllowBlank = false,
                Description = "配置文件的内容"
            });
            return list;
        }

        public override FieldForGet[] Get()
        {
            var isReadOnly = isReadonlyFunc();
            var list = innerGet(null, isReadOnly);
            if (!isReadOnly)
                addSaveButton(list);
            return list.ToArray();
        }

        public override FieldForGet[] Post(FunctionRequest request)
        {
            var list = innerGet(request);
            if (request.IsFieldIdsMatch("Save"))
            {

                if (File.Exists(file))
                {
                    File.WriteAllText(file, request.GetFieldValue(CONTENT_KEY));
                    list.Add(new FieldForGet()
                    {
                        Name = "保存成功",
                        Description = $"配置文件[{file}]保存成功！",
                        Type = FieldType.MessageBox
                    });
                }
                else
                {
                    list.Add(new FieldForGet()
                    {
                        Name = "错误",
                        Description = $"配置文件[{file}]不存在！",
                        Type = FieldType.Alert,
                        Input_ReadOnly = true
                    });
                }
                addSaveButton(list);
            }
            return list.ToArray();
        }

        private void addSaveButton(List<FieldForGet> list)
        {
            list.Add(new FieldForGet() { Id = "Save", Name = "保存", Type = FieldType.Button });
        }
    }
}