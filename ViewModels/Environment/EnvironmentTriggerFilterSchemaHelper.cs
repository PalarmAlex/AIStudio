using AIStudio.Common.Adapters;
using ISIDA.SymbiontEnv.Contract;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>
  /// Синхронизация фильтра триггера со schema/trigger-filter.json.
  /// </summary>
  public static class EnvironmentTriggerFilterSchemaHelper
  {
    public static void Initialize(
        EnvironmentTriggerRow row,
        EnvironmentTriggerData trigger,
        AdapterEnvironmentSchema schema,
        bool applyNewDefaults = false)
    {
      if (row == null)
        throw new ArgumentNullException(nameof(row));
      if (schema == null)
        throw new ArgumentNullException(nameof(schema));

      EnvironmentSchemaFieldsHelper.PopulateFields(
          row.FilterFields,
          schema.TriggerFilterFields,
          key => GetSelectedStringListValues(trigger, key),
          key => false,
          applyNewDefaults);
    }

    public static void ApplyToData(EnvironmentTriggerRow row, EnvironmentTriggerData data)
    {
      if (row == null)
        throw new ArgumentNullException(nameof(row));
      if (data == null)
        throw new ArgumentNullException(nameof(data));

      data.DocumentKinds.Clear();
      foreach (EnvironmentRecipePreconditionField field in row.FilterFields)
      {
        if (field == null || !field.IsStringListType)
          continue;
        if (!string.Equals(field.Key, EnvironmentSchemaFieldsHelper.DocumentKindsKey, StringComparison.OrdinalIgnoreCase))
          continue;

        foreach (EnvironmentRecipePreconditionListItem item in field.ListItems)
        {
          if (item == null || !item.IsSelected || string.IsNullOrWhiteSpace(item.Value))
            continue;
          data.DocumentKinds.Add(item.Value);
        }
      }
    }

    private static IList<string> GetSelectedStringListValues(EnvironmentTriggerData trigger, string key)
    {
      if (trigger == null)
        return new List<string>();

      if (string.Equals(key, EnvironmentSchemaFieldsHelper.DocumentKindsKey, StringComparison.OrdinalIgnoreCase))
        return trigger.DocumentKinds?.ToList() ?? new List<string>();

      return new List<string>();
    }
  }
}
