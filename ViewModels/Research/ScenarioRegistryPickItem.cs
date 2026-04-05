namespace AIStudio.ViewModels.Research
{
  /// <summary>Элемент списка выбора сценария в редакторе группы (полное название в списке и подсказке пункта).</summary>
  public sealed class ScenarioRegistryPickItem
  {
    public int Id { get; }
    public string FullTitle { get; }
    public string DisplayTitle { get; }

    public ScenarioRegistryPickItem(int id, string fullTitle)
    {
      Id = id;
      FullTitle = fullTitle ?? string.Empty;
      DisplayTitle = FullTitle;
    }
  }
}
