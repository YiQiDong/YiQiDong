namespace YiQiDong.Protocol.V1.Model;

public class ThreadInfo
{
    public string Id { get; set; }
    public string StartAddress { get; set; }
    public string StartTime { get; set; }
    public string ThreadState { get; set; }
    public string WaitReason { get; set; }
    public string TotalProcessorTime { get; set; }
    public string UserProcessorTime { get; set; }
    public string PrivilegedProcessorTime { get; set; }
}