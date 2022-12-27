using System.ComponentModel;

namespace TaskManager.DTO
{
    //TODO callback в клиент (когда нибудь...)
    public enum ServerMessageTypes : byte
    {
        // сообщение о установленных лимитах для каждой задачи
        LimitsInfo=1,        
        // информация о запущенной задачи (включая затрачиваемые ресурсы)
        JobInfo                 
    }
}

