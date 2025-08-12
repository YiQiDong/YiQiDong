using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using _build.Core.Snapcraft;
using Quick.Build;
using SharpCompress.Archives;
using SharpCompress.Common;
using YiQiDong.Agent.AgentTypes.TextConfigs;
using YiQiDong.Protocol.V1.Model;

namespace _build;

public class Image_PostgreSQL : IResource
{
    public string Id => "postgresql";

    public string Name => "PostgreSQL";

    public string start_postgresql_ps1_content = @"
if(Test-Path -Path ""$env:PGDATA\postmaster.pid"" -PathType Leaf)
{
    $nid = Get-Content ""$env:PGDATA\postmaster.pid"" -TotalCount 1
    $msg = ""检测到数据库服务已经启动，进程PID："" + $nid
    echo $msg
}
else
{
    if(-not(Test-Path -Path ""$env:PGDATA\PG_VERSION"" -PathType Leaf))
    {
        echo ""检测到数据目录未初始化，开始初始化...""
        initdb.exe -U root
        if ( 0 -ne $LASTEXITCODE )
        {
            $msg = ""初始化数据目录时出错，退出码："" + $LASTEXITCODE
            echo $msg
            exit $LASTEXITCODE
        }
        echo ""初始化数据目录成功。默认账号：root，默认只能本地连接，不需要密码。""
    }
    pg_ctl.exe start -l ""$env:PGDATA\postgresql.log""
    if ( 0 -ne $LASTEXITCODE )
    {
        $msg = ""启动数据库时出错，退出码："" + $LASTEXITCODE
        echo $msg
        exit $LASTEXITCODE
    }
    $nid = Get-Content ""$env:PGDATA\postmaster.pid"" -TotalCount 1
}
pg_ctl.exe status
Wait-Process -Id $nid";

public string start_postgresql_sh_content = @"
if [ -f ""$PGDATA/postmaster.pid"" ]; then
    cat ""$PGDATA/postmaster.pid"" | while read line
    do
        echo ""检测到数据库服务已经启动，进程PID：$line""
        exit -1
    done
else
    if [ ! -f ""$PGDATA/PG_VERSION"" ]; then
        echo ""检测到数据目录未初始化，开始初始化...""
        chmod +x $IMAGE_DIR/bin/initdb
        initdb -U root
        if [ $? -ne 0 ]; then
            echo ""初始化数据目录时出错，退出码：$?""
            exit $?
        fi
        echo ""初始化数据目录成功。默认账号：root，默认只能本地连接，不需要密码。""
    fi
    chmod +x $IMAGE_DIR/bin/pg_ctl
    pg_ctl start -l ""$PGDATA/postgresql.log""

    if [ $? -ne 0 ]; then
        echo ""启动数据库时出错，退出码：$?""
        exit $?
    fi

    cat ""$PGDATA/postmaster.pid"" | while read line
    do
        pg_ctl status
        wait $line
    done
fi";

    public void Invoke()
    {
        var displayDownloadProgress = new Action<QbNet.TransferProgress>(t =>
        {
            QbConsole.DisplaySameLineInConsole($"[{t.Current * 100 / t.Total}%]进度：{t.Current.ToString("N0")}/{t.Total.ToString("N0")}，速度：{t.Speed.ToString("N0")}，剩余时间：{t.RemainingTime}");
        });
        var snapcraftClient = new SnapcraftClient();
        Console.WriteLine($"正在获取{Id}版本信息...");
        var snapInfo = snapcraftClient.GetSnapInfoAsync(Id + "10").Result;

        var versions = snapInfo.channel_map.GroupBy(t=>t.version).Select(t=>t.Key).ToArray();
        Console.WriteLine("请选择版本：");
        var version = QbSelect.ArrowSelect(versions.ToDictionary(t=>t,t=>t).ToArray(), selectedForegroundColor: ConsoleColor.Green);

        var channelMaps = snapInfo.channel_map.Where(t => t.version == version).ToArray();        
        Console.WriteLine("请选择运行平台(一个都不选代表全选)：");
        var ridDict = new Dictionary<string, string>();
        if (channelMaps.Any(t => t.channel.architecture == "amd64"))
            ridDict["amd64"] = "linux-x64";
        if (channelMaps.Any(t => t.channel.architecture == "arm64"))
            ridDict["arm64"] = "linux-arm64";
        if (channelMaps.Any(t => t.channel.architecture == "armhf"))
            ridDict["armhf"] = "linux-arm";
        var ridValues = QbSelect.MultiSelect(ridDict.ToArray(), selectedForegroundColor: ConsoleColor.Green);
        if (ridValues == null || ridValues.Length == 0)
            ridValues = ridDict.Keys.ToArray();
        var cacheFolder = Path.Combine("bin", "cache");
        var tmpFolder = Path.Combine("bin", Id);
        if (Directory.Exists(tmpFolder))
        {
            Directory.Delete(tmpFolder, true);
        }

        foreach (var ridValue in ridValues)
        {
            var rid = ridDict[ridValue];
            var channelMap = channelMaps.FirstOrDefault(t => t.channel.architecture == ridValue);
            Console.WriteLine($"开始打包[{rid}]...");
            if (Directory.Exists(tmpFolder))
            {
                Console.WriteLine($"正在清理目录...");
                Directory.Delete(tmpFolder, true);
            }
            Directory.CreateDirectory(tmpFolder);
            try
            {
                string url = channelMap.download.url;
                var zipFileName = Path.GetFileName(url);
                //开始下载
                var zipFile = Path.Combine(cacheFolder, zipFileName);
                if(!File.Exists(zipFile))
                {
                    Console.WriteLine($"正在从[{url}]下载文件...");
                    using (var httpClient = snapcraftClient.GetHttpClient())
                        QbNet.DownloadFile(httpClient, url, zipFile, CancellationToken.None, displayDownloadProgress).Wait();
                }
                //解压文件
                QbCommand.Run("7z", $"x -o{tmpFolder} \"{zipFile}\"", handleExitCode: t => true);
                Directory.Move(Path.Combine(tmpFolder, "usr", "bin"), Path.Combine(tmpFolder, "bin"));
                Directory.Move(Path.Combine(tmpFolder, "usr", "lib"), Path.Combine(tmpFolder, "lib"));
                Directory.Move(Path.Combine(tmpFolder, "usr", "share"), Path.Combine(tmpFolder, "share"));
                QbFile.DeleteFiles(tmpFolder,"*.wrapper");
                Directory.Delete(Path.Combine(tmpFolder, "meta"), true);
                Directory.Delete(Path.Combine(tmpFolder, "snap"), true);
                Directory.Delete(Path.Combine(tmpFolder, "usr"), true);
                Directory.Delete(Path.Combine(tmpFolder, "etc"), true);
                Directory.Delete(Path.Combine(tmpFolder, "sbin"), true);
                Directory.Delete(Path.Combine(tmpFolder, "share", "doc"), true);
                Directory.Delete(Path.Combine(tmpFolder, "share", "man"), true);

                /*
                Directory.CreateDirectory(Path.Combine(tmpPublishFolder, "conf"));
                File.Delete(Path.Combine(tmpPublishFolder, "default_config.conf"));
                File.Delete(Path.Combine(tmpPublishFolder, "launcher.sh"));
                File.Move(Path.Combine(tmpPublishFolder, "mosquitto.conf"), Path.Combine(tmpPublishFolder, "conf", "mosquitto.conf"));
                */

                var textConfigsConfigModel = new ConfigModel()
                {
                    ContainerMetaInfos = new[]
                    {
                        new ContainerMetaInfo()
                        {
                            Platform = ["win"],
                            StartFileName="powershell.exe",
                            StartArguments= $"-ExecutionPolicy Unrestricted -File $IMAGE_DIR/bin/start_{Id}.ps1",
                            StopFileName="pg_ctl.exe",
                            StopArguments="stop",
                            WorkingDir = "$CONTAINER_DIR",
                            Environments=new []
                            {
                                new EnvironmentVariableInfo()
                                {
                                    Key="PGDATA",
                                    Value="$CONTAINER_DIR/data",
                                    Description="PostgreSQL数据目录"
                                }
                            },
                            Path = new[] { "$IMAGE_DIR/bin" },
                            ConfigFiles = new Dictionary<string, string>()
                            {
                                ["$PGDATA/postgresql.conf"] ="postgresql.conf",
                                ["$PGDATA/pg_hba.conf"] ="pg_hba.conf",
                                ["$PGDATA/pg_ident.conf"] ="pg_ident.conf",
                                ["$PGDATA/postmaster.opts"] ="postmaster.opts"
                            }
                        },
                        new ContainerMetaInfo()
                        {
                            Platform = ["linux"],
                            StartFileName="sh",
                            StartArguments= $"$IMAGE_DIR/bin/start_{Id}.sh",
                            StopFileName="pg_ctl",
                            StopArguments="stop",
                            WorkingDir = "$CONTAINER_DIR",
                            Environments=new []
                            {
                                new EnvironmentVariableInfo()
                                {
                                    Key="PGDATA",
                                    Value="$CONTAINER_DIR/data",
                                    Description="PostgreSQL数据目录"
                                },
                                new EnvironmentVariableInfo()
                                {
                                    Key="LD_LIBRARY_PATH",
                                    Value="$LD_LIBRARY_PATH:$IMAGE_DIR/lib",
                                    Description="PostgreSQL数据目录"
                                }
                            },
                            Path = new[] { "$IMAGE_DIR/bin" },
                            /*
                            ConfigFiles = new Dictionary<string, string>()
                            {
                                ["$PGDATA/postgresql.conf"] ="postgresql.conf",
                                ["$PGDATA/pg_hba.conf"] ="pg_hba.conf",
                                ["$PGDATA/pg_ident.conf"] ="pg_ident.conf",
                                ["$PGDATA/postmaster.opts"] ="postmaster.opts"
                            }
                            */
                        }
                    }
                };
                var imageInfo = new ImageInfo()
                {
                    DefaultId = Id,
                    Name = Name,
                    Version = version,
                    Tags = new[] { "数据库" },
                    BuildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Description = "PostgreSQL 是一个强大的，开源的对象关系数据库系统。",
                    Platform = new[] { rid },
                    AgentType = nameof(YiQiDong.Agent.AgentTypes.TextConfigs),
                    AgentConfig = JsonNode.Parse(JsonSerializer.Serialize(textConfigsConfigModel, new JsonSerializerOptions()
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        WriteIndented = true
                    })).AsObject()
                };
                File.WriteAllText(Path.Combine(tmpFolder, IResource.IMAGE_META_FILE),
                                JsonSerializer.Serialize(imageInfo, new JsonSerializerOptions()
                                {
                                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                                    WriteIndented = true
                                }),
                                Encoding.UTF8);
                File.WriteAllText(Path.Combine(tmpFolder, "bin", $"start_{Id}.ps1"), start_postgresql_ps1_content, Encoding.UTF8);
                File.WriteAllText(Path.Combine(tmpFolder, "bin", $"start_{Id}.sh"), start_postgresql_sh_content, Encoding.UTF8);
                var outFile = $"bin/{Name}-{version}-{rid}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.ymg";
                Console.WriteLine($"正在制作易启动镜像[{rid}]...");
                using (var archive = SharpCompress.Archives.Zip.ZipArchive.Create())
                {
                    archive.AddAllFromDirectory(tmpFolder);
                    archive.SaveTo(outFile, CompressionType.LZMA);
                }
                if (Directory.Exists(tmpFolder))
                    Directory.Delete(tmpFolder, true);
            }
            catch
            {
                throw;
            }
            finally
            {
                if (Directory.Exists(tmpFolder))
                    Directory.Delete(tmpFolder, true);
            }
        }
    }
}
