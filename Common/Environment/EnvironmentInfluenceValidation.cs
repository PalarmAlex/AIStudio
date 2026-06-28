using AIStudio.Common.Adapters;
using ISIDA.Actions;
using System.Collections.Generic;
using System.Linq;

namespace AIStudio.Common.SymbiontEnv
{
  /// <summary>Валидация EA среды (ProbeKey) и покрытия metric-probes.json.</summary>
  public static class EnvironmentInfluenceValidation
  {
    /// <summary>Каждый probe из schema имеет ровно одну строку EA с тем же ProbeKey.</summary>
    public static (bool IsValid, string ErrorMessage) ValidateProbeCoverage(
        IEnumerable<InfluenceActionSystem.GomeostasisInfluenceAction> actions)
    {
      IReadOnlyList<AdapterSchemaMetricProbe> probes = AdapterSchemaLoader.LoadMetricProbesForCurrentProject();
      if (probes == null || probes.Count == 0)
        return (true, string.Empty);

      var envActions = (actions ?? Enumerable.Empty<InfluenceActionSystem.GomeostasisInfluenceAction>())
          .Where(a => a != null && a.IsEnvironmentProbeAction)
          .ToList();

      var errors = new List<string>();
      foreach (AdapterSchemaMetricProbe probe in probes)
      {
        if (probe == null || string.IsNullOrWhiteSpace(probe.Key))
          continue;

        int count = envActions.Count(a =>
            string.Equals((a.ProbeKey ?? string.Empty).Trim(), probe.Key.Trim(), System.StringComparison.Ordinal));
        if (count == 0)
          errors.Add($"ProbeKey «{probe.Key}»: нет строки EA среды.");
        else if (count > 1)
          errors.Add($"ProbeKey «{probe.Key}»: более одной строки EA ({count}).");
      }

      if (errors.Count == 0)
        return (true, string.Empty);

      return (false, string.Join("\n", errors));
    }

    /// <summary>Level3 рефлекса — только существующие ID из InfluenceActions.dat (не RuleId rules).</summary>
    public static (bool IsValid, string ErrorMessage) ValidateLevel3InfluenceActionIds(
        IEnumerable<int> level3Ids,
        IEnumerable<InfluenceActionSystem.GomeostasisInfluenceAction> allActions)
    {
      if (level3Ids == null)
        return (true, string.Empty);

      HashSet<int> eaIds = new HashSet<int>(
          (allActions ?? Enumerable.Empty<InfluenceActionSystem.GomeostasisInfluenceAction>())
          .Select(a => a.Id));

      List<int> missing = level3Ids.Where(id => id > 0 && !eaIds.Contains(id)).Distinct().ToList();
      if (missing.Count == 0)
        return (true, string.Empty);

      return (false,
          "Level3 ссылается на несуществующие ID InfluenceActions.dat: " +
          string.Join(", ", missing) +
          ". Используйте EA с ProbeKey для метрик среды (не RuleId из legacy rules).");
    }
  }
}
