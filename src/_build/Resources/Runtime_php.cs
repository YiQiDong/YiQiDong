using _build.Resources.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using YiQiDong.Core.Protocol.V1.Model;

namespace _build.Resources
{
    public class Runtime_php : IResource
    {
        public string Id => "php";
        public string Name => "php";

        public void Invoke()
        {
            var workspaceName = "common-binaries";
            var repositoryName = "php";

            GithubCommonBinariesUtils.BuildRuntime(Id, Name, workspaceName, repositoryName, (version, rid) =>
            {
                var metaObj = new RuntimeInfo()
                {
                    Id = Id,
                    Name = Name,
                    Version = version,
                    BuildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Description = "PHP（PHP: Hypertext Preprocessor）即“超文本预处理器”，是在服务器端执行的脚本语言，尤其适用于Web开发并可嵌入HTML中。",
                    Platform = new[] { rid },
                    Path = new[] { "$RUNTIME_DIR/bin" },
                    ExecuteFiles = new[]
                    {
                        "$RUNTIME_DIR/bin/php",
                        "$RUNTIME_DIR/bin/php-cgi",
                        "$RUNTIME_DIR/bin/php-fpm"
                    },
                    TestCommand = new Dictionary<string, string[]>()
                    {
                        ["CLI版本"] = new[] { "php", "-v" },
                        ["CLI信息"] = new[] { "php", "-i" },
                        ["CGI版本"] = new[] { "php-cgi", "-v" },
                        ["CGI信息"] = new[] { "php-cgi", "-i" },
                        ["FPM版本"] = new[] { "php-fpm", "-v" },
                        ["FPM信息"] = new[] { "php-fpm", "-i" }
                    }
                };
                if (rid.StartsWith("win-"))
                    metaObj.ExecuteFiles = metaObj.ExecuteFiles.Select(t => t + ".exe").ToArray();
                return metaObj;
            });
        }
    }
}
