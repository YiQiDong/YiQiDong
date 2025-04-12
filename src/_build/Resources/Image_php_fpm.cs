using _build.Resources.Core;
using Quick.Build;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using YiQiDong.Agent.AgentTypes.TextConfigs;
using YiQiDong.Protocol.V1.Model;

namespace _build.Resources
{
    public class Image_php_fpm : IResource
    {
        public string Id => "php-fpm";

        public string Name => "php-fpm";

        private void replaceFileContent(string file, Dictionary<string, string> replaceDict)
        {
            if (File.Exists(file))
            {
                var content = File.ReadAllText(file);
                foreach (var item in replaceDict)
                {
                    content = content.Replace(item.Key, item.Value);
                }
                File.WriteAllText(file, content);
            }
        }

        public void Invoke()
        {
            var workspaceName = "common-binaries";
            var repositoryName = "php";

            GithubCommonBinariesUtils.BuildImage(Id, Name, workspaceName, repositoryName, (version, rid) =>
            {
                string binFileName;
                string startArguments;
                EnvironmentVariableInfo[] environments = null;
                Dictionary<string, string> containerFolders = null;
                string[] configFolders = null;
                switch (rid)
                {
                    case "win-x64":
                        binFileName = "bin\\php-cgi.exe";
                        startArguments = "-b $PHP_FASTCGI_BIND_PATH";
                        environments = new EnvironmentVariableInfo[]
                        {
                            new (){ Key="PHP_FASTCGI_BIND_PATH",Value = "127.0.0.1:9000",Description="php FastCGI服务模式绑定地址" }
                        };
                        break;
                    default:
                        binFileName = "bin/php-fpm";
                        startArguments = "-F -O -c conf/php.ini -y conf/php-fpm.conf -p $CONTAINER_DIR";
                        containerFolders = new Dictionary<string, string>()
                        {
                            ["$IMAGE_DIR/conf"] = "$CONTAINER_DIR/conf",
                            ["$IMAGE_DIR/php-fpm.d"] = "$CONTAINER_DIR/php-fpm.d",
                            ["$IMAGE_DIR/log"] = "$CONTAINER_DIR/log"
                        };
                        configFolders = new[]
                        {
                            "$CONTAINER_DIR/conf",
                            "$CONTAINER_DIR/php-fpm.d"
                        };
                        break;
                }

                var textConfigsConfigModel = new ConfigModel()
                {
                    ContainerMetaInfos = new[]
                     {
                        new ContainerMetaInfo()
                        {
                            StartFileName = binFileName,
                            StartArguments= startArguments,
                            WorkingDir = "$CONTAINER_DIR",
                            Environments=environments,
                            Path = new[] { "$IMAGE_DIR" },
                            ContainerFolders = containerFolders,
                            ConfigFolders = configFolders
                        }
                     }
                };
                return new ImageInfo()
                {
                    DefaultId = Id,
                    Name = Name,
                    Version = version,
                    Tags = new[] { "Web服务器" },
                    BuildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Description = "PHP：FastCGI 进程管理器（FPM）是 PHP FastCGI 的主要实现，包含大部分对高负载网站有用的功能。",
                    Platform = new[] { rid },
                    AgentType = nameof(YiQiDong.Agent.AgentTypes.TextConfigs),
                    AgentConfig = JsonNode.Parse(JsonSerializer.Serialize(textConfigsConfigModel, new JsonSerializerOptions()
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        WriteIndented = true
                    })).AsObject()
                };
            }, (version, rid, folder) =>
            {
                var exefileExt = string.Empty;
                switch (rid)
                {
                    case "win-x64":
                        exefileExt = ".exe";
                        break;
                    default:
                        QbFile.Delete(Path.Combine(folder, "bin", $"php-cgi"));
                        break;
                }
                QbFile.Delete(Path.Combine(folder, "bin", $"php{exefileExt}"));                
                //php-fpm.conf配置文件处理
                {
                    var srcFile = Path.Combine(folder, "conf", "php-fpm.conf");
                    if (File.Exists(srcFile))
                    {
                        replaceFileContent(srcFile, new Dictionary<string, string>()
                        {
                            ["@php_fpm_sysconfdir@/"] = string.Empty
                        });
                    }
                }
                //www.conf配置文件处理
                {
                    Directory.CreateDirectory(Path.Combine(folder, "php-fpm.d"));
                    var srcFile = Path.Combine(folder, "conf", "www.conf");
                    var desFile = Path.Combine(folder, "php-fpm.d", "www.conf");
                    if (File.Exists(srcFile))
                    {
                        File.Move(srcFile, desFile);
                        replaceFileContent(desFile, new Dictionary<string, string>()
                        {
                            ["@php_fpm_user@"] = "nobody",
                            ["@php_fpm_group@"] = "nobody"
                        });
                    }
                }
                //日志文件处理
                Directory.CreateDirectory(Path.Combine(folder, "log"));
                File.WriteAllText(Path.Combine(folder, "log", "php-fpm.log"), "Hello php-fpm.");
            });
        }
    }
}
