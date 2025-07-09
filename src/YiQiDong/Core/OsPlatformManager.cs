using YiQiDong.Core.Utils;

namespace YiQiDong.Core;

public class OsPlatformManager
{
    public static OsPlatformManager Instance { get; } = new OsPlatformManager();
    public void Init()
    {
        if (OperatingSystem.IsLinux())
        {
            try
            {
                Components.Pages.LinuxFirewallManage_iptables.CheckAndApplyIptablesRules();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"警告：应用iptables规则时出错，原因：{ExceptionUtils.GetExceptionMessage(ex)}");
            }
        }
    }
}
