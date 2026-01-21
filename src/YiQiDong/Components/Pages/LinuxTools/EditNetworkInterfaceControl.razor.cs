using Microsoft.AspNetCore.Components;
using Quick.Blazor.Bootstrap;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;
using YiQiDong.Core.Utils;

namespace YiQiDong.Components.Pages.LinuxTools
{
    public partial class EditNetworkInterfaceControl
    {
        public enum ActiveMethod
        {
            /// <summary>
            /// 无
            /// </summary>
            None,
            /// <summary>
            /// 自动激活
            /// </summary>
            Auto,
            /// <summary>
            /// 插入网线时激活
            /// </summary>
            AllowHotPlug
        }

        public enum NetworkInterfaceMethod
        {
            DHCP,
            Static
        }

        public class ConfigFileInfo
        {
            public string File { get; set; }
            public string[] FileLines { get; set; }
            public int StartLine { get; set; }
            public int EndLine { get; set; }
        }

        public class NetworkInterfaceNetworkConfig
        {
            public string IPVersion { get; set; }
            [Required(ErrorMessage = "必须选择配置方式")]
            public NetworkInterfaceMethod Method { get; set; }
            [Required(ErrorMessage = "必须设置IP地址")]
            public string IPAddress { get; set; }
            [Required(ErrorMessage = "必须设置子网掩码")]
            public string NetMask { get; set; }
            public string Gateway { get; set; }
            public string DnsServer { get; set; }

            public string ToString(string networkInterfaceId)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"iface {networkInterfaceId} {IPVersion} {Method.ToString().ToLower()}");
                
                if (Method == NetworkInterfaceMethod.Static)
                {
                    if (!string.IsNullOrEmpty(IPAddress))
                        sb.AppendLine($"    address {IPAddress}");
                    if (!string.IsNullOrEmpty(NetMask))
                        sb.AppendLine($"    netmask {NetMask}");
                    if (!string.IsNullOrEmpty(Gateway))
                        sb.AppendLine($"    gateway {Gateway}");
                    if (!string.IsNullOrEmpty(DnsServer))
                        sb.AppendLine($"    dns-nameserver {DnsServer}");
                }
                return sb.ToString();
            }
        }

        public class NetworkInterfaceConfig
        {
            [Required(ErrorMessage = "必须选择激活方式")]
            public ActiveMethod ActiveMetohd { get; set; }
            public NetworkInterfaceNetworkConfig IPv4Config { get; set; }
            public NetworkInterfaceNetworkConfig IPv6Config { get; set; }
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

        private const string NETWORK_CONFIGS_FOLDER = "/etc/network/interfaces.d";
        private const string NETWORK_CONFIG_ENTRY_FILE = "/etc/network/interfaces";        

        private ConfigFileInfo ActiveMethodConfig = null;
        private ConfigFileInfo IPv4Config = null;
        private ConfigFileInfo IPv6Config = null;

        private NetworkInterfaceConfig GetNetworkInterfaceConfig(DisplayNetworkInterfaceInfo model)
        {
            var config = new NetworkInterfaceConfig() { ActiveMetohd = ActiveMethod.None };
            var allConfigFileList = new List<string>();
            if (Directory.Exists(NETWORK_CONFIGS_FOLDER))
                allConfigFileList.AddRange(Directory.GetFiles(NETWORK_CONFIGS_FOLDER));
            allConfigFileList.Add(NETWORK_CONFIG_ENTRY_FILE);
            foreach (var file in allConfigFileList)
            {
                var isReadingIPv4Config = false;
                var isReadingIPv6Config = false;

                var lines = File.ReadAllLines(file);
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    //如果是注释
                    if (line.StartsWith("#"))
                        continue;

                    if (string.IsNullOrEmpty(line))
                    {
                        isReadingIPv4Config = false;
                        isReadingIPv6Config = false;
                        if (IPv4Config != null && IPv4Config.EndLine < 0)
                            IPv4Config.EndLine = i;
                        if (IPv6Config != null && IPv6Config.EndLine < 0)
                            IPv6Config.EndLine = i;
                        continue;
                    }
                    var segments = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);
                    if (segments.Length <= 0)
                        continue;
                    var key = segments[0];
                    switch (key)
                    {
                        case "auto":
                        case "allow-hotplug":
                            if (segments.Length >= 2 && segments[1] == model.Id)
                            {
                                ActiveMethodConfig = new ConfigFileInfo()
                                {
                                    File = file,
                                    FileLines = lines,
                                    StartLine = i,
                                    EndLine = i + 1
                                };
                                switch (key)
                                {
                                    case "auto":
                                        config.ActiveMetohd = ActiveMethod.Auto;
                                        break;
                                    case "allow-hotplug":
                                        config.ActiveMetohd = ActiveMethod.AllowHotPlug;
                                        break;
                                }
                            }
                            break;
                        case "iface":
                            if (segments.Length >= 4 && segments[1] == model.Id)
                            {
                                var ipVersion = segments[2];
                                switch(ipVersion)
                                {
                                    //IPv4
                                    case "inet":
                                        IPv4Config = new ConfigFileInfo()
                                        {
                                            File = file,
                                            FileLines = lines,
                                            StartLine = i,
                                            EndLine = -1
                                        };
                                        config.IPv4Config = new NetworkInterfaceNetworkConfig()
                                        {
                                            IPVersion = ipVersion,
                                            Method = Enum.Parse<NetworkInterfaceMethod>(segments[3], true)
                                        };
                                        isReadingIPv4Config = true;
                                        break;
                                    //IPv6
                                    case "inet6":
                                        IPv6Config = new ConfigFileInfo()
                                        {
                                            File = file,
                                            FileLines = lines,
                                            StartLine = i,
                                            EndLine = -1
                                        };
                                        IPv6Config = new ConfigFileInfo()
                                        {
                                            File = file,
                                            FileLines = lines,
                                            StartLine = i,
                                            EndLine = -1
                                        };
                                        config.IPv6Config = new NetworkInterfaceNetworkConfig()
                                        {
                                            IPVersion = ipVersion,
                                            Method = Enum.Parse<NetworkInterfaceMethod>(segments[3], true)
                                        };
                                        isReadingIPv6Config = true;
                                        break;
                                }
                            }
                            break;
                        case "address":
                            if (segments.Length >= 2)
                            {
                                if(isReadingIPv4Config)
                                    config.IPv4Config.IPAddress = segments[1];
                                if(isReadingIPv6Config)
                                    config.IPv6Config.IPAddress = segments[1];
                            }
                            break;
                        case "netmask":
                            if (segments.Length >= 2)
                            {
                                if(isReadingIPv4Config)
                                    config.IPv4Config.NetMask = segments[1];
                                if(isReadingIPv6Config)
                                    config.IPv6Config.NetMask = segments[1];
                            }
                            break;
                        case "gateway":
                            if (segments.Length >= 2)
                            {
                                if(isReadingIPv4Config)
                                    config.IPv4Config.Gateway = segments[1];
                                if(isReadingIPv6Config)
                                    config.IPv6Config.Gateway = segments[1];
                            }
                            break;
                        case "dns-nameserver":
                            if (segments.Length >= 2)
                            {
                                if(isReadingIPv4Config)
                                    config.IPv4Config.DnsServer = segments[1];
                                if(isReadingIPv6Config)
                                    config.IPv6Config.DnsServer = segments[1];
                            }
                            break;
                    }
                }
            }
            if (IPv4Config != null && IPv4Config.EndLine < 0)
                IPv4Config.EndLine = IPv4Config.FileLines.Length;
            if (IPv6Config != null && IPv6Config.EndLine < 0)
                IPv6Config.EndLine = IPv6Config.FileLines.Length;
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
                ErrorMessage = ExceptionUtils.GetExceptionString(ex);
            }
        }

        private ConfigFileInfo NewConfigFileInfo(bool start)
        {
            string[] fileLines = [];
            if (File.Exists(NETWORK_CONFIG_ENTRY_FILE))
                fileLines = File.ReadAllLines(NETWORK_CONFIG_ENTRY_FILE);
            int line;
            if (start)
                line = 0;
            else
                line = fileLines.Length;

            return new ConfigFileInfo()
            {
                File = NETWORK_CONFIG_ENTRY_FILE,
                FileLines = fileLines,
                StartLine = line,
                EndLine = line
            };
        }

        private void WriteToConfigFile(ConfigFileInfo configFileInfo, string content)
        {
            //修改配置文件前，读取一次
            GetNetworkInterfaceConfig(CurrentNetworkInterface);
            //备份配置文件
            if (File.Exists(configFileInfo.File))
                File.Delete(configFileInfo.File);

            var sb = new StringBuilder();

            //配置前面
            for (var i = 0; i < configFileInfo.StartLine; i++)
                sb.AppendLine(configFileInfo.FileLines[i]);

            if (!string.IsNullOrEmpty(content))
                sb.AppendLine(content);

            //配置后面
            for (var i = configFileInfo.EndLine; i < configFileInfo.FileLines.Length; i++)
                sb.AppendLine(configFileInfo.FileLines[i]);

            var newLine = Environment.NewLine;
            sb.Replace($"{newLine}{newLine}{newLine}", $"{newLine}{newLine}");
            File.WriteAllText(configFileInfo.File, sb.ToString());
            sb.Clear();
            //修改配置文件后，再读取一次
            GetNetworkInterfaceConfig(CurrentNetworkInterface);
        }

        private async void OkEditNetworkInterface()
        {
            try
            {
                modalLoading.Show($"修改网卡[{CurrentNetworkInterface.Name}]配置中...", null, true, null);
                await Task.Run(async () =>
                {
                    //配置网卡激活方式
                    if (ActiveMethodConfig == null)
                        ActiveMethodConfig = NewConfigFileInfo(true);
                    {
                        string content = null;
                        switch (CurrentNetworkInterfaceConfig.ActiveMetohd)
                        {
                            case ActiveMethod.Auto:
                                content = $"auto {CurrentNetworkInterface.Id}";
                                break;
                            case ActiveMethod.AllowHotPlug:
                                content = $"allow-hotplug {CurrentNetworkInterface.Id}";
                                break;
                        }
                        WriteToConfigFile(ActiveMethodConfig, content);
                    }
                    //配置IPv4
                    if (IPv4Config == null && CurrentNetworkInterfaceConfig.IPv4Config != null)
                        IPv4Config = NewConfigFileInfo(false);
                    if (IPv4Config != null)
                    {
                        string content = null;
                        if (CurrentNetworkInterfaceConfig.IPv4Config != null)
                            content = CurrentNetworkInterfaceConfig.IPv4Config.ToString(CurrentNetworkInterface.Id);
                        WriteToConfigFile(IPv4Config, content);
                    }
                    //配置IPv6
                    if (IPv6Config == null && CurrentNetworkInterfaceConfig.IPv6Config != null)
                        IPv6Config = NewConfigFileInfo(false);
                    if (IPv6Config != null)
                    {
                        string content = null;
                        if (CurrentNetworkInterfaceConfig.IPv6Config != null)
                            content = CurrentNetworkInterfaceConfig.IPv6Config.ToString(CurrentNetworkInterface.Id);
                        WriteToConfigFile(IPv6Config, content);
                    }
                    modalAlert.Show("成功", "修改网卡配置成功!", null, null);
                    await InvokeAsync(StateHasChanged);
                });
            }
            catch (Exception ex)
            {
                modalAlert.Show("修改网卡配置出错", ExceptionUtils.GetExceptionMessage(ex), null, null);
                await InvokeAsync(StateHasChanged);
                return;
            }
            finally
            {
                modalLoading.Close();
            }

            //如果当前网卡是启用状态，则重启网卡
            if (CurrentNetworkInterface.Status == OperationalStatus.Up)
            {
                try
                {
                    modalLoading.Show($"重启网卡[{CurrentNetworkInterface.Name}]", $"正在重启网卡[{CurrentNetworkInterface.Name}]。。。", true, null);
                    modalLoading.UpdateProgress(null, "如果修改了IP地址，请访问新的地址。");
                    //等待1秒，重启网卡
                    await Task.Delay(1000);
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
                finally
                {
                    modalLoading.Close();
                }
            }
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