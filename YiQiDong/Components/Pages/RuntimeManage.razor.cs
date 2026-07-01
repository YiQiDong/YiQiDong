using System.IO.Compression;
using Microsoft.AspNetCore.Components;
using Quick.Blazor.Bootstrap;
using Quick.Blazor.Bootstrap.Admin;
using Quick.Blazor.Bootstrap.Admin.Core;
using Quick.Blazor.Bootstrap.Admin.Utils;
using Quick.Utils;
using Tewr.Blazor.FileReader;
using YiQiDong.Core;
using YiQiDong.Core.Protocol.V1.Model;
using YiQiDong.Utils;
using static Quick.Blazor.Bootstrap.Admin.Utils.FileUploadHelper;

namespace YiQiDong.Components.Pages
{
    public partial class RuntimeManage : ComponentBase, IDisposable
    {
        private string searchKeywords;
        private string currenRuntimeId;
        private ModalLoading modalLoading;
        private ModalAlert modalAlert;
        private ModalWindow modalWindow;

        private ElementReference inputRuntimeFile;
        [Inject]
        private IFileReaderService fileReaderService { get; set; }
        //最后导入的目录
        private static string lastImportDir = null;

        private void ShowRuntimeConsole(RuntimeInfo runtimeInfo)
        {
            modalWindow.Show($"运行库 - {runtimeInfo.Name} [{runtimeInfo.Version}] - 控制台",
                new DialogParameters<Controls.RuntimeConsoleControl>()
                {
                    {x=>x.Runtime, runtimeInfo}
                });
        }

        private void ShowRuntimeFile(string runtimeId)
        {
            var dir = RuntimePathUtils.GetRuntimeFolder(runtimeId);
            modalWindow.Show("文件管理",
                new DialogParameters<Controls.FileManageControl>()
                {
                    {x=>x.Dir,dir }
                });
        }

        private void DeleteRuntime(RuntimeInfo model)
        {
            modalAlert.Show(
                "删除确认",
                $"确定要删除运行库[{model.Name} {model.Version}]?",
                new ()
                {
                    OkCallback = () =>
                    {
                        modalLoading.Show("删除运行库", $"正在删除运行库[{model.Name} {model.Version}]...", true, null);
                        Task.Run(() =>
                        {
                            try
                            {
                                var containers = Core.ContainerManager.Instance.UseRuntimeContainers(model.Id);
                                if (containers == null || containers.Length == 0)
                                {
                                    Core.RuntimeManager.Instance.DeleteRuntime(model.Id);
                                    modalAlert.Show("信息", $"删除运行库[{model.Name} {model.Version}]成功！");
                                }
                                else
                                {
                                    var containerNames = string.Join(",", containers.Select(t => t.ContainerInfo.Name));
                                    modalAlert.Show("警告", $"删除运行库[{model.Name} {model.Version}]失败！原因：容器[{containerNames}]正在使用此运行库。");
                                }
                            }
                            catch (Exception ex)
                            {
                                modalAlert.Show("错误", $"删除运行库[{model.Name} {model.Version}]时出错！原因：{ExceptionUtils.GetExceptionString(ex)}");
                            }
                            modalLoading.Close();
                            InvokeAsync(() => StateHasChanged());
                        });
                    }
                });
        }

        private async Task<string> import(string fileInfoStr, string file, string runtimeId, CancellationTokenSource cts)
        {
            modalLoading.UpdateProgress(null, null);
            modalLoading.Show($"加载运行库", $"正在加载运行库文件[{fileInfoStr}]...", true, cts.Cancel);
            modalLoading.UpdateProgress(0, "文件读取中...");
            await Task.Delay(100);
            string loadMessage = null;
            var runtimeInfo = await Core.RuntimeManager.Instance.LoadRuntimeFile(file, (total, current, name) =>
            {
                modalLoading.UpdateProgress(Convert.ToInt32(current * 100 / total), $"{current}/{total} {name}");
            }, cts.Token, runtimeId, t => loadMessage = t);

            if (cts.IsCancellationRequested)
                return null;
            if (runtimeInfo == null)
                throw new BadImageFormatException($"运行库文件[{fileInfoStr}]无效。.");
            return loadMessage;
        }

        private void ImportRuntime(string runtimeId)
        {
            var cts = new CancellationTokenSource();

            Action<string> afterSelectFileAction = async t =>
            {
                modalWindow.Close();

                lastImportDir = Path.GetDirectoryName(t);
                var fileInfoStr = Path.GetFileName(t);
                try
                {
                    var loadMessage = await import(fileInfoStr, t, runtimeId, cts);
                    if (cts.IsCancellationRequested)
                    {
                        modalAlert.Show("导入已取消", $"已取消导入运行库文件[{fileInfoStr}].");
                        return;
                    }
                    await InvokeAsync(StateHasChanged);
                    modalAlert.Show("导入运行库成功", loadMessage);
                }
                catch (TaskCanceledException)
                {
                    modalAlert.Show("导入已取消", $"已取消加载运行库文件[{fileInfoStr}].");
                }
                catch (Exception ex)
                {
                    modalAlert.Show("导入失败", ExceptionUtils.GetExceptionString(ex));
                }
                finally
                {
                    modalLoading.Close();
                }
            };
            modalWindow.Show("选择运行库文件",
                new DialogParameters<Controls.FileSelectControl>
                {
                    {x=>x.Dir, lastImportDir},
                    {x=>x.FileFilter, "*.yrt"},
                    {x=>x.FileDoubleClickToDownload, false},
                    {x=>x.FileDoubleClickCustomAction, afterSelectFileAction},
                    {x=>x.SelectAction, afterSelectFileAction},
                });
        }

        private async Task PackRuntime(string runtimeId)
        {
            var runtimeInfo =RuntimeManager.Instance.Get(runtimeId);
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            modalLoading?.Show($"打包运行库 - {runtimeInfo.Name}-{runtimeInfo.Version}", null, false, cts.Cancel);
            var folderList = new List<DirectoryInfo>();
            var fileList = new List<FileInfo>();
            long totalFileSize = 0;
            //统计要压缩的文件列表信息
            var runtimesFolder = RuntimePathUtils.GetRuntimeFolder();
            string baseFolder = null;
            var rootFolder = new DirectoryInfo(RuntimePathUtils.GetRuntimeFolder(runtimeId));            
            baseFolder = rootFolder.FullName;
            fileList.AddRange(rootFolder.GetFiles("*", SearchOption.AllDirectories));
            folderList.AddRange(rootFolder.GetDirectories("*", SearchOption.AllDirectories));
            totalFileSize = fileList.Sum(t => t.Length);

            //文件名
            string zipFileName = $"{runtimeInfo.Name}-{runtimeInfo.Version}-{string.Join("_", runtimeInfo.Platform)}.yrt";
            zipFileName = Path.Combine(runtimesFolder, zipFileName);
            if (File.Exists(zipFileName))
                File.Delete(zipFileName);
            //开始压缩
            try
            {
                using (var zipFileStream = File.Create(zipFileName))
                using (var zipArchive = new ZipArchive(zipFileStream, ZipArchiveMode.Create, true))
                {
                    //添加文件夹
                    foreach (var folder in folderList)
                    {
                        var entryName = folder.FullName.Substring(baseFolder.Length + 1) + Path.DirectorySeparatorChar;
                        entryName = PathUtils.UseUnixDirectorySeparatorChar(entryName);
                        zipArchive.CreateEntry(entryName);
                    }
                    //添加文件
                    using (var commonTransferContext = new CommonTransferContext(progressInfo =>
                    {
                        modalLoading.UpdateProgress(progressInfo.Percent, progressInfo.Message);
                    }, totalFileSize))
                    {
                        foreach (var file in fileList)
                        {
                            var entryName = file.FullName.Substring(baseFolder.Length + 1);
                            entryName = PathUtils.UseUnixDirectorySeparatorChar(entryName);
                            modalLoading.UpdateContent(entryName);
                            var zipEntry = zipArchive.CreateEntry(entryName);
                            using (var fs = file.OpenRead())
                            using (var zs = zipEntry.Open())
                                await commonTransferContext.TransferAsync(fs, zs, cancellationToken);
                        }
                    }
                }
                if (cancellationToken.IsCancellationRequested)
                {
                    try { File.Delete(zipFileName); } catch { }
                    modalAlert?.Show("打包运行库", "已取消");
                    return;
                }
                modalWindow.Show("打包运行库",
                new DialogParameters<Controls.FileManageControl>()
                {
                    {x=>x.Dir, runtimesFolder},
                    {x=>x.SelectedPath, zipFileName},
                    {x=>x.FileFilter, "*.yrt"}
                });
            }
            catch (TaskCanceledException)
            {
                try { File.Delete(zipFileName); } catch { }
                modalAlert?.Show("打包运行库", "已取消");
            }
            catch (Exception ex)
            {
                try { File.Delete(zipFileName); } catch { }
                modalAlert?.Show("打包运行库", "错误" + Environment.NewLine + ExceptionUtils.GetExceptionMessage(ex));
            }
            finally
            {
                modalLoading?.Close();
            }
        }

        private CancellationTokenSource uploadCts;
        private async Task onInputRuntimeFileChanged(string runtimeId)
        {
            var fileReaderRef = fileReaderService.CreateReference(inputRuntimeFile);

            uploadCts = new CancellationTokenSource();
            UploadFileInfo uploadingFileInfo = default;
            string uploadingFileInfoStr = null;
            string uploadingFile = null;
            try
            {
                modalLoading.Show($"上传运行库", "正在获取上传文件信息...", false, uploadCts.Cancel);
                var fileReference = (await fileReaderRef.EnumerateFilesAsync()).FirstOrDefault();
                uploadingFile = await FileUploadHelper.UploadFileAsync(fileReference,
                    fileInfo =>
                    {
                        uploadingFileInfo = fileInfo;
                        uploadingFileInfoStr = $"{fileInfo.Name} ({fileInfo.SizeString})";
                        modalLoading.Show($"上传运行库", $"正在上传运行库文件[{uploadingFileInfoStr}]...", false, uploadCts.Cancel);
                        return null;
                    },
                    progressInfo => modalLoading.UpdateProgress(progressInfo.Percent, progressInfo.Message),
                    uploadCts.Token);
                modalLoading.UpdateProgress(null, null);
                modalLoading.Show($"上传运行库", $"正在加载运行库文件[{uploadingFileInfoStr}]...", true, uploadCts.Cancel);

                var loadMessage = await import(uploadingFileInfoStr, uploadingFile, runtimeId, uploadCts);
                if (uploadCts.IsCancellationRequested)
                {
                    modalAlert.Show("上传已取消", $"已取消上传运行库文件[{uploadingFileInfoStr}].");
                    return;
                }
                await InvokeAsync(StateHasChanged);
                modalAlert?.Show("上传成功", loadMessage);

            }
            catch (OperationCanceledException)
            {
                modalAlert?.Show("上传已取消", $"已取消上传运行库文件[{uploadingFileInfoStr}].");
            }
            catch (Exception ex)
            {
                modalAlert?.Show("上传失败", ExceptionUtils.GetExceptionString(ex));
            }
            finally
            {
                if (uploadingFile != null && File.Exists(uploadingFile))
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
