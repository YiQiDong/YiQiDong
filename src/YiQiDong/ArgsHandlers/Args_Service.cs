using System.Runtime.Versioning;
using System.ServiceProcess;

namespace YiQiDong.ArgsHandlers
{
    [SupportedOSPlatform("windows")]
    public partial class WinService : ServiceBase
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            this.ServiceName = "YiQiDong";
        }

        public WinService()
        {
            InitializeComponent();
        }

        public void Start(string[] args)
        {
            OnStart(args);
        }

        protected override void OnStart(string[] args)
        {
            Program.Start();
        }

        protected override void OnStop()
        {
            Program.Stop();
        }
    }

    public class Args_Service
    {
        internal static void Invoke(string[] args)
        {
            if (OperatingSystem.IsWindows())
            {
                WinService service = new WinService();
                ServiceBase.Run(service);
            }
            else
            {
                Program.Start();
                new HostBuilder().RunConsoleAsync().Wait();
                Program.Stop();
            }
        }
    }
}
