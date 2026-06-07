using ISIDA.Common;
using ISIDA.Gomeostas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AIStudio.Common.SymbiontEnv
{
  /// <summary>
  /// Строка правила давления среды из <c>EnvironmentPressureRules.dat</c>.
  /// </summary>
  public sealed class EnvironmentPressureRuleRow
  {
    public int RuleId { get; set; }
    public string ProbeKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<int, int> Influences { get; set; } = new Dictionary<int, int>();
  }

  /// <summary>
  /// Загрузка и сохранение правил давления среды (<see cref="AppConfig.EnvironmentPressureRulesFilePath"/>).
  /// </summary>
  public static class EnvironmentPressureRulesStorage
  {
    /// <summary>
    /// Создаёт файл с минимальной шапкой, если его ещё нет или он невалиден.
    /// </summary>
    public static void EnsureFileExists()
    {
      string path = AppConfig.EnvironmentPressureRulesFilePath;
      if (string.IsNullOrWhiteSpace(path))
        return;
      string dir = Path.GetDirectoryName(path);
      if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        Directory.CreateDirectory(dir);
      if (File.Exists(path) && FileValidator.IsValidEnvironmentPressureRulesFile(path))
        return;
      WriteSeedFile(path, headerOnly: true);
    }

    /// <summary>
    /// Загружает правила давления с диска.
    /// </summary>
    public static List<EnvironmentPressureRuleRow> Load()
    {
      EnsureFileExists();
      string path = AppConfig.EnvironmentPressureRulesFilePath;
      var rows = new List<EnvironmentPressureRuleRow>();
      if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        return rows;

      foreach (string line in File.ReadLines(path))
      {
        string trimmed = line?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#", StringComparison.Ordinal))
          continue;

        string[] parts = trimmed.Split('|');
        if (parts.Length != 5 || !int.TryParse(parts[0], out int ruleId))
          throw new InvalidDataException(
              $"Неверный формат строки в EnvironmentPressureRules.dat (ожидается 5 полей): {trimmed}");

        rows.Add(new EnvironmentPressureRuleRow
        {
          RuleId = ruleId,
          ProbeKey = parts[1].Trim(),
          Name = parts[2].Trim(),
          Description = parts[3].Trim(),
          Influences = ParseInfluences(parts[4])
        });
      }

      return rows.OrderBy(r => r.RuleId).ToList();
    }

    /// <summary>
    /// Сохраняет правила давления на диск.
    /// </summary>
    public static void Save(IReadOnlyList<EnvironmentPressureRuleRow> rules)
    {
      if (rules == null)
        throw new ArgumentNullException(nameof(rules));

      EnsureFileExists();
      string path = AppConfig.EnvironmentPressureRulesFilePath;
      if (string.IsNullOrWhiteSpace(path))
        throw new InvalidOperationException("Не задан путь к файлу правил давления среды.");

      var lines = new List<string>
      {
        FileValidator.FileHeaders.EnvironmentPressureRulesFormat,
        FileValidator.FileHeaders.EnvironmentPressureRulesProbeKey,
        FileValidator.FileHeaders.EnvironmentPressureRulesInfluences
      };

      foreach (EnvironmentPressureRuleRow rule in rules.OrderBy(r => r.RuleId))
      {
        if (rule == null)
          continue;
        lines.Add(string.Join("|",
            rule.RuleId,
            rule.ProbeKey ?? string.Empty,
            rule.Name ?? string.Empty,
            rule.Description ?? string.Empty,
            InfluencesToString(rule.Influences)));
      }

      var result = FileValidator.SafeSaveFile(
          path,
          lines,
          tempPath => FileValidator.IsValidEnvironmentPressureRulesFile(tempPath),
          minLinesCount: 3,
          fileDescription: "правила давления среды");
      if (!result.Success)
        throw new IOException(result.ErrorMessage ?? "Не удалось сохранить правила давления среды.");
    }

    /// <summary>Минимальное содержимое для нового проекта (только шапка).</summary>
    public static string GetMinimalSeedContent()
    {
      var sb = new StringBuilder();
      sb.AppendLine(FileValidator.FileHeaders.EnvironmentPressureRulesFormat);
      sb.AppendLine(FileValidator.FileHeaders.EnvironmentPressureRulesProbeKey);
      sb.AppendLine(FileValidator.FileHeaders.EnvironmentPressureRulesInfluences);
      return sb.ToString();
    }

    private static void WriteSeedFile(string path, bool headerOnly)
    {
      string dir = Path.GetDirectoryName(path);
      if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        Directory.CreateDirectory(dir);
      File.WriteAllText(path, GetMinimalSeedContent(), Encoding.UTF8);
    }

    private static Dictionary<int, int> ParseInfluences(string influenceStr)
    {
      var influences = new Dictionary<int, int>();
      if (string.IsNullOrWhiteSpace(influenceStr))
        return influences;

      foreach (string pair in influenceStr.Split(';'))
      {
        string[] kv = pair.Split(':');
        if (kv.Length == 2 &&
            int.TryParse(kv[0], out int paramId) &&
            int.TryParse(kv[1], out int effect))
        {
          influences[paramId] = GomeostasSystem.ClampInt(effect, -10, 10);
        }
      }

      return influences;
    }

    private static string InfluencesToString(Dictionary<int, int> influences)
    {
      if (influences == null || influences.Count == 0)
        return string.Empty;
      return string.Join(";", influences.Select(kv => kv.Key + ":" + kv.Value));
    }
  }
}
