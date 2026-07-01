using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Quick.Blazor.Bootstrap.Terminal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using YiQiDong.Core;
using YiQiDong.Core.Protocol.V1.Model;
using YiQiDong.Core.Utils;
using YiQiDong.Utils;

namespace YiQiDong.Components.Controls
{
    public partial class RuntimeConsoleControl : ComponentBase
    {
        [Parameter]
        public RuntimeInfo Runtime { get; set; }

        private TerminalControl terminalControl;
        private string WorkingDir;
        private Dictionary<string, string> environment;
        private Dictionary<string, Action> otherButtons;

        protected override void OnParametersSet()
        {
            WorkingDir = RuntimePathUtils.GetRuntimeFolder(Runtime.Id);
            environment = RuntimeManager.Instance.GetRuntimesEnvironment(Runtime);
            environment["PATH"] = RuntimeUtils.CombineEnviromentPath(RuntimeManager.Instance.GetRuntimesPath(Runtime));
            if (Runtime.TestCommand != null)
            {
                otherButtons = new Dictionary<string, Action>();
                foreach (var testCommand in Runtime.TestCommand)
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
