using System.Xml.Serialization;

namespace TaskManager.Server;
/// <summary>
/// Различные хелперы (в данном случае тут только миграции и чтение конфигов)
/// </summary>
public static class Helpers
{
    public static void PropsMigration<T>(T props, String path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        var s = new XmlSerializer(typeof(T));
        using (var fs = new FileStream(path, FileMode.Create))
        {
            s.Serialize(fs,props);
        }
    }

    public static T? GetProperties<T>(String path)
    {
        var s = new XmlSerializer(typeof(T));
        using (var fs = new FileStream(path, FileMode.Open))
        {
            return (T)s.Deserialize(fs)!;
        }
    }
}