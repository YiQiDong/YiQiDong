using System.Diagnostics;
using YiQiDong.Protocol.V1.Model;

namespace YiQiDong.Agent.CommandExecuters
{
    public class GetThreadList
    {
        public static Protocol.V1.QpCommands.GetThreadList.Response Execute(Quick.Protocol.QpChannel channel, Protocol.V1.QpCommands.GetThreadList.Request request)
        {
            var threadList = new List<ThreadInfo>();
            var threadCollection = Process.GetCurrentProcess().Threads;
            for (var i = 0; i < threadCollection.Count; i++)
            {
                var processThread = threadCollection[i];
                string startTime = null;
                if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
                    startTime = processThread.StartTime.ToString();
                threadList.Add(new ThreadInfo()
                {
                    Id = processThread.Id.ToString(),
                    StartAddress = processThread.StartAddress.ToString(),
                    StartTime = startTime,
                    TotalProcessorTime = processThread.TotalProcessorTime.ToString(),
                    UserProcessorTime = processThread.UserProcessorTime.ToString(),
                    PrivilegedProcessorTime = processThread.PrivilegedProcessorTime.ToString(),
                    ThreadState = processThread.ThreadState.ToString(),
                    WaitReason = processThread.ThreadState == System.Diagnostics.ThreadState.Wait ? processThread.WaitReason.ToString() : null
                });
            }

            return new Protocol.V1.QpCommands.GetThreadList.Response()
            {
                Threads = threadList.ToArray()
            };
        }
    }
}
