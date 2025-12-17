using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using YiQiDong.Core;

namespace YiQiDong.Components.Controls
{
    public partial class FileSelectControl
    {
        [Parameter]
        public string Dir { get; set; }
        [Parameter]
        public string FileFilter { get; set; }
        [Parameter]
        public string SelectedPath { get; set; }
        [Parameter]
        public bool FileDoubleClickToDownload { get; set; } = true;
        [Parameter]
        public Action<string> SelectAction { get; set; }
        [Parameter]
        public Action<string> FileDoubleClickCustomAction { get; set; }

        private Quick.Blazor.Bootstrap.Admin.FileManageControl fileManageControl;

        private void Select()
        {
            SelectAction?.Invoke(fileManageControl.SelectedPath);
        }

        private void onFileDoubleClick(IJSRuntime jsRuntime)
        {
            FileDoubleClickCustomAction?.Invoke(fileManageControl.SelectedPath);
        }
    }
}
