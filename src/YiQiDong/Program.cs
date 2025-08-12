using BlazorDownloadFile;
using YiQiDong.Cluster;
using YiQiDong.Core;
using YiQiDong.Core.Utils;
using YiQiDong.Utils;
using System.Globalization;
using Quick.LiteDB.Plus;
using System.Diagnostics.CodeAnalysis;
using Tewr.Blazor.FileReader;
using YiQiDong.Components;
using Blazored.LocalStorage;

namespace YiQiDong
{
    public class Program
    {
        private static Task waitForExitTask;
        private static WebApplication app;
        public static SystemInfoContext SystemInfoContext { get; private set; }
        //是否启动完成
        public static bool IsStartSuccess = true;
        public static string StartErrorMessage;

        public static Model.ConfigModel Config { get; private set; }

        internal static void LoadConfig()
        {
            Quick.Protocol.QpAllClients.RegisterUriSchema();
            Quick.Localize.GettextResourceManager.ChangeCurrentCulture(CultureInfo.GetCultureInfo("zh-CN"));
#if (!DEBUG)
                //设置当前目录为程序所在的目录
                Environment.CurrentDirectory = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
#endif
            Config = Model.ConfigModel.Load();
        }

        public static void Main(string[] args)
        {
            //注册编码提供程序(支持GB2312等编码)
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            ArgsHandlers.ArgsHandler.Invoke(args);
        }


        public static Task Start()
        {
            try
            {
                Console.WriteLine($@"---------------------
  易启动 [{Consts.Version}]
---------------------");
                //设置数据目录
                var dataDir = Config.DataFolder;
                if (!Path.IsPathRooted(dataDir))
                    dataDir = FolderUtils.GetPathUnderProgramDir(dataDir);
                FolderUtils.DataFolder = dataDir;
                try
                {
                    //确保数据目录创建
                    if (!Directory.Exists(dataDir))
                        Directory.CreateDirectory(dataDir);
                    Console.WriteLine("正在收集系统信息...");
                    SystemInfoContext = new SystemInfoContext();
                    Console.WriteLine("正在初始化数据库...");
                    var dbFile = Path.Combine(dataDir, "Config.litedb");
                    ConfigDbContext.Init(dbFile, modelBuilder =>
                    {
                        Quick.Blazor.Bootstrap.CrontabManager.Global.Instance.OnModelCreating(modelBuilder);
                        Quick.Blazor.Bootstrap.ReverseProxy.Global.Instance.OnModelCreating(modelBuilder);
                        Glash.Blazor.Agent.Global.Instance.OnModelCreating(modelBuilder);
                        Glash.Blazor.Server.Global.Instance.OnModelCreating(modelBuilder);
                        Glash.Blazor.Client.Global.Instance.OnModelCreating(modelBuilder);
                    });
                    ConfigDbContext.CacheContext.LoadCache();
                    Quick.Blazor.Bootstrap.CrontabManager.Core.CrontabManager.Instance.Start();
                    Glash.Blazor.Agent.Core.GlashAgentManager.Instance.Init();
                    //异步加载
                    Task.Run(() =>
                    {
                        try
                        {
                            OsPlatformManager.Instance.Init();
                            Console.WriteLine("正在初始化运行库管理器...");
                            RuntimeManager.Instance.Init();
                            Console.WriteLine("正在初始化镜像管理器...");
                            ImageManager.Instance.Init();
                            Console.WriteLine("正在启动容器管理器...");
                            ContainerManager.Instance.Init();
                            Console.WriteLine("正在启动集群管理器...");
                            ClusterManager.Instance.Init();
                        }
                        catch (Exception ex)
                        {
                            IsStartSuccess = false;
                            StartErrorMessage = ExceptionUtils.GetExceptionString(ex);
                            Console.WriteLine(StartErrorMessage);
                        }
                    });

                    //检查备份目录是否存在，如果不存在，则创建
                    var backupFolder = FolderUtils.GetBackupDir();
                    if (!Directory.Exists(backupFolder))
                        Directory.CreateDirectory(backupFolder);
                }
                catch (Exception ex)
                {
                    IsStartSuccess = false;
                    StartErrorMessage = ExceptionUtils.GetExceptionString(ex);
                }
                var startWebServiceTask = StartWebService();
                startWebServiceTask.Wait();
                waitForExitTask = new Task(() => Console.WriteLine("[停止完成]"));
                return waitForExitTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine("启动时出错。原因：" + ExceptionUtils.GetExceptionMessage(ex));
                throw;
            }
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        public static async Task StartWebService()
        {
            try
            {
                await Task.Delay(100);
                Console.WriteLine($"正在准备Web服务相关资源...");
                var webUrls = Config.Urls;
#if DEBUG
                webUrls = "http://localhost:5001";
#endif
                var builder = WebApplication.CreateBuilder();
                builder.Logging.ClearProviders();
                builder.Services.AddBlazorDownloadFile();
                builder.Services.AddFileReaderService();
                builder.Services.AddBlazoredLocalStorage();
                builder.Services.AddRazorComponents()
                    .AddInteractiveServerComponents()
                    .AddHubOptions(options =>
                    {
                        //设置最大包大小为100 MB
                        options.MaximumReceiveMessageSize = 100 * 1024 * 1024;
                    });
                builder.Services.ConfigureHttpJsonOptions(options =>
                {
                    options.SerializerOptions.TypeInfoResolverChain.Add(Model.YqdContainerInfoSerializerContext.Default);
                });
                builder.Services.AddECharts();
                if (IsStartSuccess)
                    Quick.Blazor.Bootstrap.ReverseProxy.ReverseProxyManager.Instance.Load(builder.Services.AddReverseProxy());
                builder.WebHost
                    .UseUrls(webUrls.Split([',', ';']))
                    .ConfigureKestrel(options => options.AddServerHeader = false);

                app = builder.Build();
                // Configure the HTTP request pipeline.
                if (!app.Environment.IsDevelopment())
                {
                    app.UseExceptionHandler("/Error", createScopeForErrors: true);
                }
                app.UseWebSockets();
                if (IsStartSuccess)
                    app.UseGlashServer("/glash", Glash.Blazor.Server.Global.Instance.ConnectionPassword);
                app.UseNorthInterface();
                if (IsStartSuccess)
                    app.UseClusterInterface();
                app.UseAntiforgery();
                app.MapStaticAssets();
                if (IsStartSuccess)
                    app.MapReverseProxy();
                app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
                app.UseYiQiDongControllers();
                Console.WriteLine($"正在启动Web服务:{webUrls}...");
                await app.StartAsync();
                Console.WriteLine("[Web服务启动完成]");
            }
            catch (Exception ex)
            {
                throw new IOException("启动Web服务失败，请检查端口是否被占用。原因：" + ExceptionUtils.GetExceptionString(ex));
            }
        }

        public static async Task StopWebService()
        {
            Console.WriteLine("正在停止Web服务...");
            await app.StopAsync();
        }

        public static void StopContainerAndCluster()
        {
            SystemInfoContext.Dispose();
            Console.WriteLine("正在停止容器管理器...");
            ContainerManager.Instance.Stop();
            Console.WriteLine("正在停止集群管理器...");
            ClusterManager.Instance.Stop();
        }

        public static void Stop()
        {
            StopContainerAndCluster();
            StopWebService().Wait();
            waitForExitTask.Start();
        }
    }
}
