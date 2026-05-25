using Microsoft.AspNetCore.Components;
using YiQiDong.Core.Protocol.V1.Model;

namespace YiQiDong.Components.Controls
{
    public partial class ConfigFilesControl
    {
        [Parameter]
        public ConfigFileInfo[] ConfigFiles { get; set; }
        [Parameter]
        public RenderFragment ToolbarAddonButtons { get; set; }

        private Dictionary<string, ConfigFileInfo> configFileDict;
        private ConfigFileInfo ConfigFileInfo;
        private string _ConfigFile;
        public string ConfigFile
        {
            get
            {
                return _ConfigFile;
            }
            set
            {
                _ConfigFile = value;
                if (value != null)
                    ConfigFileInfo = configFileDict[value];
            }
        }

        protected override void OnParametersSet()
        {
            configFileDict = ConfigFiles.ToDictionary(t => t.FilePath, t => t);
            ConfigFile = configFileDict.Keys.FirstOrDefault();
        }
    }
}
