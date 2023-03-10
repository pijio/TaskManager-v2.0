namespace TaskManager.Server;

public class Limits
{
    public long MemoryLimit { get; set; }
    public int ProcessorTimeLimit { get; set; }
    public int AbsoluteTimeLimit { get; set; }
    public long VirtualMemoryLimit { get; set; }

    public Limits()
    {
        MemoryLimit = 1000000000;    // значения по умолчанию
        VirtualMemoryLimit = 1000000000000;    // значения по умолчанию
        ProcessorTimeLimit = (int)Math.Pow(10, 6);
        AbsoluteTimeLimit = (int)Math.Pow(10, 6);
    }
}