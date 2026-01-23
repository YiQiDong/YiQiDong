using Microsoft.AspNetCore.Components;
using Quick.Blazor.Bootstrap;
using Quick.Blazor.Bootstrap.Utils;
using YiQiDong.Components.Controls;

namespace YiQiDong.Components.Pages
{
    public partial class CommonTools : ComponentBase
    {        
        public class ToolInfo
        {
            public string Category { get; set; }
            public string Name { get; set; }
            public string IconClass { get; set; }
            public RenderFragment Content { get; set; }

            public static ToolInfo Create<T>(string category, string name, string iconClass, Dictionary<string, object> parameterDict = null)
            {
                return new ToolInfo()
                {
                    Category = category,
                    Name = name,
                    IconClass = iconClass,
                    Content = BlazorUtils.GetRenderFragment<T>(parameterDict)
                };
            }
        }

        private ModalWindow modalWindow;
        private ToolInfo[] tools;

        public CommonTools()
        {
            tools = GetTools().ToArray();
        }

        private IEnumerable<ToolInfo> GetTools()
        {
            //-----------
            //系统管理
            //-----------
            yield return ToolInfo.Create<SysTools.DateTimeManage>("系统管理", "日期时间", "fa fa-clock-o");
            if (OperatingSystem.IsLinux())
            {
                yield return ToolInfo.Create<LinuxTools.NetworkInterfaceManage>("系统管理", "网络配置", "fa fa-sitemap");
                yield return ToolInfo.Create<LinuxTools.FirewallManage_iptables>("系统管理", "防火墙管理", "fa fa-shield");
            }
            var hostsFile = "/etc/hosts";
            if (OperatingSystem.IsWindows())
                hostsFile = $@"{Environment.GetFolderPath(Environment.SpecialFolder.Windows)}\System32\drivers\etc\hosts";
            yield return ToolInfo.Create<Quick.Blazor.Bootstrap.Admin.TextEditControl>("系统管理", "Hosts文件", "fa fa-file-text", new()
            {
                [nameof(Quick.Blazor.Bootstrap.Admin.TextEditControl.File)] = hostsFile
            });
            yield return ToolInfo.Create<FileManageControl>("系统管理", "文件管理", "fa fa-folder");
            yield return ToolInfo.Create<Quick.Blazor.Bootstrap.Admin.ProcessManageControl>("系统管理", "进程管理", "fa fa-cogs");
            yield return ToolInfo.Create<Quick.Blazor.Bootstrap.Terminal.TerminalControl>("系统管理", "模拟终端", "fa fa-terminal", new Dictionary<string, object>()
            {
                [nameof(Quick.Blazor.Bootstrap.Terminal.TerminalControl.WorkingDir)] = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            });
            if (Program.IsStartSuccess)
            {
                    yield return  ToolInfo.Create<Quick.Blazor.Bootstrap.CrontabManager.CrontabManageControl>("系统管理",  "Crontab管理", "fa fa-align-justify");
            }
            //-----------
            //代理工具
            //-----------
            yield return ToolInfo.Create<GlashServerControl>("代理工具", "Glash服务端", "fa fa-server");
            yield return ToolInfo.Create<GlashAgentControl>("代理工具", "Glash代理端", "fa fa-black-tie");
            yield return ToolInfo.Create<Glash.Blazor.Client.Main>("代理工具", "Glash客户端", "fa fa-coffee");
            yield return ToolInfo.Create<Quick.Blazor.Bootstrap.ReverseProxy.ReverseProxyManageControl>("代理工具", "反向代理", "fa fa-paper-plane");
            yield return ToolInfo.Create<Quick.Blazor.Bootstrap.Admin.ProxyDownloadControl>("代理工具", "代理下载", "fa fa-cloud-download");
            //-----------
            //其他工具
            //-----------
            yield return ToolInfo.Create<TestTools.PingTestControl>("其他工具", "Ping测试", "fa fa-bolt");
            yield return ToolInfo.Create<TestTools.TcpTestControl>("其他工具", "TCP测试", "fa fa-bolt");
            yield return ToolInfo.Create<TestTools.TcpPortTestControl>("其他工具", "TCP端口扫描", "fa fa-bolt");
            yield return ToolInfo.Create<DevTools.RasKeyPairGenerator>("其他工具", "RSA密钥对生成", "fa fa-bolt");
            yield return ToolInfo.Create<DevTools.GuidGenerator>("其他工具", "GUID生成", "fa fa-bolt");
        }

        public void Show(ToolInfo toolInfo)
        {
            modalWindow.Show(toolInfo.Name, toolInfo.Content);
        }
    }
}
