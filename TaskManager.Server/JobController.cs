using System.Collections.Concurrent;
using System.Text;
using System.Threading.Channels;

namespace TaskManager.Server
{
    public class JobController
    {
        private ConcurrentDictionary<Job, Thread> _controlledProcesses;
        private readonly Mutex _mutex;
        private LimitsHelper _limitsHelper; 
        public string JobControllerId { get; set; }
        public int ProcsInPool => _controlledProcesses.Count;
        public bool IsAlive { get; set; } 
        public int MaxProcsInPool { get; private set; }
        public delegate void FreeSlot(JobController jobController);
        public event FreeSlot OnFreeSlot;
        public JobController(int maxJobs, Limits limits)
        {
            JobControllerId = Guid.NewGuid().ToString();
            MaxProcsInPool = maxJobs;
            IsAlive = true;
            _controlledProcesses = new ConcurrentDictionary<Job, Thread>();
            _limitsHelper = new LimitsHelper(limits);
            _mutex = new Mutex();
        }

        public void AddProcess(Job newJob)
        {
            var thread = new Thread(newJob.StartJob);
            _controlledProcesses.TryAdd(newJob, thread);
            newJob.Terminate += TerminateJobHanlder;
            thread.Start();
        }

        private void WriteMessageToLog(string message)
        {
            _mutex.WaitOne();
            try
            {
                using (var fs = new FileStream("ControllerLogs.log", FileMode.Append))
                {
                    fs.Write(Encoding.UTF8.GetBytes(message));
                    fs.Close();
                }
            }
            catch {}
            finally
            {
                _mutex.ReleaseMutex();
            }
        }
        
        public void StartController()
        {
            while (IsAlive)
            {
                WriteMessageToLog(GetStatsByJobs(true,true));
                foreach (var pair in _controlledProcesses)
                {
                    if (_limitsHelper.CheckAll(pair.Key.ProcInfo))
                    {
                        pair.Key.CancelJob();
                        _controlledProcesses.TryRemove(new KeyValuePair<Job, Thread>(pair.Key, _controlledProcesses[pair.Key]));
                        OnFreeSlot(this);
                        Console.WriteLine($"Задача с ID процесса {pair.Key.ProcInfo.ProcessID} завершена по причине превышения лимитов используемых ресурсов");
                        WriteMessageToLog($"Задача с ID процесса {pair.Key.ProcInfo.ProcessID} завершена по причине превышения лимитов используемых ресурсов");
                    }
                }
            }
        }
        public string GetStatsByJobs(bool ignoreEmptyControllers, bool ignoreEmptySlots=false)
        {
            if (!_controlledProcesses.IsEmpty || !ignoreEmptyControllers)
            {
                JobInfo procInfo;
                var sb = new string(' ', 5);
                var stringBuilder = new StringBuilder();
                var makeheader = false;
                stringBuilder.AppendLine($"ControllerID: {JobControllerId}");
                foreach (var pair in _controlledProcesses)
                {
                    procInfo = pair.Key.ProcInfo;
                    stringBuilder.AppendLine(
                        $"\tJobID: {pair.Key.JobId}{sb}ID Процесса:{procInfo.ProcessID}{sb}Время исполнения: {procInfo.AbsoluteTime / 1000} с.{sb}Процессорное время: {procInfo.ProcessorTime} мс.{sb}ОЗУ: {procInfo.Memory}");
                }
                if (!ignoreEmptySlots)
                {
                    for (int i = 0; i < MaxProcsInPool - ProcsInPool; i++)
                    {
                        stringBuilder.AppendLine("\tПустой слот для процесса");
                    } 
                }
                return stringBuilder.ToString();
            }
            return String.Empty;
        }
        private void TerminateJobHanlder(Job job)
        {
            _controlledProcesses.TryRemove(new KeyValuePair<Job, Thread>(job, _controlledProcesses[job]));
            OnFreeSlot(this);
            Console.WriteLine($"Задача с ID Процесса {job.ProcInfo.ProcessID} завершена без ошибок");
            WriteMessageToLog($"Задача с ID Процесса {job.ProcInfo.ProcessID} завершена без ошибок");
        }
        #region GetHashCode && Equals

        public override int GetHashCode() => JobControllerId.GetHashCode();

        public override bool Equals(object obj)
        {
            JobController other = obj as JobController;
            if (other == null) return false;
            return other.JobControllerId == JobControllerId;
        }

        #endregion
    }
}

