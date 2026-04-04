namespace AIStudio.ViewModels.Research
{
  /// <summary>Элемент списка выбора сценария: в списке — укороченное название, ToolTip — полное.</summary>
  public sealed class ScenarioRegistryPickItem
  {
    public int Id { get; }
    public string FullTitle { get; }
    public string DisplayTitle { get; }

    public ScenarioRegistryPickItem(int id, string fullTitle)
    {
      Id = id;
      FullTitle = fullTitle ?? string.Empty;
      var t = FullTitle;
      DisplayTitle = t.Length <= 50 ? t : t.Substring(0, 50) + "…";
    }
  }
}
