using ISIDA.Scenarios;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace AIStudio.Common
{
  /// <summary>Фаза валидации триады для сценария §6.14 / §13.3.</summary>
  public enum TriadValidationPhase
  {
    /// <summary>Не сценарий валидации триады.</summary>
    Unknown = 0,
    /// <summary>Фаза A — CS/US через Operator.</summary>
    A = 1,
    /// <summary>Фаза B — ритуал + coupling Niche.</summary>
    B = 2,
    /// <summary>Фаза C — Operator только через Niche.</summary>
    C = 3
  }

  /// <summary>Один пункт автоматической проверки метрики §13.3.</summary>
  public sealed class TriadMetricCheckItem
  {
    /// <summary>Название метрики.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Метрика выполнена.</summary>
    public bool Passed { get; set; }

    /// <summary>Пояснение (факт / порог).</summary>
    public string Details { get; set; } = string.Empty;
  }

  /// <summary>Итог проверки метрик триады за прогон сценария.</summary>
  public sealed class TriadValidationMetricsReport
  {
    /// <summary>Фаза A/B/C.</summary>
    public TriadValidationPhase Phase { get; set; }

    /// <summary>Название сценария.</summary>
    public string ScenarioTitle { get; set; } = string.Empty;

    /// <summary>Диапазон глобальных пульсов прогона.</summary>
    public int PulseFrom { get; set; }

    /// <summary>Диапазон глобальных пульсов прогона.</summary>
    public int PulseTo { get; set; }

    /// <summary>Пункты проверки.</summary>
    public List<TriadMetricCheckItem> Checks { get; set; } = new List<TriadMetricCheckItem>();

    /// <summary>Лог диады найден и прочитан.</summary>
    public bool DyadLogAvailable { get; set; }

    /// <summary>Все обязательные метрики выполнены.</summary>
    public bool AllPassed => Checks.Count > 0 && Checks.All(c => c.Passed);

    /// <summary>Краткая строка для UI.</summary>
    public string SummaryText
    {
      get
      {
        if (Phase == TriadValidationPhase.Unknown)
          return "Не сценарий валидации триады.";
        if (Checks.Count == 0)
          return "Метрики §13.3: нет данных для проверки.";
        int ok = Checks.Count(c => c.Passed);
        return AllPassed
            ? $"Метрики §13.3 (фаза {Phase}): все пройдены ({ok}/{Checks.Count})."
            : $"Метрики §13.3 (фаза {Phase}): {ok}/{Checks.Count} пройдено.";
      }
    }
  }

  /// <summary>
  /// Автоматическая проверка метрик валидации триады §13.3–13.4 по логам агента и AgentLogs_Dyad.jsonl.
  /// </summary>
  public static class TriadValidationMetricsChecker
  {
    private const string DyadLogFileName = "AgentLogs_Dyad.jsonl";

    /// <summary>Сценарий из пакета валидации §6.14.</summary>
    public static bool IsTriadValidationScenario(ScenarioDocument doc)
    {
      var title = doc?.Header?.Title ?? string.Empty;
      return title.StartsWith(TriadValidationScenarioBootstrap.ScenarioTitlePrefix, StringComparison.Ordinal);
    }

    /// <summary>Определяет фазу A/B/C по заголовку сценария.</summary>
    public static TriadValidationPhase DetectPhase(ScenarioDocument doc)
    {
      var title = doc?.Header?.Title ?? string.Empty;
      if (title.StartsWith("[Triad A]", StringComparison.Ordinal))
        return TriadValidationPhase.A;
      if (title.StartsWith("[Triad B]", StringComparison.Ordinal))
        return TriadValidationPhase.B;
      if (title.StartsWith("[Triad C]", StringComparison.Ordinal))
        return TriadValidationPhase.C;
      return TriadValidationPhase.Unknown;
    }

    /// <summary>
    /// Вычисляет метрики §13.3 для прогона сценария.
    /// </summary>
    public static TriadValidationMetricsReport Evaluate(
        ScenarioDocument doc,
        OperatorScenarioCompletedEventArgs completion,
        string logsFolder)
    {
      var report = new TriadValidationMetricsReport
      {
        Phase = DetectPhase(doc),
        ScenarioTitle = doc?.Header?.Title ?? string.Empty
      };

      if (report.Phase == TriadValidationPhase.Unknown)
        return report;

      int anchor = completion?.AnchorGlobalPulse ?? 0;
      int maxWithin = doc?.Lines != null && doc.Lines.Count > 0
          ? doc.Lines.Max(l => l.PulseWithinScenario)
          : completion?.LastExecutedPulseWithinScenario ?? 0;
      report.PulseFrom = anchor + 1;
      report.PulseTo = anchor + maxWithin;

      var agg = ScenarioLogComparer.AggregateByPulse(MemoryLogManager.Instance.LogEntries);
      var dyadEntries = LoadDyadEntries(logsFolder, report.PulseFrom, report.PulseTo);
      report.DyadLogAvailable = dyadEntries.Count > 0 || File.Exists(GetDyadLogPath(logsFolder));

      switch (report.Phase)
      {
        case TriadValidationPhase.A:
          EvaluatePhaseA(report, agg, dyadEntries, report.PulseFrom, report.PulseTo);
          break;
        case TriadValidationPhase.B:
          EvaluatePhaseB(report, agg, dyadEntries, report.PulseFrom, report.PulseTo);
          break;
        case TriadValidationPhase.C:
          EvaluatePhaseC(report, agg, dyadEntries, report.PulseFrom, report.PulseTo);
          break;
      }

      AddContourInputSnapshotCheck(report, dyadEntries);

      return report;
    }

    /// <summary>Добавляет HTML-блок метрик в отчёт сценария.</summary>
    public static void AppendHtmlSection(StringBuilder sb, TriadValidationMetricsReport report)
    {
      if (report == null || report.Phase == TriadValidationPhase.Unknown)
        return;

      sb.AppendLine("<h2>Метрики валидации триады (§13.3)</h2>");
      sb.AppendLine("<p class=\"muted\">Диапазон пульсов: ")
          .Append(Escape(report.PulseFrom.ToString(CultureInfo.InvariantCulture)))
          .Append("…")
          .Append(Escape(report.PulseTo.ToString(CultureInfo.InvariantCulture)))
          .Append(". Лог диады: ")
          .Append(report.DyadLogAvailable ? "найден" : "не найден или пуст")
          .AppendLine(".</p>");

      if (report.AllPassed)
        sb.AppendLine("<p class=\"summary-ok\">").Append(Escape(report.SummaryText)).AppendLine("</p>");
      else
        sb.AppendLine("<p class=\"summary-bad\">").Append(Escape(report.SummaryText)).AppendLine("</p>");

      sb.AppendLine("<table class=\"steps-zebra\"><tr><th>Метрика</th><th>Результат</th><th>Детали</th></tr>");
      foreach (var c in report.Checks)
      {
        sb.AppendLine("<tr>");
        sb.Append("<td>").Append(Escape(c.Name)).Append("</td>");
        sb.Append("<td>").Append(c.Passed
            ? "<span class=\"summary-ok\">OK</span>"
            : "<span class=\"summary-bad\">FAIL</span>").Append("</td>");
        sb.Append("<td>").Append(Escape(c.Details ?? string.Empty)).Append("</td>");
        sb.AppendLine("</tr>");
      }
      sb.AppendLine("</table>");

      if (report.Phase == TriadValidationPhase.C)
      {
        sb.AppendLine("<p class=\"muted\">Перенос на Niche₂ (§13.3): автоматически не проверяется — выполните ApplyNicheTransfer вручную после прогона.</p>");
      }
    }

    private static void EvaluatePhaseA(
        TriadValidationMetricsReport report,
        Dictionary<int, ScenarioLogComparer.AggregatedLogSnapshot> agg,
        List<JObject> dyadEntries,
        int pulseFrom,
        int pulseTo)
    {
      bool hasCr = false;
      bool hasAutomatizmPositiveAoe = false;
      int crPulse = -1;
      int autPulse = -1;
      int? autUse = null;

      for (int p = pulseFrom; p <= pulseTo; p++)
      {
        if (!agg.TryGetValue(p, out var snap))
          continue;
        if (!hasCr && snap.ConditionReflex != null && snap.ConditionReflex.Trim() != "-")
        {
          hasCr = true;
          crPulse = p;
        }
        if (snap.AutomatizmUsefulnessLogged.HasValue && snap.AutomatizmUsefulnessLogged.Value >= 0
            && snap.Automatizm != null && snap.Automatizm.Trim() != "-")
        {
          hasAutomatizmPositiveAoe = true;
          autPulse = p;
          autUse = snap.AutomatizmUsefulnessLogged;
        }
      }

      bool hasNicheAoePositive = dyadEntries.Any(e =>
          string.Equals(e["event"]?.ToString(), "niche_aoe_outcome", StringComparison.OrdinalIgnoreCase)
          && TryParseDouble(e["assessment"], out double a) && a > 0);

      report.Checks.Add(new TriadMetricCheckItem
      {
        Name = "Условный рефлекс (УР)",
        Passed = hasCr,
        Details = hasCr
            ? "Обнаружен на пульсе " + crPulse.ToString(CultureInfo.InvariantCulture) + "."
            : "В диапазоне прогона УР не зафиксирован в логе агента."
      });

      report.Checks.Add(new TriadMetricCheckItem
      {
        Name = "Automatizm с AOE≥0",
        Passed = hasAutomatizmPositiveAoe,
        Details = hasAutomatizmPositiveAoe
            ? "Пульс " + autPulse + ", полезность=" + autUse.Value.ToString(CultureInfo.InvariantCulture) + "."
            : "Нет automatizm с полезностью ≥0 в логе агента."
      });

      report.Checks.Add(new TriadMetricCheckItem
      {
        Name = "Итог фазы A (УР или AOE≥0)",
        Passed = hasCr || hasAutomatizmPositiveAoe || hasNicheAoePositive,
        Details = hasNicheAoePositive
            ? "Дополнительно: niche_aoe_outcome с assessment>0 в логе диады."
            : (hasCr || hasAutomatizmPositiveAoe
                ? "Критерий §13.4 выполнен."
                : "Ни УР, ни automatizm с AOE≥0, ни niche_aoe_outcome не обнаружены.")
      });
    }

    private static void EvaluatePhaseB(
        TriadValidationMetricsReport report,
        Dictionary<int, ScenarioLogComparer.AggregatedLogSnapshot> agg,
        List<JObject> dyadEntries,
        int pulseFrom,
        int pulseTo)
    {
      bool hasBelief2 = false;
      bool hasUsefulnessNonNeg = false;
      int beliefPulse = -1;

      for (int p = pulseFrom; p <= pulseTo; p++)
      {
        if (!agg.TryGetValue(p, out var snap))
          continue;
        if (snap.AutomatizmUsefulnessLogged == 2)
        {
          hasBelief2 = true;
          beliefPulse = p;
        }
        if (snap.AutomatizmUsefulnessLogged.HasValue && snap.AutomatizmUsefulnessLogged.Value >= 0
            && snap.Automatizm != null && snap.Automatizm.Trim() != "-")
        {
          hasUsefulnessNonNeg = true;
        }
      }

      bool hasNicheOrigin = dyadEntries.Any(e =>
          string.Equals(e["lastCreatureUpdateOrigin"]?.ToString(), "Niche", StringComparison.OrdinalIgnoreCase));

      bool hasNicheResponse = dyadEntries.Any(e =>
          HasNonEmptyDeltaObject(e["nicheResponseDelta"]) || NicheStateChanged(e));

      bool hasCoupling = dyadEntries.Any(e =>
          (e["creatureActionId"]?.Value<int>() ?? 0) > 0 && NicheStateChanged(e));

      report.Checks.Add(new TriadMetricCheckItem
      {
        Name = "Belief=2 (штатный automatizm)",
        Passed = hasBelief2,
        Details = hasBelief2
            ? "Belief=2 на пульсе " + beliefPulse.ToString(CultureInfo.InvariantCulture) + "."
            : "В логе агента нет automatizm с полезностью=2."
      });

      report.Checks.Add(new TriadMetricCheckItem
      {
        Name = "Usefulness≥0",
        Passed = hasUsefulnessNonNeg,
        Details = hasUsefulnessNonNeg
            ? "Есть automatizm с полезностью ≥0."
            : "Automatizm с неотрицательной полезностью не найден."
      });

      report.Checks.Add(new TriadMetricCheckItem
      {
        Name = "StimulusOrigin=Niche в логе диады",
        Passed = hasNicheOrigin,
        Details = hasNicheOrigin
            ? "Зафиксирован lastCreatureUpdateOrigin=Niche."
            : "Нет записей с происхождением Niche — проверьте coupling и фазу B."
      });

      report.Checks.Add(new TriadMetricCheckItem
      {
        Name = "Отклик Niche (coupling / response)",
        Passed = hasNicheResponse || hasCoupling,
        Details = hasCoupling
            ? "Изменение Niche после действия Creature (coupling)."
            : (hasNicheResponse
                ? "nicheResponseDelta или изменение nicheStateAfter."
                : "Отклик Niche в AgentLogs_Dyad.jsonl не обнаружен.")
      });
    }

    private static void EvaluatePhaseC(
        TriadValidationMetricsReport report,
        Dictionary<int, ScenarioLogComparer.AggregatedLogSnapshot> agg,
        List<JObject> dyadEntries,
        int pulseFrom,
        int pulseTo)
    {
      bool operatorRoutedToNiche = dyadEntries.Any(e =>
      {
        int actionId = e["creatureActionId"]?.Value<int>() ?? 0;
        return actionId == 0 && NicheStateChanged(e);
      });

      bool hasNicheAoeOutcome = dyadEntries.Any(e =>
          string.Equals(e["event"]?.ToString(), "niche_aoe_outcome", StringComparison.OrdinalIgnoreCase));

      bool hasOperatorDirectOnCreature = dyadEntries.Any(e =>
          string.Equals(e["lastCreatureUpdateOrigin"]?.ToString(), "Operator", StringComparison.OrdinalIgnoreCase));

      report.Checks.Add(new TriadMetricCheckItem
      {
        Name = "Operator→Niche (Δ Niche без action Creature)",
        Passed = operatorRoutedToNiche,
        Details = operatorRoutedToNiche
            ? "Параметры Niche менялись при creatureActionId=0 (маршрутизация пульта)."
            : "Нет изменения Niche без действия Creature — проверьте фазу C и operator_niche_coupling.dat."
      });

      report.Checks.Add(new TriadMetricCheckItem
      {
        Name = "Episodic rule (niche_aoe_outcome)",
        Passed = hasNicheAoeOutcome,
        Details = hasNicheAoeOutcome
            ? "Событие niche_aoe_outcome зафиксировано в логе диады."
            : "Нет niche_aoe_outcome — дождитесь закрытия окна AOE Niche после отклика среды."
      });

      report.Checks.Add(new TriadMetricCheckItem
      {
        Name = "Нет прямого Operator→Creature",
        Passed = !hasOperatorDirectOnCreature,
        Details = !hasOperatorDirectOnCreature
            ? "lastCreatureUpdateOrigin=Operator не зафиксирован в логе диады."
            : "Обнаружено прямое влияние Operator на Creature — для фазы C это нежелательно."
      });
    }

    private static void AddContourInputSnapshotCheck(TriadValidationMetricsReport report, List<JObject> dyadEntries)
    {
      bool hasContourInput = dyadEntries.Any(e =>
      {
        if (e["event"] != null)
          return false;

        string key = e["contourProbeKey"]?.ToString();
        return !string.IsNullOrWhiteSpace(key) && HasNonEmptyDeltaObject(e["contourInputDelta"]);
      });

      string sampleKey = dyadEntries
          .Where(e => e["event"] == null && !string.IsNullOrWhiteSpace(e["contourProbeKey"]?.ToString()))
          .Select(e => e["contourProbeKey"]?.ToString())
          .FirstOrDefault() ?? string.Empty;

      report.Checks.Add(new TriadMetricCheckItem
      {
        Name = "Contour InputSnapshot (§6.8)",
        Passed = hasContourInput,
        Details = hasContourInput
            ? "EnvironmentMetricProbeKey «" + sampleKey + "» → contourInputDelta в AgentLogs_Dyad.jsonl."
            : "Нет contourProbeKey/contourInputDelta — UseProbeContour=1, contour_probes.dat и воздействие с probeKey (напр. warm)."
      });
    }

    private static string GetDyadLogPath(string logsFolder)
    {
      if (string.IsNullOrWhiteSpace(logsFolder))
        logsFolder = AppConfig.LogsFolderPath;
      return Path.Combine(logsFolder ?? string.Empty, DyadLogFileName);
    }

    private static List<JObject> LoadDyadEntries(string logsFolder, int pulseFrom, int pulseTo)
    {
      var list = new List<JObject>();
      string path = GetDyadLogPath(logsFolder);
      if (!File.Exists(path))
        return list;

      foreach (var line in File.ReadAllLines(path))
      {
        if (string.IsNullOrWhiteSpace(line))
          continue;
        try
        {
          var jo = JObject.Parse(line);
          if (jo["event"] != null)
          {
            list.Add(jo);
            continue;
          }

          int? pulse = jo["pulse"]?.Value<int>();
          if (pulse.HasValue && pulse.Value >= pulseFrom && pulse.Value <= pulseTo)
            list.Add(jo);
        }
        catch
        {
          // skip malformed lines
        }
      }

      return list;
    }

    private static bool NicheStateChanged(JObject entry)
    {
      var before = entry["nicheStateBefore"] as JObject;
      var after = entry["nicheStateAfter"] as JObject;
      if (before == null || after == null)
        return false;

      foreach (var prop in after.Properties())
      {
        string key = prop.Name;
        float afterVal = prop.Value.Value<float>();
        float beforeVal = before[key]?.Value<float>() ?? afterVal;
        if (Math.Abs(afterVal - beforeVal) > 0.001f)
          return true;
      }

      return false;
    }

    private static bool HasNonEmptyDeltaObject(JToken token)
    {
      if (token is JObject obj)
      {
        foreach (var p in obj.Properties())
        {
          if (Math.Abs(p.Value.Value<float>()) > 0.001f)
            return true;
        }
      }

      return false;
    }

    private static bool TryParseDouble(JToken token, out double value)
    {
      value = 0;
      if (token == null)
        return false;
      return double.TryParse(token.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static string Escape(string s) => WebUtility.HtmlEncode(s ?? string.Empty);
  }
}
