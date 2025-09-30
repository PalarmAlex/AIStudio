using System;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics;
using System.Windows.Media;

public static class AppConfig
{
  private const string ConfigFileName = "AIStudio.Settings.xml";
  private static readonly string ConfigDirectory = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
      "ISIDA",
      "Settings");

  private static readonly string ConfigFullPath = Path.Combine(ConfigDirectory, ConfigFileName);

  static AppConfig()
  {
    InitializeConfig();
  }
  public static string DataGomeostasFolderPath => GetSetting("DataGomeostasFolderPath");
  public static string DataGomeostasTemplateFolderPath => GetSetting("DataGomeostasTemplateFolderPath");
  public static string DataActionsFolderPath => GetSetting("DataActionsFolderPath");
  public static string DataActionsTemplateFolderPath => GetSetting("DataActionsTemplateFolderPath");
  public static string SensorsFolderPath => GetSetting("SensorsFolderPath");
  public static string SensorsTemplateFolderPath => GetSetting("SensorsTemplateFolderPath");
  public static string ReflexesFolderPath => GetSetting("ReflexesFolderPath");
  public static string ReflexesTemplateFolderPath => GetSetting("ReflexesTemplateFolderPath");
  public static string SettingsPath => GetSetting("SettingsPath");
  public static int DefaultStileId => GetIntSetting("DefaultStileId", 0);
  public static int DefaultAdaptiveActionId => GetIntSetting("DefaultAdaptiveActionId", 0);
  public static int DefaultGeneticReflexId => GetIntSetting("DefaultGeneticReflexId", 0);
  public static int RecognitionThreshold => GetIntSetting("RecognitionThreshold", 3);
  public static int CompareLevel => GetIntSetting("CompareLevel", 30);
  public static int DifSensorPar => GetIntSetting("DifSensorPar", 2);
  public static int DynamicTime => GetIntSetting("DynamicTime", 50);

  /// <summary>
  /// Создает конфиг с настройками по умолчанию, если его нет
  /// </summary>
  private static void InitializeConfig()
  {
    try
    {
      if (!File.Exists(ConfigFullPath))
      {
        Directory.CreateDirectory(ConfigDirectory);

        var defaultConfig = new XDocument(
          new XElement("Configuration",
            new XElement("AppSettings",
              new XElement("DataGomeostasFolderPath", @"C:\ProgramData\ISIDA\Data\Gomeostas"),
              new XElement("DataActionsFolderPath", @"C:\ProgramData\ISIDA\Data\Actions"),
              new XElement("SensorsFolderPath", @"C:\ProgramData\ISIDA\Data\Sensors"),
              new XElement("ReflexesFolderPath", @"C:\ProgramData\ISIDA\Data\Reflexes"),
              new XElement("DataGomeostasTemplateFolderPath", @"C:\ProgramData\ISIDA\Templates\Gomeostas"),
              new XElement("DataActionsTemplateFolderPath", @"C:\ProgramData\ISIDA\Templates\Actions"),
              new XElement("SensorsTemplateFolderPath", @"C:\ProgramData\ISIDA\Templates\Sensors"),
              new XElement("ReflexesTemplateFolderPath", @"C:\ProgramData\ISIDA\Templates\Reflexes"),
              new XElement("SettingsPath", @"C:\ProgramData\ISIDA\Settings"),
              new XElement("DefaultStileId", 0),
              new XElement("DefaultAdaptiveActionId", 0),
              new XElement("DefaultGeneticReflexId", 0),
              new XElement("RecognitionThreshold", 3),
              new XElement("CompareLevel", 30),
              new XElement("DifSensorPar", 2),
              new XElement("DynamicTime", 10)
            )
          )
        );

        defaultConfig.Save(ConfigFullPath);
      }
    }
    catch (Exception ex)
    {
      Debug.WriteLine($"Ошибка инициализации конфига: {ex.Message}");
    }
  }

  /// <summary>
  /// Получает значение настройки
  /// </summary>
  public static string GetSetting(string key)
  {
    try
    {
      var doc = XDocument.Load(ConfigFullPath);
      return doc.Root?
                .Element("AppSettings")?
                .Element(key)?
                .Value;
    }
    catch (Exception ex)
    {
      Debug.WriteLine($"Ошибка чтения настройки {key}: {ex.Message}");
      return null;
    }
  }

  /// <summary>
  /// Устанавливает значение настройки
  /// </summary>
  public static void SetSetting(string key, string value)
  {
    try
    {
      var doc = XDocument.Load(ConfigFullPath);
      var element = doc.Root?
                      .Element("AppSettings")?
                      .Element(key);

      if (element != null)
        element.Value = value;
      else
        doc.Root?.Element("AppSettings")?.Add(new XElement(key, value));

      doc.Save(ConfigFullPath);
    }
    catch (Exception ex)
    {
      Debug.WriteLine($"Ошибка сохранения настройки {key}: {ex.Message}");
    }
  }

  private static int GetIntSetting(string key, int defaultValue)
  {
    string value = GetSetting(key);
    return int.TryParse(value, out int result) ? result : defaultValue;
  }

  public static void SetIntSetting(string key, int value)
  {
    try
    {
      var doc = XDocument.Load(ConfigFullPath);
      var element = doc.Root?
                      .Element("AppSettings")?
                      .Element(key);

      if (element != null)
        element.Value = value.ToString();
      else
        doc.Root?.Element("AppSettings")?.Add(new XElement(key, value.ToString()));

      doc.Save(ConfigFullPath);
    }
    catch (Exception ex)
    {
      Debug.WriteLine($"Ошибка сохранения настройки {key}: {ex.Message}");
    }
  }

  public static string GetBaseStateDisplay(int baseID)
  {
    switch (baseID)
    {
      case -1: return "ПЛОХО";
      case 0: return "НОРМА";
      case 1: return "ХОРОШО";
      default: return "Неизвестно";
    }
  }

  public static Brush GetBaseStateColor(int baseID)
  {
    switch (baseID)
    {
      case -1: return Brushes.Red;
      case 0: return new SolidColorBrush(Color.FromRgb(204, 204, 0)); // темно-желтый
      case 1: return Brushes.Green;
      default: return Brushes.Gray;
    }
  }
}