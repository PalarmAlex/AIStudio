namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>
  /// Строка реестра рецептов среды.
  /// </summary>
  public sealed class EnvironmentRecipeListItem
  {
    /// <summary>Идентификатор рецепта.</summary>
    public string Id { get; set; }
    /// <summary>Отображаемое имя.</summary>
    public string DisplayName { get; set; }
    /// <summary>ID адаптивного действия.</summary>
    public int AdaptiveActionId { get; set; }
    /// <summary>Число шагов.</summary>
    public int StepCount { get; set; }
    /// <summary>Число предупреждений валидации.</summary>
    public int WarningCount { get; set; }
    /// <summary>Текст предупреждения для таблицы.</summary>
    public string WarningText => WarningCount > 0 ? "⚠" : string.Empty;
  }
}
