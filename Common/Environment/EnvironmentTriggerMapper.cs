using System;
using System.Collections.Generic;
using System.Linq;
using AIStudio.ViewModels.SymbiontEnv;
using ISIDA.SymbiontEnv.Contract;

namespace AIStudio.Common.SymbiontEnv
{
  /// <summary>
  /// Преобразование триггеров среды (contract 3.1).
  /// </summary>
  public static class EnvironmentTriggerMapper
  {
    /// <summary>Строка таблицы.</summary>
    public static EnvironmentTriggerRow ToRow(EnvironmentTriggerData trigger)
    {
      if (trigger == null)
        return new EnvironmentTriggerRow();

      var row = new EnvironmentTriggerRow
      {
        Id = trigger.Id,
        DisplayName = trigger.DisplayName,
        ReflexTriggerCommandPatternId = trigger.ReflexTriggerCommandPatternId,
        EventKind = trigger.EventKind ?? string.Empty
      };

      if (trigger.HomeostasisDeltas != null)
      {
        foreach (HomeostasisDeltaEntry delta in trigger.HomeostasisDeltas)
        {
          if (delta == null || delta.ParameterId <= 0 || delta.Delta == 0)
            continue;
          row.HomeostasisDeltas[delta.ParameterId] = (int)delta.Delta;
        }
      }

      if (trigger.EventParameters != null)
      {
        foreach (KeyValuePair<string, string> kv in trigger.EventParameters)
          row.EventParameters[kv.Key] = kv.Value;
      }

      return row;
    }

    /// <summary>Данные для YAML.</summary>
    public static EnvironmentTriggerData ToData(EnvironmentTriggerRow row)
    {
      if (row == null)
        throw new ArgumentNullException(nameof(row));

      var data = new EnvironmentTriggerData
      {
        Id = row.Id?.Trim() ?? string.Empty,
        DisplayName = row.DisplayName ?? string.Empty,
        ReflexTriggerCommandPatternId = row.ReflexTriggerCommandPatternId,
        EventKind = row.EventKind?.Trim() ?? string.Empty
      };

      foreach (KeyValuePair<int, int> kv in row.HomeostasisDeltas.OrderBy(k => k.Key))
      {
        if (kv.Key <= 0 || kv.Value == 0)
          continue;
        data.HomeostasisDeltas.Add(new HomeostasisDeltaEntry
        {
          ParameterId = kv.Key,
          Delta = kv.Value
        });
      }

      if (row.EventParameters != null)
      {
        foreach (KeyValuePair<string, string> kv in row.EventParameters)
        {
          if (!string.IsNullOrWhiteSpace(kv.Value))
            data.EventParameters[kv.Key] = kv.Value;
        }
      }

      return data;
    }
  }
}
