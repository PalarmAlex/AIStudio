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
    /// <summary>Уровень риска (A/B/C).</summary>
    public string RiskTier { get; set; }
  }
}
