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
      RecommendedTriggerKeys = new List<string>();
      ReactiveEligible = true;
    }

    /// <summary>Идентификатор рецепта.</summary>
    public string Id { get; set; }
    /// <summary>Отображаемое имя.</summary>
    public string DisplayName { get; set; }
    /// <summary>Описание.</summary>
    public string Description { get; set; }
    /// <summary>ID моторного действия.</summary>
    public int AdaptiveActionId { get; set; }
    /// <summary>Допускается реактивное исполнение.</summary>
    public bool ReactiveEligible { get; set; }
    /// <summary>Рекомендуемые ключи триггеров (<c>EnvironmentTriggers.yaml</c>).</summary>
    public List<string> RecommendedTriggerKeys { get; set; }
    /// <summary>Шаги рецепта.</summary>
    public ObservableCollection<EnvironmentRecipeStepRow> Steps { get; }
  }
}
