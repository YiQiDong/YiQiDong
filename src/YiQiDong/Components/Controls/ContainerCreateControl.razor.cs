using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using YiQiDong.Core;
using YiQiDong.Model;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Components.Controls
{
    public partial class ContainerCreateControl : ComponentBase
    {
        private CreateContainerModel createModel = new CreateContainerModel();
        [Parameter]
        public YqdContainerInfo Model { get; set; }
        [Parameter]
        public Action<CreateContainerModel> OkAction { get; set; }
        
        public class SelectTagInfo
        {
            public string Name { get; set; }
            public bool Checked { get; set; }
        }

        public class SelectRuntimeInfo
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public Version Version { get; set; }
            public bool Checked { get; set; }
        }

        private SelectTagInfo[] selectTags;
        private SelectRuntimeInfo[] selectRuntimes;

        private void Ok()
        {
            createModel.Tags = selectTags?.Where(t => t.Checked).Select(t => t.Name).ToArray();
            createModel.RuntimeIds = selectRuntimes?.Where(t => t.Checked).Select(t => t.Id).ToArray();
            OkAction?.Invoke(createModel);
        }
        
        private void checkTag(SelectTagInfo tag)
        {
            tag.Checked = !tag.Checked;
        }

        private void checkRuntime(SelectRuntimeInfo runtime)
        {
            runtime.Checked = !runtime.Checked;
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            selectTags = TagManager.Instance.GetTags()?.Select(t => new SelectTagInfo() { Name = t }).ToArray();
            selectRuntimes = RuntimeManager.Instance.GetAll().Select(t =>
            {
                return new SelectRuntimeInfo()
                {
                    Id = t.Id,
                    Name = t.Name,
                    Version = Version.Parse(t.Version)
                };
            }).ToArray();
            if (Model == null)
            {
                createModel.PropertyChanged += CreateContainerModel_PropertyChanged;
            }
            else
            {
                createModel.ImageId = Model.ImageId;
                createModel.Id = Model.Id;
                createModel.Name = Model.Name;
                createModel.Description = Model.Description;
                createModel.Tags = Model.Tags;
                if (createModel.Tags != null && selectTags != null)
                {
                    foreach (var tag in createModel.Tags)
                    {
                        var model = selectTags.FirstOrDefault(t => t.Name == tag);
                        if (model == null)
                            continue;
                        model.Checked = true;
                    }
                }
                createModel.RuntimeIds = Model.RuntimeIds;
                if (createModel.RuntimeIds != null && selectRuntimes != null)
                {
                    foreach (var runtimeId in createModel.RuntimeIds)
                    {
                        var model = selectRuntimes.FirstOrDefault(t => t.Id == runtimeId);
                        if (model == null)
                            continue;
                        model.Checked = true;
                    }
                }
                createModel.StartScript = Model.StartScript;
                createModel.StartWarning = Model.StartWarning;
                createModel.StopScript = Model.StopScript;
                createModel.StopWarning = Model.StopWarning;
                createModel.LogIgnoreList = Model.LogIgnoreList;
                createModel.LogLevel = Model.LogLevel;
                createModel.EnableRecordLog = Model.EnableRecordLog;
                createModel.LogSaveDays = Model.LogSaveDays;
                createModel.StartCron = Model.StartCron;
                createModel.StopCron = Model.StopCron;
                createModel.RestartCron = Model.RestartCron;
                createModel.EnvironmentVariables = Model.EnvironmentVariables;
            }
        }

        private void CreateContainerModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(CreateContainerModel.ImageId):
                    var image = ImageManager.Instance.Get(createModel.ImageId);

                    foreach (var model in selectTags)
                        model.Checked = false;
                    foreach (var model in selectRuntimes)
                        model.Checked = false;
                    if (image != null)
                    {
                        if (image.Tags != null)
                        {
                            foreach (var tag in image.Tags)
                            {
                                var model = selectTags.FirstOrDefault(t => t.Name == tag);
                                if (model == null)
                                    continue;
                                model.Checked = true;
                            }
                        }
                        if (image.Runtime != null)
                        {
                            foreach (var runtimeId in image.Runtime)
                            {
                                var nameAndVersion = NameAndVersion.Parse(runtimeId);
                                var versionString = nameAndVersion.Version;
                                if (!versionString.Contains("."))
                                    versionString += ".0";
                                var version = Version.Parse(versionString);
                                var model = selectRuntimes
                                    .Where(t => t.Name == nameAndVersion.Name && t.Version >= version)
                                    .OrderBy(t => t.Version).FirstOrDefault();
                                if (model == null)
                                    continue;
                                model.Checked = true;
                            }
                        }
                    }
                    break;
            }
        }
    }
}
