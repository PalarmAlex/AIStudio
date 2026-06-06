using ISIDA.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace AIStudio.Common.Adapters
{
  /// <summary>
  /// Чтение и запись <c>AdapterId</c> в <c>AgentProperties.dat</c> (свойства симбионта).
  /// </summary>
  public static class AgentPropertiesAdapterBinding
  {
    /// <summary>Ключ строки в AgentProperties.dat.</summary>
    public const string AdapterIdKey = "AdapterId";

    /// <summary>
    /// Путь к <c>AgentProperties.dat</c> в корне проекта.
    /// </summary>
    public static string GetAgentPropertiesPath(string projectRoot)
    {
      if (string.IsNullOrWhiteSpace(projectRoot))
        throw new ArgumentException("projectRoot");

      string gomeostasPath = SettingsValidator.GetExpectedFolderPathForSetting(
          projectRoot,
          "DataGomeostasFolderPath");
      return Path.Combine(gomeostasPath, "AgentProperties.dat");
    }

    /// <summary>
    /// Читает AdapterId из файла свойств симбионта.
    /// </summary>
    public static bool TryReadAdapterId(string agentPropertiesPath, out string adapterId)
    {
      adapterId = string.Empty;
      if (string.IsNullOrWhiteSpace(agentPropertiesPath) || !File.Exists(agentPropertiesPath))
        return false;

      try
      {
        foreach (string line in File.ReadLines(agentPropertiesPath))
        {
          if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            continue;

          int sep = line.IndexOf('|');
          if (sep <= 0)
            continue;

          if (!string.Equals(line.Substring(0, sep).Trim(), AdapterIdKey, StringComparison.Ordinal))
            continue;

          adapterId = line.Substring(sep + 1).Trim();
          return true;
        }
      }
      catch
      {
        // ignore
      }

      return false;
    }

    /// <summary>
    /// Записывает или удаляет AdapterId в AgentProperties.dat (создаёт минимальный файл при отсутствии).
    /// </summary>
    public static void WriteAdapterId(string agentPropertiesPath, string adapterId)
    {
      if (string.IsNullOrWhiteSpace(agentPropertiesPath))
        throw new ArgumentException("agentPropertiesPath");

      string trimmed = string.IsNullOrWhiteSpace(adapterId) ? null : adapterId.Trim();
      string dir = Path.GetDirectoryName(agentPropertiesPath);
      if (!string.IsNullOrEmpty(dir))
        Directory.CreateDirectory(dir);

      List<string> lines;
      if (File.Exists(agentPropertiesPath))
        lines = File.ReadAllLines(agentPropertiesPath, Encoding.UTF8).ToList();
      else
        lines = CreateMinimalAgentPropertiesLines(null);

      bool replaced = false;
      for (int i = lines.Count - 1; i >= 0; i--)
      {
        string line = lines[i];
        if (line.StartsWith("#") || !line.Contains("|"))
          continue;

        if (string.Equals(line.Split('|')[0].Trim(), AdapterIdKey, StringComparison.Ordinal))
        {
          if (trimmed == null)
            lines.RemoveAt(i);
          else
            lines[i] = AdapterIdKey + "|" + trimmed;
          replaced = true;
          break;
        }
      }

      if (trimmed != null && !replaced)
        lines.Add(AdapterIdKey + "|" + trimmed);

      File.WriteAllLines(agentPropertiesPath, lines, Encoding.UTF8);
    }

    /// <summary>
    /// Создаёт минимальный AgentProperties.dat с опциональным AdapterId (для нового проекта).
    /// </summary>
    public static void EnsureMinimalAgentProperties(string agentPropertiesPath, string adapterId)
    {
      if (File.Exists(agentPropertiesPath))
      {
        if (!string.IsNullOrWhiteSpace(adapterId))
          WriteAdapterId(agentPropertiesPath, adapterId);
        return;
      }

      string dir = Path.GetDirectoryName(agentPropertiesPath);
      if (!string.IsNullOrEmpty(dir))
        Directory.CreateDirectory(dir);

      File.WriteAllLines(
          agentPropertiesPath,
          CreateMinimalAgentPropertiesLines(adapterId),
          Encoding.UTF8);
    }

    /// <summary>
    /// Переносит AdapterId из legacy Settings.xml в AgentProperties.dat и удаляет элемент из Settings.
    /// </summary>
    public static bool TryMigrateFromSettingsXml(string projectRoot, out string migratedAdapterId)
    {
      migratedAdapterId = null;
      if (string.IsNullOrWhiteSpace(projectRoot))
        return false;

      string settingsFile = Path.Combine(projectRoot, "Settings", AppConfig.StudioSettingsFileName);
      if (!File.Exists(settingsFile))
      {
        settingsFile = Path.Combine(projectRoot, "Settings", AppConfig.LegacyStudioSettingsFileName);
        if (!File.Exists(settingsFile))
          return false;
      }

      try
      {
        XDocument doc = XDocument.Load(settingsFile);
        XElement appSettings = doc.Root?.Element("AppSettings");
        XElement legacy = appSettings?.Element(SymbiontProjectAdapterSettings.LegacySettingsXmlElementName);
        if (legacy == null)
          return false;

        string id = (legacy.Value ?? string.Empty).Trim();
        legacy.Remove();
        doc.Save(settingsFile);

        if (string.IsNullOrEmpty(id))
          return false;

        string agentPropertiesPath = GetAgentPropertiesPath(projectRoot);
        WriteAdapterId(agentPropertiesPath, id);
        migratedAdapterId = id;
        return true;
      }
      catch
      {
        return false;
      }
    }

    private static List<string> CreateMinimalAgentPropertiesLines(string adapterId)
    {
      var lines = new List<string>
      {
        "# Формат: Ключ|Значение",
        "Name|Симбионт",
        "Description|",
        "IsSleeping|False",
        "IsDead|False",
        "Lifetime|0",
        "EvolutionStage|0",
        "PainValue|0",
        "JoyValue|0"
      };

      if (!string.IsNullOrWhiteSpace(adapterId))
        lines.Add(AdapterIdKey + "|" + adapterId.Trim());

      return lines;
    }
  }
}
