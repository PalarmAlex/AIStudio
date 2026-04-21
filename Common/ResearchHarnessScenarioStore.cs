using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AIStudio.Common
{
  /// <summary>Сохранённые pipe-сценарии по одному файлу на имя в каталоге метода.</summary>
  public static class ResearchHarnessScenarioStore
  {
    private const string FileExtension = ".txt";

    /// <summary>Каталог сценариев для конкретного harness_id (подкаталог от корня прогонов).</summary>
    public static string GetScenarioDirectory(string harnessId)
    {
      if (string.IsNullOrWhiteSpace(harnessId))
        throw new ArgumentException("harnessId пустой.", nameof(harnessId));
      var safe = harnessId.Trim().Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');
      return Path.Combine(ResearchHarnessPaths.RootFolder, "Scenarios", safe);
    }

    /// <summary>Имена сценариев (без расширения), отсортированные.</summary>
    public static List<string> ListScenarioNames(string harnessId)
    {
      var dir = GetScenarioDirectory(harnessId);
      if (!Directory.Exists(dir))
        return new List<string>();
      return Directory.GetFiles(dir, "*" + FileExtension, SearchOption.TopDirectoryOnly)
          .Select(Path.GetFileNameWithoutExtension)
          .Where(n => !string.IsNullOrWhiteSpace(n))
          .Distinct(StringComparer.OrdinalIgnoreCase)
          .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
          .ToList();
    }

    /// <summary>Полный путь к файлу сценария.</summary>
    public static string GetScenarioFilePath(string harnessId, string scenarioNameWithoutExtension)
    {
      var baseName = NormalizeScenarioName(scenarioNameWithoutExtension);
      ValidateScenarioName(baseName);
      return Path.Combine(GetScenarioDirectory(harnessId), baseName + FileExtension);
    }

    public static void Save(string harnessId, string scenarioNameWithoutExtension, string content)
    {
      var path = GetScenarioFilePath(harnessId, scenarioNameWithoutExtension);
      Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "");
      File.WriteAllText(path, content ?? "", Encoding.UTF8);
    }

    public static string Load(string harnessId, string scenarioNameWithoutExtension)
    {
      var path = GetScenarioFilePath(harnessId, scenarioNameWithoutExtension);
      if (!File.Exists(path))
        throw new FileNotFoundException("Сценарий не найден: " + path);
      return File.ReadAllText(path, Encoding.UTF8);
    }

    public static bool Exists(string harnessId, string scenarioNameWithoutExtension)
    {
      try
      {
        return File.Exists(GetScenarioFilePath(harnessId, scenarioNameWithoutExtension));
      }
      catch (ArgumentException)
      {
        return false;
      }
    }

    /// <summary>Проверка имени файла сценария (без пути и без расширения).</summary>
    public static void ValidateScenarioName(string normalizedNameWithoutExtension)
    {
      if (string.IsNullOrWhiteSpace(normalizedNameWithoutExtension))
        throw new ArgumentException("Укажите имя сценария (буквы, цифры, без символов пути).", nameof(normalizedNameWithoutExtension));
      var t = normalizedNameWithoutExtension.Trim();
      if (t.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        throw new ArgumentException("Имя содержит символы, недопустимые в имени файла Windows.");
      if (t.Contains("..") || t == "." || t.EndsWith(".", StringComparison.Ordinal))
        throw new ArgumentException("Недопустимое имя сценария.", nameof(normalizedNameWithoutExtension));
    }

    /// <summary>Имя без расширения .txt, если пользователь его ввёл.</summary>
    public static string NormalizeScenarioName(string name)
    {
      if (string.IsNullOrWhiteSpace(name))
        return "";
      var t = name.Trim();
      if (t.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase))
        t = t.Substring(0, t.Length - FileExtension.Length).Trim();
      return t;
    }
  }
}
