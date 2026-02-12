using Quick.Build;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using YiQiDong;
using YiQiDong.Protocol.V1.Model;

var appFolder = QbFolder.GetAppFolder();
if (appFolder == Environment.CurrentDirectory)
    Environment.CurrentDirectory = Path.GetFullPath("../../../../../");

//准备目录变量
var baseFolder = Environment.CurrentDirectory;

Console.WriteLine("----------------------------------");
Console.WriteLine("  欢迎使用易启动编译脚本");
Console.WriteLine("----------------------------------");

Console.WriteLine("请选择编译类型：");

var selectedBuildType = QbSelect.ArrowSelect(new Dictionary<string, string>()
{
    ["YiQiDong"] = "易启动",
    ["YiQiDong.TestImage"] = "易启动测试镜像"
}.ToArray()
, selectedForegroundColor: ConsoleColor.Green);

if (!Directory.Exists("bin"))
    Directory.CreateDirectory("bin");

Console.WriteLine("正在删除Release目录...");
//先删除Release目录
QbFolder.DeleteFolders("src", "Release", SearchOption.AllDirectories);

//如果是制作易启动程序包
if (selectedBuildType == "YiQiDong")
{
    Console.WriteLine("请选择编译架构(一个都不勾选代表全选)：");
    var allArchs = new[] { "win-x86", "win-x64", "linux-x64", "linux-arm64", "linux-arm", "osx-x64", "osx-arm64" };
    var selectArchs = QbSelect.MultiSelect(allArchs.ToDictionary(t => t, t => t).ToArray(), selectedForegroundColor: ConsoleColor.Green);
    if (selectArchs == null || selectArchs.Length == 0)
        selectArchs = allArchs;

    //修改常量文件中的版本号
    var ConstsFile = "src/YiQiDong/Consts.cs";
    var ConstsVersionLine = 4;
    var ConstsArchLine = 5;
    var versionLine = QbFile.ReadLine(ConstsFile, ConstsVersionLine);
    var preArchLine = QbFile.ReadLine(ConstsFile, ConstsArchLine);

    var versionRegex = new Regex(@"""(?<Version>.*?)""", RegexOptions.Singleline);
    //版本号
    var version = versionRegex.Match(versionLine).Groups["Version"].Value;

    try
    {
        foreach (var rid in selectArchs)
        {
            QbFile.WriteLine(ConstsFile, ConstsArchLine, $"public const string ARCH = \"{rid}\";");
            var publishFolderTemplate = "src/{0}/bin/Release/{1}/publish/";
            var publishFolder_YiQiDong = string.Format(publishFolderTemplate, selectedBuildType, rid);
            Console.WriteLine($"开始编译[{rid}]...");
            QbCommand.Run("dotnet", $"publish src/YiQiDong -c Release -r {rid} -p:PublishSingleFile=true --self-contained");
            //配置文件中添加版本号
            QbJson.WriteString(Path.Combine(publishFolder_YiQiDong, Consts.CONFIG_JSON_FILENAME), nameof(Consts.Version), version);
            //配置文件中添加架构
            QbJson.WriteString(Path.Combine(publishFolder_YiQiDong, Consts.CONFIG_JSON_FILENAME), nameof(Consts.ARCH), rid);

            //修改文件的行尾为Linux的行尾
            var changeLinuxFileEOfAction = new Action<string>(file =>
            {
                if (!File.Exists(file))
                    return;
                var content = File.ReadAllText(file);
                content = content.Replace("\r\n", "\n");
                File.WriteAllText(file, content);
            });
            //修改Linux相关文件的行尾
            changeLinuxFileEOfAction(Path.Combine(publishFolder_YiQiDong, "YiQiDong.service"));
            changeLinuxFileEOfAction(Path.Combine(publishFolder_YiQiDong, "YiQiDong.sh"));

            var outFile = Path.GetFullPath($"bin/YiQiDong-{version}-{rid}.zip");
            QbFile.Delete(outFile);
            System.IO.Compression.ZipFile.CreateFromDirectory(publishFolder_YiQiDong, outFile);
        }
    }
    catch
    {
        throw;
    }
    finally
    {
        QbFile.WriteLine(ConstsFile, ConstsArchLine, preArchLine);
    }
}
else if(selectedBuildType =="YiQiDong.TestImage")
{
    //版本号
    var version = "1.1." + DateTime.Now.ToString("yyyy.Mdd");
    var productName = "易启动测试镜像";
    var outFolder = "bin";
    var productDir = selectedBuildType;
    var publishFolder = $"src/{productDir}/bin/Release/publish";
    var outFile = Path.Combine(outFolder, $"{productName}-{version}.ymg");

    //再删除ymg文件
    QbFile.Delete(outFile);

    Console.WriteLine("正在发布项目...");
    QbCommand.Run("dotnet", $"publish src/{productDir} -c Release");
    //生成元信息文件                
    var metaObj = new ImageInfo()
    {
        DefaultId = selectedBuildType,
        Name = productName,
        Version = version,
        BuildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        Description = $"{productName} 是一个测试用的镜像。",
        Platform = new[] { "any" },
        Runtime = new[] { $"dotnet-10.0" },
        AgentStartup = $"{selectedBuildType}.dll"
    };
    var metaFile = Path.Combine(publishFolder, YiQiDong.Core.Consts.IMAGE_META_FILE);
    File.WriteAllText(metaFile, JsonSerializer.Serialize(metaObj, new JsonSerializerOptions()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    }), Encoding.UTF8);
    Console.WriteLine("正在制作易启动镜像...");
    using (var archive = ZipArchive.Create())
    {
        archive.AddAllFromDirectory(publishFolder);
        archive.SaveTo(outFile, CompressionType.LZMA);
    }
    Console.WriteLine("完成");
}

//打开窗口
QbGui.OpenFolder("bin");
