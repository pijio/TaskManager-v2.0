using System.Net;
using System.Net.Sockets;
using System.Xml.Serialization;

namespace TaskManager.Server
{
    public class Program
    {
        private static string ListenerPropsPath = "ListenerProps.xml";
        private static string LimitsPath = "Limits.xml";

        public static void Main()
        {
            Console.WriteLine("Провести миграцию для конфигурационных файлов? [Y/N]");
            if (Console.ReadLine()?.ToLower() == "y")
            {
                Helpers.PropsMigration(new ListenerProperties(), ListenerPropsPath);
                Helpers.PropsMigration(new Limits(), LimitsPath);
            }
            var props = Helpers.GetProperties<ListenerProperties>(ListenerPropsPath) ?? new ListenerProperties();
            var limits = Helpers.GetProperties<Limits>(LimitsPath) ?? new Limits();
            using (var listener = new TaskListener(props, limits))
            {
                listener.StartManager();
            }
        }    
    }
}
