using Microsoft.AspNetCore.Components;
using Quick.Blazor.Bootstrap;
using YiQiDong.Utils;

namespace YiQiDong.Components.Pages
{
    public partial class Home : IDisposable
    {
        private ModalWindow modalWindow;
        private ModalLoading modalLoading;
        private ModalAlert modalAlert;
        private SystemInfoContext systemInfoContext;

        [Parameter]
        public IPageNavigater IPageNavigater { get; set; }

        protected override void OnInitialized()
        {
            systemInfoContext = Program.SystemInfoContext;
            systemInfoContext.DataChanged += SystemInfoContext_DataChanged;
        }

        private void SystemInfoContext_DataChanged(object sender, EventArgs e)
        {
            InvokeAsync(StateHasChanged).Wait();
        }

        private void showFolder(string path)
        {
            modalWindow.Show("文件管理",
                new DialogParameters<Controls.FileManageControl>()
                {
                    {x=>x.Dir,path}
                });
        }

        private void showProcessView(int pid)
        {
            modalWindow.Show(
                $"进程[{pid}]",
                new DialogParameters<Quick.Blazor.Bootstrap.Admin.ProcessViewControl>()
                {
                    {x=>x.PID, pid}
                });
        }

        private void showProcessManage()
        {
            modalWindow.Show<Quick.Blazor.Bootstrap.Admin.ProcessManageControl>("进程管理");

        }

        public void Dispose()
        {
            systemInfoContext.DataChanged -= SystemInfoContext_DataChanged;
        }
    }
}
