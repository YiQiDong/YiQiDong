using Microsoft.AspNetCore.Components;
using Quick.Blazor.Bootstrap.Terminal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using YiQiDong.Core;
using YiQiDong.Core.Utils;
using YiQiDong.Protocol.V1.Model;
using YiQiDong.Utils;

namespace YiQiDong.Components.Controls
{
    public partial class ImageConsoleControl : ComponentBase
    {
        [Parameter]
        public ImageInfo Image { get; set; }

        private TerminalControl terminalControl;
        private string WorkingDir;
        private Dictionary<string, string> environment;
        private Dictionary<string, Action> otherButtons;

        protected override void OnParametersSet()
        {
            WorkingDir = ImagePathUtils.GetImageFolder(Image.Id);
            environment = ImageManager.Instance.GetImageEnvironment(Image);
            environment["PATH"] = RuntimeUtils.CombineEnviromentPath(ImageManager.Instance.GetImagePath(Image));
            if (Image.TestCommand != null)
            {
                otherButtons = new Dictionary<string, Action>();
                foreach (var testCommand in Image.TestCommand)
                {
                    otherButtons[testCommand.Key] = () =>
                    {
                        var commandLine = string.Join(' ', testCommand.Value);
                        terminalControl.ExecuteCommand(commandLine);
                    };
                }
            }
        }
    }
}
