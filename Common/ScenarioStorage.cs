using ISIDA.Common;
using ISIDA.Scenarios;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace AIStudio.Common
{
  /// <summary>Загрузка и сохранение реестра и строк сценариев (разделитель |).</summary>
  public static class ScenarioStorage
  {
    private const string RegistryFormatHeader = "# SCENARIO_REGISTRY_FORMAT|";
    private const string LinesFormatHeader = "# SCENARIO_LINES_FORMAT|";

    public static void EnsureFolder()
    {
      if (!Directory.Exists(ScenarioPaths.RootFolder))
        Directory.CreateDirectory(ScenarioPaths.RootFolder);
    }

    public static List<ScenarioHeader> LoadRegistry()
    {
      EnsureFolder();
      var path = ScenarioPaths.RegistryPath;
      if (!File.Exists(path))
        return new List<ScenarioHeader>();

      var list = new List<ScenarioHeader>();
      foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
      {
        var t = line?.Trim();
        if (string.IsNullOrEmpty(t) || t.StartsWith("#"))
          continue;
        var p = t.Split('|');
        if (p.Length < 4)
          continue;
        if (!int.TryParse(p[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
          continue;
        list.Add(new ScenarioHeader
        {
          Id = id,
          Title = Unescape(p[1]),
          Description = Unescape(p[2]),
          DateText = Unescape(p[3])
        });
      }

      return list.OrderBy(h => h.Id).ToList();
    }

    public static ScenarioDocument LoadScenario(int scenarioId)
    {
      var path = ScenarioPaths.LinesPath(scenarioId);
      if (!File.Exists(path))
        throw new FileNotFoundException("Файл строк сценария не найден", path);

      var doc = new ScenarioDocument();
      doc.Header.Id = scenarioId;

      foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
      {
        var t = line?.Trim();
        if (string.IsNullOrEmpty(t) || t.StartsWith("#"))
        {
          if (t != null && t.StartsWith("# SCENARIO_META|", StringComparison.Ordinal))
          {
            var meta = t.Substring("# SCENARIO_META|".Length).Split('|');
            if (meta.Length >= 4)
            {
              doc.Header.Title = Unescape(meta[0]);
              doc.Header.Description = Unescape(meta[1]);
              doc.Header.DateText = Unescape(meta[2]);
            }
            if (meta.Length >= 6)
            {
              // Старый формат: состояние + ключ стилей — начальные значения параметров не заданы
              doc.Header.InitialHomeostasisValues = "";
            }
            else if (meta.Length >= 5)
            {
              doc.Header.InitialHomeostasisValues = Unescape(meta[4]);
            }
          }
          continue;
        }

        var row = ParseLineRow(t);
        if (row != null)
          doc.Lines.Add(row);
      }

      return doc;
    }

    public static bool TryParseScenarioLine(string line, out ScenarioLineRow row)
    {
      row = ParseLineRow(line);
      return row != null;
    }

    private static ScenarioLineRow ParseLineRow(string line)
    {
      var p = line.Split('|');
      if (p.Length >= 8)
      {
        if (!int.TryParse(p[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int step))
          return null;
        if (!int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int pulse))
          return null;
        var kind = p[2].Trim().Equals("W", StringComparison.OrdinalIgnoreCase)
            ? ScenarioLineKind.WaitClick
            : ScenarioLineKind.Pult;
        if (!int.TryParse(p[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int tone))
          return null;
        if (!int.TryParse(p[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int mood))
          return null;
        var actions = string.IsNullOrWhiteSpace(p[5])
            ? new List<int>()
            : p[5].Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)
                .Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToList();
        var phrase = Unescape(p[6]);
        if (!int.TryParse(p[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out int rw))
          return null;

        return new ScenarioLineRow
        {
          StepIndex = step,
          PulseWithinScenario = pulse,
          Kind = kind,
          ToneId = tone,
          MoodId = mood,
          ActionIds = actions,
          Phrase = phrase,
          ResetWaitingPeriod = rw != 0
        };
      }

      if (p.Length < 7)
        return null;
      if (!int.TryParse(p[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int pulseLegacy))
        return null;
      var kindL = p[1].Trim().Equals("W", StringComparison.OrdinalIgnoreCase)
          ? ScenarioLineKind.WaitClick
          : ScenarioLineKind.Pult;
      if (!int.TryParse(p[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int toneL))
        return null;
      if (!int.TryParse(p[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int moodL))
        return null;
      var actionsL = string.IsNullOrWhiteSpace(p[4])
          ? new List<int>()
          : p[4].Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)
              .Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToList();
      var phraseL = Unescape(p[5]);
      if (!int.TryParse(p[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out int rwL))
        return null;

      return new ScenarioLineRow
      {
        StepIndex = 0,
        PulseWithinScenario = pulseLegacy,
        Kind = kindL,
        ToneId = toneL,
        MoodId = moodL,
        ActionIds = actionsL,
        Phrase = phraseL,
        ResetWaitingPeriod = rwL != 0
      };
    }

    public static (bool Success, string Error) SaveRegistry(IEnumerable<ScenarioHeader> headers)
    {
      EnsureFolder();
      var lines = new List<string>
      {
        "# Реестр сценариев оператора",
        $"{RegistryFormatHeader}{ScenarioDocument.FormatVersion}",
        "# Id|Title|Description|Date"
      };
      foreach (var h in headers.OrderBy(x => x.Id))
      {
        lines.Add(string.Join("|",
            h.Id.ToString(CultureInfo.InvariantCulture),
            Escape(h.Title ?? ""),
            Escape(h.Description ?? ""),
            Escape(h.DateText ?? "")));
      }

      return FileValidator.SafeSaveFile(
          ScenarioPaths.RegistryPath,
          lines,
          path => ValidateRegistryFile(path),
          minLinesCount: 3,
          fileDescription: "реестр сценариев");
    }

    private static bool ValidateRegistryFile(string path)
    {
      try
      {
        var all = File.ReadAllLines(path, Encoding.UTF8);
        return all.Any(l => l.TrimStart().StartsWith(RegistryFormatHeader, StringComparison.Ordinal));
      }
      catch
      {
        return false;
      }
    }

    public static (bool Success, string Error) SaveScenarioLines(ScenarioDocument doc)
    {
      EnsureFolder();
      var lines = new List<string>
      {
        "# Строки сценария оператора",
        $"{LinesFormatHeader}{ScenarioDocument.LinesFileFormatVersion}",
        $"# SCENARIO_META|{Escape(doc.Header.Title ?? "")}|{Escape(doc.Header.Description ?? "")}|{Escape(doc.Header.DateText ?? "")}|{Escape(doc.Header.InitialHomeostasisValues ?? "")}",
        "# Step|Pulse|Kind(P|W)|ToneId|MoodId|ActionIds|Phrase|ResetWait",
        "# Kind=W — только клик по плашке ожидания; P — воздействия с пульта. Пульс — расчётный (задержка между шагами = период ожидания ответа оператора, пульсов)."
      };

      foreach (var row in doc.Lines.OrderBy(r => r.StepIndex))
      {
        var kind = row.Kind == ScenarioLineKind.WaitClick ? "W" : "P";
        var ids = row.ActionIds == null || row.ActionIds.Count == 0
            ? ""
            : string.Join(",", row.ActionIds.Select(i => i.ToString(CultureInfo.InvariantCulture)));
        lines.Add(string.Join("|",
            row.StepIndex.ToString(CultureInfo.InvariantCulture),
            row.PulseWithinScenario.ToString(CultureInfo.InvariantCulture),
            kind,
            row.ToneId.ToString(CultureInfo.InvariantCulture),
            row.MoodId.ToString(CultureInfo.InvariantCulture),
            ids,
            Escape(row.Phrase ?? ""),
            row.ResetWaitingPeriod ? "1" : "0"));
      }

      var path = ScenarioPaths.LinesPath(doc.Header.Id);
      return FileValidator.SafeSaveFile(
          path,
          lines,
          p => ValidateLinesFile(p),
          minLinesCount: 3,
          fileDescription: "строки сценария");
    }

    private static bool ValidateLinesFile(string path)
    {
      try
      {
        var all = File.ReadAllLines(path, Encoding.UTF8);
        return all.Any(l => l.TrimStart().StartsWith(LinesFormatHeader, StringComparison.Ordinal));
      }
      catch
      {
        return false;
      }
    }

    public static int NextScenarioId()
    {
      var list = LoadRegistry();
      if (list.Count == 0)
        return 1;
      return list.Max(h => h.Id) + 1;
    }

    public static string Escape(string s)
    {
      if (string.IsNullOrEmpty(s))
        return "";
      return s.Replace("\\", "\\\\").Replace("|", "\\|");
    }

    public static string Unescape(string s)
    {
      if (string.IsNullOrEmpty(s))
        return "";
      var sb = new StringBuilder(s.Length);
      for (int i = 0; i < s.Length; i++)
      {
        if (s[i] == '\\' && i + 1 < s.Length)
        {
          i++;
          sb.Append(s[i]);
        }
        else
          sb.Append(s[i]);
      }
      return sb.ToString();
    }

    public static void DeleteScenarioFiles(int scenarioId)
    {
      var path = ScenarioPaths.LinesPath(scenarioId);
      if (File.Exists(path))
        File.Delete(path);
    }
  }
}
