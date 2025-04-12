using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using YiQiDong.Cluster.Model;

namespace YiQiDong.Components.Controls
{
    public partial class ClusterCreateControl
    {
        private ClusterConfig createModel = new ClusterConfig();
        [Parameter]
        public ClusterConfig Model { get; set; }
        [Parameter]
        public Action<ClusterConfig> OkAction { get; set; }

        private void Ok()
        {
            OkAction?.Invoke(createModel);
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (Model == null)
            {

            }
            else
            {
                createModel.SelfNodeUrl = Model.SelfNodeUrl;
                createModel.OppositeNodeUrl = Model.OppositeNodeUrl;
                createModel.OppositeNodePassword = Model.OppositeNodePassword;
                createModel.AutoStart = Model.AutoStart;
                createModel.ClusterContainerList = Model.ClusterContainerList;
            }
        }

        public static Dictionary<string, object> PrepareParameter(ClusterConfig model, Action<ClusterConfig> okAction)
        {
            return new Dictionary<string, object>()
            {
                [nameof(Model)] = model,
                [nameof(OkAction)] = okAction
            };
        }
    }
}
