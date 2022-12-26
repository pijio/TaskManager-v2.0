using System.Diagnostics;

namespace TaskManager.Server;

public class Job
{
    public delegate void TerminateHandler(Job job);
    public string JobId { get; set; }
    public string ProgPath { get; set; }
    public JobInfo ProcInfo { get; set; }
    private bool IsAlive;
    public event TerminateHandler Terminate;
    public Job(string progPath)
    {
        JobId = Guid.NewGuid().ToString();
        ProgPath = progPath;
        ProcInfo = new JobInfo();
        IsAlive = true;
    }

    #region GetHashCode && Equals

    public override int GetHashCode() => JobId.GetHashCode();

    public override bool Equals(object obj)
    {
        Job other = obj as Job;
        if (other == null) return false;
        return other.JobId == JobId;
    }

    #endregion
    public void StartJob()
    {
        try
        {
            using (var process = Process.Start(new ProcessStartInfo { FileName = ProgPath, UseShellExecute = false }))
            {
                ProcInfo.ProcessID = process.Id;
                while (!process.HasExited && IsAlive)
                {
                    process.Refresh();
                    ProcInfo.Memory = process.PrivateMemorySize64;
                    ProcInfo.AbsoluteTime = (int)(DateTime.Now - process.StartTime).TotalMilliseconds;
                    ProcInfo.ProcessorTime = (int)process.TotalProcessorTime.TotalMilliseconds;
                }
                if(!IsAlive) process.Kill();
                Terminate(this);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Произошла ошибка при запуске задачи");
        }
    }
    
    public void CancelJob() => IsAlive = false;

}