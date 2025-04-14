using Microsoft.AspNetCore.Components;
using Microsoft.Diagnostics.Runtime;
using Quick.Blazor.Bootstrap;
using Quick.Blazor.Bootstrap.Admin.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YiQiDong.Components.Controls;
using YiQiDong.Core.Utils;
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
            toolList.AddRange(new[]
            {
                ToolInfo.Create<FileManageControl>("文件管理","fa fa-folder"),
                ToolInfo.Create<Quick.Blazor.Bootstrap.Admin.ProcessManageControl>("进程管理","fa fa-cogs",
                Quick.Blazor.Bootstrap.Admin.ProcessManageControl.PrepareParameters(GetProcessViewOtherButtons(
                    new Lazy<ModalLoading>(()=>modalLoading),
                    new Lazy<ModalAlert>(()=>modalAlert)))),
                ToolInfo.Create<NetworkInterfaceManage>("网卡管理","fa fa-sitemap"),
                ToolInfo.Create<Quick.Blazor.Bootstrap.Terminal.TerminalControl>("模拟终端","fa fa-terminal",new Dictionary<string, object>()
                {
                    [nameof(Quick.Blazor.Bootstrap.Terminal.TerminalControl.WorkingDir)]=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                }),
                ToolInfo.Create<Quick.Blazor.Bootstrap.Admin.ProxyDownloadControl>("代理下载","fa fa-cloud-download"),
                ToolInfo.Create<WebFileTransferManage>("Web文件传输","fa fa-file")
            });
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

        public static ProcessInfoButton[] GetProcessViewOtherButtons(Lazy<ModalLoading> modalLoading, Lazy<ModalAlert> modalAlert)
        {
            if (modalLoading == null)
                throw new ArgumentNullException(nameof(modalLoading));
            if (modalAlert == null)
                throw new ArgumentNullException(nameof(modalAlert));
            return
            [
                new ProcessInfoButton()
                {
                    Name = "线程堆栈(dotnet)",
                    IsVisiableFunc = processInfo => true,
                    OnClickAction=processInfo =>
                    {
                        modalLoading.Value.Show("加载中...", null, true);
                        Task.Run(() =>
                        {
                            string content = null;
                            Process process = null;
                            try
                            {
                                process = Process.GetProcessById(processInfo.PID);
                                StringBuilder sb = new StringBuilder();
                                sb.AppendLine($"时间：{DateTime.Now},进程ID：{process.Id}，线程数：{process.Threads.Count}");
                                using (var dataTarget = DataTarget.AttachToProcess(process.Id, false))
                                {
                                    var clrVersions = dataTarget.ClrVersions;
                                    if (clrVersions.Length == 0)
                                    {
                                        sb.AppendLine($"在进程[{process.Id}]中未发现CLR环境。");
                                    }
                                    else
                                    {
                                        ClrInfo runtimeInfo = clrVersions[0];
                                        using (var runtime = runtimeInfo.CreateRuntime())
                                        {
                                            foreach (var t in runtime.Threads)
                                            {
                                                var stackTraceLines = t.EnumerateStackTrace().Select(f =>
                                                {
                                                    if (f.Method != null)
                                                    {
                                                        return f.Method.Signature;
                                                    }
                                                    return null;
                                                }).Where(t => t != null).ToArray();
                                                if (stackTraceLines == null || stackTraceLines.Length == 0)
                                                    continue;
                                                sb.AppendLine($"线程[{t.ManagedThreadId}]");
                                                foreach (var line in stackTraceLines)
                                                {
                                                    sb.AppendLine("    " + line);
                                                }
                                            }
                                        }
                                    }
                                }
                                content = sb.ToString();
                            }
                            catch (Exception ex)
                            {
                                content = ExceptionUtils.GetExceptionString(ex);
                            }
                            modalAlert.Value.Show($"进程[Id:{processInfo.PID}, 名称:{processInfo.Name}] - 线程堆栈追踪", content, usePreTag: true);
                            modalLoading.Value.Close();
                        });
                    }
                }
            ];
        }
    }
}
