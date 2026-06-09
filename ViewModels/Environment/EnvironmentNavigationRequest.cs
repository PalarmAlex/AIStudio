namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>Запрос навигации между вкладками shell.</summary>
  public sealed class EnvironmentNavigationRequest
  {
    public EnvironmentShellTab Tab { get; set; }
    public string TriggerId { get; set; }
    public string RecipeId { get; set; }
    public int InfluenceActionId { get; set; }
    public int AdaptiveActionId { get; set; }
  }
}
