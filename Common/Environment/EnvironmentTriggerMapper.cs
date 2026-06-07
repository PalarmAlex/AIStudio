using System;
using System.Collections.Generic;
using System.Globalization;
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
    private const string KindPart = "part";
    private const string KindAssembly = "assembly";
    private const string KindDrawing = "drawing";

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
        DocumentKindPart = HasKind(trigger, KindPart),
        DocumentKindAssembly = HasKind(trigger, KindAssembly),
        DocumentKindDrawing = HasKind(trigger, KindDrawing),
        DetectRules = trigger.DetectRules?
            .Select(r => new EnvironmentTriggerDetectRow
            {
              Kind = r?.Kind ?? string.Empty,
              Environment = r?.Environment ?? string.Empty,
              Enabled = r != null && r.Enabled,
              CommandIdsText = FormatCommandIds(r?.Parameters)
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
        data.DocumentKinds.Add(KindPart);
      if (row.DocumentKindAssembly)
        data.DocumentKinds.Add(KindAssembly);
      if (row.DocumentKindDrawing)
        data.DocumentKinds.Add(KindDrawing);
      if (row.DetectRules != null)
      {
        foreach (EnvironmentTriggerDetectRow rule in row.DetectRules)
        {
          var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
          SetCommandIds(parameters, ParseCommandIds(rule?.CommandIdsText));
          data.DetectRules.Add(new EnvironmentTriggerDetectData
          {
            Kind = rule?.Kind ?? string.Empty,
            Environment = rule?.Environment ?? string.Empty,
            Enabled = rule != null && rule.Enabled,
            Parameters = parameters
          });
        }
      }
      return data;
    }

    private static bool HasKind(EnvironmentTriggerData trigger, string kind)
    {
      if (trigger.DocumentKinds == null || trigger.DocumentKinds.Count == 0)
        return string.Equals(kind, KindPart, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(kind, KindAssembly, StringComparison.OrdinalIgnoreCase);
      return trigger.DocumentKinds.Any(
          x => string.Equals(x, kind, StringComparison.OrdinalIgnoreCase));
    }

    private static string FormatCommandIds(IDictionary<string, string> parameters)
    {
      if (parameters == null ||
          !parameters.TryGetValue("command_ids", out string text) ||
          string.IsNullOrWhiteSpace(text))
        return string.Empty;
      return text.Replace(",", ", ");
    }

    private static List<int> ParseCommandIds(string text)
    {
      var list = new List<int>();
      if (string.IsNullOrWhiteSpace(text))
        return list;
      foreach (string part in text.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
      {
        if (int.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
          list.Add(id);
      }
      return list;
    }

    private static void SetCommandIds(IDictionary<string, string> parameters, IList<int> ids)
    {
      if (parameters == null || ids == null || ids.Count == 0)
        return;
      parameters["command_ids"] = string.Join(
          ",",
          ids.Select(id => id.ToString(CultureInfo.InvariantCulture)));
    }
  }
}
