using Microsoft.AspNetCore.Components;
using Quick.Blazor.Bootstrap;
using Quick.Blazor.Bootstrap.Utils;
using System;
using System.Collections.Generic;
using YiQiDong.Components.Controls;

namespace YiQiDong.Components.Controls
{
    public partial class CommonToolsControl : ComponentBase
    {
        private ModalWindow modalWindow;

        [Parameter]
        public ToolInfo[] Tools { get; set; }

        public class ToolInfo
        {
            public string Name { get; set; }
            public string IconClass { get; set; }
            public RenderFragment Content { get; set; }

            public static ToolInfo Create<T>(string name, string iconClass, Dictionary<string, object> parameterDict = null)
            {
                return new ToolInfo()
                {
                    Name = name,
                    IconClass = iconClass,
                    Content = BlazorUtils.GetRenderFragment<T>(parameterDict)
                };
            }
        }

        public void Show(ToolInfo toolInfo)
        {
            modalWindow.Show(toolInfo.Name, toolInfo.Content);
        }
    }
}
