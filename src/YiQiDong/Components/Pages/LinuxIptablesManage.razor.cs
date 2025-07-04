using Microsoft.AspNetCore.Components;
using Quick.Blazor.Bootstrap;
using Quick.Blazor.Bootstrap.Utils;
using YiQiDong.Utils;

namespace YiQiDong.Components.Pages;

public partial class LinuxIptablesManage : ComponentBase
{
    private const string CONFIG_FILE = "iptables_rules.conf";
    private ModalAlert modalAlert;
    private ModalLoading modalLoading;
    private string Content;
    private string fullConfigFile;

    protected override void OnInitialized()
    {
        try
        {
            fullConfigFile = FolderUtils.GetPathUnderDataDir(CONFIG_FILE);
            if (File.Exists(fullConfigFile))
                Content = File.ReadAllText(fullConfigFile);
        }
        catch (Exception ex)
        {
            Content = ExceptionUtils.GetExceptionString(ex);
        }
    }

    public static void CheckAndApplyIptablesRules(bool ignoreWhenFileNotExist = true)
    {
        var fullConfigFile = FolderUtils.GetPathUnderDataDir(CONFIG_FILE);
        var isConfigExist = File.Exists(fullConfigFile);
        if (ignoreWhenFileNotExist && !isConfigExist)
            return;
        string content = null;
        if (isConfigExist)
            File.ReadAllText(fullConfigFile);

        var ret = Quick.Shell.Utils.ProcessUtils.ExecuteShell("iptables -F");
        if (ret.ExitCode != 0)
            throw new IOException($"退出码:{ret.ExitCode}，输出：{ret.Output}{ret.Error}");

        var tmpFile = Path.GetTempFileName();
        try
        {
            using (var fs = File.OpenWrite(tmpFile))
            using (var writer = new StreamWriter(fs))
            {
                writer.WriteLine("*filter");
                if (!string.IsNullOrEmpty(content))
                    writer.WriteLine(content);
                writer.WriteLine("COMMIT");
            }
            ret = Quick.Shell.Utils.ProcessUtils.ExecuteShell($"iptables-restore < \"{tmpFile}\"");
            if (ret.ExitCode != 0)
                throw new IOException($"退出码:{ret.ExitCode}，输出：{ret.Output}{ret.Error}");
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    private async Task Save()
    {
        try
        {
            modalLoading.Show("保存", "正在保存...", true);
            if (File.Exists(fullConfigFile))
                File.Delete(fullConfigFile);
            if (!string.IsNullOrEmpty(Content))
                File.WriteAllText(fullConfigFile, Content);
        }
        catch (Exception ex)
        {
            modalAlert.Show("错误", "保存时出错，原因：" + ExceptionUtils.GetExceptionMessage(ex));
            return;
        }
        finally
        {
            modalLoading.Close();
        }

        try
        {
            modalLoading.Show("应用", "正在应用规则...", true);
            await Task.Delay(1000);
            CheckAndApplyIptablesRules(false);
        }
        catch (Exception ex)
        {
            modalAlert.Show("错误", "应用规则时出错，原因：" + ExceptionUtils.GetExceptionMessage(ex));
            return;
        }
        finally
        {
            modalLoading.Close();
        }
        modalAlert.Show("成功", "保存成功！");
    }
}
