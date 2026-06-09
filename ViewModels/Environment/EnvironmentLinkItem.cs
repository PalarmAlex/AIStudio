namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>Связь для панели «Связи» редакторов среды.</summary>
  public sealed class EnvironmentLinkItem
  {
    public string Category { get; set; }
    public string Title { get; set; }
    public string Detail { get; set; }
    public string TargetKind { get; set; }
    public string TargetId { get; set; }
    public bool HasGap { get; set; }
  }
}
