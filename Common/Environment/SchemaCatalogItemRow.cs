namespace AIStudio.Common.SymbiontEnv
{
  /// <summary>Элемент каталога действий в SchemaActionPanel.</summary>
  public sealed class SchemaCatalogItemRow
  {
    public string Id { get; set; }
    public string Label { get; set; }
    public string Description { get; set; }

    public string DisplayText
    {
      get
      {
        if (string.IsNullOrWhiteSpace(Label))
          return Id ?? string.Empty;
        if (string.IsNullOrWhiteSpace(Id))
          return Label;
        return Label;
      }
    }
  }
}
