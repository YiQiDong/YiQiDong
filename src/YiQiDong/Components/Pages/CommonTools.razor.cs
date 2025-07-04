using Microsoft.AspNetCore.Components;
using Quick.Blazor.Bootstrap;
using YiQiDong.Components.Controls;
using static YiQiDong.Components.Controls.CommonToolsControl;

namespace YiQiDong.Components.Pages
{
    public partial class CommonTools : ComponentBase
    {
        private ToolInfo[] tools;
        private ModalLoading modalLoading;
        private ModalAlert modalAlert;

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            var toolList = new List<ToolInfo>();
            if (OperatingSystem.IsLinux())
            {
                toolList.Add(ToolInfo.Create<NetworkInterfaceManage>("网卡管理", "fa fa-sitemap"));
                toolList.Add(ToolInfo.Create<LinuxIptablesManage>("iptables管理","fa fa-shield"));
            }
            toolList.AddRange(
            [
                ToolInfo.Create<FileManageControl>("文件管理","fa fa-folder"),
                ToolInfo.Create<Quick.Blazor.Bootstrap.Admin.ProcessManageControl>("进程管理","fa fa-cogs"),
                ToolInfo.Create<Quick.Blazor.Bootstrap.Terminal.TerminalControl>("模拟终端","fa fa-terminal",new Dictionary<string, object>()
                {
                    [nameof(Quick.Blazor.Bootstrap.Terminal.TerminalControl.WorkingDir)]=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                }),
                ToolInfo.Create<Quick.Blazor.Bootstrap.Admin.ProxyDownloadControl>("代理下载","fa fa-cloud-download")
            ]);
            if (Program.IsStartSuccess)
            {
                toolList.AddRange(new[]
                {
                    ToolInfo.Create<Quick.Blazor.Bootstrap.ReverseProxy.ReverseProxyManageControl>("反向代理", "fa fa-paper-plane"),
                    ToolInfo.Create<CommonTools_Glash>("Glash代理", "fa fa-paper-plane"),
                    ToolInfo.Create<Quick.Blazor.Bootstrap.CrontabManager.CrontabManageControl>("Crontab管理", "fa fa-align-justify")
                });
            }
            tools = toolList.ToArray();
        }
    }
}
