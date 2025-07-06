using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Quick.Blazor.Bootstrap;
using Quick.Blazor.Bootstrap.ReverseProxy.Model;
using YiQiDong.Core;
using YiQiDong.Core.Utils;
using YiQiDong.Model;
using YiQiDong.Protocol.V1.Model;
using YiQiDong.Utils;

namespace YiQiDong.Components.Controls
{
    public partial class ContainerConsoleControl : ComponentBase, IDisposable
    {
        [Parameter]
        public ContainerContext Container { get; set; }
        //父窗口
        [Parameter]
        public ModalWindow ModalWindow { get; set; }

        [Inject]
        public IJSRuntime JSRuntime { get; set; }

        private ModalAlert modalAlert;
        private ModalWindow modalWindow;
        private ModalLoading modalLoading;

        public int ConsoleRows = 20;

        private FunctionInfo[] ContainerFunctions;
        private ReverseProxyRule[] ReverseProxyRules;

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                Container.ConsoleHistoryChanged += OnCurrentContainerConsoleHistoryChanged;
                Container.FunctionListChanged += Container_FunctionListChanged;
                Container.ReverseProxyRuleListChanged += Container_ReverseProxyRuleListChanged; ;
                refreshFunctionList();
                refreshReverseProxyRuleList();
                scrollToBottom();
            }
        }

        private void refreshFunctionList()
        {
            Task.Run(() =>
            {
                ContainerFunctions = Container.GetFunctionList();
                InvokeAsync(StateHasChanged);
            });
        }

        private void refreshReverseProxyRuleList()
        {
            Task.Run(() =>
            {
                ReverseProxyRules = Container.GetReverseProxyRuleList();
                InvokeAsync(StateHasChanged);
            });
        }

        private void Container_FunctionListChanged(object sender, EventArgs e)
        {
            refreshFunctionList();
        }

        private void Container_ReverseProxyRuleListChanged(object sender, EventArgs e)
        {
            refreshReverseProxyRuleList();
        }

        void IDisposable.Dispose()
        {
            Container.ConsoleHistoryChanged -= OnCurrentContainerConsoleHistoryChanged;
            Container.FunctionListChanged -= Container_FunctionListChanged;
            Container.ReverseProxyRuleListChanged -= Container_ReverseProxyRuleListChanged;
        }

        private void ContainerConfigFiles()
        {
            modalWindow.Show($"{Container.ContainerInfo.Name} - 配置文件", new DialogParameters<ContainerConfigFilesControl>
            {
                {x=>x.Container, Container}
            });
        }

        private void ConfigContainer(FunctionInfo functionInfo)
        {
            modalWindow.Show($"{Container.ContainerInfo.Name} - {functionInfo.Name}", new DialogParameters<ContainerFunctionControl>()
            {
                {x=>x.Container,Container},
                {x=>x.Function,functionInfo}
            });
        }

        private void ConsoleSetRows(int rows)
        {
            ConsoleRows = rows;
        }

        private void ClearContainerHistory()
        {
            Container.ClearContainerHistory();
        }

        private void scrollToBottom()
        {
            JSRuntime.InvokeVoidAsync("eval",
    @"this.setTimeout(function () {
var els = document.getElementsByName('console');
for(var i=0;i<els.length;i++)
els[i].scrollTop = els[i].scrollHeight;
},100);"
                );
        }

        private void OnCurrentContainerConsoleHistoryChanged(object sender, EventArgs e)
        {
            if (!Container.ShowConsoleHistory)
                return;
            InvokeAsync(() =>
            {
                StateHasChanged();
                scrollToBottom();
            });
        }

        private void StartContainer(ContainerContext container)
        {
            if (container == null)
                return;
            Action doStartContainer = () => container.Start();
            var warning = container.ContainerInfo.StartWarning;
            if (string.IsNullOrEmpty(warning))
            {
                doStartContainer();
            }
            else
            {
                modalAlert.Show(
                    $"容器[{container.ContainerInfo.Name}]启动警告",
                    warning,
                    () => doStartContainer());
            }
        }

        private void StopContainer(ContainerContext container)
        {
            if (container == null)
                return;

            Action doStopContainer = () =>
            {
                modalLoading.Show("停止容器", $"正在停止容器[{container.ContainerInfo.Name}]...", true, null);
                Task.Run(async () =>
                {
                    await container.Stop();
                    modalLoading.Close();
                    await InvokeAsync(StateHasChanged);
                });
            };
            var warning = container.ContainerInfo.StopWarning;
            if (string.IsNullOrEmpty(warning))
            {
                doStopContainer();
            }
            else
            {
                modalAlert.Show(
                    $"容器[{container.ContainerInfo.Name}]停止警告",
                    warning,
                    () => doStopContainer());
            }
        }

        private void RestartContainer(ContainerContext container)
        {
            if (container == null)
                return;
            modalLoading.Show("重启容器", $"正在重启容器[{container.ContainerInfo.Name}]...", true, null);
            Task.Run(async () =>
            {
                await container.Stop();
                container.Disable();
                while (container.IsConnected)
                    await Task.Delay(1000);
                container.Enable();
                while (!container.IsConnected)
                    await Task.Delay(1000);
                await container.Start();
                modalLoading.Close();
                await InvokeAsync(StateHasChanged);
                modalAlert.Show("重启容器", $"容器[{container.ContainerInfo.Name}]重启完成");
            });
        }


        private void EditContainer(YqdContainerInfo containerInfo)
        {
            modalWindow.Show("编辑容器", new DialogParameters<ContainerCreateControl>()
            {
                {x=> x.Model,containerInfo},
                {x=> x.OkAction,t =>
                    {
                        try
                        {
                            ContainerManager.Instance.Update(containerInfo, t);
                            ModalWindow?.UpdateTitle($"容器 - {containerInfo.Name} - 控制台");
                            modalWindow.Close();
                        }
                        catch (Exception ex)
                        {
                            modalAlert.Show("编辑容器时出错", ExceptionUtils.GetExceptionMessage(ex));
                        }
                    }
                }
            });
        }

        private void EnableContainer(ContainerContext container)
        {
            if (container == null)
                return;

            container.Enable();
        }

        private void DisableContainer(ContainerContext container)
        {
            if (container == null)
                return;

            container.Disable();
        }

        private void QueryLog()
        {
            var containerDir = ContainerPathUtils.GetContainerFolder(Container.ContainerInfo.Id);
            var logDir = Path.Combine(containerDir, "YiQiDong.Container.Logs");
            if (!Directory.Exists(logDir))
                logDir = containerDir;

            Action<string> afterSelectFileAction = t =>
            {
                modalWindow.Close();
                var logFile = t;

                modalWindow.Show($"容器[{Container.ContainerInfo.Name}] - 日志 - {Path.GetFileName(logFile)}", new DialogParameters<LogFileViewer>
                {
                    {x=>x.LogFile,logFile}
                });
            };

            modalWindow.Show($"选择要查看的日志文件", new DialogParameters<FileSelectControl>()
            {
                {x=>x.Dir, logDir},
                {x=>x.FileFilter, logDir},
                {x=>x.FileFilter, "*.log"},
                {x=>x.FileDoubleClickToDownload, false},
                {x=>x.FileDoubleClickCustomAction, afterSelectFileAction},
                {x=>x.SelectAction, afterSelectFileAction},
            });
        }
    }
}
