using YiQiDong.Core.Protocol.V1.Model;

namespace YiQiDong.Core.Utils;

public class ContainerConfigFileUtils
{
    public static ConfigFileInfo[] GetConfigFiles(string[] configFolders, Dictionary<string, string> configFiles, string configFileEncoding = "UTF-8")
    {
        var list = new List<ConfigFileInfo>();
        if (configFolders != null)
            foreach (var folder in configFolders)
            {
                var folderLastName = Path.GetFileName(folder);
                foreach (var file in Directory.GetFiles(folder))
                {
                    var name = $"{folderLastName}/{file.Substring(folder.Length + 1)}";
                    list.Add(new ConfigFileInfo()
                    {
                        Name = name,
                        FilePath = file,
                        FileEncoding = configFileEncoding
                    });
                }
            }
        if (configFiles != null)
            foreach (var item in configFiles)
            {
                var file = item.Key;
                list.Add(new ConfigFileInfo()
                {
                    Name = item.Value,
                    FilePath = file,
                    FileEncoding = configFileEncoding
                });
            }
        return list.ToArray();
    }
}
