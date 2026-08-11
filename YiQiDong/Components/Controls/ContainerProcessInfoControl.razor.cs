using Microsoft.AspNetCore.Components;
using YiQiDong.Core;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Components.Controls
{
    public partial class ContainerProcessInfoControl : ComponentBase
    {
        [Parameter]
        public ContainerContext Container { get; set; }

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
    }
}
