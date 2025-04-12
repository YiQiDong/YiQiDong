using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using YiQiDong.Model;
using YiQiDong.Utils;

namespace YiQiDong.Core
{
    public class ContainerManager
    {
        public static ContainerManager Instance { get; } = new ContainerManager();

        private List<ContainerContext> ContainerList = new List<ContainerContext>();
        private Dictionary<string, ContainerContext> containerIdContainerDict = new Dictionary<string, ContainerContext>();
        private Dictionary<string, ContainerContext> containerNameContainerDict = new Dictionary<string, ContainerContext>();

        public event EventHandler ContainerChanged;
        //是否已经初始化完成
        public bool IsInited { get; private set; } = false;

        private void refreshContainerDict()
        {
            containerIdContainerDict = ContainerList.ToDictionary(t => t.ContainerInfo.Id, t => t);
            containerNameContainerDict = ContainerList.ToDictionary(t => t.ContainerInfo.Name, t => t);
        }

        public void RaiseEvent_ContainerChanged()
        {
            ContainerChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Init()
        {
            var containersFolder = ContainerPathUtils.GetContainerFolder();
            if (!Directory.Exists(containersFolder))
                Directory.CreateDirectory(containersFolder);
            foreach (var containerFolder in Directory.GetDirectories(containersFolder))
            {
                var containerMetaFile = Path.Combine(containerFolder, Consts.CONTAINER_META_FILE);
                if (!File.Exists(containerMetaFile))
                    continue;
                var content = File.ReadAllText(containerMetaFile);
                var containerMeta = YqdContainerInfo.Parse(content);
                if (containerMeta == null)
                    continue;
                containerMeta.Id = Path.GetFileName(containerFolder);
                containerMeta.Image = ImageManager.Instance.Get(containerMeta.ImageId);
                //检查添加标签
                if (containerMeta.Tags != null)
                {
                    foreach (var tag in containerMeta.Tags)
                    {
                        if (!TagManager.Instance.Contains(tag))
                            TagManager.Instance.Add(tag);
                    }
                }
                var containerContext = new ContainerContext(containerMeta);
                containerContext.BeginEnable();
                ContainerList.Add(containerContext);

                //每加载一个启用的容器后，等待指定的间隔时间
                if (containerMeta.Enable)
                    Thread.Sleep(Program.Config.AgentInitInterval);
            }
            refreshContainerDict();
            IsInited = true;
            RaiseEvent_ContainerChanged();
        }

        public Tuple<string, string> GenerateNewContainerIdAndName(string imageDefaultId, string imageName)
        {
            for (var i = 1; ; i++)
            {
                var id = $"{imageDefaultId}-{i}";
                var name = $"{imageName}-{i}";
                //如果容器编号或者名称已经存在，则尝试下一个
                if (ContainerList.Any(t => t.ContainerInfo.Id == id || t.ContainerInfo.Name == name))
                    continue;
                return new Tuple<string, string>(id, name);
            }
        }

        public void Stop()
        {
            List<Task> stopTaskList = new List<Task>();
            foreach (var container in ContainerList.ToArray())
            {
                container.ContainerInfo.Enable = false;
                stopTaskList.Add(container.Stop(false));
            }
            Task.WaitAll(stopTaskList.ToArray());
        }

        public ContainerContext Get(string containerId)
        {
            containerIdContainerDict.TryGetValue(containerId, out var containerContext);
            return containerContext;
        }

        public ContainerContext GetByName(string containerName)
        {
            containerNameContainerDict.TryGetValue(containerName, out var containerContext);
            return containerContext;
        }

        public ContainerContext[] UseImageContainers(string imageId)
        {
            return ContainerList.Where(t => t.ContainerInfo.ImageId == imageId).ToArray();
        }

        public ContainerContext[] UseRuntimeContainers(string runtimeId)
        {
            return ContainerList.Where(t => t.ContainerInfo.RuntimeIds != null && t.ContainerInfo.RuntimeIds.Contains(runtimeId)).ToArray();
        }

        public ContainerContext[] Query(string tag, string keywords)
        {
            IEnumerable<ContainerContext> query = ContainerList;
            if (!string.IsNullOrEmpty(tag))
                query = query.Where(t => t.ContainerInfo.Tags != null && t.ContainerInfo.Tags.Contains(tag));
            if (!string.IsNullOrEmpty(keywords))
                query = query.Where(t => t.ContainerInfo.Name.Contains(keywords));
            //按名称排序
            query = query.OrderBy(t => t.ContainerInfo.Name);
            return query.ToArray();
        }

        public ContainerContext[] GetAll()
        {
            return ContainerList.ToArray();
        }

        public void Create(YqdContainerInfo model)
        {
            var preContainer = Get(model.Id);
            if (preContainer != null)
                throw new IOException($"编号为[{model.Id}]的容器已经存在！");
            model.Enable = false;
            model.Image = ImageManager.Instance.Get(model.ImageId);
            SaveContainerFile(model);
            var containerContext = new ContainerContext(model);
            containerContext.BeginEnable();
            ContainerList.Add(containerContext);
            refreshContainerDict();
            RaiseEvent_ContainerChanged();
        }

        public void Create(CreateContainerModel newModel)
        {
            var containerMeta = new YqdContainerInfo()
            {
                Id = newModel.Id,
                Name = newModel.Name,
                Description = newModel.Description,
                Tags = newModel.Tags,
                RuntimeIds = newModel.RuntimeIds,
                ImageId = newModel.ImageId,
                StartScript = newModel.StartScript,
                StartWarning = newModel.StartWarning,
                StopScript = newModel.StopScript,
                StopWarning = newModel.StopWarning,
                LogIgnoreList = newModel.LogIgnoreList,
                LogLevel = newModel.LogLevel,
                EnableRecordLog = newModel.EnableRecordLog,
                LogSaveDays = newModel.LogSaveDays,
                StartCron = newModel.StartCron,
                StopCron = newModel.StopCron,
                RestartCron = newModel.RestartCron,
                EnvironmentVariables = newModel.EnvironmentVariables
            };
            ContainerManager.Instance.Create(containerMeta);
        }

        public void SaveContainerFile(YqdContainerInfo model)
        {
            var containerFolder = ContainerPathUtils.GetContainerFolder(model.Id);
            if (!Directory.Exists(containerFolder))
                Directory.CreateDirectory(containerFolder);
            var containerMetaFile = Path.Combine(containerFolder, Consts.CONTAINER_META_FILE);
            var copyModel = YqdContainerInfo.Parse(model.ToJsonString());
            copyModel.Image = null;
            File.WriteAllText(containerMetaFile, copyModel.ToJsonString(), Encoding.UTF8);
        }

        public void Delete(ContainerContext context)
        {
            if (context == null)
                return;
            context.ContainerInfo.Enable = false;
            context.BeginDisable();
            context.Dispose();

            var containerFolder = ContainerPathUtils.GetContainerFolder(context.ContainerInfo.Id);
            var errorCount = 0;
            while (true)
            {
                try
                {
                    Directory.Delete(containerFolder, true);
                    break;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    if (errorCount >= 10)
                        throw new IOException($"删除目录[{containerFolder}]时出错。", ex);
                    Thread.Sleep(5000);
                }
            }

            ContainerList.Remove(context);
            refreshContainerDict();
            RaiseEvent_ContainerChanged();
        }

        public void Update(YqdContainerInfo containerInfo, CreateContainerModel newModel)
        {
            //如果容器编号有改变
            if (containerInfo.Id != newModel.Id)
            {
                var checkContainerContext = ContainerManager.Instance.Get(newModel.Id);
                if (checkContainerContext != null && containerInfo != checkContainerContext.ContainerInfo)
                    throw new IOException($"已经存在编号为[{newModel.Id}]的容器");
                //移动原容器到新目录
                Directory.Move(ContainerPathUtils.GetContainerFolder(containerInfo.Id), ContainerPathUtils.GetContainerFolder(newModel.Id));
                //修改编号
                containerInfo.Id = newModel.Id;
            }
            containerInfo.Name = newModel.Name;
            containerInfo.Description = newModel.Description;
            containerInfo.Tags = newModel.Tags;
            containerInfo.RuntimeIds = newModel.RuntimeIds;
            containerInfo.StartScript = newModel.StartScript;
            containerInfo.StartWarning = newModel.StartWarning;
            containerInfo.StopScript = newModel.StopScript;
            containerInfo.StopWarning = newModel.StopWarning;
            containerInfo.LogIgnoreList = newModel.LogIgnoreList;
            containerInfo.LogLevel = newModel.LogLevel;
            containerInfo.EnableRecordLog = newModel.EnableRecordLog;
            containerInfo.LogSaveDays = newModel.LogSaveDays;
            containerInfo.StartCron = newModel.StartCron;
            containerInfo.StopScript = newModel.StopScript;
            containerInfo.RestartCron = newModel.RestartCron;
            containerInfo.EnvironmentVariables = newModel.EnvironmentVariables;
            if (containerInfo.Image == null || !string.IsNullOrEmpty(newModel.ImageId))
            {
                containerInfo.ImageId = newModel.ImageId;
                containerInfo.Image = ImageManager.Instance.Get(newModel.ImageId);
            }
            SaveContainerFile(containerInfo);
            RaiseEvent_ContainerChanged();
        }
    }
}
