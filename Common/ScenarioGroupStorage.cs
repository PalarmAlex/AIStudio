using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ISIDA.Common;

namespace AIStudio.Common
{
  /// <summary>Сохранение реестра групп и файлов состава группы.</summary>
  public static class ScenarioGroupStorage
  {
    private const string GroupRegistryFormatHeader = "# SCENARIO_GROUP_REGISTRY_FORMAT|";
    private const string GroupLinesFormatHeader = "# SCENARIO_GROUP_LINES_FORMAT|";

    public static void EnsureFolder() => ScenarioStorage.EnsureFolder();

    public static List<ScenarioGroupHeader> LoadGroupRegistry()
    {
      EnsureFolder();
      var path = ScenarioPaths.GroupRegistryPath;
      if (!File.Exists(path))
        return new List<ScenarioGroupHeader>();

      var list = new List<ScenarioGroupHeader>();
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
        list.Add(new ScenarioGroupHeader
        {
          Id = id,
          Title = ScenarioStorage.Unescape(p[1]),
          Description = ScenarioStorage.Unescape(p[2]),
          DateText = ScenarioStorage.Unescape(p[3])
        });
      }

      return list.OrderBy(h => h.Id).ToList();
    }

    public static (bool Success, string Error) SaveGroupRegistry(IEnumerable<ScenarioGroupHeader> headers)
    {
      EnsureFolder();
      var lines = new List<string>
      {
        "# Реестр групп сценариев оператора",
        $"{GroupRegistryFormatHeader}{ScenarioGroupDocument.GroupLinesFileFormatVersion}",
        "# Id|Title|Description|Date"
      };
      foreach (var h in headers.OrderBy(x => x.Id))
      {
        lines.Add(string.Join("|",
            h.Id.ToString(CultureInfo.InvariantCulture),
            ScenarioStorage.Escape(h.Title ?? ""),
            ScenarioStorage.Escape(h.Description ?? ""),
            ScenarioStorage.Escape(h.DateText ?? "")));
      }

      return FileValidator.SafeSaveFile(
          ScenarioPaths.GroupRegistryPath,
          lines,
          path => ValidateGroupRegistryFile(path),
          minLinesCount: 3,
          fileDescription: "реестр групп сценариев");
    }

    private static bool ValidateGroupRegistryFile(string path)
    {
      try
      {
        var all = File.ReadAllLines(path, Encoding.UTF8);
        return all.Any(l => l.TrimStart().StartsWith(GroupRegistryFormatHeader, StringComparison.Ordinal));
      }
      catch
      {
        return false;
      }
    }

    public static ScenarioGroupDocument LoadGroup(int groupId)
    {
      var path = ScenarioPaths.GroupLinesPath(groupId);
      if (!File.Exists(path))
        throw new FileNotFoundException("Файл группы сценариев не найден", path);

      return ParseGroupContent(File.ReadAllLines(path, Encoding.UTF8), groupId);
    }

    public static ScenarioGroupDocument ParseGroupFromLines(string[] lines, int provisionalId)
    {
      return ParseGroupContent(lines ?? Array.Empty<string>(), provisionalId);
    }

    private static ScenarioGroupDocument ParseGroupContent(string[] lines, int groupId)
    {
      var doc = new ScenarioGroupDocument { Id = groupId };
      foreach (var line in lines)
      {
        var t = line?.Trim();
        if (string.IsNullOrEmpty(t) || !t.StartsWith("#"))
          continue;

        if (t.StartsWith("# SCENARIO_GROUP_META|", StringComparison.Ordinal))
        {
          var meta = t.Substring("# SCENARIO_GROUP_META|".Length).Split('|');
          if (meta.Length >= 1)
            doc.Title = ScenarioStorage.Unescape(meta[0]);
          if (meta.Length >= 2)
            doc.Description = ScenarioStorage.Unescape(meta[1]);
          if (meta.Length >= 3)
            doc.DateText = ScenarioStorage.Unescape(meta[2]);
          if (meta.Length >= 4
              && int.TryParse(meta[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int coeff))
          {
            if (coeff == 1 || coeff == 5 || coeff == 10 || coeff == 20 || coeff == 50 || coeff == 100)
              doc.RunPulseTimingCoefficient = coeff;
          }
          if (meta.Length >= 5
              && int.TryParse(meta[4].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int rf)
              && Enum.IsDefined(typeof(ScenarioGroupReportFormat), rf))
          {
            doc.ReportFormat = (ScenarioGroupReportFormat)rf;
          }
          continue;
        }

        if (t.StartsWith("# SCENARIO_GROUP_MEMBER|", StringComparison.Ordinal))
        {
          var p = t.Substring("# SCENARIO_GROUP_MEMBER|".Length).Split('|');
          if (p.Length < 7)
            continue;
          if (!int.TryParse(p[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int sort))
            continue;
          if (!int.TryParse(p[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int sid))
            continue;
          int.TryParse(p[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int pst);
          var row = new ScenarioGroupMemberRow
          {
            SortOrderInGroup = sort,
            ScenarioId = sid,
            PreRunTargetStage = pst,
            PreRunClearAgentData = Parse01(p[3]),
            PreRunNormalHomeostasisState = Parse01(p[4]),
            ScenarioObservationMode = Parse01(p[5]),
            ScenarioAuthoritativeRecording = Parse01(p[6])
          };
          doc.Members.Add(row);
        }
      }

      return doc;
    }

    private static bool Parse01(string s)
    {
      return int.TryParse(s?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) && v != 0;
    }

    public static (bool Success, string Error) SaveGroup(ScenarioGroupDocument doc)
    {
      if (doc == null)
        return (false, "Нет данных группы.");
      EnsureFolder();
      int coeff = doc.RunPulseTimingCoefficient;
      if (coeff != 1 && coeff != 5 && coeff != 10 && coeff != 20 && coeff != 50 && coeff != 100)
        coeff = 1;

      int rf = (int)doc.ReportFormat;
      if (!Enum.IsDefined(typeof(ScenarioGroupReportFormat), rf))
        rf = (int)ScenarioGroupReportFormat.Detailed;

      var lines = new List<string>
      {
        "# Группа сценариев оператора",
        $"{GroupLinesFormatHeader}{ScenarioGroupDocument.GroupLinesFileFormatVersion}",
        $"# SCENARIO_GROUP_META|{ScenarioStorage.Escape(doc.Title ?? "")}|{ScenarioStorage.Escape(doc.Description ?? "")}|{ScenarioStorage.Escape(doc.DateText ?? "")}|{coeff.ToString(CultureInfo.InvariantCulture)}|{rf.ToString(CultureInfo.InvariantCulture)}",
        "# SCENARIO_GROUP_MEMBER|SortOrder|ScenarioId|PreRunStage|Clear|Norm|Obs|Auth"
      };

      foreach (var m in doc.Members.OrderBy(x => x.SortOrderInGroup).ThenBy(x => x.ScenarioId))
      {
        lines.Add(
            "# SCENARIO_GROUP_MEMBER|"
            + m.SortOrderInGroup.ToString(CultureInfo.InvariantCulture) + "|"
            + m.ScenarioId.ToString(CultureInfo.InvariantCulture) + "|"
            + m.PreRunTargetStage.ToString(CultureInfo.InvariantCulture) + "|"
            + (m.PreRunClearAgentData ? "1" : "0") + "|"
            + (m.PreRunNormalHomeostasisState ? "1" : "0") + "|"
            + (m.ScenarioObservationMode ? "1" : "0") + "|"
            + (m.ScenarioAuthoritativeRecording ? "1" : "0"));
      }

      var path = ScenarioPaths.GroupLinesPath(doc.Id);
      return FileValidator.SafeSaveFile(
          path,
          lines,
          p => ValidateGroupLinesFile(p),
          minLinesCount: 4,
          fileDescription: "файл группы сценариев");
    }

    private static bool ValidateGroupLinesFile(string path)
    {
      try
      {
        var all = File.ReadAllLines(path, Encoding.UTF8);
        return all.Any(l => l.TrimStart().StartsWith(GroupLinesFormatHeader, StringComparison.Ordinal));
      }
      catch
      {
        return false;
      }
    }

    public static int NextGroupId()
    {
      var list = LoadGroupRegistry();
      if (list.Count == 0)
        return 1;
      return list.Max(h => h.Id) + 1;
    }

    public static void DeleteGroupFiles(int groupId)
    {
      var path = ScenarioPaths.GroupLinesPath(groupId);
      if (File.Exists(path))
        File.Delete(path);
    }
  }
}
