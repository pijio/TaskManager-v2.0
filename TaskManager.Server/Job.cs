using System.Data;
using System.Diagnostics;

namespace TaskManager.Server;

public class Job
{
    private bool IsAlive;
    private Process _process;
    public delegate void TerminateHandler(Job job);
    public string JobId { get; set; }
    public string ProgPath { get; set; }
    public JobInfo ProcInfo { get; set; }
    public event TerminateHandler Terminate;
    public Job(string progPath)
    {
        JobId = Guid.NewGuid().ToString();
        ProgPath = progPath;
        ProcInfo = new JobInfo();
        try
        {
            _process = Process.Start(new ProcessStartInfo { FileName = ProgPath, UseShellExecute = false });
            if (_process == null) throw new Exception("Произошла ошибка при запуске задачи");
            ProcInfo.ProcessID = _process.Id;
            IsAlive = true;
            UpdateInfo();
        }
        catch {}
    }
    public void StartJob()
    {
        try
        {
            while (!_process.HasExited && IsAlive)
            {
                UpdateInfo();
            }
            _process.Kill();
            Terminate(this);
        }
        catch (Exception e)
        {
            Console.WriteLine("Произошла ошибка при запуске задачи");
        }
    }

    private void UpdateInfo()
    {
        _process.Refresh();
        ProcInfo.Memory = _process.PrivateMemorySize64;
        ProcInfo.AbsoluteTime = (int)(DateTime.Now - _process.StartTime).TotalMilliseconds;
        ProcInfo.ProcessorTime = (int)_process.TotalProcessorTime.TotalMilliseconds;
    }
    public void CancelJob() => IsAlive = false;
    
    #region GetHashCode && Equals

    public override int GetHashCode() => JobId.GetHashCode();

    public override bool Equals(object obj)
    {
        Job other = obj as Job;
        if (other == null) return false;
        return other.JobId == JobId;
    }

    #endregion

}