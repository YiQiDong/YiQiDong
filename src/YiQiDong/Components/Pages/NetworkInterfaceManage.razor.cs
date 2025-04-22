using Quick.Blazor.Bootstrap;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using YiQiDong.Core.Utils;
using static YiQiDong.Components.Controls.EditNetworkInterfaceControl;

#pragma warning disable CA1416
namespace YiQiDong.Components.Pages
{
    public partial class NetworkInterfaceManage
    {
        public ModalLoading modalLoading { get; private set; }
        public ModalAlert modalAlert { get; private set; }
        public ModalWindow modalWindow { get; private set; }
        public ToastStack toastStack { get; private set; }
        private DisplayNetworkInterfaceInfo[] NetworkInterfaces;

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    refreshNetworkInterfaces();
                }
                catch (Exception ex)
                {
                    modalAlert.Show("错误", ExceptionUtils.GetExceptionString(ex));
                }
            }
        }

        private void refreshNetworkInterfaces()
        {
            NetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(t => t.NetworkInterfaceType != NetworkInterfaceType.Loopback
                            && t.NetworkInterfaceType != NetworkInterfaceType.Ppp
                            && t.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    .Select(t => new DisplayNetworkInterfaceInfo()
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Description = t.Description,
                        MacAddress = t.GetPhysicalAddress()?.ToString(),
                        IpAddress = string.Join(",", t.GetIPProperties().UnicastAddresses.Select(t => t.Address).Where(t => t.AddressFamily == AddressFamily.InterNetwork)),
                        Status = t.OperationalStatus,
                        Type = t.NetworkInterfaceType
                    })
                    .ToArray();
        }

        private void btnRefresh_Click()
        {
            modalLoading.Show("刷新网卡列表", "刷新网卡列表中...", true, null);
            Task.Run(() =>
            {
                refreshNetworkInterfaces();
                modalLoading.Close();
                InvokeAsync(StateHasChanged);
            });
        }

        private void EditNI(DisplayNetworkInterfaceInfo model)
        {
            try
            {
                modalWindow.Show<Controls.EditNetworkInterfaceControl>($"编辑网卡[{model.Name}]", new Dictionary<string, object>()
                {
                    [nameof(Controls.EditNetworkInterfaceControl.CurrentNetworkInterface)] = model
                });
            }
            catch (Exception ex)
            {
                modalAlert.Show("获取网卡配置时出错", ExceptionUtils.GetExceptionString(ex), null, null);
                return;
            }
        }

        private void EnableNI(DisplayNetworkInterfaceInfo model)
        {
            modalAlert.Show(
                "启用确认",
                $"确定要启用网卡[{model.Name}]?",
                () =>
                {
                    modalLoading.Show("启用网卡", $"正在启用网卡[{model.Name} - {model.Description}]...", true, null);
                    Task.Run(() =>
                    {
                        try
                        {
                            innerEnableNI(model);
                            refreshNetworkInterfaces();
                            toastStack.AddToast("信息", $"启用网卡[{model.Name} - {model.Description}]成功！", BackgroundTheme.success);
                        }
                        catch (Exception ex)
                        {
                            toastStack.AddToast("错误", $"启用网卡网卡[{model.Name} - {model.Description}]时出错！原因：{ExceptionUtils.GetExceptionMessage(ex)}", BackgroundTheme.danger);
                        }
                        refreshNetworkInterfaces();
                        modalLoading.Close();
                        InvokeAsync(() => StateHasChanged());
                    });
                },
                null);
        }

        private void DislabeNI(DisplayNetworkInterfaceInfo model)
        {
            modalAlert.Show(
                "禁用确认",
                $"确定要禁用网卡[{model.Name}]?",
                () =>
                {
                    modalLoading.Show("禁用网卡", $"正在禁用网卡[{model.Name} - {model.Description}]...", true, null);
                    Task.Run(() =>
                    {
                        try
                        {
                            innerDisableNI(model);
                            refreshNetworkInterfaces();
                            toastStack.AddToast("信息", $"禁用网卡[{model.Name} - {model.Description}]成功！", BackgroundTheme.success);
                        }
                        catch (Exception ex)
                        {
                            toastStack.AddToast("错误", $"禁用网卡网卡[{model.Name} - {model.Description}]时出错！原因：{ExceptionUtils.GetExceptionMessage(ex)}", BackgroundTheme.danger);
                        }
                        refreshNetworkInterfaces();
                        modalLoading.Close();
                        InvokeAsync(() => StateHasChanged());
                    });
                },
                null);
        }
    }
}
#pragma warning restore CA1416