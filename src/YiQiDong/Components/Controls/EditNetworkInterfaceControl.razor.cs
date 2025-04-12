using Microsoft.AspNetCore.Components;
using Quick.Blazor.Bootstrap;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using YiQiDong.Core.Utils;

#pragma warning disable CA1416
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

        public static void queryWin32_NetworkAdapter(string id, Action<ManagementObject> handler)
        {
            var wmiQuery = new SelectQuery($"select * from Win32_NetworkAdapter where GUID = '{id}'");
            var searchProcedure = new ManagementObjectSearcher(wmiQuery);
            var items = searchProcedure.Get();
            if (items.Count == 0)
                throw new IOException($"未找到编号为[{id}]的网卡。");
            foreach (ManagementObject item in items)
                handler(item);
        }

        public static void queryWin32_NetworkAdapterConfiguration(string id, Action<ManagementObject> handler)
        {
            var wmiQuery = new SelectQuery($"select * from Win32_NetworkAdapterConfiguration where SettingID = '{id}'");
            var searchProcedure = new ManagementObjectSearcher(wmiQuery);
            var items = searchProcedure.Get();
            if (items.Count == 0)
                throw new IOException($"未找到编号为[{id}]的网卡。");
            foreach (ManagementObject item in items)
                handler(item);
        }

        private async void OkEditNetworkInterface()
        {
            try
            {
                modalLoading.Show($"修改网卡[{CurrentNetworkInterface.Name}]配置中...", null, true, null);
                await Task.Run(() =>
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        //设置IP地址、子网掩码
                        var wmiQuery = new SelectQuery($"select * from Win32_NetworkAdapterConfiguration where SettingID = '{CurrentNetworkInterface.Id}'");
                        var searchProcedure = new ManagementObjectSearcher(wmiQuery);
                        var items = searchProcedure.Get();
                        if (items.Count == 0)
                            throw new IOException($"未找到编号为[{CurrentNetworkInterface.Id}]的网卡。");

                        foreach (ManagementObject item in items)
                        {
                            switch (CurrentNetworkInterfaceConfig.Method)
                            {
                                case NetworkInterfaceMethod.DHCP:
                                    item.InvokeMethod("EnableDHCP", null);
                                    queryWin32_NetworkAdapterConfiguration(CurrentNetworkInterface.Id, item =>
                                    {
                                        //设置DNS服务器
                                        item.InvokeMethod("SetDNSServerSearchOrder", null);
                                    });
                                    break;
                                case NetworkInterfaceMethod.Static:
                                    ManagementBaseObject newIP = item.GetMethodParameters("EnableStatic");
                                    newIP["IPAddress"] = CurrentNetworkInterfaceConfig.IPAddress.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                    newIP["SubnetMask"] = CurrentNetworkInterfaceConfig.NetMask.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                    item.InvokeMethod("EnableStatic", newIP, null);
                                    queryWin32_NetworkAdapterConfiguration(CurrentNetworkInterface.Id, item =>
                                    {
                                        //设置网关
                                        {
                                            var inPar = item.GetMethodParameters("SetGateways");
                                            inPar["DefaultIPGateway"] = CurrentNetworkInterfaceConfig.Gateway.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                            item.InvokeMethod("SetGateways", inPar, null);
                                        }
                                        //设置DNS服务器
                                        {
                                            var inPar = item.GetMethodParameters("SetDNSServerSearchOrder");
                                            inPar["DNSServerSearchOrder"] = CurrentNetworkInterfaceConfig.DnsServer.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                            item.InvokeMethod("SetDNSServerSearchOrder", inPar, null);
                                        }
                                    });
                                    break;
                            }
                        }
                    }
                    else
                    {
                        var configFile = $"/etc/network/interfaces.d/{CurrentNetworkInterface.Name}";
                        if (File.Exists(configFile))
                            File.Delete(configFile);
                        using (var fs = File.OpenWrite(configFile))
                        using (var writer = new StreamWriter(fs))
                        {
                            writer.WriteLine($"allow-hotplug {CurrentNetworkInterface.Name}");
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                queryWin32_NetworkAdapter(model.Id, item =>
                {
                    var ret = (uint)item.InvokeMethod("Enable", null);
                    if (ret != 0)
                        throw new ApplicationException("错误码：" + ret);
                });
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                queryWin32_NetworkAdapter(model.Id, item =>
                {
                    var ret = (uint)item.InvokeMethod("Disable", null);
                    if (ret != 0)
                        throw new ApplicationException("错误码：" + ret);
                });
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
#pragma warning restore CA1416