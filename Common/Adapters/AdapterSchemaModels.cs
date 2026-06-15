using System.Collections.Generic;

namespace AIStudio.Common.Adapters
{
  /// <summary>Подсказки редактора для полей argsSchema (handlers-catalog.json).</summary>
  public static class AdapterSchemaEditorHints
  {
    public const string TemplatePlaceholder = "template_placeholder";
    public const string PropertyName = "property_name";
  }

  /// <summary>Допустимое значение enum-параметра schema (ключ + опциональная подпись).</summary>
  public sealed class AdapterSchemaArgValueOption
  {
    public string Key { get; set; }
    public string Label { get; set; }

    /// <summary>Текст для combobox: подпись или ключ, если подписи нет.</summary>
    public string Display =>
        string.IsNullOrWhiteSpace(Label) ? (Key ?? string.Empty) : Label;
  }

  /// <summary>Параметр argsSchema handler'а из handlers-catalog.json.</summary>
  public sealed class AdapterSchemaHandlerArg
  {
    public string Key { get; set; }
    public string Label { get; set; }
    public string Type { get; set; }
    public bool Required { get; set; }
    /// <summary>Опционально: template_placeholder, property_name (см. recipe-template-catalog.json).</summary>
    public string EditorHint { get; set; }
    public IList<AdapterSchemaArgValueOption> Values { get; set; }
  }

  /// <summary>Плейсхолдер шаблона значения из recipe-template-catalog.json.</summary>
  public sealed class AdapterSchemaTemplatePlaceholder
  {
    public string Token { get; set; }
    public string Label { get; set; }
    public string Description { get; set; }

    public string DisplayText
    {
      get
      {
        if (string.IsNullOrWhiteSpace(Token))
          return Label ?? string.Empty;
        if (string.IsNullOrWhiteSpace(Label))
          return Token;
        return Label + " (" + Token + ")";
      }
    }
  }

  /// <summary>Имя пользовательского свойства документа из recipe-template-catalog.json.</summary>
  public sealed class AdapterSchemaPropertyNameEntry
  {
    public string Name { get; set; }
    public string Label { get; set; }
    public string Description { get; set; }

    public string DisplayText
    {
      get
      {
        if (string.IsNullOrWhiteSpace(Name))
          return Label ?? string.Empty;
        if (string.IsNullOrWhiteSpace(Label) || string.Equals(Label, Name, System.StringComparison.Ordinal))
          return Name;
        return Label + " (" + Name + ")";
      }
    }
  }

  /// <summary>Справочники подстановок для редактора шагов рецепта.</summary>
  public sealed class AdapterSchemaRecipeTemplateCatalog
  {
    public IList<AdapterSchemaTemplatePlaceholder> Placeholders { get; set; } =
        new List<AdapterSchemaTemplatePlaceholder>();

    public IList<AdapterSchemaPropertyNameEntry> PropertyNames { get; set; } =
        new List<AdapterSchemaPropertyNameEntry>();
  }

  /// <summary>Handler invoke из handlers-catalog.json.</summary>
  public sealed class AdapterSchemaHandler
  {
    public string Id { get; set; }
    public string Label { get; set; }
    public string Description { get; set; }
    public IList<AdapterSchemaHandlerArg> ArgsSchema { get; set; } = new List<AdapterSchemaHandlerArg>();

    public string DisplayText
    {
      get
      {
        if (string.IsNullOrWhiteSpace(Id))
          return Label ?? string.Empty;
        if (string.IsNullOrWhiteSpace(Label))
          return Id;
        return Label + " (" + Id + ")";
      }
    }
  }

  /// <summary>Запись каталога рецептов из recipe-catalog.json.</summary>
  public sealed class AdapterSchemaRecipeCatalogEntry
  {
    public string Id { get; set; }
    public string Label { get; set; }
    public string Description { get; set; }

    public string DisplayText
    {
      get
      {
        if (string.IsNullOrWhiteSpace(Id))
          return Label ?? string.Empty;
        if (string.IsNullOrWhiteSpace(Label))
          return Id;
        return Label + " (" + Id + ")";
      }
    }
  }

  /// <summary>Параметр события из trigger-detect.json.</summary>
  public sealed class AdapterSchemaEventParameter
  {
    public string Key { get; set; }
    public string Label { get; set; }
    public string Type { get; set; }
    public bool Required { get; set; }
    public IList<AdapterSchemaArgValueOption> Values { get; set; }
  }

  /// <summary>Тип события из trigger-detect.json.</summary>
  public sealed class AdapterSchemaDetectKind
  {
    public string Kind { get; set; }
    public string Label { get; set; }
    public IList<AdapterSchemaEventParameter> Parameters { get; set; } = new List<AdapterSchemaEventParameter>();
  }

  /// <summary>Ключ пробы метрики среды из metric-probes.json.</summary>
  public sealed class AdapterSchemaMetricProbe
  {
    public string Key { get; set; }
    public string Label { get; set; }
    public string Description { get; set; }

    /// <summary>Текст для combobox в редакторе воздействий.</summary>
    public string DisplayText
    {
      get
      {
        if (string.IsNullOrWhiteSpace(Key))
          return Label ?? string.Empty;
        if (string.IsNullOrWhiteSpace(Label))
          return Key;
        return Label + " (" + Key + ")";
      }
    }

    /// <summary>Пустое значение — воздействие оператора (пульт), не среды.</summary>
    public static AdapterSchemaMetricProbe OperatorOnly { get; } = new AdapterSchemaMetricProbe
    {
      Key = string.Empty,
      Label = "(оператор, не среда)"
    };
  }

  /// <summary>Запись каталога триггеров из trigger-catalog.json.</summary>
  public sealed class AdapterSchemaTriggerCatalogEntry
  {
    public string Id { get; set; }
    public string Label { get; set; }
    public string Description { get; set; }

    public string DisplayText
    {
      get
      {
        if (string.IsNullOrWhiteSpace(Id))
          return Label ?? string.Empty;
        if (string.IsNullOrWhiteSpace(Label))
          return Id;
        return Label + " (" + Id + ")";
      }
    }
  }

  /// <summary>Schema пакета адаптера для редакторов среды.</summary>
  public sealed class AdapterEnvironmentSchema
  {
    public IList<AdapterSchemaHandler> Handlers { get; set; } = new List<AdapterSchemaHandler>();
    public IList<AdapterSchemaDetectKind> TriggerDetectKinds { get; set; } = new List<AdapterSchemaDetectKind>();
    public IList<AdapterSchemaMetricProbe> MetricProbes { get; set; } = new List<AdapterSchemaMetricProbe>();
    public IList<AdapterSchemaRecipeCatalogEntry> RecipeCatalog { get; set; } = new List<AdapterSchemaRecipeCatalogEntry>();
    public IList<AdapterSchemaTriggerCatalogEntry> TriggerCatalog { get; set; } = new List<AdapterSchemaTriggerCatalogEntry>();
    public AdapterSchemaRecipeTemplateCatalog RecipeTemplateCatalog { get; set; } =
        new AdapterSchemaRecipeTemplateCatalog();
  }
}
