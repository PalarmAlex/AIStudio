using System;
using System.IO;
using System.Xml.Serialization;

namespace AIStudio.Common
{
  public static class XmlFile
  {
    public static void SaveToXml<T>(T obj, string filePath)
    {
      try
      {
        var directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
          Directory.CreateDirectory(directory);
        }

        var serializer = new XmlSerializer(typeof(T));
        using (var writer = new StreamWriter(filePath))
        {
          serializer.Serialize(writer, obj);
        }
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException($"Ошибка сохранения XML файла: {filePath}", ex);
      }
    }

    public static T LoadFromXml<T>(string filePath) where T : new()
    {
      try
      {
        if (!File.Exists(filePath))
        {
          return new T();
        }

        var serializer = new XmlSerializer(typeof(T));
        using (var reader = new StreamReader(filePath))
        {
          return (T)serializer.Deserialize(reader);
        }
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException($"Ошибка загрузки XML файла: {filePath}", ex);
      }
    }
  }
}