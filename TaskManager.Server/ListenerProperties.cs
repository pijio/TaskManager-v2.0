namespace TaskManager.Server;

[Serializable]
public class ListenerProperties
{
    public int Port { get; set; } = 1337;
    public int MaxControllers { get; set; } = 5;
    public int MaxJobs { get; set; } = 20;
}