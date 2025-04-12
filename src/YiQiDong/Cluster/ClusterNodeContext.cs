using Quick.Protocol;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YiQiDong.Cluster.Model;
using YiQiDong.Core.Utils;

namespace YiQiDong.Cluster
{
    public class ClusterNodeContext
    {
        private ClusterConfig config;

        public bool Connected { get; private set; } = false;
        private string _CurrentState;
        public string CurrentState
        {
            get { return _CurrentState; }
            private set
            {
                _CurrentState = value;
                CurrentStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler ConnectStateChanged;
        public event EventHandler CurrentStateChanged;

        public QpChannel Channel => client;

        private QpClient client;
        private QpClientOptions clientOptions;
        private CancellationTokenSource cts;
        
        public ClusterNodeContext(ClusterConfig config)
        {
            this.config = config;
        }

        public void Start()
        {
            Stop();
            cts = new CancellationTokenSource();
            
            var uri = new Uri(config.OppositeNodeUrl);
            clientOptions = QpClientOptions.Parse(uri);
            clientOptions.Password = config.OppositeNodePassword;
            clientOptions.TransportTimeout = config.TransportTimeout;
            clientOptions.InstructionSet = new QpInstruction[] { Protocol.Instruction.Instance };

            client = clientOptions.CreateClient();
            client.Disconnected += Client_Disconnected;
            beginConnect(cts.Token);
        }

        private void Client_Disconnected(object sender, EventArgs e)
        {
            lock (this)
            {
                if (Connected)
                {
                    Connected = false;
                    CurrentState = "连接已断开";
                    ConnectStateChanged?.Invoke(this, EventArgs.Empty);
                    delayToConnect(cts.Token);
                }
            }
        }

        private ClusterConfig GetOppositeConfig()
        {
            return new ClusterConfig()
            {
                SelfNodeUrl = config.OppositeNodeUrl,
                OppositeNodeUrl = config.SelfNodeUrl,
                OppositeNodePassword = Program.Config.Password,
                AutoStart = config.AutoStart,
                ClusterContainerList = config.ClusterContainerList
                             .Select(t => new ClusterContainerInfo()
                             {
                                 ContainerName = t.ContainerName,
                                 IsSelfNodeActive = !t.IsSelfNodeActive
                             })
                             .ToList(),
            };
        }

        public async Task UpdateConfigAsync()
        {
            await client.SendCommand(new Protocol.QpCommands.UpdateConfig.Request()
            {
                Config = GetOppositeConfig()
            });
        }

        public async Task StartClusterAsync()
        {
            await client.SendCommand(new Protocol.QpCommands.StartCluster.Request());
        }

        public async Task StopClusterAsync()
        {
            await client.SendCommand(new Protocol.QpCommands.StopCluster.Request());
        }

        public async Task DeleteClusterAsync()
        {
            await client.SendCommand(new Protocol.QpCommands.DeleteCluster.Request());
        }

        private void delayToConnect(CancellationToken token)
        {
            Task.Delay(5000, token).ContinueWith(task =>
            {
                if (task.IsCanceled)
                    return;
                beginConnect(token);
            });
        }

        private void beginConnect(CancellationToken token)
        {
            CurrentState = $"正在连接到[{config.OppositeNodeUrl}]...";
            client.ConnectAsync().ContinueWith(async task =>
            {
                if (token.IsCancellationRequested)
                    return;
                if (task.IsFaulted)
                {
                    CurrentState = "连接失败";
                    delayToConnect(token);
                    return;
                }
                //创建集群
                CurrentState = "正在创建集群...";
                try
                {
                    await client.SendCommand(new Protocol.QpCommands.CreateCluster.Request()
                    {
                        Config = GetOppositeConfig()
                    });
                }
                catch (Exception ex)
                {
                    CurrentState = "创建集群失败,原因:" + ExceptionUtils.GetExceptionMessage(ex);
                    delayToConnect(token);
                    return;
                }
                //连接成功
                CurrentState = $"已连接";
                Connected = true;
                ConnectStateChanged?.Invoke(this, EventArgs.Empty);
            });
        }

        public void Stop()
        {
            if (client != null)
            {
                client.Disconnected -= Client_Disconnected;
                client.Close();
            }
            if (cts != null)
            {
                cts.Cancel();
                cts = null;
            }
        }
    }
}
