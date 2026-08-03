using Microsoft.AspNetCore.Components;
using YiQiDong.Core;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Components.Controls
{
    public partial class ContainerProcessInfoControl : ComponentBase
    {
        [Parameter]
        public ContainerContext Container { get; set; }
    }
}
