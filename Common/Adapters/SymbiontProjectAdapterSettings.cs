using System;
using System.IO;
using System.Xml.Linq;
using ISIDA.Common;

namespace AIStudio.Common.Adapters
{
  /// <summary>
  /// Элемент <c>AdapterId</c> в <c>{project}\Settings\Settings.xml</c> (MVP, фаза 4).
  /// </summary>
  public static class SymbiontProjectAdapterSettings
  {
    /// <summary>Имя узла в AppSettings.</summary>
    public const string AdapterIdElementName = "AdapterId";

    /// <summary>
    /// Читает AdapterId из файла настроек проекта.
    /// </summary>
    public static bool TryReadFromSettingsFile(string settingsFilePath, out string adapterId)
    {
      adapterId = string.Empty;
      if (string.IsNullOrWhiteSpace(settingsFilePath) || !File.Exists(settingsFilePath))
        return false;

      try
      {
        XElement appSettings = XDocument.Load(settingsFilePath).Root?.Element("AppSettings");
        if (appSettings == null)
          return false;

        adapterId = (appSettings.Element(AdapterIdElementName)?.Value ?? string.Empty).Trim();
        return adapterId.Length > 0;
      }
      catch
      {
        return false;
      }
    }

    /// <summary>
    /// Читает AdapterId из корня проекта.
    /// </summary>
    public static bool TryReadFromProjectRoot(string projectRoot, out string adapterId)
    {
      adapterId = string.Empty;
      if (string.IsNullOrWhiteSpace(projectRoot))
        return false;

      string path = Path.Combine(projectRoot, "Settings", AppConfig.StudioSettingsFileName);
      if (!File.Exists(path))
      {
        path = Path.Combine(projectRoot, "Settings", AppConfig.LegacyStudioSettingsFileName);
        if (!File.Exists(path))
          return false;
      }

      return TryReadFromSettingsFile(path, out adapterId);
    }

    /// <summary>
    /// AdapterId активного проекта из хаба студии (<see cref="AppConfig"/>).
    /// </summary>
    public static bool TryGetCurrentAdapterId(out string adapterId)
    {
      adapterId = (AppConfig.GetSetting(AdapterIdElementName) ?? string.Empty).Trim();
      return adapterId.Length > 0;
    }

    /// <summary>
    /// Записывает или обновляет AdapterId в Settings.xml проекта (только при создании / ручной правке вне UI).
    /// </summary>
    public static void WriteAdapterIdToProjectSettings(string projectRoot, string adapterId)
    {
      if (string.IsNullOrWhiteSpace(projectRoot))
        throw new ArgumentException("projectRoot");
      if (string.IsNullOrWhiteSpace(adapterId))
        throw new ArgumentException("adapterId");

      string settingsDir = Path.Combine(Path.GetFullPath(projectRoot), "Settings");
      Directory.CreateDirectory(settingsDir);

      string settingsFile = Path.Combine(settingsDir, AppConfig.StudioSettingsFileName);
      XDocument doc;
      if (File.Exists(settingsFile))
        doc = XDocument.Load(settingsFile);
      else
      {
        doc = new XDocument(new XElement("Configuration", new XElement("AppSettings")));
        doc.Save(settingsFile);
        doc = XDocument.Load(settingsFile);
      }

      XElement appSettings = doc.Root?.Element("AppSettings");
      if (appSettings == null)
      {
        appSettings = new XElement("AppSettings");
        doc.Root?.Add(appSettings);
      }

      XElement el = appSettings.Element(AdapterIdElementName);
      if (el != null)
        el.Value = adapterId.Trim();
      else
        appSettings.Add(new XElement(AdapterIdElementName, adapterId.Trim()));

      doc.Save(settingsFile);
    }
  }
}
