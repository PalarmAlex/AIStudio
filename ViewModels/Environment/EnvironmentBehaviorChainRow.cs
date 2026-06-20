namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>Строка цепочки на экране «Обзор поведения».</summary>
  public sealed class EnvironmentBehaviorChainRow
  {
    public string TriggerId { get; set; }
    public string TriggerTitle { get; set; }
    public string EventKind { get; set; }
    public int ReflexTriggerCommandPatternId { get; set; }
    public int AdaptiveActionId { get; set; }
    public string RecipeId { get; set; }
    public string RecipeTitle { get; set; }
    public int StepCount { get; set; }
    public bool HasGap { get; set; }
    public string StatusText { get; set; }
    public string DetailText { get; set; }
  }
}
