using Microsoft.AspNetCore.Components;
using YiQiDong.Components.Controls;
using static YiQiDong.Components.Controls.CommonToolsControl;

namespace YiQiDong.Components.Pages
{
    public partial class CommonTools_Glash : ComponentBase
    {
        private ToolInfo[] tools;

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            tools = new[]
            {
                ToolInfo.Create<GlashServerControl>("Glash服务端","fa fa-server"),
                ToolInfo.Create<GlashAgentControl>("Glash代理端","fa fa-black-tie"),
                ToolInfo.Create<Glash.Blazor.Client.Main>("Glash客户端","fa fa-coffee")
            };
        }
    }
}
