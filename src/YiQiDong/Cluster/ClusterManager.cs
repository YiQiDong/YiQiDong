using Quick.Protocol;
using Quick.Protocol.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using YiQiDong.Cluster.Model;
using YiQiDong.Core;
using YiQiDong.Utils;

namespace YiQiDong.Cluster
{
    public class ClusterManager
    {
        public const string CLUSTER_CONFIG_FILE = "cluster.json";
        private string clusterConfigFile;
        public static ClusterManager Instance { get; } = new ClusterManager();

        private CommandExecuterManager commandExecuterManager = new CommandExecuterManager();
        private Dictionary<string, ClusterContainerInfo> clusterConainterDict = new Dictionary<string, ClusterContainerInfo>();
        private Queue<string> logQueue = new Queue<string>();
        public event EventHandler<string> NewLogArrived;
        public event EventHandler ClusterChanged;
        public ClusterConfig Config { get; private set; }
        public ClusterNodeContext OppositeNodeContext { get; private set; }
        public QpChannel OppositeNodeChannel { get; private set; }

        public string[] GetLogs()
        {
            lock (logQueue)
                return logQueue.ToArray();
        }

        public void PushLog(string log)
        {
            var line = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: {log}";
            lock (logQueue)
            {
                while (logQueue.Count > 100)
                    logQueue.Dequeue();
                logQueue.Enqueue(line);
            }
            NewLogArrived?.Invoke(this, line);
        }

        public bool IsConnected
        {
            get
            {
                var context = OppositeNodeContext;
                if (context == null)
                    return false;
                if(!context.Connected)
                    return false;
                if (OppositeNodeChannel == null)
                    return false;
                return true;
            }
        }

        public async void StartCluster(bool isSelfNodeOperate = true)
        {
            Config.AutoStart = true;
            saveConfig();
            //当是自身节点操作时才通知对方节点
            if (isSelfNodeOperate)
                await OppositeNodeContext.StartClusterAsync();
            RaiseEvent_ClusterChanged();

            //启动自身节点激活的容器
            foreach (var clusterContainer in Config.ClusterContainerList)
            {
                if (!clusterContainer.IsSelfNodeActive)
                    continue;
                var containerContext = ContainerManager.Instance.GetByName(clusterContainer.ContainerName);
                if (!containerContext.ContainerInfo.AutoStart)
                {
                    PushLog($"启动容器[{containerContext.ContainerInfo.Name}]");
                    await containerContext.Start();
                }
            }
            PushLog($"已启动集群");
        }

        public async void StopCluster(bool isSelfNodeOperate = true)
        {
            Config.AutoStart = false;
            saveConfig();
            //当是自身节点操作时才通知对方节点
            if (isSelfNodeOperate)
                if (OppositeNodeContext.Connected)
                    _ = OppositeNodeContext.StopClusterAsync();
            
            RaiseEvent_ClusterChanged();

            //停止集群容器
            foreach (var clusterContainer in Config.ClusterContainerList)
            {
                var containerContext = ContainerManager.Instance.GetByName(clusterContainer.ContainerName);
                if (containerContext.ContainerInfo.AutoStart)
                {
                    PushLog($"停止容器[{containerContext.ContainerInfo.Name}]");
                    await containerContext.Stop();
                }
            }
            PushLog($"已停止集群");
        }

        private void RaiseEvent_ClusterChanged()
        {
            ClusterChanged?.Invoke(this, EventArgs.Empty);
        }

        public async Task AddClusterContainer(ClusterContainerInfo item)
        {
            Config.ClusterContainerList.Add(item);
            lock (clusterConainterDict)
                clusterConainterDict[item.ContainerName] = item;
            await OppositeNodeContext.UpdateConfigAsync();
            saveConfig();
            RaiseEvent_ClusterChanged();
        }

        public async Task DeleteClusterContainer(ClusterContainerInfo item)
        {
            Config.ClusterContainerList.Remove(item);
            lock (clusterConainterDict)
                if (clusterConainterDict.ContainsKey(item.ContainerName))
                    clusterConainterDict.Remove(item.ContainerName);
            await OppositeNodeContext.UpdateConfigAsync();
            saveConfig();
            RaiseEvent_ClusterChanged();
        }

        public ClusterManager()
        {
            commandExecuterManager.Register(new Protocol.QpCommands.GetContainerList.Request(), ExecuteCommand_GetContainerList);
            commandExecuterManager.Register(new Protocol.QpCommands.CreateCluster.Request(), ExecuteCommand_CreateCluster);
            commandExecuterManager.Register(new Protocol.QpCommands.DeleteCluster.Request(), ExecuteCommand_DeleteCluster);
            commandExecuterManager.Register(new Protocol.QpCommands.UpdateConfig.Request(), ExecuteCommand_UpdateConfig);
            commandExecuterManager.Register(new Protocol.QpCommands.StartCluster.Request(), ExecuteCommand_StartCluster);
            commandExecuterManager.Register(new Protocol.QpCommands.StopCluster.Request(), ExecuteCommand_StopCluster);
        }

        public bool ContainsContainer(string containerName)
        {
            return clusterConainterDict.ContainsKey(containerName);
        }

        public void Init()
        {
            clusterConfigFile = FolderUtils.GetPathUnderDataDir(CLUSTER_CONFIG_FILE);
            if (File.Exists(clusterConfigFile))
            {
                var content = File.ReadAllText(clusterConfigFile);
                Config = JsonSerializer.Deserialize(content,ClusterConfigSerializerContext.Default.ClusterConfig);
                clusterConainterDict = Config.ClusterContainerList.ToDictionary(t => t.ContainerName, t => t);
                CreateAndStartOppositeNodeContext();
                if (Config.AutoStart)
                {
                    StartCluster(false);
                    var preConfig = Config;
                    //如果15秒后，还未连接上对方节点，则判定对方节点断线
                    Task.Delay(TimeSpan.FromSeconds(15)).ContinueWith(t =>
                    {
                        var currentConfig = Config;
                        if (currentConfig == null || currentConfig != preConfig)
                            return;
                        if (currentConfig.AutoStart && !IsConnected)
                            //触发对方节点断线后处理方法
                            OppositeNodeChannel_Disconnected(this, EventArgs.Empty);
                    });
                }
            }
        }

        public void Stop()
        {
            StopOppositeNodeContext();
        }

        private void saveConfig()
        {
            if (File.Exists(clusterConfigFile))
                File.Delete(clusterConfigFile);
            if (Config != null)
            {
                var content = JsonSerializer.Serialize(Config, ClusterConfigSerializerContext.Default.ClusterConfig);
                File.WriteAllText(clusterConfigFile, content);
            }
        }

        private void CreateAndStartOppositeNodeContext()
        {
            StopOppositeNodeContext();
            if (!string.IsNullOrEmpty(Config.OppositeNodeUrl))
            {
                OppositeNodeContext = new ClusterNodeContext(Config);
                OppositeNodeContext.ConnectStateChanged += ClusterNodeContext_ConnectStateChanged;
                OppositeNodeContext.CurrentStateChanged += ClusterNodeContext_CurrentStateChanged;
                OppositeNodeContext.Start();
            }
        }

        private void StopOppositeNodeContext()
        {
            if (OppositeNodeContext != null)
            {
                OppositeNodeContext.ConnectStateChanged -= ClusterNodeContext_ConnectStateChanged;
                OppositeNodeContext.CurrentStateChanged -= ClusterNodeContext_CurrentStateChanged;
                OppositeNodeContext.Stop();
                OppositeNodeContext = null;
            }
        }

        public void Create(ClusterConfig model)
        {
            Config = model;
            saveConfig();
            CreateAndStartOppositeNodeContext();
            RaiseEvent_ClusterChanged();
            PushLog("已创建集群");
        }

        private void ClusterNodeContext_CurrentStateChanged(object sender, EventArgs e) => RaiseEvent_ClusterChanged();
        private void ClusterNodeContext_ConnectStateChanged(object sender, EventArgs e)
        {
            RaiseEvent_ClusterChanged();
            if (OppositeNodeContext.Connected)
                PushLog($"已连接到对方节点，URL：{Config.OppositeNodeUrl}");
            else
                PushLog($"与对方节点的连接已断开，URL：{Config.OppositeNodeUrl}");
        }

        public void Delete()
        {
            Config = null;
            lock (clusterConainterDict)
                clusterConainterDict.Clear();
            saveConfig();
            if (OppositeNodeContext != null)
                _ = OppositeNodeContext.DeleteClusterAsync();

            StopOppositeNodeContext();
            RaiseEvent_ClusterChanged();
            PushLog("已删除集群");
        }

        public void HandleServerOptions(QpServerOptions serverOptions)
        {
            serverOptions.RegisterCommandExecuterManager(commandExecuterManager);
            serverOptions.InstructionSet = new[] { Protocol.Instruction.Instance };
        }

        private Protocol.QpCommands.GetContainerList.Response ExecuteCommand_GetContainerList(QpChannel channel, Protocol.QpCommands.GetContainerList.Request request)
        {
            return new Protocol.QpCommands.GetContainerList.Response()
            {
                Items = ContainerManager.Instance.GetAll()
                            .Select(t => t.ContainerInfo)
                            .ToArray()
            };
        }

        private Protocol.QpCommands.CreateCluster.Response ExecuteCommand_CreateCluster(QpChannel channel, Protocol.QpCommands.CreateCluster.Request request)
        {
            //如果当前没有配置集群，则创建
            if (Config == null)
            {
                if (request.Config.AutoStart)
                    throw new CommandException(1, "集群启动状态的配置不匹配");
                PushLog($"对方节点发来创建集群请求");
                Create(request.Config);
            }
            //如果已经配置了集群，则验证配置
            else
            {
                if (Config.AutoStart != request.Config.AutoStart)
                    throw new CommandException(1, "集群启动状态的配置不匹配");
                if (Config.SelfNodeUrl != request.Config.SelfNodeUrl)
                    throw new CommandException(1, "自身节点URL的配置不匹配");
                if (Config.OppositeNodeUrl != request.Config.OppositeNodeUrl)
                    throw new CommandException(1, "对方节点URL的配置不匹配");
                if (JsonSerializer.Serialize(Config.ClusterContainerList,ClusterConfigSerializerContext.Default.ListClusterContainerInfo)
                    != JsonSerializer.Serialize(request.Config.ClusterContainerList,ClusterConfigSerializerContext.Default.ListClusterContainerInfo))
                    throw new CommandException(1, "集群容器列表的配置不匹配");
                if (OppositeNodeContext == null)
                    CreateAndStartOppositeNodeContext();
            }
            OppositeNodeChannel = channel;
            OppositeNodeChannel.Disconnected += OppositeNodeChannel_Disconnected;
            RaiseEvent_ClusterChanged();
            PushLog($"对方节点已上线，通道：{channel.ChannelName}");
            //如果集群配置为自动启动
            if (Config.AutoStart)
            {
                PushLog("对方节点上线，停止不属于自身节点激活的容器");
                foreach (var containerInfo in Config.ClusterContainerList)
                {
                    if (containerInfo.IsSelfNodeActive)
                        continue;
                    var containerContext = ContainerManager.Instance.GetByName(containerInfo.ContainerName);
                    if (containerContext == null)
                        continue;
                    if (containerContext.ContainerInfo.AutoStart)
                    {
                        PushLog($"停止容器[{containerContext.ContainerInfo.Name}]");
                        _ = containerContext.Stop();
                    }
                }
            }
            return new Protocol.QpCommands.CreateCluster.Response();
        }

        private Protocol.QpCommands.DeleteCluster.Response ExecuteCommand_DeleteCluster(QpChannel channel, Protocol.QpCommands.DeleteCluster.Request request)
        {
            if (Config != null)
            {
                PushLog($"对方节点发来删除集群请求");
                Delete();
            }
            return new Protocol.QpCommands.DeleteCluster.Response();
        }

        private Protocol.QpCommands.UpdateConfig.Response ExecuteCommand_UpdateConfig(QpChannel channel, Protocol.QpCommands.UpdateConfig.Request request)
        {
            Config.ClusterContainerList = request.Config.ClusterContainerList;
            clusterConainterDict = Config.ClusterContainerList.ToDictionary(t => t.ContainerName, t => t);
            saveConfig();
            RaiseEvent_ClusterChanged();
            return new Protocol.QpCommands.UpdateConfig.Response();
        }

        private void OppositeNodeChannel_Disconnected(object sender, EventArgs e)
        {
            if (OppositeNodeChannel != null)
            {
                OppositeNodeChannel.Disconnected -= OppositeNodeChannel_Disconnected;
                OppositeNodeChannel = null;
                RaiseEvent_ClusterChanged();
            }
            PushLog("对方节点已下线");

            //如果集群配置为自动启动
            if (Config != null && Config.AutoStart)
            {
                PushLog("对方节点断线，启动不属于自身节点激活的容器");
                foreach (var containerInfo in Config.ClusterContainerList)
                {
                    if (containerInfo.IsSelfNodeActive)
                        continue;
                    var containerContext = ContainerManager.Instance.GetByName(containerInfo.ContainerName);
                    if (containerContext == null)
                        continue;
                    if (!containerContext.ContainerInfo.AutoStart)
                    {
                        PushLog($"启动容器[{containerContext.ContainerInfo.Name}]");
                        containerContext.Start();
                    }
                }
            }
        }

        private Protocol.QpCommands.StartCluster.Response ExecuteCommand_StartCluster(QpChannel channel, Protocol.QpCommands.StartCluster.Request request)
        {
            PushLog($"对方节点发来启动集群请求");
            StartCluster(false);
            return new Protocol.QpCommands.StartCluster.Response();
        }

        private Protocol.QpCommands.StopCluster.Response ExecuteCommand_StopCluster(QpChannel channel, Protocol.QpCommands.StopCluster.Request request)
        {
            PushLog($"对方节点发来停止集群请求");
            StopCluster(false);
            return new Protocol.QpCommands.StopCluster.Response();
        }
    }
}
