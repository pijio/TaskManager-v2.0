namespace TaskManager.Server;

public class Limits
{
    public long MemoryLimit { get; set; }
    public int ProcessorTimeLimit { get; set; }
    public int AbsoluteTimeLimit { get; set; }
    public Limits()
    {
        MemoryLimit = 60156416;    // значения по умолчанию
        ProcessorTimeLimit = (int)Math.Pow(10, 4);
        AbsoluteTimeLimit = (int)Math.Pow(10, 4);
    }
}