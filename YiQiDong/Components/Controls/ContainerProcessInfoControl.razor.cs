using Microsoft.AspNetCore.Components;
using Quick.Utils;
using YiQiDong.Core;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Components.Controls
{
    public partial class ContainerProcessInfoControl : ComponentBase
    {
        [Parameter]
        public ContainerContext Container { get; set; }
        private Dictionary<string, string> enviromentVariables;
        private string[] enviromentVariablesErrorLines;

        public bool EnableCompress
        {
            get => Container.ProcessChannel != null && Container.ProcessChannel.EnableCompress;
            set { }
        }

        public bool EnableEncrypt
        {
            get => Container.ProcessChannel != null && Container.ProcessChannel.EnableEncrypt;
            set { }
        }

        public bool EnableNetstat
        {
            get => Container.ProcessChannel != null && Container.ProcessChannel.Options.EnableNetstat;
            set { }
        }

        public async Task RefreshEnviromentVariables()
        {
            try
            {
                enviromentVariablesErrorLines = null;
                enviromentVariables = null;
                enviromentVariables = await Container.GetEnviromentVariables();
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                enviromentVariablesErrorLines = ExceptionUtils.GetExceptionString(ex).Split(Environment.NewLine);
            }
        }
    }
}
