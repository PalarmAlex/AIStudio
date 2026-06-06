namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>
  /// Строка шага рецепта в редакторе.
  /// </summary>
  public sealed class EnvironmentRecipeStepRow
  {
    /// <summary>Тип шага.</summary>
    public string StepType { get; set; }
    /// <summary>Параметры (key=value по строкам).</summary>
    public string ParametersText { get; set; }
  }
}
