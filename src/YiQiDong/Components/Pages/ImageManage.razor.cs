using Microsoft.AspNetCore.Components;
using Quick.Blazor.Bootstrap;
using Quick.Blazor.Bootstrap.Admin.Utils;
using System.Text;
using YiQiDong.Core;
using YiQiDong.Protocol.V1.Model;
using YiQiDong.Utils;
using Tewr.Blazor.FileReader;
using static Quick.Blazor.Bootstrap.Admin.Utils.FileUploadHelper;
using Quick.Utils;

namespace YiQiDong.Components.Pages
{
    public partial class ImageManage : ComponentBase, IDisposable
    {
        private string searchTag;
        private string searchKeywords;
        private string currenImageId;
        private ModalLoading modalLoading;
        private ModalAlert modalAlert;
        private ModalWindow modalWindow;
        private ElementReference inputImageFile;
        [Inject]
        private IFileReaderService fileReaderService { get; set; }

        //最后导入的目录
        private static string lastImportDir = null;

        private void ShowImageConsole(ImageInfo imageInfo)
        {
            modalWindow.Show($"镜像 - {imageInfo.Name} [{imageInfo.Version}] - 控制台",
                new DialogParameters<Controls.ImageConsoleControl>()
                {
                    {x=>x.Image, imageInfo}
                });
        }

        private void ShowImageFile(string imageId)
        {
            var dir = Utils.ImagePathUtils.GetImageFolder(imageId);
            modalWindow.Show("文件管理",
                new DialogParameters<Controls.FileManageControl>()
                {
                    {x=>x.Dir, dir}
                });
        }

        private void DeleteImage(ImageInfo model)
        {
            modalAlert.Show(
                "删除确认",
                $"确定要删除镜像[{model.Name} {model.Version}]?",
                () =>
                {
                    modalLoading.Show("删除镜像", $"正在删除镜像[{model.Name} {model.Version}]...", true, null);
                    Task.Run(() =>
                    {
                        try
                        {
                            var containers = ContainerManager.Instance.UseImageContainers(model.Id);
                            if (containers == null || containers.Length == 0)
                            {
                                Core.ImageManager.Instance.DeleteImage(model.Id);
                                modalAlert.Show("信息", $"删除镜像[{model.Name} {model.Version}]成功！");
                            }
                            else
                            {
                                var containerNames = string.Join(",", containers.Select(t => t.ContainerInfo.Name));
                                modalAlert.Show("警告", $"删除镜像[{model.Name} {model.Version}]失败！原因：容器[{containerNames}]正在使用此镜像。");
                            }
                        }
                        catch (Exception ex)
                        {
                            modalAlert.Show("错误", $"删除镜像[{model.Name} {model.Version}]时出错！原因：{ExceptionUtils.GetExceptionString(ex)}");
                        }
                        modalLoading.Close();
                        InvokeAsync(() => StateHasChanged());
                    });
                });
        }

        private async Task<string> import(string fileInfoStr, string file, string imageId, CancellationTokenSource cts)
        {
            string fileHead = null;
            //读取文件头
            using (var fs = File.OpenRead(file))
            {
                var buffer = new byte[2];
                var ret = fs.Read(buffer);
                fileHead = Encoding.ASCII.GetString(buffer);
                if (!YmgFileUtils.IsYmgHead(fileHead))
                    throw new ApplicationException($"文件[{fileInfoStr}]不是有效的镜像文件！");
            }

            modalLoading.UpdateProgress(null, null);
            modalLoading.Show($"加载镜像", $"正在加载镜像文件[{fileInfoStr}]...", true, cts.Cancel);
            string loadMessage = null;
            var imageInfo = await Core.ImageManager.Instance.LoadImageFile(fileHead, file, (total, current, name) =>
            {
                modalLoading.UpdateProgress(Convert.ToInt32(current * 100 / total), $"{current}/{total} {name}");
            }, cts.Token, imageId, t => loadMessage = t);

            if (cts.IsCancellationRequested)
                return null;
            if (imageInfo == null)
                throw new BadImageFormatException($"镜像文件[{fileInfoStr}]无效。.");
            return loadMessage;
        }

        private void ImportImage(string imageId)
        {
            var cts = new CancellationTokenSource();

            Action<string> afterSelectFileAction = async t =>
            {
                modalWindow.Close();

                lastImportDir = Path.GetDirectoryName(t);
                var fileInfoStr = Path.GetFileName(t);
                try
                {
                    modalLoading.Show($"导入镜像", $"导入中...", true, cts.Cancel);
                    await Task.Delay(100);
                    var loadMessage = await import(fileInfoStr, t, imageId, cts);
                    if (cts.IsCancellationRequested)
                    {
                        modalAlert.Show("导入已取消", $"已取消导入镜像文件[{fileInfoStr}].");
                        return;
                    }
                    await InvokeAsync(StateHasChanged);
                    modalAlert.Show("导入镜像成功", loadMessage);
                }
                catch (TaskCanceledException)
                {
                    modalAlert.Show("导入已取消", $"已取消加载镜像文件[{fileInfoStr}].");
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
            modalWindow.Show("选择镜像文件", new DialogParameters<Controls.FileSelectControl>()
            {
                {x=>x.Dir, lastImportDir},
                {x=>x.FileFilter, "*.ymg"},
                {x=>x.FileDoubleClickToDownload, false},
                {x=>x.FileDoubleClickCustomAction, afterSelectFileAction},
                {x=>x.SelectAction, afterSelectFileAction},
            });
        }
        
        private CancellationTokenSource uploadCts;

        private async Task onInputImageFileChanged(string imageId)
        {
            var fileReaderRef = fileReaderService.CreateReference(inputImageFile);
            uploadCts = new CancellationTokenSource();
            UploadFileInfo uploadingFileInfo = default;
            string uploadingFileInfoStr = null;
            string uploadingFile = null;
            try
            {
                modalLoading.Show($"上传镜像", "正在获取上传文件信息...", false, uploadCts.Cancel);
                var fileReference = (await fileReaderRef.EnumerateFilesAsync()).FirstOrDefault();
                uploadingFile = await FileUploadHelper.UploadFileAsync(fileReference,
                    fileInfo =>
                    {
                        uploadingFileInfo = fileInfo;
                        uploadingFileInfoStr = $"{fileInfo.Name} ({fileInfo.SizeString})";
                        modalLoading.Show($"上传镜像", $"正在上传镜像文件[{uploadingFileInfoStr}]...", false, uploadCts.Cancel);
                        return null;
                    },
                    progressInfo => modalLoading.UpdateProgress(progressInfo.Percent, progressInfo.Message),
                    uploadCts.Token);

                modalLoading.UpdateProgress(null, null);
                modalLoading.Show($"上传镜像", $"正在加载镜像文件[{uploadingFileInfoStr}]...", true, uploadCts.Cancel);
                var loadMessage = await import(uploadingFileInfoStr, uploadingFile, imageId, uploadCts);
                if (uploadCts.IsCancellationRequested)
                {
                    modalAlert.Show("上传已取消", $"已取消上传镜像文件[{uploadingFileInfoStr}].");
                    return;
                }
                modalAlert?.Show("上传成功", loadMessage);
                await InvokeAsync(StateHasChanged);
            }
            catch (OperationCanceledException)
            {
                modalAlert?.Show("上传已取消", $"已取消上传镜像文件[{uploadingFileInfoStr}].");
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
