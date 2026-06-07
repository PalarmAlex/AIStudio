using System.Collections.Generic;
using System.Collections.ObjectModel;
using ISIDA.SymbiontEnv.Contract;

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
      PreconditionFields = new ObservableCollection<EnvironmentRecipePreconditionField>();
      RecommendedTriggerInfluenceIds = new List<int>();
      RiskTier = EnvironmentRecipeRiskTier.B;
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
    /// <summary>Уровень риска.</summary>
    public EnvironmentRecipeRiskTier RiskTier { get; set; }
    /// <summary>Допускается реактивное исполнение.</summary>
    public bool ReactiveEligible { get; set; }
    /// <summary>Рекомендуемые ID воздействий (триггеры).</summary>
    public List<int> RecommendedTriggerInfluenceIds { get; set; }
    /// <summary>Поля предусловий (из schema/recipe-preconditions.json).</summary>
    public ObservableCollection<EnvironmentRecipePreconditionField> PreconditionFields { get; }
    /// <summary>Шаги рецепта.</summary>
    public ObservableCollection<EnvironmentRecipeStepRow> Steps { get; }
    /// <summary>Метка лога после успеха.</summary>
    public string PostconditionLog { get; set; }
    /// <summary>Заметки для теста.</summary>
    public string TestNotes { get; set; }
    /// <summary>Текст рекомендуемых воздействий для отображения.</summary>
    public string RecommendedTriggersDisplay { get; set; }
  }
}
