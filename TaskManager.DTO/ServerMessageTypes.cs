using System.ComponentModel;

namespace TaskManager.DTO
{
    public enum ServerMessageTypes : byte
    {
        // сообщение о установленных лимитах для каждой задачи
        LimitsInfo=1,        
        // информация о запущенной задачи (включая затрачиваемые ресурсы)
        JobInfo                 
    }
}

