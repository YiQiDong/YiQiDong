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

        public NetworkInterfaceConfig CurrentNetworkInterfaceConfig { get; set; }
        private string ErrorMessage;

        [Parameter]
        public DisplayNetworkInterfaceInfo CurrentNetworkInterface { get; set; }

        public ModalLoading modalLoading { get; private set; }
        public ModalAlert modalAlert { get; private set; }

        private const string NETWORK_CONFIGS_FOLDER = $"/etc/network/interfaces.d";
        private const string NETWORK_CONFIG_ENTRY_FILE = $"/etc/network/interfaces";

        private int startLine = -1;
        private int endLine = -1;
        private string currentConfigFile = null;
        private string[] currentConfigFileLines = null;

        private NetworkInterfaceConfig GetNetworkInterfaceConfig(DisplayNetworkInterfaceInfo model)
        {
            NetworkInterfaceConfig config = null;
            if (OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException();
            }
            else
            {
                config = new NetworkInterfaceConfig() { Method = NetworkInterfaceMethod.DHCP };
                var allConfigFileList = Directory.GetFiles(NETWORK_CONFIGS_FOLDER).ToList();
                allConfigFileList.Add(NETWORK_CONFIG_ENTRY_FILE);
                foreach (var file in allConfigFileList)
                {
                    var lines = File.ReadAllLines(file);
                    for (var i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i].Trim();
                        //如果是注释
                        if (line.StartsWith("#"))
                            continue;
                        if (line.StartsWith("auto ") && line.EndsWith(" " + model.Id))
                        {
                            startLine = i;
                            currentConfigFile = file;
                            currentConfigFileLines = lines;
                        }
                        if (startLine < 0)
                            continue;

                        //读取到空行，说明读取此网卡配置完成
                        if (string.IsNullOrEmpty(line))
                        {
                            endLine = i;
                            break;
                        }

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
                    if (endLine < 0)
                        endLine = lines.Length;
                }
            }
            return config;
        }

        protected override void OnParametersSet()
        {
            try
            {
                CurrentNetworkInterfaceConfig = GetNetworkInterfaceConfig(CurrentNetworkInterface);
            }
            catch (Exception ex)
            {
                ErrorMessage = ExceptionUtils.GetExceptionMessage(ex);
            }
        }

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
                        var bakNetworkConfigFile = NETWORK_CONFIG_ENTRY_FILE + ".bak";
                        if (File.Exists(bakNetworkConfigFile))
                            File.Delete(bakNetworkConfigFile);
                        File.Move(NETWORK_CONFIG_ENTRY_FILE, bakNetworkConfigFile);

                        using (var fs = File.OpenWrite(currentConfigFile))
                        using (var writer = new StreamWriter(fs))
                        {
                            //配置前面
                            for (var i = 0; i < startLine; i++)
                                writer.WriteLine(currentConfigFileLines[i]);
                            //本网卡部分
                            writer.WriteLine($"auto {CurrentNetworkInterface.Name}");
                            writer.WriteLine($"iface {CurrentNetworkInterface.Name} inet {CurrentNetworkInterfaceConfig.Method.ToString().ToLower()}");
                            if (CurrentNetworkInterfaceConfig.Method == NetworkInterfaceMethod.Static)
                            {
                                if (!string.IsNullOrEmpty(CurrentNetworkInterfaceConfig.IPAddress))
                                    writer.WriteLine($"address {CurrentNetworkInterfaceConfig.IPAddress}");
                                if (!string.IsNullOrEmpty(CurrentNetworkInterfaceConfig.NetMask))
                                    writer.WriteLine($"netmask {CurrentNetworkInterfaceConfig.NetMask}");
                                if (!string.IsNullOrEmpty(CurrentNetworkInterfaceConfig.Gateway))
                                    writer.WriteLine($"gateway {CurrentNetworkInterfaceConfig.Gateway}");
                                if (!string.IsNullOrEmpty(CurrentNetworkInterfaceConfig.DnsServer))
                                    writer.WriteLine($"dns-nameserver {CurrentNetworkInterfaceConfig.DnsServer}");
                            }
                            //配置后面
                            for (var i = endLine; i < currentConfigFileLines.Length; i++)
                                writer.WriteLine(currentConfigFileLines[i]);
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