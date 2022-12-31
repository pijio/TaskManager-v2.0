using System.Data;
using System.Diagnostics;

namespace TaskManager.Server;

public class Job
{
    private bool IsAlive;
    private Process _process;
    private bool OnError;
    public delegate void TerminateHandler(Job job, bool succsesful);
    public string JobId { get; set; }
    public string ProgPath { get; set; }
    public JobInfo ProcInfo { get; set; }
    public event TerminateHandler Terminate;
    public Job(string progPath)
    {
        JobId = Guid.NewGuid().ToString();
        ProgPath = progPath;
    }
    /// <summary>
    /// поток выполнения задачи
    /// </summary>
    /// <exception cref="Exception"></exception>
    public void StartJob()
    {
        try
        {
            _process = Process.Start(new ProcessStartInfo { FileName = ProgPath, UseShellExecute = false });
            if (_process == null) throw new Exception("Произошла ошибка при запуске задачи");
            ProcInfo = new JobInfo();
            ProcInfo.ProcessID = _process.Id;
            UpdateInfo();
            IsAlive = true;
            while (!_process.HasExited ^ !IsAlive)
            {
                UpdateInfo();
            }
            _process.Kill();
            Terminate(this, OnError ? IsAlive : true);
        }
        catch
        {
            Terminate(this, false);
        }
    }
    /// <summary>
    /// Обновление стейта процесса
    /// </summary>
    private void UpdateInfo()
    {
        _process.Refresh();
        ProcInfo.Memory = _process.PrivateMemorySize64;
        ProcInfo.AbsoluteTime = (int)(DateTime.Now - _process.StartTime).TotalMilliseconds;
        ProcInfo.ProcessorTime = (int)_process.TotalProcessorTime.TotalMilliseconds;
        ProcInfo.VirtualMemory = _process.VirtualMemorySize64;
        ProcInfo.Handle = _process.Handle;
    }
    /// <summary>
    /// Отмена задачи извне
    /// </summary>
    public void CancelJob(bool onError = true)
    {
        IsAlive = false;
        OnError = onError;
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

}