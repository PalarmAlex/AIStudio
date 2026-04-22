using ISIDA.Common;
using ISIDA.Reflexes;
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
        int pst = -1;
        if (int.TryParse(p[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedPst))
          pst = parsedPst;
        list.Add(new ScenarioHeader
        {
          Id = id,
          Title = Unescape(p[1]),
          Description = Unescape(p[2]),
          PreRunTargetStage = pst
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

      bool expectationMode = false;
      foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
      {
        var t = line?.Trim();
        if (string.IsNullOrEmpty(t))
          continue;

        if (t.StartsWith(LinesFormatHeader, StringComparison.Ordinal))
          continue;

        if (expectationMode)
        {
          if (t.StartsWith("# COL_SKIP|", StringComparison.Ordinal))
          {
            ParseLogExpectationColSkip(t, doc);
            continue;
          }
          if (t.StartsWith("#"))
            continue;
          TryParseLogExpectationRow(t, doc);
          continue;
        }

        if (t.StartsWith("# SCENARIO_LOG_EXPECTATIONS", StringComparison.Ordinal))
        {
          expectationMode = true;
          doc.LogExpectationColumnSkips = doc.LogExpectationColumnSkips ?? new ScenarioLogExpectationColumnSkips();
          doc.LogExpectations = doc.LogExpectations ?? new List<ScenarioLogExpectationRow>();
          continue;
        }

        if (t.StartsWith("#"))
        {
          if (t.StartsWith("# SCENARIO_META|", StringComparison.Ordinal))
          {
            var meta = t.Substring("# SCENARIO_META|".Length).Split('|');
            ApplyScenarioMeta(doc.Header, meta);
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

    /// <summary>Разбор <c># SCENARIO_META|…</c> текущего формата: Title|Description|InitialHomeostasis|…</summary>
    public static void ApplyScenarioMeta(ScenarioHeader header, string[] meta)
    {
      if (header == null || meta == null)
        return;
      if (meta.Length >= 2)
      {
        header.Title = Unescape(meta[0]);
        header.Description = Unescape(meta[1]);
      }
      if (meta.Length < 3)
        return;
      const int homeoValuesIndex = 2;
      header.InitialHomeostasisValues = Unescape(meta[homeoValuesIndex]);
      if (meta.Length < homeoValuesIndex + 8)
        return;
      int i = homeoValuesIndex;
      int.TryParse(meta[i + 1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int pst);
      header.PreRunTargetStage = pst;
      header.PreRunClearAgentData =
          int.TryParse(meta[i + 2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int cl)
          && cl != 0;
      header.ScenarioObservationMode =
          int.TryParse(meta[i + 3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int om)
          && om != 0;
      header.ScenarioAuthoritativeRecording =
          int.TryParse(meta[i + 4].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int ar)
          && ar != 0;
      header.PreRunNormalHomeostasisState =
          int.TryParse(meta[i + 5].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int nh)
          && nh != 0;
      if (int.TryParse(meta[i + 6].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int psi)
          && (psi == 1 || psi == 2 || psi == 3 || psi == 4))
        header.PulseStepIncrement = psi;
      TryApplyRunPulseCoefficient(meta[i + 7], header);
    }

    private static void TryApplyRunPulseCoefficient(string raw, ScenarioHeader header)
    {
      if (!int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int ptc))
        return;
      if (ptc == 1 || ptc == 5 || ptc == 10 || ptc == 20 || ptc == 50 || ptc == 100)
        header.RunPulseTimingCoefficient = ptc;
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

        int visualColor = AgentVisualColor.White;
        if (p.Length > 8 && int.TryParse(p[8].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int vc)
            && AgentVisualColor.IsValidCode(vc))
          visualColor = vc;

        return new ScenarioLineRow
        {
          StepIndex = step,
          PulseWithinScenario = pulse,
          Kind = kind,
          ToneId = tone,
          MoodId = mood,
          VisualColorId = visualColor,
          ActionIds = actions,
          Phrase = phrase,
          ResetWaitingPeriod = rw != 0
        };
      }

      return null;
    }

    private static void ParseLogExpectationColSkip(string line, ScenarioDocument doc)
    {
      var sk = doc.LogExpectationColumnSkips ?? new ScenarioLogExpectationColumnSkips();
      doc.LogExpectationColumnSkips = sk;
      var raw = line.Substring("# COL_SKIP|".Length);
      var p = raw.Split('|');
      if (p.Length < 11)
        return;
      bool Skip(int i) =>
          int.TryParse(p[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) && v != 0;

      sk.SkipState = Skip(0);
      sk.SkipStyle = Skip(1);
      sk.SkipTheme = Skip(2);
      sk.SkipTrigger = Skip(3);
      sk.SkipOrUm = Skip(4);
      sk.SkipGeneticReflex = Skip(5);
      sk.SkipConditionReflex = Skip(6);
      sk.SkipAutomatizm = Skip(7);
      sk.SkipReflexChain = Skip(8);
      sk.SkipAutomatizmChain = Skip(9);
      sk.SkipMainCycle = Skip(10);
      if (p.Length >= 12)
        sk.SkipDanger = Skip(11);
      if (p.Length >= 13)
        sk.SkipVeryActual = Skip(12);
      if (p.Length >= 14)
        sk.SkipAutomatizmUsefulness = Skip(13);
    }

    /// <summary>Делит строку ожиданий по «|», не экранированным обратным слэшем (поля могут содержать \| после записи Escape).</summary>
    private static List<string> SplitLogExpectationLine(string line)
    {
      var parts = new List<string>();
      var cur = new StringBuilder(line.Length);
      for (int i = 0; i < line.Length; i++)
      {
        char c = line[i];
        if (c != '|')
        {
          cur.Append(c);
          continue;
        }
        int backslashes = 0;
        for (int j = i - 1; j >= 0 && line[j] == '\\'; j--)
          backslashes++;
        if ((backslashes % 2) == 1)
        {
          cur.Append('|');
          continue;
        }
        parts.Add(cur.ToString());
        cur.Clear();
      }
      parts.Add(cur.ToString());
      return parts;
    }

    private static void TryParseLogExpectationRow(string line, ScenarioDocument doc)
    {
      if (doc.LogExpectations == null)
        doc.LogExpectations = new List<ScenarioLogExpectationRow>();

      var p = SplitLogExpectationLine(line);
      if (p.Count < 15)
        return;
      if (!int.TryParse(p[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int step))
        return;
      if (!int.TryParse(p[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int pulse))
        return;

      doc.LogExpectations.Add(new ScenarioLogExpectationRow
      {
        StepIndex = step,
        PulseWithinScenario = pulse,
        StateText = Unescape(p[2]),
        StyleText = Unescape(p[3]),
        ThemeText = Unescape(p[4]),
        TriggerText = Unescape(p[5]),
        OrUmText = Unescape(p[6]),
        DangerText = Unescape(p[7]),
        VeryActualText = Unescape(p[8]),
        GeneticReflexText = Unescape(p[9]),
        ConditionReflexText = Unescape(p[10]),
        AutomatizmText = Unescape(p[11]),
        ReflexChainText = Unescape(p[12]),
        AutomatizmChainText = Unescape(p[13]),
        MainCycleText = Unescape(p[14]),
        AutomatizmUsefulnessText = p.Count >= 16 ? Unescape(p[15]) : "-"
      });
    }

    public static (bool Success, string Error) SaveRegistry(IEnumerable<ScenarioHeader> headers)
    {
      EnsureFolder();
      var lines = new List<string>
      {
        "# Реестр сценариев оператора",
        $"{RegistryFormatHeader}{ScenarioDocument.FormatVersion}",
        "# Id|Title|Description|PreRunTargetStage"
      };
      foreach (var h in headers.OrderBy(x => x.Id))
      {
        lines.Add(string.Join("|",
            h.Id.ToString(CultureInfo.InvariantCulture),
            Escape(h.Title ?? ""),
            Escape(h.Description ?? ""),
            h.PreRunTargetStage.ToString(CultureInfo.InvariantCulture)));
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
        $"# SCENARIO_META|{Escape(doc.Header.Title ?? "")}|{Escape(doc.Header.Description ?? "")}|{Escape(doc.Header.InitialHomeostasisValues ?? "")}|{doc.Header.PreRunTargetStage.ToString(CultureInfo.InvariantCulture)}|{(doc.Header.PreRunClearAgentData ? "1" : "0")}|{(doc.Header.ScenarioObservationMode ? "1" : "0")}|{(doc.Header.ScenarioAuthoritativeRecording ? "1" : "0")}|{(doc.Header.PreRunNormalHomeostasisState ? "1" : "0")}|{doc.Header.PulseStepIncrement.ToString(CultureInfo.InvariantCulture)}|{doc.Header.RunPulseTimingCoefficient.ToString(CultureInfo.InvariantCulture)}",
        "# Step|Pulse|Kind(P|W)|ToneId|MoodId|ActionIds|Phrase|ResetWait|VisualColorId",
        "# Kind=W — только клик по плашке ожидания; P — воздействия с пульта. Пульс — по шагам и режиму приращения из метаданных (см. настройки проекта). VisualColorId — код зрительного канала (0…8), см. AgentVisualColor."
      };

      foreach (var row in doc.Lines.OrderBy(r => r.StepIndex))
      {
        var kind = row.Kind == ScenarioLineKind.WaitClick ? "W" : "P";
        var ids = row.ActionIds == null || row.ActionIds.Count == 0
            ? ""
            : string.Join(",", row.ActionIds.Select(i => i.ToString(CultureInfo.InvariantCulture)));
        int visualSave = AgentVisualColor.IsValidCode(row.VisualColorId) ? row.VisualColorId : AgentVisualColor.White;
        lines.Add(string.Join("|",
            row.StepIndex.ToString(CultureInfo.InvariantCulture),
            row.PulseWithinScenario.ToString(CultureInfo.InvariantCulture),
            kind,
            row.ToneId.ToString(CultureInfo.InvariantCulture),
            row.MoodId.ToString(CultureInfo.InvariantCulture),
            ids,
            Escape(row.Phrase ?? ""),
            row.ResetWaitingPeriod ? "1" : "0",
            visualSave.ToString(CultureInfo.InvariantCulture)));
      }

      var skc = doc.LogExpectationColumnSkips ?? new ScenarioLogExpectationColumnSkips();
      lines.Add("# SCENARIO_LOG_EXPECTATIONS|1");
      lines.Add("# COL_SKIP|" + string.Join("|",
          skc.SkipState ? "1" : "0",
          skc.SkipStyle ? "1" : "0",
          skc.SkipTheme ? "1" : "0",
          skc.SkipTrigger ? "1" : "0",
          skc.SkipOrUm ? "1" : "0",
          skc.SkipGeneticReflex ? "1" : "0",
          skc.SkipConditionReflex ? "1" : "0",
          skc.SkipAutomatizm ? "1" : "0",
          skc.SkipReflexChain ? "1" : "0",
          skc.SkipAutomatizmChain ? "1" : "0",
          skc.SkipMainCycle ? "1" : "0",
          skc.SkipDanger ? "1" : "0",
          skc.SkipVeryActual ? "1" : "0",
          skc.SkipAutomatizmUsefulness ? "1" : "0"));
      lines.Add("# Step|Pulse|State|Style|Theme|Trigger|OrUm|Opasno|Actualno|GenRef|CondRef|Aut|RefChain|AutChain|Cycle|Use");
      foreach (var exp in (doc.LogExpectations ?? new List<ScenarioLogExpectationRow>()).OrderBy(e => e.StepIndex))
      {
        lines.Add(string.Join("|",
            exp.StepIndex.ToString(CultureInfo.InvariantCulture),
            exp.PulseWithinScenario.ToString(CultureInfo.InvariantCulture),
            Escape(exp.StateText ?? ""),
            Escape(exp.StyleText ?? ""),
            Escape(exp.ThemeText ?? ""),
            Escape(exp.TriggerText ?? ""),
            Escape(exp.OrUmText ?? ""),
            Escape(exp.DangerText ?? ""),
            Escape(exp.VeryActualText ?? ""),
            Escape(exp.GeneticReflexText ?? ""),
            Escape(exp.ConditionReflexText ?? ""),
            Escape(exp.AutomatizmText ?? ""),
            Escape(exp.ReflexChainText ?? ""),
            Escape(exp.AutomatizmChainText ?? ""),
            Escape(exp.MainCycleText ?? ""),
            Escape(exp.AutomatizmUsefulnessText ?? "")));
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
      return s.Replace("\\", "\\\\")
              .Replace("|", "\\|")
              .Replace("\r\n", "\\n")
              .Replace("\n", "\\n")
              .Replace("\r", "\\n");
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
          if (s[i] == 'n')
            sb.Append('\n');
          else
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
