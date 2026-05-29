using ISIDA.Common;
using ISIDA.Gomeostas;

namespace AIStudio.Common
{
  /// <summary>
  /// Стадия Creature для блокировки справочников (включая редактор Niche).
  /// </summary>
  public static class EditorStageAccess
  {
    /// <summary>
    /// Стадия, от которой зависит возможность редактирования справочника.
    /// Для Niche — стадия симбионта Creature, не локального агента среды.
    /// </summary>
    public static int ResolveEditingEvolutionStage(GomeostasSystem dataGomeostas, EditorSubjectScope scope)
    {
      if (scope != null && scope.IsEnvironment)
        return AppGlobalState.EvolutionStage;

      return dataGomeostas?.GetAgentState()?.EvolutionStage ?? 0;
    }

    public static bool IsStageZeroForEditing(int evolutionStage) => evolutionStage == 0;
  }
}
