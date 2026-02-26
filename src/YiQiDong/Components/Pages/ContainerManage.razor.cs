using Quick.Blazor.Bootstrap;
using YiQiDong.Core;
using YiQiDong.Core.Utils;
using System.IO.Compression;
using YiQiDong.Model;
using YiQiDong.Utils;
using Microsoft.AspNetCore.Components;
using System.Text;
using System.Diagnostics;
using Quick.Blazor.Bootstrap.Admin.Utils;

namespace YiQiDong.Components.Pages
{
    public partial class ContainerManage : ComponentBase, IDisposable
    {
        private string searchTag;
        private string searchKeywords;

        private ModalLoading modalLoading;
        private ModalAlert modalAlert;
        private ModalWindow modalWindow;
        private static readonly UnitStringConverting storageUSC = UnitStringConverting.StorageUnitStringConverting;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            refreshContainers();
            ContainerManager.Instance.ContainerChanged += ContainerManager_ContainerChanged;
        }

        private void ContainerManager_ContainerChanged(object sender, EventArgs e)
        {
            InvokeAsync(StateHasChanged);
        }

        private void refreshContainers()
        {
            InvokeAsync(StateHasChanged);
        }

        private void CreateContainer()
        {
            modalWindow.Show("创建容器", new DialogParameters<Controls.ContainerCreateControl>()
            {
                {x=>x.OkAction,t =>
                    {
                        try
                        {
                            ContainerManager.Instance.Create(t);
                            modalWindow.Close();
                        }
                        catch (Exception ex)
                        {
                            modalAlert.Show("创建容器时出错", ExceptionUtils.GetExceptionMessage(ex));
                        }
                    }
                }
            });
        }

        private void ContainerBackupManage()
        {
            modalWindow.Show<Controls.ContainerBackupManageControl>("容器备份管理");
        }

        private void EditContainer(YqdContainerInfo containerInfo)
        {
            modalWindow.Show("编辑容器", new DialogParameters<Controls.ContainerCreateControl>()
            {
                {x=>x.Model, containerInfo},
                {x=>x.OkAction, t =>
                    {
                        try
                        {
                            ContainerManager.Instance.Update(containerInfo, t);
                            modalWindow.Close();
                        }
                        catch (Exception ex)
                        {
                            modalAlert.Show("编辑容器时出错", ExceptionUtils.GetExceptionMessage(ex));
                        }
                    }
                }
            });
        }

        private string getProcessDescription(Process process)
        {
            try
            {
                var sb = new StringBuilder();
                if (OperatingSystem.IsWindows())
                    sb.Append(process.MainModule.ModuleName);
                else
                    sb.Append(process.ProcessName);
                sb.Append($" [{process.Id}]");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return ExceptionUtils.GetExceptionMessage(ex);
            }
        }

        private void ShowProcessInfo(ContainerContext containerContext)
        {
            modalWindow.Show(
                $"{containerContext.ContainerInfo.Name} - 进程",
                new DialogParameters<Quick.Blazor.Bootstrap.Admin.ProcessViewControl>()
                {
                    {x=>x.PID, containerContext.Process.Id}
                }
            );
        }

        private void DeleteContainer(ContainerContext container)
        {
            modalAlert.Show(
                "删除确认",
                $"确定要删除容器[{container.ContainerInfo.Name}]?",
                () =>
                {
                    modalLoading.Show("删除容器", $"正在删除容器[{container.ContainerInfo.Name}]...", true, null);
                    Task.Run(() =>
                    {
                        try
                        {
                            ContainerManager.Instance.Delete(container);
                            refreshContainers();
                            modalAlert.Show("信息", $"删除容器[{container.ContainerInfo.Name}]成功!");
                        }
                        catch (Exception ex)
                        {
                            modalAlert.Show("错误", $"删除容器[{container.ContainerInfo.Name}]时出错！原因：{ExceptionUtils.GetExceptionString(ex)}");
                        }
                        modalLoading.Close();
                    });
                },
                null);
        }

        private void EnableContainer(ContainerContext container)
        {
            if (container == null)
                return;

            container.Enable();
        }

        private void DisableContainer(ContainerContext container)
        {
            if (container == null)
                return;

            container.Disable();
        }

        private void StartContainer(ContainerContext container)
        {
            if (container == null)
                return;
            Action doStartContainer = () => container.Start();
            var warning = container.ContainerInfo.StartWarning;
            if (string.IsNullOrEmpty(warning))
            {
                doStartContainer();
            }
            else
            {
                modalAlert.Show(
                    $"容器[{container.ContainerInfo.Name}]启动警告",
                    warning,
                    () => doStartContainer());
            }
        }

        private void StopContainer(ContainerContext container)
        {
            if (container == null)
                return;
            Action doStopContainer = () =>
            {
                modalLoading.Show("停止容器", $"正在停止容器[{container.ContainerInfo.Name}]...", true, null);
                Task.Run(async () =>
                {
                    try
                    {
                        await container.Stop();
                    }
                    catch (Exception ex)
                    {
                        modalAlert.Show("错误", $"停止容器[{container.ContainerInfo.Name}]时出错，原因：{ExceptionUtils.GetExceptionString(ex)}");
                    }
                    finally
                    {
                        modalLoading.Close();
                        await InvokeAsync(StateHasChanged);
                    }
                });
            };
            var warning = container.ContainerInfo.StopWarning;
            if (string.IsNullOrEmpty(warning))
            {
                doStopContainer();
            }
            else
            {
                modalAlert.Show(
                    $"容器[{container.ContainerInfo.Name}]停止警告",
                    warning,
                    () => doStopContainer());
            }
        }

        private void RestartContainer(ContainerContext container)
        {
            if (container == null)
                return;
            modalLoading.Show("重启容器", $"正在重启容器[{container.ContainerInfo.Name}]...", true, null);
            Task.Run(async () =>
            {
                try
                {
                    await container.Restart();
                    modalAlert.Show("重启容器", $"容器[{container.ContainerInfo.Name}]重启完成");
                }
                catch (Exception ex)
                {
                    modalAlert.Show("错误", $"重启容器[{container.ContainerInfo.Name}]时出错，原因：{ExceptionUtils.GetExceptionString(ex)}");
                }
                finally
                {
                    modalLoading.Close();
                    await InvokeAsync(StateHasChanged);
                }
            });
        }

        private void ShowContainerConsole(ContainerContext container)
        {
            modalWindow.Show($"容器 - {container.ContainerInfo.Name} - 控制台",
                new DialogParameters<Controls.ContainerConsoleControl>()
                {
                    {x=>x.Container, container}
                });
        }

        private void ShowContainerFile(ContainerContext container)
        {
            var dir = Utils.ContainerPathUtils.GetContainerFolder(container.ContainerInfo.Id);
            modalWindow.Show("文件管理",
                new DialogParameters<Controls.FileManageControl>()
                {
                    {x=> x.Dir,dir}
                });
        }

        private void travelFolder(DirectoryInfo di, Action<FileInfo> fileHandler, Action<DirectoryInfo> dirHandler, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;

            foreach (var subFi in di.GetFiles())
            {
                if (token.IsCancellationRequested)
                    return;
                fileHandler?.Invoke(subFi);
            }
            foreach (var subDi in di.GetDirectories())
            {
                if (token.IsCancellationRequested)
                    return;
                dirHandler?.Invoke(subDi);
                travelFolder(subDi, fileHandler, dirHandler, token);
            }
        }

        private void BackupContainer(ContainerContext container)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            modalLoading.Show("备份容器 - 统计容器文件", null, true, () => cts.Cancel());
            Task.Run(() =>
            {
                int fileCount = 0;
                long fileTotalSize = 0;
                string fileTotalSizeStr = null;
                var containerFolder = ContainerPathUtils.GetContainerFolder(container.ContainerInfo.Id);
                travelFolder(new DirectoryInfo(containerFolder), file =>
                {
                    fileCount++;
                    fileTotalSize += file.Length;
                    fileTotalSizeStr = storageUSC.GetString(fileTotalSize, 2, true);
                    modalLoading.UpdateContent($"文件[数量：{fileCount}    大小：{fileTotalSizeStr}B]");
                }, null, cts.Token);

                modalLoading.Close();
                if (cts.IsCancellationRequested)
                    return;

                modalAlert.Show("备份容器", $"统计容器文件完成[数量: {fileCount} ,大小: {storageUSC.GetString(fileTotalSize, 2, true)}B]。确定要备份容器[{container.ContainerInfo.Name}]？", () =>
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            modalLoading.Show("备份容器 - 压缩文件", null, true, () => cts.Cancel());
                            var backupFolder = FolderUtils.GetBackupDir();
                            if (!Directory.Exists(backupFolder))
                                Directory.CreateDirectory(backupFolder);
                            var backupFile = Path.Combine(backupFolder, $"{container.ContainerInfo.Name}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.ycr");
                            using (var zipArchive = ZipFile.Open(backupFile, ZipArchiveMode.Create))
                            {
                                int compressedFileCount = 0;
                                long compressedSize = 0;
                                travelFolder(new DirectoryInfo(containerFolder), file =>
                                {
                                    var entryName = file.FullName.Substring(containerFolder.Length + 1).Replace("\\", "/");

                                    modalLoading.UpdateProgress(Convert.ToInt32(compressedSize * 100 / fileTotalSize), $"[{compressedFileCount}/{fileTotalSize}][{storageUSC.GetString(compressedSize, 2, true)}B/{fileTotalSizeStr}B] 正在压缩{entryName}...");

                                    var entry = zipArchive.CreateEntry(entryName);
                                    using (var fs = file.OpenRead())
                                    using (var entryStream = entry.Open())
                                        fs.CopyToAsync(entryStream, cts.Token).Wait();
                                    compressedFileCount++;
                                    compressedSize += file.Length;
                                }, null, cts.Token);
                            }
                            if (cts.IsCancellationRequested)
                            {
                                File.Delete(backupFile);
                                return;
                            }

                            modalAlert.Show("备份容器", $"容器[{container.ContainerInfo.Name}]已经备份成功！", null, null);
                        }
                        catch (Exception ex)
                        {
                            modalAlert.Show("备份容器", $"备份容器[{container.ContainerInfo.Name}]时出错，原因：{ExceptionUtils.GetExceptionMessage(ex)}", null, null);
                        }
                        finally
                        {
                            modalLoading.Close();
                        }
                    });
                }, null);
            });
        }

        public void Dispose()
        {
            ContainerManager.Instance.ContainerChanged -= ContainerManager_ContainerChanged;
        }
    }
}
