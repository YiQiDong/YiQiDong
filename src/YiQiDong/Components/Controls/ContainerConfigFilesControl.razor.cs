using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using YiQiDong.Core;
using YiQiDong.Core.Protocol.V1.Model;

namespace YiQiDong.Components.Controls
{
    public partial class ContainerConfigFilesControl
    {
        [Parameter]
        public ContainerContext Container { get; set; }

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
                ConfigFileInfo = configFileDict[value];
            }
        }

        protected override void OnParametersSet()
        {
            configFileDict = Container.ConfigFiles.ToDictionary(t => t.FilePath, t => t);
            ConfigFile = configFileDict.Keys.FirstOrDefault();
        }
    }
}
