using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Xml.Linq;

public static class AppConfig
{
  private const string ConfigFileName = "AIStudio.Settings.xml";
  private static readonly string ConfigDirectory = Path.Combine(
      Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
      "Settings");

  private static readonly string ConfigFullPath = Path.Combine(ConfigDirectory, ConfigFileName);

  // Флаг первого запуска
  private static bool _isFirstRunInitialized = false;

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
  public static int DefaultStileId => GetIntSetting("DefaultStileId", (int)GetDefaultValueSettings("DefaultStileId"));
  public static int DefaultAdaptiveActionId => GetIntSetting("DefaultAdaptiveActionId", (int)GetDefaultValueSettings("DefaultAdaptiveActionId"));
  public static int DefaultGeneticReflexId => GetIntSetting("DefaultGeneticReflexId", (int)GetDefaultValueSettings("DefaultGeneticReflexId"));
  public static int RecognitionThreshold => GetIntSetting("RecognitionThreshold", (int)GetDefaultValueSettings("RecognitionThreshold"));
  public static int CompareLevel => GetIntSetting("CompareLevel", (int)GetDefaultValueSettings("CompareLevel"));
  public static float DifSensorPar => GetFloatSetting("DifSensorPar", (float)GetDefaultValueSettings("DifSensorPar"));
  public static int DynamicTime => GetIntSetting("DynamicTime", (int)GetDefaultValueSettings("DynamicTime"));
  public static float DefaultBaseThreshold => GetFloatSetting("DefaultBaseThreshold", (float)GetDefaultValueSettings("DefaultBaseThreshold"));
  public static float DefaultKCompetition => GetFloatSetting("DefaultKCompetition", (float)GetDefaultValueSettings("DefaultKCompetition"));

  /// <summary>
  /// Инициализирует конфигурацию и проверяет первый запуск
  /// </summary>
  private static void InitializeConfig()
  {
    try
    {
      bool isNewConfig = !File.Exists(ConfigFullPath);

      if (isNewConfig)
      {
        Directory.CreateDirectory(ConfigDirectory);
        CreateDefaultConfig();

        // Помечаем для последующей обработки первого запуска
        _isFirstRunInitialized = true;
      }
      else
      {
        // Проверяем, нужно ли обновить пути (для случаев обновления программы)
        CheckAndUpdatePaths();
      }
    }
    catch (Exception ex)
    {
      Debug.WriteLine($"Ошибка инициализации конфига: {ex.Message}");
    }
  }

  /// <summary>
  /// Создает конфиг с настройками по умолчанию
  /// </summary>
  private static void CreateDefaultConfig()
  {
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
          new XElement("DifSensorPar", 0.02),
          new XElement("DynamicTime", 50),
          new XElement("DefaultKCompetition", 0.3),
          new XElement("DefaultBaseThreshold", 0.2)
        )
      )
    );

    defaultConfig.Save(ConfigFullPath);
  }

  /// <summary>
  /// Проверяет и обновляет пути при необходимости
  /// </summary>
  private static void CheckAndUpdatePaths()
  {
    try
    {
      string currentInstallPath = GetApplicationInstallPath();

      // Если программа установлена не в Program Files, обновляем пути
      if (!IsProgramFilesPath(currentInstallPath))
      {
        UpdateConfigPaths(currentInstallPath);
      }
    }
    catch (Exception ex)
    {
      Debug.WriteLine($"Ошибка при проверке путей: {ex.Message}");
    }
  }

  /// <summary>
  /// Получает путь установки приложения
  /// </summary>
  public static string GetApplicationInstallPath()
  {
    return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
  }

  /// <summary>
  /// Проверяет, находится ли путь в Program Files
  /// </summary>
  private static bool IsProgramFilesPath(string path)
  {
    string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
    string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

    return path.StartsWith(programFiles, StringComparison.OrdinalIgnoreCase) ||
           path.StartsWith(programFilesX86, StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Обновляет пути в конфигурации на основе пути установки
  /// </summary>
  public static void UpdateConfigPaths(string installPath)
  {
    try
    {
      string drive = Path.GetPathRoot(installPath);

      SetSetting("DataGomeostasFolderPath", Path.Combine(installPath, "Data", "Gomeostas"));
      SetSetting("DataActionsFolderPath", Path.Combine(installPath, "Data", "Actions"));
      SetSetting("SensorsFolderPath", Path.Combine(installPath, "Data", "Sensors"));
      SetSetting("ReflexesFolderPath", Path.Combine(installPath, "Data", "Reflexes"));
      SetSetting("DataGomeostasTemplateFolderPath", Path.Combine(installPath, "Templates", "Gomeostas"));
      SetSetting("DataActionsTemplateFolderPath", Path.Combine(installPath, "Templates", "Actions"));
      SetSetting("SensorsTemplateFolderPath", Path.Combine(installPath, "Templates", "Sensors"));
      SetSetting("ReflexesTemplateFolderPath", Path.Combine(installPath, "Templates", "Reflexes"));
      SetSetting("SettingsPath", Path.Combine(installPath, "Settings"));

      Debug.WriteLine($"Конфигурационные пути обновлены для установки в: {installPath}");
    }
    catch (Exception ex)
    {
      Debug.WriteLine($"Ошибка обновления путей конфигурации: {ex.Message}");
    }
  }

  /// <summary>
  /// Вызывается при первом запуске приложения для окончательной настройки
  /// </summary>
  public static void InitializeFirstRun()
  {
    if (_isFirstRunInitialized)
    {
      string installPath = GetApplicationInstallPath();

      // Если установка не в Program Files, обновляем пути
      if (!IsProgramFilesPath(installPath))
      {
        UpdateConfigPaths(installPath);
      }

      _isFirstRunInitialized = false;
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

  private static float GetFloatSetting(string key, float defaultValue)
  {
    string value = GetSetting(key);
    return float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float result) ? result : defaultValue;
  }

  public static void SetFloatSetting(string key, float value)
  {
    try
    {
      var doc = XDocument.Load(ConfigFullPath);
      var element = doc.Root?
                      .Element("AppSettings")?
                      .Element(key);

      // Сохраняем с инвариантной культурой, чтобы разделитель был точкой
      string stringValue = value.ToString(System.Globalization.CultureInfo.InvariantCulture);

      if (element != null)
        element.Value = stringValue;
      else
        doc.Root?.Element("AppSettings")?.Add(new XElement(key, stringValue));

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

  /// <summary>
  /// Получает значение настройки по умолчанию по имени
  /// </summary>
  public static object GetDefaultValueSettings(string settingName)
  {
    switch (settingName)
    {
      case "DefaultStileId":
      case "DefaultAdaptiveActionId":
      case "DefaultGeneticReflexId":
        return 0;
      case "RecognitionThreshold":
        return 3;
      case "DynamicTime":
        return 50;
      case "CompareLevel":
        return 30;
      case "DifSensorPar":
        return 0.02f;
      case "DefaultBaseThreshold":
        return 0.2f;
      case "DefaultKCompetition":
        return 0.3f;
      default:
        return null;
    }
  }
}