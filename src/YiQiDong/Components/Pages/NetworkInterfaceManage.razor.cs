using Quick.Blazor.Bootstrap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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

        private NetworkInterfaceConfig GetNetworkInterfaceConfig(DisplayNetworkInterfaceInfo model)
        {
            NetworkInterfaceConfig config = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                queryWin32_NetworkAdapterConfiguration(model.Id, item =>
                {
                    var tmpArray = (string[])item.GetPropertyValue("IPAddress");
                    var ipAddress = (string)null;
                    var ipAddressList = new string[0];
                    if (tmpArray != null)
                    {
                        ipAddressList = tmpArray.Where(t => !t.Contains(":")).ToArray();
                        ipAddress = string.Join(",", ipAddressList);
                    }

                    var netMask = (string)null;
                    tmpArray = (string[])item.GetPropertyValue("IPSubnet");
                    if (tmpArray != null)
                        netMask = string.Join(",", tmpArray.Take(ipAddressList.Length));

                    tmpArray = (string[])item.GetPropertyValue("DefaultIPGateway");
                    var gateway = (string)null;
                    if (tmpArray != null)
                        gateway = string.Join(",", tmpArray);

                    tmpArray = (string[])item.GetPropertyValue("DNSServerSearchOrder");
                    var dnsServer = (string)null;
                    if (tmpArray != null)
                        dnsServer = string.Join(",", tmpArray);

                    config = new NetworkInterfaceConfig()
                    {
                        Method = item.GetPropertyValue("DHCPEnabled").ToString() == "True" ? NetworkInterfaceMethod.DHCP : NetworkInterfaceMethod.Static,
                        IPAddress = ipAddress,
                        NetMask = netMask,
                        Gateway = gateway,
                        DnsServer = dnsServer
                    };
                });
            }
            else
            {
                config = new NetworkInterfaceConfig() { Method = NetworkInterfaceMethod.DHCP };
                var configFile = $"/etc/network/interfaces.d/{model.Name}";
                if (File.Exists(configFile))
                {
                    var lines = File.ReadAllLines(configFile);
                    foreach (var t in lines)
                    {
                        var line = t.Trim();
                        if (string.IsNullOrEmpty(line))
                            continue;
                        //如果是注释
                        if (line.StartsWith("#"))
                            continue;
                        var segments = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        var key = segments[0];
                        switch (key)
                        {
                            case "iface":
                                if (segments.Length >= 4)
                                {
                                    switch (segments[3])
                                    {
                                        case "dhcp":
                                            config.Method = NetworkInterfaceMethod.DHCP;
                                            break;
                                        case "static":
                                            config.Method = NetworkInterfaceMethod.Static;
                                            break;
                                    }
                                }
                                break;
                            case "address":
                                if (segments.Length >= 2)
                                    config.IPAddress = segments[1];
                                break;
                            case "netmask":
                                if (segments.Length >= 2)
                                    config.NetMask = segments[1];
                                break;
                            case "gateway":
                                if (segments.Length >= 2)
                                    config.Gateway = segments[1];
                                break;
                            case "dns-nameserver":
                                if (segments.Length >= 2)
                                    config.DnsServer = segments[1];
                                break;
                        }
                    }
                }
            }
            return config;
        }

        private void EditNI(DisplayNetworkInterfaceInfo model)
        {
            try
            {
                var config = GetNetworkInterfaceConfig(model);
                modalWindow.Show<Controls.EditNetworkInterfaceControl>($"编辑网卡[{model.Name}]", new Dictionary<string, object>()
                {
                    [nameof(Controls.EditNetworkInterfaceControl.CurrentNetworkInterface)] = model,
                    [nameof(Controls.EditNetworkInterfaceControl.CurrentNetworkInterfaceConfig)] = config
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