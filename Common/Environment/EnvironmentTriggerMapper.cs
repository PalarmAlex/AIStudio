using System;
using System.Collections.Generic;
using AIStudio.ViewModels.SymbiontEnv;
using ISIDA.SymbiontEnv.Contract;

namespace AIStudio.Common.SymbiontEnv
{
  /// <summary>
  /// Преобразование триггеров среды.
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
        InfluenceActionId = trigger.InfluenceActionId,
        EventKind = trigger.EventKind ?? string.Empty
      };

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
        InfluenceActionId = row.InfluenceActionId,
        EventKind = row.EventKind?.Trim() ?? string.Empty
      };

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
