using _build;
using Quick.Build;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using YiQiDong;

var appFolder = QbFolder.GetAppFolder();
if (appFolder == Environment.CurrentDirectory)
    Environment.CurrentDirectory = Path.GetFullPath("../../../../../");

//版本号
var version = "1.1." + DateTime.Now.ToString("yyyy.Mdd");

//准备目录变量
var baseFolder = Environment.CurrentDirectory;

Console.WriteLine("----------------------------------");
Console.WriteLine("  欢迎使用弈启动编译脚本");
Console.WriteLine("----------------------------------");

Console.WriteLine("请选择编译类型：");

var selectedBuildType = QbSelect.ArrowSelect(new Dictionary<string, string>()
{
    ["YiQiDong"] = "弈启动",
    ["Images"] = "常用镜像",
    ["Runtimes"] = "常用运行库"
}.ToArray()
, selectedForegroundColor: ConsoleColor.Green);

if (!Directory.Exists("bin"))
    Directory.CreateDirectory("bin");

Console.WriteLine("正在删除Release目录...");
//先删除Release目录
QbFolder.DeleteFolders("src", "Release", SearchOption.AllDirectories);

//如果是制作弈启动程序包
if (selectedBuildType == "YiQiDong")
{
    Console.WriteLine("请选择编译架构(一个都不勾选代表全选)：");
    var allArchs = new[] { "win-x64", "linux-x64", "linux-arm64", "linux-arm", "osx-x64" };
    var selectArchs = QbSelect.MultiSelect(allArchs.ToDictionary(t => t, t => t).ToArray(), selectedForegroundColor: ConsoleColor.Green);
    if (selectArchs == null || selectArchs.Length == 0)
        selectArchs = allArchs;

    //修改常量文件中的版本号
    var ConstsFile = "src/YiQiDong/Consts.cs";
    var ConstsVersionLine = 4;
    var ConstsArchLine = 5;
    var preVersionLine = QbFile.ReadLine(ConstsFile, ConstsVersionLine);
    var preArchLine = QbFile.ReadLine(ConstsFile, ConstsArchLine);

    try
    {
        QbFile.WriteLine(ConstsFile, ConstsVersionLine, $"public const string Version = \"{version}\";");
        foreach (var rid in selectArchs)
        {
            QbFile.WriteLine(ConstsFile, ConstsArchLine, $"public const string ARCH = \"{rid}\";");
            var publishFolderTemplate = "src/{0}/bin/Release/{1}/publish/";
            var publishFolder_YiQiDong = string.Format(publishFolderTemplate, selectedBuildType, rid);
            Console.WriteLine($"开始编译[{rid}]...");
            QbCommand.Run("dotnet", $"publish src/YiQiDong -c Release -r {rid} --self-contained");
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
        QbFile.WriteLine(ConstsFile, ConstsVersionLine, preVersionLine);
        QbFile.WriteLine(ConstsFile, ConstsArchLine, preArchLine);
    }
}
else if (selectedBuildType == "Images")
{
    var runtimeDict = new Dictionary<string, IResource>();
    foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
    {
        if (type.IsClass
            && type.Name.StartsWith("Image_")
            && typeof(IResource).IsAssignableFrom(type))
        {
            var runtime = (IResource)Activator.CreateInstance(type);
            runtimeDict[runtime.Id] = runtime;
        }
    }
    var selectRuntime = QbSelect.ArrowSelect(runtimeDict.ToDictionary(t => t.Key, t => t.Value.Name).ToArray(), selectedForegroundColor: ConsoleColor.Green);
    runtimeDict[selectRuntime].Invoke();
}
else if (selectedBuildType == "Runtimes")
{
    var runtimeDict = new Dictionary<string, IResource>();
    foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
    {
        if (type.IsClass
            && type.Name.StartsWith("Runtime_")
            && typeof(IResource).IsAssignableFrom(type))
        {
            var runtime = (IResource)Activator.CreateInstance(type);
            runtimeDict[runtime.Id] = runtime;
        }
    }
    var selectRuntime = QbSelect.ArrowSelect(runtimeDict.ToDictionary(t => t.Key, t => t.Value.Name).ToArray(), selectedForegroundColor: ConsoleColor.Green);
    runtimeDict[selectRuntime].Invoke();
}
//打开窗口
QbGui.OpenFolder("bin");
