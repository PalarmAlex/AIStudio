using ISIDA.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;

public static class AppConfig
{
  private const string ConfigFileName = "AIStudio.Settings.xml";
  private static string ConfigDirectory = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
      "ISIDA", "Settings"
  );

  private static string ConfigFullPath = Path.Combine(ConfigDirectory, ConfigFileName);

  static AppConfig()
  {
    InitializeConfig();
  }

  public static string DataGomeostasFolderPath => GetSetting("DataGomeostasFolderPath");
  public static string DataActionsFolderPath => GetSetting("DataActionsFolderPath");
  public static string SensorsFolderPath => GetSetting("SensorsFolderPath");
  public static string ReflexesFolderPath => GetSetting("ReflexesFolderPath");
  public static string PsychicDataFolderPath => GetSetting("PsychicDataFolderPath");
  public static string SettingsPath => GetSetting("SettingsPath");
  public static string LogsFolderPath => GetSetting("LogsFolderPath");
  public static ResearchLogger.LogFormat LogFormat => GetLogFormatSetting("DefaultFormatLog", ResearchLogger.LogFormat.All);
  public static int FirstRun => GetIntSetting("FirstRun", (int)GetDefaultValueSettings("FirstRun"));
  public static bool LogEnabled => GetBoolSetting("LogEnabled", (bool)GetDefaultValueSettings("LogEnabled"));
  public static int DefaultStileId => GetIntSetting("DefaultStileId", (int)GetDefaultValueSettings("DefaultStileId"));
  public static int DefaultAdaptiveActionId => GetIntSetting("DefaultAdaptiveActionId", (int)GetDefaultValueSettings("DefaultAdaptiveActionId"));
  public static int RecognitionThreshold => GetIntSetting("RecognitionThreshold", (int)GetDefaultValueSettings("RecognitionThreshold"));
  public static int CompareLevel => GetIntSetting("CompareLevel", (int)GetDefaultValueSettings("CompareLevel"));
  public static float DifSensorPar => GetFloatSetting("DifSensorPar", (float)GetDefaultValueSettings("DifSensorPar"));
  public static int DynamicTime => GetIntSetting("DynamicTime", (int)GetDefaultValueSettings("DynamicTime"));
  public static int ReflexActionDisplayDuration => GetIntSetting("ReflexActionDisplayDuration", (int)GetDefaultValueSettings("ReflexActionDisplayDuration"));
  public static int WaitingPeriodForActionsVal => GetIntSetting("WaitingPeriodForActionsVal", (int)GetDefaultValueSettings("WaitingPeriodForActionsVal"));

  /// <summary>
  /// Инициализирует конфигурацию и проверяет первый запуск
  /// </summary>
  public static void InitializeConfig()
  {
    try
    {
      // Если конфиг не существует -создаем
      if (!File.Exists(ConfigFullPath))
        CreateDefaultConfig();

      // Проверяем первый запуск
      int firstRunValue = GetIntSetting("FirstRun", 0);
      if (firstRunValue == 0)
      {
        UpdateConfigPaths();
        SetIntSetting("FirstRun", 1);
      }
    }
    catch (Exception ex)
    {
      Logger.Error(ex.Message);
    }
  }

  /// <summary>
  /// Создает конфиг с настройками по умолчанию
  /// </summary>
  private static void CreateDefaultConfig()
  {
    string programDataPath = Environment.GetFolderPath(
        Environment.SpecialFolder.CommonApplicationData);

    string appDataPath = Path.Combine(programDataPath, "ISIDA");

    var defaultConfig = new XDocument(
      new XElement("Configuration",
        new XElement("AppSettings",
          new XElement("DataGomeostasFolderPath", Path.Combine(appDataPath, "Data", "Gomeostas")),
          new XElement("DataActionsFolderPath", Path.Combine(appDataPath, "Data", "Actions")),
          new XElement("SensorsFolderPath", Path.Combine(appDataPath, "Data", "Sensors")),
          new XElement("ReflexesFolderPath", Path.Combine(appDataPath, "Data", "Reflexes")),
          new XElement("PsychicDataFolderPath", Path.Combine(appDataPath, "Data", "Psychic")),
          new XElement("SettingsPath", Path.Combine(appDataPath, "Settings")),
          new XElement("LogsFolderPath", Path.Combine(appDataPath, "Logs")),
          new XElement("DefaultStileId", 0),
          new XElement("DefaultAdaptiveActionId", 0),
          new XElement("RecognitionThreshold", 3),
          new XElement("CompareLevel", 30),
          new XElement("DifSensorPar", 0.02),
          new XElement("DynamicTime", 50),
          new XElement("ReflexActionDisplayDuration", 3),
          new XElement("WaitingPeriodForActionsVal", 30),
          new XElement("FirstRun", 1),
          new XElement("LogEnabled", false),
          new XElement("LogFormat", "All")
        )
      )
    );

    defaultConfig.Save(ConfigFullPath);
  }

  /// <summary>
  /// Обновляет пути в конфигурации на основе пути установки
  /// </summary>
  public static void UpdateConfigPaths()
  {
    string programDataPath = Environment.GetFolderPath(
    Environment.SpecialFolder.CommonApplicationData);

    string appDataPath = Path.Combine(programDataPath, "ISIDA");
    try
    {
      SetSetting("DataGomeostasFolderPath", Path.Combine(appDataPath, "Data", "Gomeostas"));
      SetSetting("DataActionsFolderPath", Path.Combine(appDataPath, "Data", "Actions"));
      SetSetting("SensorsFolderPath", Path.Combine(appDataPath, "Data", "Sensors"));
      SetSetting("ReflexesFolderPath", Path.Combine(appDataPath, "Data", "Reflexes"));
      SetSetting("PsychicDataFolderPath", Path.Combine(appDataPath, "Data", "Psychic"));
      SetSetting("SettingsPath", Path.Combine(appDataPath, "Settings"));
      SetSetting("LogsFolderPath", Path.Combine(appDataPath, "Logs"));

      Logger.Info($"Конфигурационные пути обновлены для установки в: {appDataPath}");
    }
    catch (Exception ex)
    {
      Logger.Error(ex.Message);
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
      Logger.Error(ex.Message);
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
      Logger.Error(ex.Message);
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
      Logger.Error(ex.Message);
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
      Logger.Error(ex.Message);
    }
  }

  private static bool GetBoolSetting(string key, bool defaultValue)
  {
    string value = GetSetting(key);
    if (bool.TryParse(value, out bool result))
      return result;

    return defaultValue;
  }

  public static void SetBoolSetting(string key, bool value)
  {
    try
    {
      var doc = XDocument.Load(ConfigFullPath);
      var element = doc.Root?
                      .Element("AppSettings")?
                      .Element(key);

      if (element != null)
        element.Value = value.ToString().ToLowerInvariant();
      else
        doc.Root?.Element("AppSettings")?.Add(new XElement(key, value.ToString().ToLowerInvariant()));

      doc.Save(ConfigFullPath);
    }
    catch (Exception ex)
    {
      Logger.Error(ex.Message);
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

  private static ResearchLogger.LogFormat GetLogFormatSetting(string key, ResearchLogger.LogFormat defaultValue)
  {
    string value = GetSetting(key);

    // Сначала пробуем распарсить как строку enum
    if (Enum.TryParse<ResearchLogger.LogFormat>(value, true, out var result))
      return result;

    // Если не получается, пробуем как число (для обратной совместимости)
    if (int.TryParse(value, out int intValue) && Enum.IsDefined(typeof(ResearchLogger.LogFormat), intValue))
      return (ResearchLogger.LogFormat)intValue;

    return defaultValue;
  }

  public static void SetLogFormatSetting(string key, ResearchLogger.LogFormat value)
  {
    SetSetting(key, value.ToString());
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
      case "ReflexActionDisplayDuration":
        return 3;
      case "WaitingPeriodForActionsVal":
        return 30;
      case "CompareLevel":
        return 30;
      case "DifSensorPar":
        return 0.02f;
      case "FirstRun":
        return 0;
      case "LogEnabled":
        return false;
      case "LogFormat":
        return ResearchLogger.LogFormat.All; ;
      default:
        return null;
    }
  }
}