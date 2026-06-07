using AIStudio.Common.Adapters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>
  /// Общая логика полей schema (bool, stringList) для редакторов среды.
  /// </summary>
  public static class EnvironmentSchemaFieldsHelper
  {
    public const string DocumentKindsKey = "document_kinds";

    public static void PopulateFields(
        ObservableCollection<EnvironmentRecipePreconditionField> target,
        IList<AdapterSchemaField> schemaFields,
        Func<string, IList<string>> getStringListSelected,
        Func<string, bool> getBoolSelected,
        bool applyNewDefaults = false)
    {
      if (target == null)
        throw new ArgumentNullException(nameof(target));
      target.Clear();
      if (schemaFields == null || schemaFields.Count == 0)
        return;

      foreach (AdapterSchemaField schemaField in schemaFields)
      {
        if (schemaField == null || string.IsNullOrWhiteSpace(schemaField.Key))
          continue;

        string key = schemaField.Key.Trim();
        string fieldType = schemaField.Type ?? "bool";
        var field = new EnvironmentRecipePreconditionField
        {
          Key = key,
          Label = string.IsNullOrWhiteSpace(schemaField.Label) ? key : schemaField.Label,
          FieldType = fieldType
        };

        if (string.Equals(fieldType, "stringList", StringComparison.OrdinalIgnoreCase))
        {
          IList<string> enumValues = schemaField.EnumValues ?? new List<string>();
          IList<string> selected = getStringListSelected?.Invoke(key) ?? new List<string>();
          foreach (string enumValue in enumValues)
          {
            if (string.IsNullOrWhiteSpace(enumValue))
              continue;
            field.ListItems.Add(new EnvironmentRecipePreconditionListItem
            {
              Value = enumValue,
              Label = enumValue,
              IsSelected = selected.Any(
                  x => string.Equals(x, enumValue, StringComparison.OrdinalIgnoreCase))
            });
          }

          if (applyNewDefaults)
            ApplyNewDocumentKindDefaults(field);
        }
        else
        {
          field.IsChecked = getBoolSelected != null && getBoolSelected(key);
        }

        target.Add(field);
      }
    }

    public static string BuildSummary(IEnumerable<EnvironmentRecipePreconditionField> fields)
    {
      if (fields == null)
        return string.Empty;

      var parts = new List<string>();
      foreach (EnvironmentRecipePreconditionField field in fields)
      {
        if (field == null)
          continue;

        if (field.IsBoolType)
        {
          if (field.IsChecked)
            parts.Add(field.Label);
          continue;
        }

        if (!field.IsStringListType || field.ListItems == null)
          continue;

        IList<string> selected = field.ListItems
            .Where(item => item != null && item.IsSelected)
            .Select(item => item.Label)
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .ToList();
        if (selected.Count == 0)
          continue;

        parts.Add(field.Label + ": " + string.Join(", ", selected));
      }

      return string.Join("; ", parts);
    }

    public static IList<EnvironmentRecipePreconditionField> CloneFields(
        IEnumerable<EnvironmentRecipePreconditionField> fields)
    {
      var clones = new List<EnvironmentRecipePreconditionField>();
      if (fields == null)
        return clones;

      foreach (EnvironmentRecipePreconditionField field in fields)
      {
        if (field == null)
          continue;
        var clone = new EnvironmentRecipePreconditionField
        {
          Key = field.Key,
          Label = field.Label,
          FieldType = field.FieldType,
          IsChecked = field.IsChecked
        };
        foreach (EnvironmentRecipePreconditionListItem item in field.ListItems)
        {
          if (item == null)
            continue;
          clone.ListItems.Add(new EnvironmentRecipePreconditionListItem
          {
            Value = item.Value,
            Label = item.Label,
            IsSelected = item.IsSelected
          });
        }
        clones.Add(clone);
      }
      return clones;
    }

    public static void ReplaceFields(
        ObservableCollection<EnvironmentRecipePreconditionField> target,
        IEnumerable<EnvironmentRecipePreconditionField> source)
    {
      if (target == null)
        return;
      target.Clear();
      foreach (EnvironmentRecipePreconditionField field in CloneFields(source))
        target.Add(field);
    }

    private static void ApplyNewDocumentKindDefaults(EnvironmentRecipePreconditionField field)
    {
      if (field?.ListItems == null || field.ListItems.Count == 0)
        return;
      if (!string.Equals(field.Key, DocumentKindsKey, StringComparison.OrdinalIgnoreCase))
        return;

      foreach (EnvironmentRecipePreconditionListItem item in field.ListItems)
      {
        if (item == null)
          continue;
        item.IsSelected = !string.Equals(item.Value, "drawing", StringComparison.OrdinalIgnoreCase);
      }
    }
  }
}
