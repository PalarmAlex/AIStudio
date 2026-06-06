using System.Collections.Generic;

namespace AIStudio.Common.Adapters
{
  /// <summary>Поле preconditions/trigger-filter из schema JSON.</summary>
  public sealed class AdapterSchemaField
  {
    public string Key { get; set; }
    public string Label { get; set; }
    public string Type { get; set; }
    public IList<string> EnumValues { get; set; }
    public bool Required { get; set; }
  }

  /// <summary>Тип шага рецепта из recipe-steps.json.</summary>
  public sealed class AdapterSchemaStepType
  {
    public string Type { get; set; }
    public string Label { get; set; }
  }

  /// <summary>Kind detect из trigger-detect.json.</summary>
  public sealed class AdapterSchemaDetectKind
  {
    public string Kind { get; set; }
    public string Label { get; set; }
  }

  /// <summary>Schema пакета адаптера для редакторов среды.</summary>
  public sealed class AdapterEnvironmentSchema
  {
    public IList<AdapterSchemaField> RecipePreconditions { get; set; } = new List<AdapterSchemaField>();
    public IList<AdapterSchemaStepType> RecipeStepTypes { get; set; } = new List<AdapterSchemaStepType>();
    public IList<AdapterSchemaField> TriggerFilterFields { get; set; } = new List<AdapterSchemaField>();
    public IList<AdapterSchemaDetectKind> TriggerDetectKinds { get; set; } = new List<AdapterSchemaDetectKind>();

    public bool HasRecipePrecondition(string key)
    {
      if (string.IsNullOrWhiteSpace(key) || RecipePreconditions == null)
        return false;

      for (int i = 0; i < RecipePreconditions.Count; i++)
      {
        if (string.Equals(RecipePreconditions[i]?.Key, key, System.StringComparison.OrdinalIgnoreCase))
          return true;
      }

      return false;
    }

    public bool HasDocumentKind(string kind)
    {
      if (RecipePreconditions == null || string.IsNullOrWhiteSpace(kind))
        return false;

      for (int i = 0; i < RecipePreconditions.Count; i++)
      {
        AdapterSchemaField field = RecipePreconditions[i];
        if (!string.Equals(field?.Key, "document_kinds", System.StringComparison.OrdinalIgnoreCase))
          continue;

        if (field.EnumValues == null || field.EnumValues.Count == 0)
          return true;

        for (int j = 0; j < field.EnumValues.Count; j++)
        {
          if (string.Equals(field.EnumValues[j], kind, System.StringComparison.OrdinalIgnoreCase))
            return true;
        }
      }

      return false;
    }
  }
}
