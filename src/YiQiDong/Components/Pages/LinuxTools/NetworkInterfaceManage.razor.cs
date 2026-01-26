using Quick.Blazor.Bootstrap;
using Quick.Blazor.Bootstrap.Utils;
using Quick.Shell;
using Quick.Shell.Utils;
using YiQiDong.Core.Protocol.V1.Model;

namespace YiQiDong.Components.Pages.LinuxTools
{
    public partial class NetworkInterfaceManage
    {
        public ModalLoading modalLoading { get; private set; }
        public ModalAlert modalAlert { get; private set; }
        public ModalWindow modalWindow { get; private set; }
        public ToastStack toastStack { get; private set; }

        private ConfigFileInfo[] networkConfigFiles;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            string[] configFiles =
            [
                "/etc/network/interfaces"
            ];
            string[] configFolders =
            [
                "/etc/network/interfaces.d",
                "/etc/netplan",
                "/etc/sysconfig/network-scripts",
                "/etc/systemd/network"
            ];
            var ConfigFileEncoding = "UTF-8";
            var list = new List<ConfigFileInfo>();

            foreach (var file in configFiles)
            {
                if (!File.Exists(file))
                    continue;
                list.Add(new ConfigFileInfo()
                {
                    Name = file,
                    FilePath = file,
                    FileEncoding = ConfigFileEncoding
                });
            }
            foreach (var folder in configFolders)
            {
                if (!Directory.Exists(folder))
                    continue;
                foreach (var file in Directory.GetFiles(folder))
                {
                    list.Add(new ConfigFileInfo()
                    {
                        Name = file,
                        FilePath = file,
                        FileEncoding = ConfigFileEncoding
                    });
                }
            }
            networkConfigFiles = list.ToArray();
        }

        private ShellProcessResult inner_restartNetworkService()
        {
            string[] networkServiceNames =
            [
                "systemd-networkd",
                "networking",
                "network"
            ];
            foreach (var networkServiceName in networkServiceNames)
            {
                var ret = ProcessUtils.ExecuteShell($"systemctl list-unit-files {networkServiceName}.service");
                if (ret.ExitCode == 0 && !ret.Output.Contains("disabled"))
                {
                    ret = ProcessUtils.ExecuteShell($"systemctl restart {networkServiceName}");
                    return ret;
                }
            }
            throw new NotImplementedException("未找到启用的网络服务！");
        }

        private void restartNetworkService()
        {
            modalAlert.Show("确认", "是否重启网络网络？", () =>
            {
                Task.Run(() =>
                {
                    modalLoading.Show("重启服务", "正在重启网络服务...", true);
                    try
                    {
                        var ret = inner_restartNetworkService();
                        if (ret.ExitCode != 0)
                            throw new ApplicationException(ret.Error);
                        modalAlert.Show("成功", "重启网络服务成功");
                    }
                    catch (Exception ex)
                    {
                        modalAlert.Show("错误", "重启网络服务时出错，原因：" + ExceptionUtils.GetExceptionMessage(ex));

                    }
                    modalLoading.Close();
                });
            });
        }

        private void help()
        {
            modalAlert.Show("帮助", @"[/etc/network/interfaces]文件：
自动激活网卡：
auto {网卡，示例：eth0}

手动激活网卡：
manual {网卡，示例：eth0}

网卡IPv4动态配置(DHCP)：
iface {网卡，示例：eth0} inet dhcp

网卡IPv4静态配置(最后一行是修改网卡MAC地址，可选)：
iface {网卡} inet static
    address {IPv4地址：示例：192.168.112.241}
    netmask {子网掩码：示例：255.255.255.0}
    gateway {网关地址：示例：192.168.112.1}
    pre-up ip link set dev {网卡，示例：eth0} address {MAC地址，示例：1E:BE:98:09:C4:1F}

网卡IPv6动态配置(DHCP)：
iface {网卡，示例：eth0} inet6 dhcp

网卡IPv6静态配置：
iface {网卡} inet6 static
    address {IPv6地址：示例：2001:db8:203:ec8::1}
    netmask {子网掩码：示例：64}
    gateway {网关地址：示例：2001:db8::1}

", usePreTag: true);
        }
    }
}