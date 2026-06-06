namespace AIStudio.Common.Adapters
{
  /// <summary>Элемент списка выбора адаптера в свойствах симбионта.</summary>
  public sealed class AdapterSelectionItem
  {
    /// <summary>Идентификатор пакета или null для «без адаптера».</summary>
    public string Id { get; set; }

    /// <summary>Текст в ComboBox.</summary>
    public string DisplayName { get; set; }

    /// <summary>Пункт «Без адаптера».</summary>
    public static AdapterSelectionItem None { get; } = new AdapterSelectionItem
    {
      Id = null,
      DisplayName = "(Без адаптера)"
    };
  }
}
