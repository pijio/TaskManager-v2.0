namespace TaskManager.Server;

public class JobInfo
{
    public long Memory { get; set; }
    public int ProcessorTime { get; set; }
    public int AbsoluteTime { get; set; }
    public int ProcessID { get; set; }
    public long VirtualMemory { get; set; }
    public IntPtr Handle { get; set; }
}