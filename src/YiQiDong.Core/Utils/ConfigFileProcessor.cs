using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace YiQiDong.Core.Utils
{
    public class ConfigFileProcessor
    {
        public static ConfigFileProcessor Default { get; } = new ConfigFileProcessor(Environment.CurrentDirectory);
        public string ConfigFolder { get; private set; }

        public ConfigFileProcessor(string configFolder)
        {
            ConfigFolder = configFolder;
        }

        private string getTypeFilePath(Type type, string fileSuffix)
        {
            var fileName = type.FullName;
            if (!string.IsNullOrEmpty(fileSuffix))
                fileName += "." + fileSuffix;
            fileName += ".json";
            return Path.Combine(ConfigFolder, fileName);
        }

        public string GetTypeFilePath<T>(string fileSuffix)
        {
            return getTypeFilePath(typeof(T), fileSuffix);
        }

        public T Load<T>(JsonTypeInfo<T> jsonTypeInfo, string fileSuffix = null)
        {
            try
            {
                var file = GetTypeFilePath<T>(fileSuffix);
                if (!File.Exists(file))
                    return default;
                var content = File.ReadAllText(file);
                return JsonSerializer.Deserialize(content, jsonTypeInfo);
            }
            catch
            {
                return default;
            }
        }

        public void Save<T>(T configObj, JsonTypeInfo<T> jsonTypeInfo, string fileSuffix = null)
        {
            if (configObj == null)
                return;

            var file = getTypeFilePath(typeof(T), fileSuffix);
            if (File.Exists(file))
                File.Delete(file);
            var content = JsonSerializer.Serialize(configObj, jsonTypeInfo);
            File.WriteAllText(file, content, Encoding.UTF8);
        }
    }
}
