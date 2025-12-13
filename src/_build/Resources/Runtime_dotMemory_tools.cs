using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Quick.Build;
using SharpCompress.Archives;

namespace _build.Resources;

public class Runtime_dotMemory_tools : IResource
{
    public string Id => "dotMemory-tools";

    public string Name => "dotMemory Tools";

    public string PackageIdPrefix = "JetBrains.dotMemory.Console";

    public void Invoke()
    {
        Console.WriteLine($"正在获取{Name}版本列表...");

        SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
        FindPackageByIdResource resource = repository.GetResourceAsync<FindPackageByIdResource>().Result;
        var versions = resource.GetAllVersionsAsync(
            $"{PackageIdPrefix}.windows-x64",
             new SourceCacheContext(),
             NullLogger.Instance,
             CancellationToken.None)
        .Result
        .Where(t => !t.IsPrerelease)
        .OrderByDescending(t => t);

        Console.WriteLine($"请选择{Name}版本：");
        var version = QbSelect.ArrowSelect(versions.Select(t => t.ToString()).ToDictionary(t => t, t => t).ToArray(), selectedForegroundColor: ConsoleColor.Green);

        var archDict = new Dictionary<string, string>()
        {
            ["win-x64"] = "windows-x64",
            ["linux-x64"] = "linux-x64",
            ["linux-arm64"] = "linux-arm64",
            ["linux-arm"] = "linux-arm",
            ["osx-x64"] = "macos-x64",
            ["osx-arm64"] = "macos-arm64"
        };
        Console.WriteLine("请选择打包架构(一个都不勾选代表全选)：");
        var selectArchs = QbSelect.MultiSelect(archDict.ToArray(), selectedForegroundColor: ConsoleColor.Green);
        if (selectArchs == null || selectArchs.Length == 0)
            selectArchs = archDict.Keys.ToArray();

        foreach (var arch in selectArchs)
        {
            Console.WriteLine($"正在打包[{arch}]...");
            var packageId = $"{PackageIdPrefix}.{archDict[arch]}";
            var packageVersion = new NuGetVersion(version);

            var tmpFileName = Path.Combine("bin", $"{packageId}_{version}.bin");
            var tmpFolder = Path.Combine("bin", $"{Id}-{version}-{arch}");
            if (Directory.Exists(tmpFolder))
                Directory.Delete(tmpFolder, true);
            try
            {
                Console.WriteLine($"正在下载[{tmpFileName}]...");
                using (var fileStream = File.Create(tmpFileName))
                    resource.CopyNupkgToStreamAsync(
                        packageId,
                        packageVersion,
                        fileStream,
                        new SourceCacheContext(),
                        NullLogger.Instance,
                        CancellationToken.None).Wait();
                Console.WriteLine($"文件[{tmpFileName}]下载完成，正在解压...");
                using (var archive = SharpCompress.Archives.Zip.ZipArchive.Open(tmpFileName))
                    archive.ExtractToDirectory(tmpFolder);

                Console.WriteLine($"正在打包易启动运行库文件...");
                //生成元信息文件
                var metaObj = new YiQiDong.Core.Protocol.V1.Model.RuntimeInfo()
                {
                    Id = $"{Name}-{version}",
                    Name = Id,
                    Version = version,
                    BuildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Description = "The dotMemory console tool lets you start a profiling session and get memory snapshots from the command line.",
                    Platform = new[] { arch },
                    Environment = new Dictionary<string, string>()
                    {
                        ["DOTMEMORY_ROOT"] = "$RUNTIME_DIR",
                    },
                    Path = ["$RUNTIME_DIR"],
                    ExecuteFiles = [
                        "$RUNTIME_DIR/dotmemory",
                        "$RUNTIME_DIR/runtime-dotnet.sh",
                        "$RUNTIME_DIR/" + arch +"/dotnet/dotnet"],
                    TestCommand = new Dictionary<string, string[]>()
                    {
                        ["帮助"] = ["dotmemory"]
                    }
                };
                if (arch.StartsWith("win-"))
                        metaObj.ExecuteFiles = [];

                var publishFolder = Path.Combine(tmpFolder, "tools");
                var metaFile = Path.Combine(publishFolder, IResource.RUNTIME_META_FILE);
                File.WriteAllText(metaFile, JsonSerializer.Serialize(metaObj, new JsonSerializerOptions()
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = true,
                }), Encoding.UTF8);
                //打包
                using (var archive2 = SharpCompress.Archives.Zip.ZipArchive.Create())
                {
                    archive2.AddAllFromDirectory(publishFolder);
                    archive2.SaveTo($"bin/{Id}-{version}-{arch}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.yrt",
                        new SharpCompress.Writers.WriterOptions(SharpCompress.Common.CompressionType.LZMA));
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (Directory.Exists(tmpFolder))
                    Directory.Delete(tmpFolder, true);
                if (File.Exists(tmpFileName))
                    File.Delete(tmpFileName);
            }
        }
    }
}
