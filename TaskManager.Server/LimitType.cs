namespace TaskManager.Server;

public enum LimitType : byte
{
    MemoryLimit,
    ProcessorTimeLimit,
    AbsoluteTimelimit
}