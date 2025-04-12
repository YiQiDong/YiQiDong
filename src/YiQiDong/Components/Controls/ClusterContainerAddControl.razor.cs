using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YiQiDong.Cluster;
using YiQiDong.Cluster.Model;
using YiQiDong.Core;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Components.Controls
{
    public partial class ClusterContainerAddControl
    {
        private ClusterContainerInfo createModel = new ClusterContainerInfo();
        [Parameter]
        public ClusterContainerInfo Model { get; set; }
        [Parameter]
        public Action<ClusterContainerInfo> OkAction { get; set; }

        private string[] containerNames;

        private void Ok()
        {
            OkAction?.Invoke(createModel);
        }

        protected override async Task OnParametersSetAsync()
        {
            if (Model == null)
            {

            }
            else
            {
                createModel.ContainerName = Model.ContainerName;
                createModel.IsSelfNodeActive = Model.IsSelfNodeActive;
            }

            var rep = await ClusterManager.Instance.OppositeNodeContext.Channel.SendCommand(
                new Cluster.Protocol.QpCommands.GetContainerList.Request());
            var oppositeContainerNames = rep.Items
                    .Where(t => !t.AutoStart)
                    .Select(t => t.Name)
                    .ToHashSet();

            var selfNodeContainerNames = ContainerManager.Instance.GetAll()
                    .Select(t => t.ContainerInfo)
                    .Where(t => !t.AutoStart)
                    .Select(t => t.Name)
                    .ToHashSet();

            containerNames = selfNodeContainerNames
                .Where(t => oppositeContainerNames.Contains(t) && !ClusterManager.Instance.ContainsContainer(t))
                .ToArray();
            await InvokeAsync(StateHasChanged);
        }

        public static Dictionary<string, object> PrepareParameter(ClusterContainerInfo model, Action<ClusterContainerInfo> okAction)
        {
            return new Dictionary<string, object>()
            {
                [nameof(Model)] = model,
                [nameof(OkAction)] = okAction
            };
        }
    }
}
