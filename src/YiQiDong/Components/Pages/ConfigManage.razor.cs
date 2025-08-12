using Microsoft.AspNetCore.Components;
using Quick.Blazor.Bootstrap;
using Quick.Blazor.Bootstrap.Admin.Utils;
using Quick.Build;
using Quick.Shell.PowerShell;
using Quick.Shell.Utils;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text;
using Tewr.Blazor.FileReader;
using YiQiDong.Components.Controls;
using YiQiDong.Core.Utils;
using YiQiDong.Core.Utils.Unix;
using YiQiDong.Utils;
using static Quick.Blazor.Bootstrap.Admin.Utils.FileUploadHelper;

namespace YiQiDong.Components.Pages
{
    public partial class ConfigManage : ComponentBase, IDisposable
    {
        public class PasswordManageModel
        {
            [Required(ErrorMessage = "请输入原密码")]
            public string OldPassword { get; set; }
            [Required(ErrorMessage = "请输入新密码")]
            public string NewPassword { get; set; }
            [Required(ErrorMessage = "请再次输入新密码")]
            public string NewPassword2 { get; set; }
        }

        private ModalAlert modalAlert;
        private ModalWindow modalWindow;
        private ModalLoading modalLoading;

        private Model.ConfigModel configModel;
        private PasswordManageModel passwordManageModel;
        private ElementReference inputUpdateFile;
        [Inject]
        private IFileReaderService fileReaderService { get; set; }
        private string argUpdateFile;

        protected override void OnInitialized()
        {
            configModel = Program.Config.Clone();
            passwordManageModel = new PasswordManageModel();

            base.OnInitialized();
        }
        private void ModifyConfigModel()
        {
            try
            {
                //是否需要重启Web服务
                var isNeedRestartWebService =
                    Program.Config.Urls != configModel.Urls;
                //是否需要重启服务
                var isNeedRestartService = Program.Config.DataFolder != configModel.DataFolder
                    || Program.Config.AgentInitInterval != configModel.AgentInitInterval
                    || Program.Config.AgentTransportTimeout != configModel.AgentTransportTimeout;

                Program.Config.Title = configModel.Title;
                Program.Config.Urls = configModel.Urls;
                Program.Config.DataFolder = configModel.DataFolder;
                Program.Config.DefaultHtml = configModel.DefaultHtml;
                Program.Config.AgentInitInterval = configModel.AgentInitInterval;
                Program.Config.AgentTransportTimeout = configModel.AgentTransportTimeout;
                Program.Config.Save();

                passwordManageModel = new PasswordManageModel();
                if (isNeedRestartService)
                {
                    modalAlert.Show("提示", "修改配置成功！检测到有配置修改后需要重新启动易启动后生效，是否现在重启易启动服务？", () =>
                    {
                        modalLoading.Show("正在重启易启动服务", "准备中...", true);
                        Task.Delay(1000).ContinueWith(async t =>
                        {
                            try
                            {
                                modalLoading.UpdateProgress(null, "正在停止容器与群集...");
                                Program.StopContainerAndCluster();

                                modalLoading.UpdateContent("重启易启动服务过程中此页面会显示连接断开，并不会自动重新连接，更新成功后请访问新设置的URL地址。");
                                await Task.Delay(1000);
                                restartService();
                            }
                            catch (Exception ex)
                            {
                                modalAlert.Show("重启易启动服务出错", ExceptionUtils.GetExceptionString(ex), null, null, true);
                                modalLoading.Close();
                            }
                        });
                    }, null);
                }
                else if (isNeedRestartWebService)
                {
                    modalAlert.Show("提示", "修改配置成功！需要重新启动易启动Web服务后生效，是否现在重启易启动Web服务？", () =>
                    {
                        modalLoading.Show("正在重启易启动Web服务", "准备中...", true);
                        Task.Delay(1000).ContinueWith(async t =>
                        {
                            try
                            {
                                modalLoading.UpdateContent("重启易启动Web服务过程中此页面会显示连接断开，并不会自动重新连接，更新成功后请刷新页面或者访问新设置的URL地址。");
                                await Task.Delay(1000);
                                await restartWebService();
                            }
                            catch (Exception ex)
                            {
                                modalAlert.Show("重启易启动Web服务出错", ExceptionUtils.GetExceptionString(ex), null, null, true);
                                modalLoading.Close();
                            }
                        });
                    }, null);
                }
                else
                {
                    modalAlert.Show("提示", "修改配置成功！");
                }
            }
            catch (Exception ex)
            {
                modalAlert.Show("错误", "修改配置时出错，原因：" + ExceptionUtils.GetExceptionString(ex), null, null, true);
            }
        }

        private void ModifyPassword()
        {
            if (passwordManageModel.OldPassword != Program.Config.Password)
            {
                modalAlert.Show("提示", "原密码不正确！", null, null);
                return;
            }
            if (passwordManageModel.NewPassword != passwordManageModel.NewPassword2)
            {
                modalAlert.Show("提示", "两次输入的新密码不匹配！", null, null);
                return;
            }
            try
            {
                Program.Config.Password = passwordManageModel.NewPassword;
                Program.Config.Save();
                passwordManageModel = new PasswordManageModel();
                modalAlert.Show("提示", "修改密码修改成功！", null, null);
            }
            catch (Exception ex)
            {
                modalAlert.Show("错误", "修改密码时出错，原因：" + ExceptionUtils.GetExceptionString(ex), null, null, true);
            }
        }

        private void btnSelect_Click()
        {
            string dir = null;
            if (!string.IsNullOrEmpty(argUpdateFile) && File.Exists(argUpdateFile))
                dir = Path.GetDirectoryName(argUpdateFile);

            modalWindow.Show("选择易启动更新文件", new DialogParameters<FileSelectControl>()
            {
                {x=>x.FileFilter, "*.zip"},
                {x=> x.SelectAction, t =>
                {
                    argUpdateFile = t;
                    modalWindow.Close();
                    InvokeAsync(StateHasChanged);
                }}
            });
        }

        private void btnStartUpdate_Click(string updateFile, bool deleteUpdateFile)
        {
            try
            {
                var versionAndArch = UpdateUtils.GetVersionAndArchFromUpdateFile(updateFile);
                modalAlert.Show("更新", $"确定要将易启动程序由版本[{Consts.Version}]更新到[{versionAndArch.Version}]？", () =>
                {
                    modalAlert.Close();
                    Task.Run(() => beginUpdate(updateFile, deleteUpdateFile));
                }, () =>
                {
                    if (deleteUpdateFile)
                        File.Delete(updateFile);
                });
            }
            catch (Exception ex)
            {
                if (deleteUpdateFile)
                    File.Delete(updateFile);
                modalAlert.Show("错误", ExceptionUtils.GetExceptionMessage(ex));
            }
        }

        private void btnStartUpdate_Click()
        {
            btnStartUpdate_Click(argUpdateFile, false);
        }

        private async Task beginUpdate(string updateFile, bool deleteUpdateFile)
        {
            //在非Windows环境加载Quick.Build.dll文件，防止替换程序文件后，重启服务时出现BadImageFormatException
            _ = typeof(QbFile).Assembly.GetManifestResourceNames();

            var isRuningOnWindows = OperatingSystem.IsWindows();
            modalLoading.Show("正在更新", "准备中...", true);
            try
            {
                modalLoading.UpdateProgress(null, "正在停止容器与群集...");
                Program.StopContainerAndCluster();

                var installDir = Environment.CurrentDirectory;
                var tmpDir = installDir;
                if (isRuningOnWindows)
                    tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                //解压文件
                await UpdateUtils.Update(tmpDir, updateFile, Console.WriteLine, t => modalLoading.UpdateProgress(t, $"{t}%"));
                await Task.Delay(1000);
                if (deleteUpdateFile)
                    File.Delete(updateFile);
                if (isRuningOnWindows)
                {
                    modalLoading.UpdateProgress(null, "正在替换程序文件并重启服务...");

                    var psFileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.ps1");
                    var psFileEncoding = Encoding.GetEncoding(Thread.CurrentThread.CurrentCulture.TextInfo.ANSICodePage);
                    var psFileContent = @$"sc.exe stop {Consts.SERVICE_NAME_WIN32}
do
{{
    try{{ Remove-Item ""{installDir}"" -Recurse -Force }}
    catch{{ }}
    $installDirExist=(Test-Path ""{installDir}"")
    Start-Sleep -s 1
}}while($installDirExist -eq ""True"")
do
{{
    try{{ Copy-Item ""{tmpDir}"" ""{installDir}"" -Recurse}}
    catch{{ }}
    try{{ Remove-Item ""{tmpDir}"" -Recurse -Force }}
    catch{{ }}
    $tmpDirExist=(Test-Path ""{tmpDir}"")
    Start-Sleep -s 1
}}while($tmpDirExist -eq ""True"")
sc.exe start {Consts.SERVICE_NAME_WIN32}
Remove-Item ""{psFileName}""
";
                    File.WriteAllText(psFileName, psFileContent, psFileEncoding);
                    var ret = PowerShellProcessContext.ExecutePs1File(psFileName, true);
                    throw new IOException($"更新脚本进程退出码：{ret.ExitCode}");
                }
                else
                {
                    //添加可执行权限
                    UnixUtils.AddExecutePermissionToFile(nameof(YiQiDong));
                    restartService();
                }
            }
            catch (Exception ex)
            {
                modalAlert.Show("更新出错", ExceptionUtils.GetExceptionString(ex), null, null, true);
                modalLoading.Close();
            }
        }

        private async Task restartWebService()
        {
            await Program.StopWebService();
            await Program.StartWebService();
        }

        private void restartService()
        {
            modalLoading.UpdateProgress(null, "正在重启服务...");
            if (OperatingSystem.IsWindows())
            {
                ProcessUtils.ExecuteShell("sc restart YiQiDong.Service", true);
            }
            else
            {
                var shFile = $"{nameof(YiQiDong)}.sh";
                UnixUtils.AddExecutePermissionToFile(shFile);
                //如果是在chroot环境中运行
                if (UnixUtils.IsRuningInChroot())
                {
                    Console.WriteLine($"检测到chroot环境");
                    Process.Start(new ProcessStartInfo(shFile, "run chroot")
                    {
                        WorkingDirectory = Environment.CurrentDirectory
                    });
                    Environment.Exit(0);
                }
                //如果是在docker环境中运行
                else if (UnixUtils.IsRuningInDocker())
                {
                    Console.WriteLine("检测到docker环境");
                    Environment.Exit(0);
                }
                //否则重启服务
                else
                {
                    if (OperatingSystem.IsMacOS())
                    {
                        var serviceDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "LaunchAgents");
                        var serviceFile = $"YiQiDong.Updater.plist";
                        QbFile.WriteLine(serviceFile, 11, $"      <string>{Environment.CurrentDirectory}/YiQiDong.Updater.sh</string>");
                        if (!Directory.Exists(serviceDir))
                            Directory.CreateDirectory(serviceDir);
                        //正在将服务文件安装到系统服务目录
                        ProcessUtils.ExecuteShell("cp {serviceFile} {serviceDir}/{serviceFile}");
                        //正在启用服务
                        ProcessUtils.ExecuteShell("launchctl load -w {serviceDir}/{serviceFile}");
                    }
                    else
                    {
                        if (UnixUtils.IsRuningWithRoot())
                            ProcessUtils.ExecuteShell("systemctl restart YiQiDong");
                        else
                            ProcessUtils.ExecuteShell("systemctl --user restart YiQiDong");
                    }
                }
            }
        }

        private CancellationTokenSource uploadCts;
        private async Task onInputUpdateFileChanged()
        {
            var fileReaderRef = fileReaderService.CreateReference(inputUpdateFile);

            var uploadSuccess = false;
            uploadCts = new CancellationTokenSource();
            UploadFileInfo uploadingFileInfo = default;
            string uploadingFileInfoStr = null;
            string uploadingFile = null;
            try
            {
                modalLoading.Show($"上传更新文件", "正在获取上传文件信息...", false, uploadCts.Cancel);
                var fileReference = (await fileReaderRef.EnumerateFilesAsync()).FirstOrDefault();
                uploadingFile = await FileUploadHelper.UploadFileAsync(fileReference,
                    fileInfo =>
                    {
                        uploadingFileInfo = fileInfo;
                        uploadingFileInfoStr = $"{fileInfo.Name} ({fileInfo.SizeString})";
                        modalLoading.Show($"上传更新文件", $"正在上传更新文件[{uploadingFileInfoStr}]...", false, uploadCts.Cancel);
                        return null;
                    },
                    progressInfo => modalLoading.UpdateProgress(progressInfo.Percent, progressInfo.Message),
                    uploadCts.Token);
                uploadSuccess = true;
                btnStartUpdate_Click(uploadingFile, true);
            }
            catch (OperationCanceledException)
            {
                modalAlert?.Show("上传已取消", $"已取消上传更新文件[{uploadingFileInfoStr}].");
            }
            catch (Exception ex)
            {
                modalAlert?.Show("上传失败", ExceptionUtils.GetExceptionMessage(ex));
            }
            finally
            {
                if (!uploadSuccess
                    && uploadingFile != null
                    && File.Exists(uploadingFile))
                    try { File.Delete(uploadingFile); } catch { }
                await fileReaderRef.ClearValue();
                modalLoading?.Close();
            }
        }

        public void Dispose()
        {
            uploadCts?.Cancel();
            uploadCts = null;
        }
    }
}
