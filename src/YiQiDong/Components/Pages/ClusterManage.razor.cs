using Quick.Blazor.Bootstrap;
using Quick.Blazor.Bootstrap.Admin;
using System;
using YiQiDong.Cluster;
using YiQiDong.Cluster.Model;
using YiQiDong.Core.Utils;

namespace YiQiDong.Components.Pages
{
    public partial class ClusterManage : IDisposable
    {
        private ModalLoading modalLoading;
        private ModalAlert modalAlert;
        private ModalWindow modalWindow;
        private LogViewControl clusterLogView;
        protected override void OnInitialized()
        {
            base.OnInitialized();
            ClusterManager.Instance.ClusterChanged += ClusterManager_ClusterChanged;
            ClusterManager.Instance.NewLogArrived += ClusterManager_NewLogArrived;
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);
            if (firstRender)
            {
                foreach (var line in ClusterManager.Instance.GetLogs())
                    pushLog(line);
            }
        }

        private void ClusterManager_NewLogArrived(object sender, string e)
        {
            pushLog(e);
        }

        private void pushLog(string log)
        {
            clusterLogView.AddLine(log);
        }

        public void Dispose()
        {
            ClusterManager.Instance.ClusterChanged -= ClusterManager_ClusterChanged;
            ClusterManager.Instance.NewLogArrived -= ClusterManager_NewLogArrived;
        }

        private void ClusterManager_ClusterChanged(object sender, EventArgs e)
        {
            InvokeAsync(StateHasChanged);
        }

        private void CreateCluster()
        {
            modalWindow.Show("创建集群", new DialogParameters<Controls.ClusterCreateControl>()
            {
                {x=>x.OkAction,t =>
                    {
                        ClusterManager.Instance.Create(t);
                        modalWindow.Close();
                    }
                }
            });
        }

        private void DeleteCluster()
        {
            modalAlert.Show("删除确认", "确定要删除集群？", () =>
            {
                ClusterManager.Instance.Delete();
            });
        }

        private void StartCluster()
        {
            ClusterManager.Instance.StartCluster();
        }

        public void StopCluster()
        {
            ClusterManager.Instance.StopCluster();
        }

        private void AddClusterContainer()
        {
            modalWindow.Show("添加集群容器", new DialogParameters<Controls.ClusterContainerAddControl>()
            {
                {x=>x.OkAction, async t =>
                    {
                        try
                        {
                            await ClusterManager.Instance.AddClusterContainer(t);
                            modalWindow.Close();
                        }
                        catch (Exception ex)
                        {
                            modalAlert.Show("错误", "添加节点时失败，原因：" + ExceptionUtils.GetExceptionMessage(ex));
                        }
                    }
                }
            });
        }

        private void DeleteClusterContainer(ClusterContainerInfo item)
        {
            modalAlert.Show("删除确认", $"确定要从集群中删除容器[{item.ContainerName}]？", async () =>
            {
                await ClusterManager.Instance.DeleteClusterContainer(item);
            });
        }
    }
}
