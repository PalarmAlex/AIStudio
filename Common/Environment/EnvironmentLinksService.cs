using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AIStudio.ViewModels.SymbiontEnv;
using ISIDA.Reflexes;
using ISIDA.SymbiontEnv.Contract;

namespace AIStudio.Common.SymbiontEnv
{
  /// <summary>Read-only сборка связей триггеров, рефлексов и рецептов (contract 3.1).</summary>
  public static class EnvironmentLinksService
  {
    public static IList<EnvironmentBehaviorChainRow> BuildChains(
        IList<EnvironmentTriggerData> triggers,
        IList<EnvironmentRecipeData> recipes,
        GeneticReflexesSystem geneticReflexes)
    {
      var chains = new List<EnvironmentBehaviorChainRow>();
      if (triggers == null)
        return chains;

      var recipesByAdaptive = new Dictionary<int, List<EnvironmentRecipeData>>();
      if (recipes != null)
      {
        foreach (EnvironmentRecipeData recipe in recipes)
        {
          if (recipe == null || recipe.AdaptiveActionId <= 0)
            continue;
          if (!recipesByAdaptive.TryGetValue(recipe.AdaptiveActionId, out List<EnvironmentRecipeData> list))
          {
            list = new List<EnvironmentRecipeData>();
            recipesByAdaptive[recipe.AdaptiveActionId] = list;
          }
          list.Add(recipe);
        }
      }

      foreach (EnvironmentTriggerData trigger in triggers.OrderBy(t => t?.Id ?? string.Empty, StringComparer.OrdinalIgnoreCase))
      {
        if (trigger == null || string.IsNullOrWhiteSpace(trigger.Id))
          continue;

        int commandPatternId = trigger.ReflexTriggerCommandPatternId;
        var adaptiveIds = FindAdaptiveActionIds(geneticReflexes, commandPatternId);
        EnvironmentRecipeData linkedRecipe = FindRecipe(recipesByAdaptive, adaptiveIds, trigger, recipes);
        bool hasGap = linkedRecipe == null;
        int stepCount = linkedRecipe?.Steps?.Count ?? 0;

        chains.Add(new EnvironmentBehaviorChainRow
        {
          TriggerId = trigger.Id,
          TriggerTitle = trigger.DisplayName ?? trigger.Id,
          EventKind = trigger.EventKind ?? string.Empty,
          ReflexTriggerCommandPatternId = commandPatternId,
          AdaptiveActionId = linkedRecipe?.AdaptiveActionId ?? (adaptiveIds.Count > 0 ? adaptiveIds[0] : 0),
          RecipeId = linkedRecipe?.Id ?? string.Empty,
          RecipeTitle = linkedRecipe?.DisplayName ?? string.Empty,
          StepCount = stepCount,
          HasGap = hasGap,
          StatusText = hasGap ? "⚠" : "✓",
          DetailText = BuildDetailText(trigger, linkedRecipe, adaptiveIds)
        });
      }

      return chains;
    }

    public static IList<EnvironmentLinkItem> BuildTriggerLinks(
        EnvironmentTriggerRow trigger,
        IList<EnvironmentRecipeData> recipes,
        GeneticReflexesSystem geneticReflexes)
    {
      var links = new List<EnvironmentLinkItem>();
      if (trigger == null)
        return links;

      links.Add(new EnvironmentLinkItem
      {
        Category = "Триггер",
        Title = trigger.DisplayName ?? trigger.Id,
        Detail = FormatMechanicalPath(trigger.HomeostasisDeltas)
      });

      if (trigger.ReflexTriggerCommandPatternId > 0)
      {
        links.Add(new EnvironmentLinkItem
        {
          Category = "Command",
          Title = trigger.ReflexTriggerCommandPatternId.ToString(CultureInfo.InvariantCulture),
          Detail = string.IsNullOrWhiteSpace(trigger.ReflexTriggerCommandPatternText)
              ? "паттерн genetic reflex (справочно)"
              : trigger.ReflexTriggerCommandPatternText
        });
      }

      var adaptiveIds = FindAdaptiveActionIds(geneticReflexes, trigger.ReflexTriggerCommandPatternId);
      foreach (int adaptiveId in adaptiveIds)
      {
        links.Add(new EnvironmentLinkItem
        {
          Category = "G_AD",
          Title = adaptiveId.ToString(CultureInfo.InvariantCulture),
          Detail = "через genetic reflex (command_pattern_ids)",
          TargetKind = "adaptive_action",
          TargetId = adaptiveId.ToString(CultureInfo.InvariantCulture)
        });
      }

      if (recipes != null)
      {
        foreach (EnvironmentRecipeData recipe in recipes)
        {
          if (recipe == null)
            continue;
          bool byReflex = adaptiveIds.Contains(recipe.AdaptiveActionId);
          bool byRecommended = recipe.RecommendedTriggerKeys != null
              && recipe.RecommendedTriggerKeys.Any(k =>
                  string.Equals(k, trigger.Id, StringComparison.OrdinalIgnoreCase));
          if (!byReflex && !byRecommended)
            continue;

          links.Add(new EnvironmentLinkItem
          {
            Category = "Рецепт",
            Title = recipe.DisplayName ?? recipe.Id,
            Detail = recipe.Id,
            TargetKind = "recipe",
            TargetId = recipe.Id
          });
        }
      }

      if (!links.Any(l => string.Equals(l.Category, "Рецепт", StringComparison.Ordinal)))
      {
        links.Add(new EnvironmentLinkItem
        {
          Category = "Разрыв",
          Title = "Нет рецепта",
          Detail = "Триггер не связан с G_AD через genetic reflex или recommended_trigger_keys",
          HasGap = true
        });
      }

      return links;
    }

    private static List<int> FindAdaptiveActionIds(GeneticReflexesSystem geneticReflexes, int commandPatternId)
    {
      var result = new List<int>();
      if (geneticReflexes == null || commandPatternId <= 0)
        return result;

      foreach (GeneticReflexesSystem.GeneticReflex reflex in geneticReflexes.GetAllGeneticReflexes())
      {
        if (reflex?.CommandPatternIds == null || !reflex.CommandPatternIds.Contains(commandPatternId))
          continue;
        if (reflex.AdaptiveActions == null)
          continue;
        foreach (int adaptiveId in reflex.AdaptiveActions)
        {
          if (adaptiveId > 0 && !result.Contains(adaptiveId))
            result.Add(adaptiveId);
        }
      }

      return result;
    }

    private static EnvironmentRecipeData FindRecipe(
        Dictionary<int, List<EnvironmentRecipeData>> recipesByAdaptive,
        List<int> adaptiveIds,
        EnvironmentTriggerData trigger,
        IList<EnvironmentRecipeData> recipes)
    {
      foreach (int adaptiveId in adaptiveIds)
      {
        if (!recipesByAdaptive.TryGetValue(adaptiveId, out List<EnvironmentRecipeData> list))
          continue;
        EnvironmentRecipeData match = list.FirstOrDefault();
        if (match != null)
          return match;
      }

      if (recipes == null || trigger == null)
        return null;

      return recipes.FirstOrDefault(r =>
          r != null
          && r.RecommendedTriggerKeys != null
          && r.RecommendedTriggerKeys.Any(k =>
              string.Equals(k, trigger.Id, StringComparison.OrdinalIgnoreCase)));
    }

    private static string BuildDetailText(
        EnvironmentTriggerData trigger,
        EnvironmentRecipeData recipe,
        List<int> adaptiveIds)
    {
      string eventText = trigger.EventKind ?? "?";
      string mechanicalText = FormatMechanicalPath(trigger.HomeostasisDeltas);
      string commandText = trigger.ReflexTriggerCommandPatternId > 0
          ? "cmd:" + trigger.ReflexTriggerCommandPatternId.ToString(CultureInfo.InvariantCulture)
          : string.Empty;

      string path = eventText;
      if (!string.IsNullOrWhiteSpace(mechanicalText))
        path += " → Δ[" + mechanicalText + "]";
      if (!string.IsNullOrWhiteSpace(commandText))
        path += " → " + commandText;

      if (recipe != null)
        return path + " → G_AD " + recipe.AdaptiveActionId + " → " + recipe.Id;

      if (adaptiveIds.Count > 0)
        return path + " → G_AD " + adaptiveIds[0] + " → (нет рецепта)";

      return path + " → (нет genetic reflex / рецепта)";
    }

    private static string FormatMechanicalPath(IList<HomeostasisDeltaEntry> deltas)
    {
      if (deltas == null || deltas.Count == 0)
        return string.Empty;

      return string.Join("; ", deltas
          .Where(d => d != null && d.ParameterId > 0 && d.Delta != 0)
          .OrderBy(d => d.ParameterId)
          .Select(d => d.ParameterId + ":" + d.Delta.ToString(CultureInfo.InvariantCulture)));
    }

    private static string FormatMechanicalPath(IDictionary<int, int> deltas)
    {
      if (deltas == null || deltas.Count == 0)
        return string.Empty;

      return string.Join("; ", deltas
          .Where(kv => kv.Key > 0 && kv.Value != 0)
          .OrderBy(kv => kv.Key)
          .Select(kv => kv.Key + ":" + kv.Value.ToString(CultureInfo.InvariantCulture)));
    }
  }
}
