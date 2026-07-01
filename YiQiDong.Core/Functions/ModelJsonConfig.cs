using Quick.Fields;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Core.Functions
{
    public abstract class ModelJsonConfig<T> : AbstractFunction
        where T : class, new()
    {
        public const string DEFAULT_CONFIG_FILE_NAME = "config.json";

        private string containerFolder;
        private string configFileName;
        private Func<bool> isReadOnlyFunc;
        private JsonTypeInfo<T> jsonTypeInfo;
        protected T Model;
        private Dictionary<Func<FunctionRequest, bool>, Action<FunctionRequest, T, List<FieldForGet>>> requestHandlerDict = new Dictionary<Func<FunctionRequest, bool>, Action<FunctionRequest, T, List<FieldForGet>>>();

        public virtual T ReadConfig()
        {
            var file = Path.Combine(containerFolder, configFileName);
            if (!File.Exists(file))
                return new T();
            return JsonSerializer.Deserialize(File.ReadAllText(file), jsonTypeInfo);
        }

        public virtual void WriteConfig(T model)
        {
            var file = Path.Combine(containerFolder, configFileName);
            File.WriteAllText(file, JsonSerializer.Serialize(model, jsonTypeInfo), Encoding.UTF8);
        }

        public ModelJsonConfig(JsonTypeInfo<T> jsonTypeInfo, string containerFolder, Func<bool> isReadOnlyFunc, string configFileName = DEFAULT_CONFIG_FILE_NAME)
        {
            this.jsonTypeInfo = jsonTypeInfo;
            this.containerFolder = containerFolder;
            this.isReadOnlyFunc = isReadOnlyFunc;
            this.configFileName = configFileName;

            //容器配置文件如果不存在，则创建一份
            if (!File.Exists(Path.Combine(containerFolder, configFileName)))
                WriteConfig(new T());

            RegisterRequestHandler(request => request.IsFieldIdsMatch("Save"), (request, requestModel, list) =>
            {
                try
                {
                    Model = requestModel;
                    WriteConfig(Model);
                    list.Add(new FieldForGet()
                    {
                        Name = "保存成功！",
                        Description = $"配置文件[{configFileName}]保存成功！",
                        Type = FieldType.MessageBox
                    });
                }
                catch (Exception ex)
                {
                    list.Add(new FieldForGet()
                    {
                        Name = "错误",
                        Description = $"保存配置时出错，原因：{ex.Message}",
                        Type = FieldType.Alert,
                        Input_ReadOnly = true
                    });
                }
            });
        }

        /// <summary>
        /// 注册请求处理器
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="handler"></param>
        protected void RegisterRequestHandler(Func<FunctionRequest, bool> condition, Action<FunctionRequest, T, List<FieldForGet>> handler)
        {
            requestHandlerDict[condition] = handler;
        }

        protected void addSaveButton(List<FieldForGet> list)
        {
            list.Add(new FieldForGet() { Id = "Save", Name = "保存", Type = FieldType.Button });
        }

        protected abstract List<FieldForGet> innerGet(FunctionRequest request, T requestModel, bool isReadOnly = false);

        public override FieldForGet[] Execute(FunctionRequest request)
        {
            if (request == null)
                Model = ReadConfig();
            var isReadOnly = isReadOnlyFunc();
            var requestModel = request?.Convert<T>(jsonTypeInfo);
            var list = innerGet(request, requestModel, isReadOnly);
            if (request != null)
            {
                foreach (var item in requestHandlerDict)
                {
                    var condition = item.Key;
                    var handler = item.Value;
                    if (!condition(request))
                        continue;
                    handler(request, requestModel, list);
                    break;
                }
            }
            if (!isReadOnly)
                addSaveButton(list);
            return list.ToArray();
        }
    }
}
