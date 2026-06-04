using System;
using System.Collections.Generic;
using System.Linq;
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

      return new EnvironmentTriggerRow
      {
        Id = trigger.Id,
        DisplayName = trigger.DisplayName,
        InfluenceActionId = trigger.InfluenceActionId,
        DocumentKindPart = HasKind(trigger, EnvironmentDocumentKind.Part),
        DocumentKindAssembly = HasKind(trigger, EnvironmentDocumentKind.Assembly),
        DocumentKindDrawing = HasKind(trigger, EnvironmentDocumentKind.Drawing),
        DetectRules = trigger.DetectRules?
            .Select(r => new EnvironmentTriggerDetectRow
            {
              Kind = r?.Kind ?? string.Empty,
              Environment = r?.Environment ?? string.Empty,
              Enabled = r != null && r.Enabled,
              CommandIdsText = FormatCommandIds(r?.CommandIds)
            })
            .ToList() ?? new List<EnvironmentTriggerDetectRow>()
      };
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
        InfluenceActionId = row.InfluenceActionId
      };

      if (row.DocumentKindPart)
        data.DocumentKinds.Add(EnvironmentDocumentKind.Part);
      if (row.DocumentKindAssembly)
        data.DocumentKinds.Add(EnvironmentDocumentKind.Assembly);
      if (row.DocumentKindDrawing)
        data.DocumentKinds.Add(EnvironmentDocumentKind.Drawing);

      if (row.DetectRules != null)
      {
        foreach (EnvironmentTriggerDetectRow rule in row.DetectRules)
        {
          data.DetectRules.Add(new EnvironmentTriggerDetectData
          {
            Kind = rule?.Kind ?? string.Empty,
            Environment = rule?.Environment ?? string.Empty,
            Enabled = rule != null && rule.Enabled,
            CommandIds = ParseCommandIds(rule?.CommandIdsText)
          });
        }
      }

      return data;
    }

    private static bool HasKind(EnvironmentTriggerData trigger, EnvironmentDocumentKind kind)
    {
      if (trigger.DocumentKinds == null || trigger.DocumentKinds.Count == 0)
        return kind == EnvironmentDocumentKind.Part || kind == EnvironmentDocumentKind.Assembly;

      return trigger.DocumentKinds.Contains(kind);
    }

    private static string FormatCommandIds(IList<int> ids)
    {
      if (ids == null || ids.Count == 0)
        return string.Empty;
      return string.Join(", ", ids);
    }

    private static List<int> ParseCommandIds(string text)
    {
      var list = new List<int>();
      if (string.IsNullOrWhiteSpace(text))
        return list;

      foreach (string part in text.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
      {
        if (int.TryParse(part.Trim(), out int id))
          list.Add(id);
      }

      return list;
    }
  }
}
