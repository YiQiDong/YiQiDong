using SharpCompress.Archives;
using YiQiDong.Core.Protocol.V1.Model;
using YiQiDong.Core.Utils;
using YiQiDong.Utils;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Archives.Zip;
using YiQiDong.Core.Utils.Unix;
using Quick.Utils;
using SharpCompress.Readers;

namespace YiQiDong.Core
{
    public class RuntimeManager
    {
        public const string VAR_RUNTIME_DIR = "$RUNTIME_DIR";

        public static RuntimeManager Instance { get; } = new RuntimeManager();

        private List<RuntimeInfo> RuntimeList = new List<RuntimeInfo>();
        private Dictionary<string, RuntimeInfo> runtimeIdRuntimeDict = new Dictionary<string, RuntimeInfo>();
        private Dictionary<string, RuntimeInfo[]> runtimeNameRuntimesDict = new Dictionary<string, RuntimeInfo[]>();
        //重试最大次数
        private const int RETRY_MAX_TIMES = 10;
        //重试间隔
        private const int RETRY_INTERVAL = 5000;

        public void Init()
        {
            var runtimesFolder = RuntimePathUtils.GetRuntimeFolder();
            if (!Directory.Exists(runtimesFolder))
                Directory.CreateDirectory(runtimesFolder);
            RuntimeList.Clear();
            foreach (var runtimeFolder in Directory.GetDirectories(runtimesFolder))
            {
                var runtimeInfo = LoadRuntimeDir(runtimeFolder);
                if (runtimeInfo == null)
                    continue;
                RuntimeList.Add(runtimeInfo);
            }
            refreshRuntimeDict();
        }

        private void refreshRuntimeDict()
        {
            RuntimeList = RuntimeList.OrderBy(x => x.Id).ToList();
            runtimeIdRuntimeDict = RuntimeList.ToDictionary(t => t.Id, t => t);
            runtimeNameRuntimesDict = RuntimeList.GroupBy(t => t.Name).ToDictionary(t => t.Key, t => t.ToArray());
        }

        private RuntimeInfo LoadRuntimeDir(string dir)
        {
            var runtimeMetaFile = Path.Combine(dir, Consts.RUNTIME_META_FILE);
            if (!File.Exists(runtimeMetaFile))
                return null;
            var content = File.ReadAllText(runtimeMetaFile);            
            try
            {
                var runtimeInfo = RuntimeInfo.Parse(content);
                if (runtimeInfo == null)
                    return null;
                runtimeInfo.Id = Path.GetFileName(dir);
                return runtimeInfo;
            }
            catch(Exception ex)
            {
                ConsoleUtils.ConsoleWriteLine($"[运行库管理器]加载[{dir}]时出错：{ExceptionUtils.GetExceptionString(ex)}");
                return null;
            }
        }

        private void CheckExecuteFiles(RuntimeInfo runtimeInfo)
        {
            //Windows系统，不需要添加权限
            if (OperatingSystem.IsWindows())
                return;
            if (runtimeInfo.ExecuteFiles != null && runtimeInfo.ExecuteFiles.Length > 0)
            {
                foreach (var t in runtimeInfo.ExecuteFiles)
                {
                    var executeFile = GetPathInRuntime(runtimeInfo, t);
                    UnixUtils.AddExecutePermissionToFile(executeFile);
                }
            }
        }

        public async Task<RuntimeInfo> LoadRuntimeFile(string file, Action<int, int, string> progressHandler, CancellationToken cancellationToken, string preRuntimeId, Action<string> messageHandler)
        {
            var tmpRuntimeDir = RuntimePathUtils.GetRuntimeFolder(Guid.NewGuid().ToString("N"));
            RuntimeInfo runtimeInfo = null;
            var preRuntimeInfo = Get(preRuntimeId);

            try
            {
                using (var ymgFileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                using(var archive = ArchiveFactory.OpenArchive(ymgFileStream))
                {
                    var totalEntryCount = 0;
                    var runtimeMetaContent = string.Empty;
                    //读取运行库文件元信息
                    using (var archiveReader = archive.ExtractAllEntries())
                        while (archiveReader.MoveToNextEntry())
                        {
                            var entry = archiveReader.Entry;
                            totalEntryCount++;
                            if (entry.Key == Consts.RUNTIME_META_FILE)
                            {
                                using (var entryStream = archiveReader.OpenEntryStream())
                                using (var reader = new StreamReader(entryStream))
                                    runtimeMetaContent = reader.ReadToEnd();
                            }
                        }
                    if (string.IsNullOrEmpty(runtimeMetaContent))
                        throw new FileNotFoundException("文件中未找到易启动运行库元信息");

                    runtimeInfo = RuntimeInfo.Parse(runtimeMetaContent);
                    //验证运行库架构是否匹配
                    var isRidMatch = runtimeInfo.Platform.Any(t => RuntimeUtils.IsMatchRID(t));
                    if (!isRidMatch)
                        throw new NotSupportedException($"运行库的架构[{string.Join(",", runtimeInfo.Platform)}]不匹配当前计算机架构[{RuntimeUtils.GetCurrentRID()}]");
                    //验证版本号是否合法
                    if (!Version.TryParse(runtimeInfo.Version, out _))
                        throw new NotSupportedException($"版本号[{runtimeInfo.Version}]不是有效的版本号");

                    var checkRuntimeInfo = GetItemByNameAndVersion(runtimeInfo.Name, runtimeInfo.Version);
                    //如果是添加运行库
                    if (preRuntimeInfo == null)
                    {
                        //验证运行库是否存在
                        if (preRuntimeInfo != null)
                            throw new ApplicationException($"上传的运行库[{runtimeInfo.Name} {runtimeInfo.Version}]已经存在！");
                    }
                    //如果是替换运行库
                    else
                    {
                        if (checkRuntimeInfo != null && checkRuntimeInfo != preRuntimeInfo)
                            throw new ApplicationException($"上传的运行库[{runtimeInfo.Name} {runtimeInfo.Version}]已经存在！");
                        //验证运行库名称是否匹配
                        if (preRuntimeInfo.Name != runtimeInfo.Name)
                            throw new ApplicationException($"上传的运行库[{runtimeInfo.Name} {runtimeInfo.Version}]与要替换的运行库[{preRuntimeInfo.Name} {preRuntimeInfo.Version}]名称不匹配，无法替换！");
                    }

                    //解压运行库文件
                    if (!Directory.Exists(tmpRuntimeDir))
                        Directory.CreateDirectory(tmpRuntimeDir);
                    var currentEntryCount = 0;
                    using (var archiveReader = archive.ExtractAllEntries())
                        while (archiveReader.MoveToNextEntry())
                        {
                            if (cancellationToken.IsCancellationRequested)
                                break;
                            currentEntryCount++;
                            progressHandler?.Invoke(totalEntryCount, currentEntryCount, archiveReader.Entry.Key);
                            archiveReader.WriteEntryToDirectory(tmpRuntimeDir);
                        }
                }

                //如果没有被取消，则加载运行库
                if (!cancellationToken.IsCancellationRequested)
                    runtimeInfo = LoadRuntimeDir(tmpRuntimeDir);

                if (runtimeInfo == null)
                {
                    await Task.Delay(1000).ContinueWith(t =>
                    {
                        if (Directory.Exists(tmpRuntimeDir))
                            Directory.Delete(tmpRuntimeDir, true);
                    });
                    return null;
                }

                runtimeInfo.Id = $"{runtimeInfo.Name}-{runtimeInfo.Version}";
                var newRuntimeFolder = RuntimePathUtils.GetRuntimeFolder(runtimeInfo.Id);
                ContainerContext[] containers = null;
                //添加运行库
                if (preRuntimeInfo == null)
                {
                    //删除之前运行库的目录
                    for (var i = 0; i < RETRY_MAX_TIMES; i++)
                    {
                        progressHandler.Invoke(RETRY_MAX_TIMES, i + 1, $"正在删除之前运行库目录...");
                        if (!Directory.Exists(newRuntimeFolder))
                            break;
                        try { Directory.Delete(newRuntimeFolder, true); }
                        catch (Exception ex)
                        {
                            if (i + 1 >= RETRY_MAX_TIMES)
                                throw new IOException($"目录[{newRuntimeFolder}]无法删除，替换失败。", ex);
                            Thread.Sleep(RETRY_INTERVAL);
                        }
                    }
                    //移动运行库目录
                    var sourceFolder = tmpRuntimeDir;
                    var desFolder = newRuntimeFolder;                    
                    for (var i = 0; i < RETRY_MAX_TIMES; i++)
                    {
                        progressHandler.Invoke(RETRY_MAX_TIMES, i + 1, $"正在移动运行库目录...");
                        if (!Directory.Exists(sourceFolder))
                            break;
                        try { Directory.Move(sourceFolder, desFolder); }
                        catch (Exception ex)
                        {
                            if (i + 1 >= RETRY_MAX_TIMES)
                                throw new IOException($"将目录[{sourceFolder}]移动到[{desFolder}]时出错。", ex);
                            Thread.Sleep(RETRY_INTERVAL);
                        }
                    }
                    CheckExecuteFiles(runtimeInfo);
                    containers = ContainerManager.Instance.UseRuntimeContainers(runtimeInfo.Id);
                    messageHandler?.Invoke($"已添加运行库[{runtimeInfo.Name} {runtimeInfo.Version}].");
                }
                //替换运行库。
                else
                {
                    containers = ContainerManager.Instance.UseRuntimeContainers(preRuntimeInfo.Id);
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
                    var preRuntimeFolder = RuntimePathUtils.GetRuntimeFolder(preRuntimeInfo.Id);
                    //删除之前运行库的目录
                    for (var i = 0; i < RETRY_MAX_TIMES; i++)
                    {
                        progressHandler.Invoke(RETRY_MAX_TIMES, i + 1, $"正在删除之前运行库目录...");
                        if (!Directory.Exists(preRuntimeFolder)
                            && !Directory.Exists(newRuntimeFolder))
                            break;

                        try
                        {
                            if (Directory.Exists(preRuntimeFolder))
                                Directory.Delete(preRuntimeFolder, true);
                            if (Directory.Exists(newRuntimeFolder))
                                Directory.Delete(newRuntimeFolder, true);
                        }
                        catch (Exception ex)
                        {
                            if (i + 1 >= RETRY_MAX_TIMES)
                                throw new IOException($"之前运行库目录无法删除，替换失败。", ex);
                            Thread.Sleep(RETRY_INTERVAL);
                        }
                    }
                    //移动运行库目录
                    var sourceFolder = tmpRuntimeDir;
                    var desFolder = newRuntimeFolder;
                    for (var i = 0; i < RETRY_MAX_TIMES; i++)
                    {
                        progressHandler.Invoke(RETRY_MAX_TIMES, i + 1, $"正在移动运行库目录...");
                        if (!Directory.Exists(sourceFolder))
                            break;
                        try { Directory.Move(sourceFolder, desFolder); }
                        catch (Exception ex)
                        {
                            if (i + 1 >= RETRY_MAX_TIMES)
                                throw new IOException($"将目录[{sourceFolder}]移动到[{desFolder}]时出错。", ex);
                            Thread.Sleep(RETRY_INTERVAL);
                        }
                    }
                    //修改关联容器的运行库名称
                    foreach (var container in containers)
                    {
                        container.ContainerInfo.RuntimeIds = container.ContainerInfo.RuntimeIds
                            .Select(t =>
                            {
                                if (t == preRuntimeId)
                                    return runtimeInfo.Id;
                                return t;
                            }).ToArray();
                        ContainerManager.Instance.SaveContainerFile(container.ContainerInfo);
                    }
                    RuntimeList.Remove(preRuntimeInfo);
                    CheckExecuteFiles(runtimeInfo);
                    messageHandler?.Invoke($"已替换运行库[{runtimeInfo.Name} {runtimeInfo.Version}].");
                }
                //添加运行库
                RuntimeList.Add(runtimeInfo);
                refreshRuntimeDict();
                //容器启用
                if (containers != null)
                    for (var i = 0; i < containers.Length; i++)
                    {
                        var container = containers[i];
                        progressHandler.Invoke(i + 1, containers.Length, $"正在启用容器[{container.ContainerInfo.Name}]...");
                        container.BeginEnable();
                    }
                return runtimeInfo;
            }
            catch
            {
                await Task.Run(() =>
                {
                    for (var i = 0; i < RETRY_MAX_TIMES; i++)
                    {
                        progressHandler.Invoke(RETRY_MAX_TIMES, i + 1, $"正在删除临时运行库目录...");
                        if (!Directory.Exists(tmpRuntimeDir))
                            break;
                        try { Directory.Delete(tmpRuntimeDir, true); }
                        catch
                        {
                            Thread.Sleep(RETRY_INTERVAL);
                        }
                    }
                });
                throw;
            }
        }

        public RuntimeInfo Get(string runtimeId)
        {
            if (string.IsNullOrEmpty(runtimeId))
                return null;
            if (runtimeIdRuntimeDict.TryGetValue(runtimeId, out var runtimeInfo))
                return runtimeInfo;
            return null;
        }

        public RuntimeInfo[] GetAll()
        {
            return RuntimeList.ToArray();
        }

        public RuntimeInfo[] GetItemsByName(string name)
        {
            if (runtimeNameRuntimesDict.TryGetValue(name, out var runtimeInfoArray))
                return runtimeInfoArray;
            return new RuntimeInfo[0];
        }

        public RuntimeInfo GetItemByNameAndVersion(string name, string version)
        {
            return GetItemsByName(name).FirstOrDefault(t => t.Version == version);
        }

        public RuntimeInfo[] Query(string keywords)
        {
            IEnumerable<RuntimeInfo> query = RuntimeList;
            if (!string.IsNullOrEmpty(keywords))
                query = query.Where(t => t.Name.Contains(keywords));
            //按名称和版本号排序
            query = query.OrderBy(t => t.Version);
            query = query.OrderBy(t => t.Name);
            return query.ToArray();
        }

        public void DeleteRuntime(string runtimeId)
        {
            var model = Get(runtimeId);
            if (model == null)
                return;
            var runtimeFolder = RuntimePathUtils.GetRuntimeFolder(model.Id);
            if (Directory.Exists(runtimeFolder))
                Directory.Delete(runtimeFolder, true);
            RuntimeList.Remove(model);
            refreshRuntimeDict();
        }

        public string GetPathInRuntime(RuntimeInfo runtime, string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            var runtimeDir = RuntimePathUtils.GetRuntimeFolder(runtime.Id);
            return path.Replace(VAR_RUNTIME_DIR, runtimeDir);
        }

        public string[] GetRuntimesPath(params RuntimeInfo[] runtimes)
        {
            List<string> pathList = new List<string>();
            if (runtimes != null)
            {
                foreach (var runtime in runtimes)
                {
                    //添加PATH
                    if (runtime.Path != null)
                    {
                        foreach (var path in runtime.Path)
                        {
                            pathList.Add(GetPathInRuntime(runtime, path));
                        }
                    }
                }
            }
            return pathList.ToArray();
        }

        public Dictionary<string, string> GetRuntimesEnvironment(params RuntimeInfo[] runtimes)
        {
            var enviroment = new Dictionary<string, string>();
            if (runtimes != null)
            {
                foreach (var runtime in runtimes)
                {
                    //处理环境变量
                    if (runtime.Environment != null)
                    {
                        foreach (var item in runtime.Environment)
                        {
                            enviroment[item.Key] = GetPathInRuntime(runtime, item.Value);
                        }
                    }
                }
            }
            return enviroment;
        }
    }
}
