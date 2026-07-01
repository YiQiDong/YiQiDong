using Quick.Utils;
using YiQiDong.Utils;

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
                Components.Pages.LinuxTools.FirewallManage_iptables.CheckAndApplyIptablesRules();
            }
            catch (Exception ex)
            {
                ConsoleUtils.ConsoleWriteLine($"警告：应用iptables规则时出错，原因：{ExceptionUtils.GetExceptionMessage(ex)}");
            }
        }
    }
}
