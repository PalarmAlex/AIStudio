using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Gomeostas;
using ISIDA.SymbiontEnv.Contract;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIStudio.Common.SymbiontEnv
{
  /// <summary>
  /// Имитация фазы A Velum на пульте AIStudio: только сдвиг параметров гомеостаза,
  /// без образа пускового стимула и без <see cref="InfluenceActionSystem.ApplyMultipleInfluenceActions"/>.
  /// </summary>
  public sealed class VirtualProbePressureApplier
  {
    private const float VirtualBadMetric = 0f;

    private readonly GomeostasSystem _gomeostas;
    private readonly InfluenceActionSystem _influenceActionSystem;

    /// <summary>Вызывается после фиксации давления метрик среды (для немедленной строки лога на текущем пульсе).</summary>
    public Action AfterEnvironmentProbeRecorded { get; set; }

    public VirtualProbePressureApplier(GomeostasSystem gomeostas, InfluenceActionSystem influenceActionSystem)
    {
      _gomeostas = gomeostas ?? throw new ArgumentNullException(nameof(gomeostas));
      _influenceActionSystem = influenceActionSystem ?? throw new ArgumentNullException(nameof(influenceActionSystem));
    }

    /// <summary>
    /// Явное давление (+) и отпускание (−) выбранных метрик среды.
    /// </summary>
    public void ApplyExplicit(
        IReadOnlyList<int> pressureActionIds,
        IReadOnlyList<int> releaseActionIds)
    {
      if (AppGlobalState.ObservationMode)
        return;

      var rules = BuildPressureRules();
      float epsilon = MetricProbeThresholds.DefaultMetricDeltaEpsilon;
      var merged = new Dictionary<int, float>();

      var pressureKeys = CollectProbeKeys(pressureActionIds);
      if (pressureKeys.Count > 0)
      {
        var probeSnapshot = pressureKeys.ToDictionary(k => k, _ => VirtualBadMetric, StringComparer.Ordinal);
        if (EnvironmentMetricPressureComposer.TryComposePressureWrites(
                probeSnapshot,
                rules,
                influenceScaleByKey: null,
                pressureKeys.ToList(),
                epsilon,
                GetCurrentParameterValues,
                out Dictionary<int, float> pressure))
        {
          MergeWrites(merged, pressure);
        }
      }

      var releaseKeys = CollectProbeKeys(releaseActionIds);
      foreach (string key in releaseKeys)
      {
        EnvironmentProbePressureRule rule = rules.FirstOrDefault(r =>
            string.Equals(r.ProbeKey, key, StringComparison.Ordinal));
        if (rule == null)
          continue;

        if (EnvironmentMetricParameterRelease.TryComposeReleaseWrites(
                key,
                rule,
                GetParameterState,
                out Dictionary<int, float> release))
        {
          MergeWrites(merged, release);
        }
      }

      if (merged.Count > 0)
        ApplyWrites(merged);

      if (RecordEnvironmentProbeLog(pressureActionIds, releaseActionIds))
        AfterEnvironmentProbeRecorded?.Invoke();
    }

    /// <returns>true, если зафиксирована хотя бы одна метрика для лога.</returns>
    private bool RecordEnvironmentProbeLog(IReadOnlyList<int> pressureActionIds, IReadOnlyList<int> releaseActionIds)
    {
      bool any = false;
      if (pressureActionIds != null)
      {
        foreach (int id in pressureActionIds.Where(i => i > 0).Distinct())
        {
          int mag = _influenceActionSystem.GetInfluenceMagnitudeSum(id);
          if (mag == 0)
            continue;
          AppGlobalState.RecordEnvironmentProbeAction(id, mag);
          any = true;
        }
      }
      if (releaseActionIds != null)
      {
        foreach (int id in releaseActionIds.Where(i => i > 0).Distinct())
        {
          int mag = _influenceActionSystem.GetInfluenceMagnitudeSum(id);
          if (mag == 0)
            continue;
          AppGlobalState.RecordEnvironmentProbeAction(id, -mag);
          any = true;
        }
      }
      return any;
    }

    private HashSet<string> CollectProbeKeys(IReadOnlyList<int> actionIds)
    {
      var probeKeys = new HashSet<string>(StringComparer.Ordinal);
      if (actionIds == null)
        return probeKeys;

      foreach (InfluenceActionSystem.GomeostasisInfluenceAction ea in EnumerateEnvironmentProbeActions())
      {
        if (!actionIds.Contains(ea.Id))
          continue;
        string key = (ea.ProbeKey ?? string.Empty).Trim();
        if (key.Length > 0)
          probeKeys.Add(key);
      }

      return probeKeys;
    }

    private IEnumerable<InfluenceActionSystem.GomeostasisInfluenceAction> EnumerateEnvironmentProbeActions() =>
        _influenceActionSystem.GetAllInfluenceActions()
            .Where(a => a.IsEnvironmentProbeAction && a.IsActive);

    private List<EnvironmentProbePressureRule> BuildPressureRules()
    {
      return EnumerateEnvironmentProbeActions()
          .Select(a => new EnvironmentProbePressureRule
          {
            ProbeKey = (a.ProbeKey ?? string.Empty).Trim(),
            Influences = a.Influences ?? new Dictionary<int, int>()
          })
          .Where(r => r.ProbeKey.Length > 0)
          .ToList();
    }

    private IReadOnlyDictionary<int, float> GetCurrentParameterValues(IEnumerable<int> ids) =>
        _gomeostas.HostGetParameterValues(ids);

    private GomeostasisParameterState GetParameterState(int paramId)
    {
      GomeostasSystem.ParameterData param = _gomeostas.GetParameter(paramId);
      if (param == null)
        return null;

      return new GomeostasisParameterState
      {
        Id = param.Id,
        Value = param.Value,
        Speed = param.Speed,
        NormaWell = param.NormaWell,
        CriticalMinValue = param.CriticalMinValue,
        CriticalMaxValue = param.CriticalMaxValue
      };
    }

    private void ApplyWrites(Dictionary<int, float> writes)
    {
      bool isCritical = false;
      foreach (KeyValuePair<int, float> kv in writes)
      {
        GomeostasSystem.ParameterData param = _gomeostas.GetParameter(kv.Key);
        if (param == null)
          continue;

        param.Value = kv.Value;
        if (_gomeostas.Calculator.IsParameterInBadZone(param))
          isCritical = true;
      }

      if (writes.Count > 0)
        _gomeostas.OnExternalInfluenceApplied(isCritical);
    }

    private static void MergeWrites(Dictionary<int, float> target, Dictionary<int, float> source)
    {
      if (source == null)
        return;

      foreach (KeyValuePair<int, float> kv in source)
        target[kv.Key] = kv.Value;
    }
  }
}
