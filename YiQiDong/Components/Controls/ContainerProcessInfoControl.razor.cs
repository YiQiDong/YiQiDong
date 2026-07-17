using Microsoft.AspNetCore.Components;
using YiQiDong.Core;

namespace YiQiDong.Components.Controls
{
    public partial class ContainerProcessInfoControl : ComponentBase
    {
        [Parameter]
        public ContainerContext Container { get; set; }
    }
}
