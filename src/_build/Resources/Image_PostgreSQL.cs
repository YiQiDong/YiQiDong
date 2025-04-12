using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
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

    public void Invoke()
    {
        var resourceFolder = Path.Combine("bin", "resource", Id);
        var versions = Directory.GetDirectories(resourceFolder).Select(t => Path.GetFileName(t)).ToArray();
        if (versions == null || versions.Length == 0)
        {
            Console.WriteLine($"资源目录[{resourceFolder}]下面未找到版本目录。");
            return;
        }
        Console.WriteLine("请选择版本：");
        var version = QbSelect.ArrowSelect(versions.ToDictionary(t => t, t => t).ToArray(), selectedForegroundColor: ConsoleColor.Green);
        var versionFolder = Path.Combine(resourceFolder, version);
        var rids = Directory.GetDirectories(versionFolder).Select(t => Path.GetFileName(t)).ToArray();
        if (rids == null || rids.Length == 0)
        {
            Console.WriteLine($"版本目录[{versionFolder}]下面未找到架构目录。");
            return;
        }
        Console.WriteLine("请选择打包架构(一个都不勾选代表全选)：");
        var selectArchs = QbSelect.MultiSelect(rids.ToDictionary(t => t, t => t).ToArray(), selectedForegroundColor: ConsoleColor.Green);
        if (selectArchs == null || selectArchs.Length == 0)
            selectArchs = rids;

        foreach (var rid in selectArchs)
        {
            var ridFolder = Path.Combine(versionFolder, rid);
            var tmpFolder = Path.Combine("bin", Id);
            if (Directory.Exists(tmpFolder))
                Directory.Delete(tmpFolder, true);
            QbFolder.Copy(ridFolder, tmpFolder);
            var textConfigsConfigModel = new ConfigModel()
            {
                ContainerMetaInfos = new[]
                {
                    new ContainerMetaInfo()
                    {
                        StartFileName="powershell.exe",
                        StartArguments= $"-ExecutionPolicy Unrestricted -File $IMAGE_DIR\\bin\\start_{Id}.ps1",
                        StopFileName="pg_ctl.exe",
                        StopArguments="stop",
                        WorkingDir = "$CONTAINER_DIR",
                        Environments=new []
                        {
                            new EnvironmentVariableInfo()
                            {
                                Key="PGDATA",
                                Value="$CONTAINER_DIR\\data",
                                Description="PostgreSQL数据目录"
                            }
                        },
                        Path = new[] { "$IMAGE_DIR\\bin" },
                        ConfigFiles = new Dictionary<string, string>()
                        {
                            ["$PGDATA/postgresql.conf"] ="postgresql.conf",
                            ["$PGDATA/pg_hba.conf"] ="pg_hba.conf",
                            ["$PGDATA/pg_ident.conf"] ="pg_ident.conf",
                            ["$PGDATA/postmaster.opts"] ="postmaster.opts"
                        }
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
            var outFile = $"bin/{Name}-{version}-{rid}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.ymg";
            Console.WriteLine($"正在制作弈启动镜像[{rid}]...");
            using (var archive = SharpCompress.Archives.Zip.ZipArchive.Create())
            {
                archive.AddAllFromDirectory(tmpFolder);
                archive.SaveTo(outFile, CompressionType.LZMA);
            }
            if (Directory.Exists(tmpFolder))
                Directory.Delete(tmpFolder, true);
        }
    }
}
