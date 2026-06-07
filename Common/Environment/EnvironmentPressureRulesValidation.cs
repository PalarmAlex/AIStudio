using System.Collections.Generic;
using System.Linq;

namespace AIStudio.Common.SymbiontEnv
{
  /// <summary>
  /// Проверки, связывающие рефлексы и правила давления среды.
  /// </summary>
  public static class EnvironmentPressureRulesValidation
  {
    /// <summary>
    /// Level3 рефлекса может ссылаться только на стимулы из <c>InfluenceActions.dat</c>, не на RuleId pressure rules.
    /// </summary>
    public static (bool IsValid, string ErrorMessage) ValidateLevel3NotPressureRuleIds(IEnumerable<int> level3Ids)
    {
      if (level3Ids == null)
        return (true, string.Empty);

      HashSet<int> pressureRuleIds = new HashSet<int>(
          EnvironmentPressureRulesStorage.Load().Select(r => r.RuleId));
      if (pressureRuleIds.Count == 0)
        return (true, string.Empty);

      List<int> invalid = level3Ids.Where(id => pressureRuleIds.Contains(id)).Distinct().ToList();
      if (invalid.Count == 0)
        return (true, string.Empty);

      return (false,
          "Level3 не может содержать RuleId правил давления среды (EnvironmentPressureRules.dat): " +
          string.Join(", ", invalid) +
          ". Используйте ID стимулов из InfluenceActions.dat.");
    }
  }
}
