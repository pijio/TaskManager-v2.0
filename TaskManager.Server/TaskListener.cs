using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TaskManager.DTO;

namespace TaskManager.Server;

public class TaskListener : IDisposable
{
    private Socket _serverSocket;
    private bool _disposed = false;
    private ConcurrentDictionary<JobController, Thread> _jobControllers;
    private ConcurrentQueue<Job> _jobsQueue;
    private ListenerProperties _props;
    private Limits _limits;
    public TaskListener(ListenerProperties p, Limits limits)
    {
        var endpoint = new IPEndPoint(IPAddress.Any, p.Port);
        _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _serverSocket.Bind(endpoint);
        _serverSocket.Listen();
        _jobControllers = new ConcurrentDictionary<JobController, Thread>();
        _jobsQueue = new ConcurrentQueue<Job>();
        _props = p;
        _limits = limits;
    }

    private string GetProgPath(Socket client)
    {
        var buffer = new byte[4];
        client.Receive(buffer);
        var messageSize =  BitConverter.ToInt32(buffer);
        var data = new byte[messageSize];
        client.Receive(data);
        return Encoding.UTF8.GetString(data, 0, messageSize);
    }

    private ClientMessageTypes GetMessageType(Socket client)
    {
        byte[] data = new byte[1];
        client.Receive(data);
        return (ClientMessageTypes)data[0];
    }
    /// <summary>
    /// Добавление задачи в контроллеры
    /// </summary>
    /// <param name="progPath"></param>
    private void AddProcToController(string progPath)
    {
        var maxJobPerContr = _props.MaxJobs / _props.MaxControllers;
        try
        {
            var job = new Job(progPath);
            var freeController =
                _jobControllers.FirstOrDefault(c =>
                        c.Key.ProcsInPool == _jobControllers.Min(e => e.Key.ProcsInPool) &&
                        c.Key.ProcsInPool != maxJobPerContr)
                    .Key; // ищем минимально заполненный контроллер
            if (freeController is null) // если все контроллеры заняты добавляем задачу в очередь
            {
                _jobsQueue.Enqueue(job);
                return;
            }

            freeController.AddProcess(job);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
    /// <summary>
    /// Метод, который прослушивает все контроллеры задач
    /// </summary>
    private void ControllersListener()
    {
        while (true)
        {
            foreach (var controller in _jobControllers)
            {
                var info = controller.Key.GetStatsByJobs(false);
                if (info != String.Empty)
                {
                    Console.WriteLine(info);
                }
            }
            Thread.Sleep(2000);
            Console.Clear();
            // вообще по хорошему надо было разделить приложение на слушателя сокета и слушателя контроллеров
            // и сделать так чтобы они писали в разные окна, тогда была бы возможность поглядывать в окно
            // сокета и туда тоже что то выводить
        }
    }
    /// <summary>
    /// Обработчик события освобождения свободного места в контроллере для последующего извлечения из очереди задач
    /// </summary>
    /// <param name="controller"></param>
    private void FreeSlotHanlder(JobController controller)
    {
        Job job;
        if (_jobsQueue.TryDequeue(out job))
        {
            controller.AddProcess(job);
        }
    }
    /// <summary>
    /// Метод обслуживающий сокет
    /// </summary>
    public void StartManager()
    {
        var port = _props.Port;
        Console.WriteLine($"Запуск прослушивателя задач на порту {port}");
        Console.WriteLine("Инициализирую контроллеры процессов...");
        for (int i = 0; i < _props.MaxControllers; i++)
        {
            var controller = new JobController(_props.MaxJobs/_props.MaxControllers, _limits);
            controller.OnFreeSlot += FreeSlotHanlder;
            var thread = new Thread(controller.StartController);
            _jobControllers.TryAdd(controller, thread);
            thread.Start();
        }
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Прослушиватель запущен успешно. Ожидаем подключения");
        Console.ResetColor();
        if (!File.Exists("ControllerLogs.log"))
            File.Create("ControllerLogs.log");
        try
        {
            var mainControllerThread = new Thread(ControllersListener);
            mainControllerThread.Start();
            while (true)
            {
                var client = _serverSocket.Accept();
                switch (GetMessageType(client))
                {
                    case ClientMessageTypes.JobContext:
                        string prog = GetProgPath(client);
                        AddProcToController(prog);
                        break; 
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    #region Халяль реализация IDisposible

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            _serverSocket.Dispose();
        }
        _disposed = true;
    }

    ~TaskListener()
    {
        Dispose(false);
    }

    #endregion
}