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
        /// <summary>
        /// Основной поток контроллера задач
        /// </summary>
        public void StartController()
        {
            while (IsAlive)
            {
                WriteMessageToLog(GetStatsByJobs(true,true));
                foreach (var pair in _controlledProcesses)
                {
                    if (pair.Key.ProcInfo!=null && _limitsHelper.CheckAll(pair.Key.ProcInfo))
                    {
                        pair.Key.CancelJob();
                        pair.Value.Join();
                        OnFreeSlot(this);
                        Console.WriteLine($"Задача с ID процесса {pair.Key.ProcInfo.ProcessID} завершена по причине превышения лимитов используемых ресурсов");
                        WriteMessageToLog($"Задача с ID процесса {pair.Key.ProcInfo.ProcessID} завершена по причине превышения лимитов используемых ресурсов");
                    }
                }
            }
        }

        /// <summary>
        /// Управление задачами (рестарт/остановка)
        /// </summary>
        public void ManageJobs()
        {
            while (true)
            {
                foreach (var job in _controlledProcesses.Keys)
                {
                    Console.WriteLine($"JobId: {job.JobId}");
                }
                Console.WriteLine("Введите строку в таком формате: ПорядковыйНомерЗадачи:kill\\restart");
                try
                {
                    var input = Console.ReadLine().Split(':');
                    var job = _controlledProcesses.Keys.ElementAt(Convert.ToInt32(input[0]));
                    switch (input[1].ToLower())
                    {
                        case "kill":
                            job.CancelJob(false);
                            _controlledProcesses[job].Join();
                            OnFreeSlot(this);
                            break;
                        case "restart":
                            var prog = job.ProgPath;
                            job.CancelJob(false);
                            _controlledProcesses.TryRemove(
                                new KeyValuePair<Job, Thread>(job, _controlledProcesses[job]));
                            var newJob = new Job(prog);
                            _controlledProcesses.TryAdd(newJob, new Thread(newJob.StartJob));
                            _controlledProcesses[newJob].Start();
                            break;
                        default:
                            throw new Exception();
                    }
                }catch { Console.WriteLine("Некорректный ввод!");}
            }
        }
        /// <summary>
        /// статистика задач текущего контроллера в виде строки
        /// </summary>
        /// <param name="ignoreEmptyControllers">true если в случае пустого контроллера строка формировалась пустой</param>
        /// <param name="ignoreEmptySlots">true если нужно включить строку "Пустой слот для процесса" если контроллер заполнен частично</param>
        /// <returns>Строка содержащая информацию о каждой из задач</returns>
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
                    if(procInfo is null) continue;
                    stringBuilder.AppendLine(
                        $"\tJobID: {pair.Key.JobId}{sb}ID Процесса:{procInfo.ProcessID}{sb}Дескриптор {procInfo.Handle.ToString()}{sb}Время исполнения:{procInfo.AbsoluteTime / 1000} с.{sb}Процессорное время:{procInfo.ProcessorTime} мс.{sb}ОЗУ:{procInfo.Memory}{sb}Виртуальная память:{procInfo.VirtualMemory}");
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

        /// <summary>
        /// обработчик события завершения задачи (успешного/неуспешного)
        /// </summary>
        /// <param name="job"></param>
        private void TerminateJobHanlder(Job job, bool succsesful)
        {
            _controlledProcesses.TryRemove(new KeyValuePair<Job, Thread>(job, _controlledProcesses[job]));
            OnFreeSlot(this);
            Console.WriteLine($"Задача с ID Процесса {job.ProcInfo.ProcessID} завершена {(succsesful ? "без ошибок":"с ошибками")}");
            WriteMessageToLog($"Задача с ID Процесса {job.ProcInfo.ProcessID} завершена {(succsesful ? "без ошибок":"с ошибками")}");
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

