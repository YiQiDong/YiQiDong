using Quick.Blazor.Bootstrap;
using Quick.Blazor.Bootstrap.Utils;
using Quick.Shell;
using Quick.Shell.Utils;
using Yarp.ReverseProxy.Utilities.Tls;
using YiQiDong.Components.Controls;
using YiQiDong.Core.Protocol.V1.Model;

namespace YiQiDong.Components.Pages.LinuxTools
{
    public partial class NetworkInterfaceManage
    {
        public ModalLoading modalLoading { get; private set; }
        public ModalAlert modalAlert { get; private set; }
        public ModalWindow modalWindow { get; private set; }
        public ToastStack toastStack { get; private set; }

        private void editNetworkConfigFiles()
        {
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
            modalWindow.Show($"网络配置文件", new DialogParameters<ConfigFilesControl>
            {
                {x=>x.ConfigFiles, list.ToArray()}
            });
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
            modalAlert.Show("确认", "是否重启网络？", () =>
            {
                Task.Run(() =>
                {
                    modalLoading.Show("重启网络服务", "正在重启网络服务...", true);
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
    }
}