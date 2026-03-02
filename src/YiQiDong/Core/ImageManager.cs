using Quick.Utils;
using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Archives.Zip;
using YiQiDong.Core.Utils;
using YiQiDong.Core.Utils.Unix;
using YiQiDong.Protocol.V1.Model;
using YiQiDong.Utils;

namespace YiQiDong.Core
{
    public class ImageManager
    {
        public const string VAR_IMAGE_DIR = "$IMAGE_DIR";
        public static ImageManager Instance { get; } = new ImageManager();

        private List<ImageInfo> ImageList = new List<ImageInfo>();
        private Dictionary<string, ImageInfo> imageIdImageDict = new Dictionary<string, ImageInfo>();
        //重试最大次数
        private const int RETRY_MAX_TIMES = 10;
        //重试间隔
        private const int RETRY_INTERVAL = 5000;

        public void Init()
        {
            var imagesFolder = ImagePathUtils.GetImageFolder();
            if (!Directory.Exists(imagesFolder))
                Directory.CreateDirectory(imagesFolder);
            ImageList.Clear();
            foreach (var imageFolder in Directory.GetDirectories(imagesFolder))
            {
                var imageInfo = LoadImageDir(imageFolder);
                if (imageInfo == null)
                    continue;
                ImageList.Add(imageInfo);
            }
            refreshImageDict();
        }

        private void refreshImageDict()
        {
            imageIdImageDict = ImageList.ToDictionary(t => t.Id, t => t);
        }

        private ImageInfo LoadImageDir(string dir)
        {
            var imageMetaFile = Path.Combine(dir, Consts.IMAGE_META_FILE);
            if (!File.Exists(imageMetaFile))
                return null;
            var content = File.ReadAllText(imageMetaFile);
            try
            {
                var imageInfo = ImageInfo.Parse(content);
                if (imageInfo == null)
                    return null;
                imageInfo.Id = Path.GetFileName(dir);
                //添加权限(非Windows操作系统)
                if (!OperatingSystem.IsWindows() && imageInfo.Path != null && imageInfo.Path.Length > 0)
                {
                    foreach (var t in imageInfo.Path)
                    {
                        var path = Path.Combine(dir, t);
                        if (!Directory.Exists(path))
                            continue;
                        foreach (var executeFile in Directory.GetFiles(path))
                            UnixUtils.AddExecutePermissionToFile(executeFile);
                    }
                }
                return imageInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[镜像管理器]加载[{dir}]时出错：{ExceptionUtils.GetExceptionString(ex)}");
                return null;
            }
        }

        private void CheckExecuteFiles(ImageInfo imageInfo)
        {
            //Windows系统，不需要添加权限
            if (OperatingSystem.IsWindows())
                return;
            if (imageInfo.ExecuteFiles != null && imageInfo.ExecuteFiles.Length > 0)
            {
                foreach (var t in imageInfo.ExecuteFiles)
                {
                    var executeFile = GetPathInImage(imageInfo, t);
                    UnixUtils.AddExecutePermissionToFile(executeFile);
                }
            }
        }

        public async Task<ImageInfo> LoadImageFile(string file, Action<int, int, string> progressHandler, CancellationToken cancellationToken, string preImageId, Action<string> messageHandler)
        {
            var newImageDir = ImagePathUtils.GetImageFolder(Guid.NewGuid().ToString("N"));
            ImageInfo imageInfo = null;
            var preImageInfo = Get(preImageId);
            try
            {
                using (var ymgFileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                using(var archive = ArchiveFactory.OpenArchive(ymgFileStream))
                {
                    var entriesCount = archive.Entries.Count();
                    //读取镜像文件元信息
                    var imageMetaEntry = archive.Entries.FirstOrDefault(t => t.Key == Consts.IMAGE_META_FILE);
                    if (imageMetaEntry == null)
                        throw new FileNotFoundException("文件中未找到易启动镜像元信息");
                    var imageMetaContent = string.Empty;
                    using (var imageMetaEntryStream = imageMetaEntry.OpenEntryStream())
                    using (var reader = new StreamReader(imageMetaEntryStream))
                        imageMetaContent = reader.ReadToEnd();

                    imageInfo = ImageInfo.Parse(imageMetaContent);
                    //验证镜像架构是否匹配
                    var isRidMatch = imageInfo.Platform.Any(t => RuntimeUtils.IsMatchRID(t));
                    if (!isRidMatch)
                        throw new NotSupportedException($"镜像的架构[{string.Join(",", imageInfo.Platform)}]不匹配当前计算机架构[{RuntimeUtils.GetCurrentRID()}]");


                    //如果是添加镜像
                    if (preImageInfo == null)
                    {
                        //验证镜像是否存在
                        if (GetItemByNameAndVersion(imageInfo.Name, imageInfo.Version) != null)
                            throw new ApplicationException($"上传的镜像[{imageInfo.Name} {imageInfo.Version}]已经存在！");
                    }
                    //如果是替换镜像
                    else
                    {
                        //验证镜像名称是否匹配
                        if (preImageInfo.Name != imageInfo.Name)
                            throw new ApplicationException($"上传的镜像[{imageInfo.Name} {imageInfo.Version}]与要替换的镜像[{preImageInfo.Name} {preImageInfo.Version}]名称不匹配，无法替换！");
                    }

                    //解压镜像文件
                    var currentEntryCount = 0;
                    foreach (var entry in archive.Entries)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;
                        currentEntryCount++;
                        progressHandler?.Invoke(entriesCount, currentEntryCount, entry.Key);

                        if (entry.IsDirectory)
                        {
                            var dir = Path.Combine(newImageDir, entry.Key);
                            if (!Directory.Exists(dir))
                                try
                                {
                                    Directory.CreateDirectory(dir);
                                }
                                catch (Exception ex)
                                {
                                    throw new IOException($"创建目录[{dir}]时出错。", ex);
                                }
                        }
                        else
                        {
                            var ex_file = Path.Combine(newImageDir, entry.Key);
                            var dir = Path.GetDirectoryName(ex_file);
                            if (!Directory.Exists(dir))
                                try
                                {
                                    Directory.CreateDirectory(dir);
                                }
                                catch (Exception ex)
                                {
                                    throw new IOException($"创建目录[{dir}]时出错。", ex);
                                }
                            try
                            {
                                await entry.WriteToFileAsync(ex_file);
                            }
                            catch (Exception ex)
                            {
                                throw new IOException($"解压文件[{entry.Key}]时出错",ex);
                            }
                        }
                    }
                }

                //如果被取消
                if (cancellationToken.IsCancellationRequested)
                    imageInfo = null;
                else
                    imageInfo = LoadImageDir(newImageDir);

                if (imageInfo == null)
                {
                    await Task.Delay(1000).ContinueWith(t =>
                    {
                        if (Directory.Exists(newImageDir))
                            Directory.Delete(newImageDir, true);
                    });
                    return null;
                }

                ContainerContext[] containers = null;
                //添加镜像
                if (preImageInfo == null)
                {
                    //如果镜像中有默认镜像编号，且此编号目前没有使用
                    if (!string.IsNullOrEmpty(imageInfo.DefaultId))
                    {
                        var imageIdIndex = 1;
                        var imageId = imageInfo.DefaultId;
                        while (Get(imageId) != null)
                        {
                            imageId = $"{imageInfo.DefaultId}-{imageIdIndex}";
                            imageIdIndex++;
                        }

                        var sourceImageFolder = ImagePathUtils.GetImageFolder(imageInfo.Id);
                        var desImageFolder = ImagePathUtils.GetImageFolder(imageId);
                        //移动镜像目录
                        for (var i = 0; i < RETRY_MAX_TIMES; i++)
                        {
                            progressHandler.Invoke(RETRY_MAX_TIMES, i + 1, $"正在移动镜像目录...");
                            if (!Directory.Exists(sourceImageFolder))
                                break;
                            try { Directory.Move(sourceImageFolder, desImageFolder); }
                            catch (Exception ex)
                            {
                                if (i + 1 >= RETRY_MAX_TIMES)
                                    throw new IOException($"将目录[{sourceImageFolder}]移动到[{desImageFolder}]时出错。", ex);
                                Thread.Sleep(RETRY_INTERVAL);
                            }
                        }
                        imageInfo.Id = imageId;
                    }
                    containers = ContainerManager.Instance.UseImageContainers(imageInfo.Id);
                    CheckExecuteFiles(imageInfo);
                    messageHandler?.Invoke($"已添加镜像[{imageInfo.Name} {imageInfo.Version}].");
                }
                //替换镜像。
                else
                {
                    containers = ContainerManager.Instance.UseImageContainers(preImageInfo.Id);
                    //容器禁用
                    for (var i = 0; i < containers.Length; i++)
                    {
                        var container = containers[i];
                        progressHandler.Invoke(containers.Length, i + 1, $"正在禁用容器[{container.ContainerInfo.Name}]...");

                        var preEnable = container.ContainerInfo.Enable;
                        var preAutoStart = container.ContainerInfo.AutoStart;
                        container.ContainerInfo.Enable = false;
                        container.BeginDisable();
                        container.ContainerInfo.AutoStart = preAutoStart;
                        container.ContainerInfo.Enable = preEnable;
                    }
                    var preImageFolder = ImagePathUtils.GetImageFolder(preImageInfo.Id);
                    //删除之前镜像目录
                    for (var i = 0; i < RETRY_MAX_TIMES; i++)
                    {
                        progressHandler.Invoke(RETRY_MAX_TIMES, i + 1, $"正在删除之前镜像目录...");
                        if (!Directory.Exists(preImageFolder))
                            break;
                        try { Directory.Delete(preImageFolder, true); }
                        catch (Exception ex)
                        {
                            if (i + 1 >= RETRY_MAX_TIMES)
                                throw new IOException($"目录[{preImageFolder}]无法删除，替换失败。", ex);
                            Thread.Sleep(RETRY_INTERVAL);
                        }
                    }

                    //如果新镜像没有初始镜像编号
                    //或者新镜像初始编号与被替换镜像编号相同
                    //或者新镜像初始编号的镜像已经存在了
                    if (string.IsNullOrEmpty(imageInfo.DefaultId)
                        || imageInfo.DefaultId == preImageInfo.Id
                        || Get(imageInfo.DefaultId) != null)
                    {
                        var sourceImageFolder = ImagePathUtils.GetImageFolder(imageInfo.Id);
                        var desImageFolder = preImageFolder;
                        //移动镜像目录
                        for (var i = 0; i < RETRY_MAX_TIMES; i++)
                        {
                            progressHandler.Invoke(RETRY_MAX_TIMES, i + 1, $"正在移动镜像目录...");
                            if (!Directory.Exists(sourceImageFolder))
                                break;
                            try { Directory.Move(sourceImageFolder, desImageFolder); }
                            catch (Exception ex)
                            {
                                if (i + 1 >= RETRY_MAX_TIMES)
                                    throw new IOException($"将目录[{sourceImageFolder}]移动到[{desImageFolder}]时出错。", ex);
                                Thread.Sleep(RETRY_INTERVAL);
                            }
                        }
                        imageInfo.Id = preImageInfo.Id;
                    }
                    else
                    {
                        var sourceImageFolder = ImagePathUtils.GetImageFolder(imageInfo.Id);
                        var desImageFolder = ImagePathUtils.GetImageFolder(imageInfo.DefaultId);
                        //移动镜像目录
                        for (var i = 0; i < RETRY_MAX_TIMES; i++)
                        {
                            progressHandler.Invoke(RETRY_MAX_TIMES, i + 1, $"正在移动镜像目录...");
                            if (!Directory.Exists(sourceImageFolder))
                                break;
                            try { Directory.Move(sourceImageFolder, desImageFolder); }
                            catch (Exception ex)
                            {
                                if (i + 1 >= RETRY_MAX_TIMES)
                                    throw new IOException($"将目录[{sourceImageFolder}]移动到[{desImageFolder}]时出错。", ex);
                                Thread.Sleep(RETRY_INTERVAL);
                            }
                        }
                        imageInfo.Id = imageInfo.DefaultId;
                        //修改关联容器的镜像名称
                        foreach (var container in containers)
                        {
                            container.ContainerInfo.ImageId = imageInfo.Id;
                            ContainerManager.Instance.SaveContainerFile(container.ContainerInfo);
                        }
                    }
                    ImageList.Remove(preImageInfo);
                    CheckExecuteFiles(imageInfo);
                    messageHandler?.Invoke($"已替换镜像[{imageInfo.Name} {imageInfo.Version}].");
                }

                ImageList.Add(imageInfo);
                refreshImageDict();
                //容器启用
                if (containers != null)
                    for (var i = 0; i < containers.Length; i++)
                    {
                        var container = containers[i];
                        progressHandler.Invoke(i + 1, containers.Length, $"正在启用容器[{container.ContainerInfo.Name}]...");
                        container.ContainerInfo.Image = imageInfo;
                        container.BeginEnable();
                    }
                //重新加载标签
                TagManager.Instance.Reload();
                return imageInfo;
            }
            catch
            {
                await Task.Run(() =>
                {
                    for (var i = 0; i < RETRY_MAX_TIMES; i++)
                    {
                        progressHandler.Invoke(RETRY_MAX_TIMES, i + 1, $"正在删除临时镜像目录...");
                        if (!Directory.Exists(newImageDir))
                            break;
                        try { Directory.Delete(newImageDir, true); }
                        catch
                        {
                            Thread.Sleep(RETRY_INTERVAL);
                        }
                    }
                });
                throw;
            }
        }

        public ImageInfo Get(string imageId)
        {
            if (string.IsNullOrEmpty(imageId))
                return null;
            if (imageIdImageDict.TryGetValue(imageId, out var imageInfo))
                return imageInfo;
            return null;
        }

        public ImageInfo[] GetItemsByName(string name)
        {
            return ImageList.Where(t => t.Name == name).ToArray();
        }

        public ImageInfo GetItemByNameAndVersion(string name, string version)
        {
            return ImageList.FirstOrDefault(t => t.Name == name && t.Version == version);
        }

        public ImageInfo[] Query(string tag, string keywords)
        {
            IEnumerable<ImageInfo> query = ImageList;
            if (!string.IsNullOrEmpty(tag))
                query = query.Where(t => t.Tags != null && t.Tags.Contains(tag));
            if (!string.IsNullOrEmpty(keywords))
                query = query.Where(t => t.Name.Contains(keywords));
            //按名称和版本号排序
            query = query.OrderBy(t => t.Version);
            query = query.OrderBy(t => t.Name);
            return query.ToArray();
        }

        public void DeleteImage(string imageId)
        {
            var model = Get(imageId);
            if (model == null)
                return;
            var imageFolder = ImagePathUtils.GetImageFolder(model.Id);
            if (Directory.Exists(imageFolder))
                Directory.Delete(imageFolder, true);
            ImageList.Remove(model);
            refreshImageDict();
            TagManager.Instance.Reload();
        }

        public string GetPathInImage(string imageId, string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            var imageDir = ImagePathUtils.GetImageFolder(imageId);
            return path.Replace(VAR_IMAGE_DIR, imageDir);
        }

        public string GetPathInImage(ImageInfo imageInfo, string path)
        {
            return GetPathInImage(imageInfo.Id, path);
        }


        public string[] GetImagePath(ImageInfo image)
        {
            List<string> pathList = new List<string>();

            //添加PATH
            if (image.Path != null)
            {
                foreach (var path in image.Path)
                {
                    pathList.Add(GetPathInImage(image, path));
                }
            }
            return pathList.ToArray();
        }

        public Dictionary<string, string> GetImageEnvironment(ImageInfo image)
        {
            var enviroment = new Dictionary<string, string>();

            //处理环境变量
            if (image.Environment != null)
            {
                foreach (var item in image.Environment)
                {
                    enviroment[item.Key] = GetPathInImage(image.Id, item.Value);
                }
            }
            return enviroment;
        }
    }
}
