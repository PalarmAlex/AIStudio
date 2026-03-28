using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISIDA.Scenarios;

namespace AIStudio.Common
{
  /// <summary>Слияние записей лога по глобальному пульсу и сравнение с ожиданиями сценария.</summary>
  public static class ScenarioLogComparer
  {
    public sealed class AggregatedLogSnapshot
    {
      public string State { get; set; } = "-";
      public string Style { get; set; } = "-";
      public string Theme { get; set; } = "-";
      public string Trigger { get; set; } = "-";
      public string OrUm { get; set; } = "-";
      public string GeneticReflex { get; set; } = "-";
      public string ConditionReflex { get; set; } = "-";
      public string Automatizm { get; set; } = "-";
      public string ReflexChain { get; set; } = "-";
      public string AutomatizmChain { get; set; } = "-";
      public string MainCycle { get; set; } = "-";
    }

    /// <summary>Группирует записи по пульсу: для каждого поля берётся последнее не «-» значение по времени.</summary>
    public static Dictionary<int, AggregatedLogSnapshot> AggregateByPulse(
        IEnumerable<MemoryLogManager.LogEntry> entries)
    {
      var result = new Dictionary<int, AggregatedLogSnapshot>();
      if (entries == null)
        return result;

      foreach (var g in entries.Where(e => e.Pulse.HasValue).GroupBy(e => e.Pulse.Value))
      {
        var ordered = g.OrderBy(e => e.Timestamp).ToList();
        var snap = new AggregatedLogSnapshot();
        foreach (var e in ordered)
        {
          snap.State = MergeField(snap.State, e.DisplayBaseID);
          snap.Style = MergeField(snap.Style, e.DisplayBaseStyleID);
          snap.Theme = MergeField(snap.Theme, e.DisplayThinkingThemeId);
          snap.Trigger = MergeField(snap.Trigger, e.DisplayTriggerStimulusID);
          snap.OrUm = MergeField(snap.OrUm, e.DisplayOrUm);
          snap.GeneticReflex = MergeField(snap.GeneticReflex, e.DisplayGeneticReflexID);
          snap.ConditionReflex = MergeField(snap.ConditionReflex, e.DisplayConditionReflexID);
          snap.Automatizm = MergeField(snap.Automatizm, e.DisplayAutomatizmID);
          snap.ReflexChain = MergeField(snap.ReflexChain, e.DisplayReflexChainInfo);
          snap.AutomatizmChain = MergeField(snap.AutomatizmChain, e.DisplayAutomatizmChainInfo);
          snap.MainCycle = MergeField(snap.MainCycle, e.DisplayMainThinkingCycle);
        }
        result[g.Key] = snap;
      }
      return result;
    }

    /// <summary>Если кандидат не «-», подставляет его; иначе оставляет текущее значение.</summary>
    private static string MergeField(string current, string candidate)
    {
      var c = NormalizeDisplay(candidate);
      if (c != "-")
        return c;
      return current;
    }

    /// <summary>Пустая ячейка редактора → ожидается «-» в логе.</summary>
    public static string NormalizeExpected(string raw)
    {
      if (string.IsNullOrWhiteSpace(raw))
        return "-";
      return raw.Trim();
    }

    public static string NormalizeDisplay(string raw)
    {
      if (string.IsNullOrWhiteSpace(raw))
        return "-";
      var t = raw.Trim();
      return t.Length == 0 ? "-" : t;
    }

    public sealed class StepCompareResult
    {
      public int StepIndex { get; set; }
      public int PulseWithinScenario { get; set; }
      public int GlobalPulse { get; set; }
      public bool Ok { get; set; }
      public string Details { get; set; } = "";
    }

    public static List<StepCompareResult> Compare(
        ScenarioDocument doc,
        int anchorGlobalPulse,
        IReadOnlyDictionary<int, AggregatedLogSnapshot> byGlobalPulse)
    {
      var list = new List<StepCompareResult>();
      if (doc?.Lines == null || doc.LogExpectations == null)
        return list;

      var expByStep = (doc.LogExpectations ?? new List<ScenarioLogExpectationRow>())
          .GroupBy(e => e.StepIndex)
          .ToDictionary(g => g.Key, g => g.First());

      foreach (var line in doc.Lines.OrderBy(l => l.StepIndex))
      {
        var step = line.StepIndex;
        var pulseWithin = line.PulseWithinScenario;
        var globalPulse = anchorGlobalPulse + pulseWithin;

        expByStep.TryGetValue(step, out var exp);
        if (exp == null)
        {
          list.Add(new StepCompareResult
          {
            StepIndex = step,
            PulseWithinScenario = pulseWithin,
            GlobalPulse = globalPulse,
            Ok = true,
            Details = "Нет строки ожиданий — нечего сравнивать."
          });
          continue;
        }

        byGlobalPulse.TryGetValue(globalPulse, out var actual);
        actual = actual ?? new AggregatedLogSnapshot();

        var mismatches = new List<string>();

        // Пусто — не сравниваем; непустое ожидание сравнивается с фактом (в т.ч. «-» с прочерком в логе).
        void Check(string label, string expectedRaw, string actualVal)
        {
          if (string.IsNullOrWhiteSpace(expectedRaw))
            return;
          var e = expectedRaw.Trim();
          var a = NormalizeDisplay(actualVal);
          if (!string.Equals(e, a, StringComparison.Ordinal))
            mismatches.Add($"{label}: ожид. «{e}», факт «{a}»");
        }

        Check("Состояние", exp.StateText, actual.State);
        Check("Стиль", exp.StyleText, actual.Style);
        Check("Тема", exp.ThemeText, actual.Theme);
        Check("Триггер", exp.TriggerText, actual.Trigger);
        Check("ОР/УМ", exp.OrUmText, actual.OrUm);
        Check("Б/у рефлекс", exp.GeneticReflexText, actual.GeneticReflex);
        Check("Усл. рефлекс", exp.ConditionReflexText, actual.ConditionReflex);
        Check("Автоматизм", exp.AutomatizmText, actual.Automatizm);
        Check("Цепочка РФ", exp.ReflexChainText, actual.ReflexChain);
        Check("Цепочка АВ", exp.AutomatizmChainText, actual.AutomatizmChain);
        Check("Цикл М", exp.MainCycleText, actual.MainCycle);

        var ok = mismatches.Count == 0;
        var sb = new StringBuilder();
        if (ok)
          sb.Append("Все проверяемые поля совпали.");
        else
          sb.Append(string.Join("; ", mismatches));

        list.Add(new StepCompareResult
        {
          StepIndex = step,
          PulseWithinScenario = pulseWithin,
          GlobalPulse = globalPulse,
          Ok = ok,
          Details = sb.ToString()
        });
      }

      return list;
    }
  }
}
