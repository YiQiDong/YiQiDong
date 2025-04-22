using Microsoft.AspNetCore.Components;
using Quick.Blazor.Bootstrap;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net.NetworkInformation;
using YiQiDong.Core.Utils;

namespace YiQiDong.Components.Controls
{
    public partial class EditNetworkInterfaceControl
    {
        public enum NetworkInterfaceMethod
        {
            DHCP,
            Static
        }
        public class NetworkInterfaceConfig
        {
            [Required(ErrorMessage = "必须选择配置方式")]
            public NetworkInterfaceMethod Method { get; set; }
            [Required(ErrorMessage = "必须设置IP地址")]
            public string IPAddress { get; set; }
            [Required(ErrorMessage = "必须设置子网掩码")]
            public string NetMask { get; set; }
            public string Gateway { get; set; }
            public string DnsServer { get; set; }
        }

        public class DisplayNetworkInterfaceInfo
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string MacAddress { get; set; }
            public string IpAddress { get; set; }
            public OperationalStatus Status { get; set; }
            public NetworkInterfaceType Type { get; set; }
        }

        [Parameter]
        public NetworkInterfaceConfig CurrentNetworkInterfaceConfig { get; set; }

        [Parameter]
        public DisplayNetworkInterfaceInfo CurrentNetworkInterface { get; set; }

        public ModalLoading modalLoading { get; private set; }
        public ModalAlert modalAlert { get; private set; }

        private async void OkEditNetworkInterface()
        {
            try
            {
                modalLoading.Show($"修改网卡[{CurrentNetworkInterface.Name}]配置中...", null, true, null);
                await Task.Run(() =>
                {
                    if (OperatingSystem.IsWindows())
                    {
                        throw new PlatformNotSupportedException();
                    }
                    else
                    {
                        var configFile = $"/etc/network/interfaces.d/{CurrentNetworkInterface.Name}";
                        if (File.Exists(configFile))
                            File.Delete(configFile);
                        using (var fs = File.OpenWrite(configFile))
                        using (var writer = new StreamWriter(fs))
                        {
                            writer.WriteLine($"auto {CurrentNetworkInterface.Name}");
                            writer.WriteLine($"iface {CurrentNetworkInterface.Name} inet {CurrentNetworkInterfaceConfig.Method.ToString().ToLower()}");
                            if (CurrentNetworkInterfaceConfig.Method == NetworkInterfaceMethod.Static)
                            {
                                writer.WriteLine($"address {CurrentNetworkInterfaceConfig.IPAddress}");
                                writer.WriteLine($"netmask {CurrentNetworkInterfaceConfig.NetMask}");
                                if (!string.IsNullOrEmpty(CurrentNetworkInterfaceConfig.Gateway))
                                    writer.WriteLine($"gateway {CurrentNetworkInterfaceConfig.Gateway}");
                                if (!string.IsNullOrEmpty(CurrentNetworkInterfaceConfig.DnsServer))
                                    writer.WriteLine($"dns-nameserver {CurrentNetworkInterfaceConfig.DnsServer}");
                            }
                        }
                    }
                    modalAlert.Show("成功", "修改网卡配置成功!", null, null);
                    InvokeAsync(StateHasChanged);
                });
            }
            catch (Exception ex)
            {
                modalAlert.Show("修改网卡配置出错", ExceptionUtils.GetExceptionMessage(ex), null, null);
                await InvokeAsync(StateHasChanged);
                return;
            }

            //如果当前网卡是启用状态，则重启网卡
            if (CurrentNetworkInterface.Status == OperationalStatus.Up)
            {
                try
                {
                    modalLoading.Show($"重启网卡[{CurrentNetworkInterface.Name}]中...", null, true, null);
                    await Task.Run(() =>
                    {
                        innerDisableNI(CurrentNetworkInterface);
                        innerEnableNI(CurrentNetworkInterface);
                    });
                }
                catch (Exception ex)
                {
                    modalAlert.Show("重启网卡时出错", ExceptionUtils.GetExceptionMessage(ex), null, null);
                    await InvokeAsync(StateHasChanged);
                    return;
                }
            }
            modalLoading.Close();
            await InvokeAsync(StateHasChanged);
        }


        public static void innerEnableNI(DisplayNetworkInterfaceInfo model)
        {
            //如果是Windows平台，则调用WMI
            if (OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException();
            }
            //否则调用ifup
            else
            {
                ProcessStartInfo psi = new ProcessStartInfo("ifup", model.Name);
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                var process = Process.Start(psi);
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEnd();
                    throw new ApplicationException(error);
                }
            }
        }

        public static void innerDisableNI(DisplayNetworkInterfaceInfo model)
        {
            //如果是Windows平台，则调用WMI
            if (OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException();
            }
            //否则调用ifdown
            else
            {
                ProcessStartInfo psi = new ProcessStartInfo("ifdown", model.Name);
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                var process = Process.Start(psi);
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEnd();
                    throw new ApplicationException(error);
                }
            }
        }
    }
}