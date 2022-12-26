using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using TaskManager.DTO;

namespace TaskManager.Client
{
    class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(@"Запуск задачи: TaskManager.Client.exe [path?\program.exe]");
                Console.ReadKey();
                return;
            }            
            var progpath = args[0];
            if (!Regex.IsMatch(progpath, @"\w*.exe$")) return; // проверка на корректность введеной строки
            var endpoint =
                new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                endpoint.Connect("127.0.0.1", 1337);
                var type = new byte[] { (byte)ClientMessageTypes.JobContext };
                var message = Encoding.UTF8.GetBytes(args[0]);
                var size = BitConverter.GetBytes(message.Length);
                endpoint.Send(type);
                endpoint.Send(size);
                endpoint.Send(message);
                Console.WriteLine("Запрос на создание задачи отправлен");
            }
            catch 
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Что-то пошло не так...\nПопробуйте попытку позже");
                Console.ResetColor();
            }
            finally
            {
                endpoint.Dispose();
            }
        }
    }
}