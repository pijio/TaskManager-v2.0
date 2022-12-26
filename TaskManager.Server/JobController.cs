using System.Collections.Concurrent;
using System.Threading.Channels;

namespace TaskManager.Server
{
    public class JobController
    {
        public string JobControllerId { get; set; }
        private ConcurrentDictionary<Job, Thread> _controlledProcesses;
        public int ProcsInPool => _controlledProcesses.Count;
        public bool IsAlive { get; set; } 
        public int MaxProcsInPool { get; private set; }
        private LimitsHelper _limitsHelper;
        public JobController(int maxJobs, Limits limits)
        {
            JobControllerId = Guid.NewGuid().ToString();
            MaxProcsInPool = maxJobs;
            IsAlive = true;
            _controlledProcesses = new ConcurrentDictionary<Job, Thread>();
            _limitsHelper = new LimitsHelper(limits);
        }

        public void AddProcess(Job newJob)
        {
            var thread = new Thread(newJob.StartJob);
            _controlledProcesses.TryAdd(newJob, thread);
            newJob.Terminate += TerminateJobHanlder;
            thread.Start();
        }
        
        public void StartController()
        {
            while (IsAlive)
            {
                foreach (var pair in _controlledProcesses)
                {
                    List<LimitType> exBy;
                    if (_limitsHelper.CheckAll(pair.Key.ProcInfo, out exBy))
                    {
                        pair.Key.CancelJob();
                    }
                }
            }
        }

        private void TerminateJobHanlder(Job job)
        {
            _controlledProcesses.TryRemove(new KeyValuePair<Job, Thread>(job, _controlledProcesses[job]));
            Console.WriteLine($"Process with id {job.ProcInfo.ProcessID} terminated");
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

