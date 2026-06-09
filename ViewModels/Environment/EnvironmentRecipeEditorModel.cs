using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>
  /// Модель детального редактора рецепта среды.
  /// </summary>
  public sealed class EnvironmentRecipeEditorModel
  {
    /// <summary>
    /// Создаёт пустую модель.
    /// </summary>
    public EnvironmentRecipeEditorModel()
    {
      Steps = new ObservableCollection<EnvironmentRecipeStepRow>();
      RecommendedTriggerInfluenceIds = new List<int>();
      ReactiveEligible = true;
    }

    /// <summary>Идентификатор рецепта.</summary>
    public string Id { get; set; }
    /// <summary>Отображаемое имя.</summary>
    public string DisplayName { get; set; }
    /// <summary>Описание.</summary>
    public string Description { get; set; }
    /// <summary>ID адаптивного действия.</summary>
    public int AdaptiveActionId { get; set; }
    /// <summary>Допускается реактивное исполнение.</summary>
    public bool ReactiveEligible { get; set; }
    /// <summary>Рекомендуемые ID воздействий (подсказка для настройки рефлексов).</summary>
    public List<int> RecommendedTriggerInfluenceIds { get; set; }
    /// <summary>Шаги рецепта.</summary>
    public ObservableCollection<EnvironmentRecipeStepRow> Steps { get; }
  }
}
