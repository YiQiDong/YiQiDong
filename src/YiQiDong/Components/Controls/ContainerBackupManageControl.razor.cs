using Microsoft.JSInterop;
using Quick.Blazor.Bootstrap;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using YiQiDong.Core;
using YiQiDong.Core.Utils;
using YiQiDong.Model;
using YiQiDong.Utils;

namespace YiQiDong.Components.Controls
{
    public partial class ContainerBackupManageControl
    {
        private string Dir = FolderUtils.GetBackupDir();
        private Quick.Blazor.Bootstrap.Admin.FileManageControl fileManageControl;

        private ModalAlert modalAlert;
        private ModalWindow modalWindow;
        private ModalLoading modalLoading;
        private UnitStringConverting storageUSC = UnitStringConverting.StorageUnitStringConverting;

        private void Download(IJSRuntime JSRuntime, string path) => FileManageControl.HttpDownload(JSRuntime, path);

        private void ImportContainer()
        {
            var file = fileManageControl.SelectedPath;
            YqdContainerInfo containerInfo = null;
            using (var zipArchive = ZipFile.OpenRead(file))
            {
                var metaFileEntry = zipArchive.GetEntry(Consts.CONTAINER_META_FILE);
                if (metaFileEntry == null)
                {
                    modalAlert.Show("导入容器失败", "选择的文件中未找到容器元数据文件！");
                    return;
                }
                using (var fs = metaFileEntry.Open())
                using (var reader = new StreamReader(fs))
                {
                    var content = reader.ReadToEnd();
                    containerInfo = YqdContainerInfo.Parse(content);
                }
            }
            var currentImageInfo = ImageManager.Instance.Get(containerInfo.ImageId);
            //查找其他版本的镜像
            if (currentImageInfo == null)
            {
                modalAlert.Show("导入容器失败", $"当前镜像列表中不存在编号为[{containerInfo.ImageId}]的镜像，请先上传此镜像后，再尝试导入容器。");
                return;
            }

            var idAndName = ContainerManager.Instance.GenerateNewContainerIdAndName(currentImageInfo.DefaultId ?? currentImageInfo.Id, currentImageInfo.Name);
            modalWindow.Show<ContainerCreateControl>("导入容器", new DialogParameters<ContainerCreateControl>
            {
                {x=>x.Model,containerInfo},
                {x=>x.OkAction,t =>
                {
                    try
                    {
                        t.Id = idAndName.Item1;
                        t.Name = idAndName.Item2;
                        t.ImageId = currentImageInfo.Id;
                        ContainerManager.Instance.Create(t);
                        modalWindow.Close();
                    }
                    catch(Exception ex)
                    {
                        modalAlert.Show("导入容器时出错", ExceptionUtils.GetExceptionMessage(ex));
                        return;
                    }

                    //解压文件
                    modalLoading.Show("导入容器", "解压文件中...", true);
                    Task.Run(() =>
                    {
                        try
                        {
                            var baseDir = ContainerPathUtils.GetContainerFolder(containerInfo.Id);
                            using (var zipArchive = ZipFile.OpenRead(file))
                            {
                                var totalFileCount = zipArchive.Entries.Count;
                                var currentFile = 0;
                                foreach (var entry in zipArchive.Entries)
                                {
                                    currentFile++;
                                    if (entry.Name == Consts.CONTAINER_META_FILE)
                                        continue;
                                    modalLoading.UpdateProgress(currentFile * 100 / totalFileCount, $"[{currentFile}/{totalFileCount}] {entry.FullName} ({storageUSC.GetString(entry.Length, 1, true)}B)");
                                    var extractFileName = Path.Combine(baseDir, entry.FullName);
                                    var extractFileFolder = Path.GetDirectoryName(extractFileName);
                                    if (!Directory.Exists(extractFileFolder))
                                        Directory.CreateDirectory(extractFileFolder);
                                    entry.ExtractToFile(extractFileName);
                                }
                            }
                            modalLoading.UpdateProgress(100, "解压完成");
                        }
                        catch (Exception ex)
                        {
                            modalAlert.Show("导入容器失败", $"解压文件过程中出错，原因：{ExceptionUtils.GetExceptionMessage(ex)}");
                        }
                        finally
                        {
                            modalLoading.Close();
                        }
                    });
                }}
            });
        }
    }
}
