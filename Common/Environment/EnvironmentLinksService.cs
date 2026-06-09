using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AIStudio.Common.Adapters;
using AIStudio.ViewModels.SymbiontEnv;
using ISIDA.Reflexes;
using ISIDA.SymbiontEnv.Contract;

namespace AIStudio.Common.SymbiontEnv
{
  /// <summary>Read-only сборка связей триггеров, рефлексов и рецептов.</summary>
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

        int eaId = trigger.InfluenceActionId;
        var adaptiveIds = FindAdaptiveActionIds(geneticReflexes, eaId);
        EnvironmentRecipeData linkedRecipe = FindRecipe(recipesByAdaptive, adaptiveIds, trigger, recipes);
        bool hasGap = linkedRecipe == null;
        int stepCount = linkedRecipe?.Steps?.Count ?? 0;

        chains.Add(new EnvironmentBehaviorChainRow
        {
          TriggerId = trigger.Id,
          TriggerTitle = trigger.DisplayName ?? trigger.Id,
          EventKind = trigger.EventKind ?? string.Empty,
          InfluenceActionId = eaId,
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
        Detail = "EA " + trigger.InfluenceActionId.ToString(CultureInfo.InvariantCulture)
      });

      var adaptiveIds = FindAdaptiveActionIds(geneticReflexes, trigger.InfluenceActionId);
      foreach (int adaptiveId in adaptiveIds)
      {
        links.Add(new EnvironmentLinkItem
        {
          Category = "G_AD",
          Title = adaptiveId.ToString(CultureInfo.InvariantCulture),
          Detail = "через рефлекс",
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
          bool byRecommended = recipe.RecommendedTriggerInfluenceIds != null
              && recipe.RecommendedTriggerInfluenceIds.Contains(trigger.InfluenceActionId);
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
          Detail = "EA " + trigger.InfluenceActionId + " не связан с G_AD через рефлекс",
          HasGap = true
        });
      }

      return links;
    }

    public static IList<EnvironmentLinkItem> BuildRecipeLinks(
        EnvironmentRecipeEditorModel model,
        IList<EnvironmentTriggerData> triggers,
        GeneticReflexesSystem geneticReflexes,
        AdapterEnvironmentSchema schema)
    {
      var links = new List<EnvironmentLinkItem>();
      if (model == null)
        return links;

      links.Add(new EnvironmentLinkItem
      {
        Category = "G_AD",
        Title = model.AdaptiveActionId.ToString(CultureInfo.InvariantCulture),
        Detail = model.DisplayName ?? model.Id
      });

      if (model.RecommendedTriggerInfluenceIds != null)
      {
        foreach (int eaId in model.RecommendedTriggerInfluenceIds)
        {
          EnvironmentTriggerData trigger = triggers?.FirstOrDefault(
              t => t != null && t.InfluenceActionId == eaId);
          links.Add(new EnvironmentLinkItem
          {
            Category = "EA (рекомендуемый)",
            Title = eaId.ToString(CultureInfo.InvariantCulture),
            Detail = trigger != null ? trigger.DisplayName ?? trigger.Id : "триггер не найден",
            TargetKind = trigger != null ? "trigger" : null,
            TargetId = trigger?.Id,
            HasGap = trigger == null
          });
        }
      }

      if (geneticReflexes != null && model.AdaptiveActionId > 0)
      {
        foreach (GeneticReflexesSystem.GeneticReflex reflex in geneticReflexes.GetAllGeneticReflexes())
        {
          if (reflex?.AdaptiveActions == null || !reflex.AdaptiveActions.Contains(model.AdaptiveActionId))
            continue;
          links.Add(new EnvironmentLinkItem
          {
            Category = "Рефлекс",
            Title = "ID " + reflex.Id.ToString(CultureInfo.InvariantCulture),
            Detail = "Level3: " + string.Join(", ", reflex.Level3 ?? new List<int>()),
            TargetKind = "reflex",
            TargetId = reflex.Id.ToString(CultureInfo.InvariantCulture)
          });
        }
      }

      if (model.Steps != null && schema?.Handlers != null)
      {
        var knownHandlers = new HashSet<string>(
            schema.Handlers.Select(h => h?.Id).Where(id => !string.IsNullOrWhiteSpace(id)),
            StringComparer.OrdinalIgnoreCase);
        foreach (EnvironmentRecipeStepRow step in model.Steps)
        {
          if (step == null || !string.Equals(step.StepKind, EnvironmentRecipeStepSchemaHelper.StepTypeInvoke, StringComparison.OrdinalIgnoreCase))
            continue;
          if (!knownHandlers.Contains(step.HandlerId ?? string.Empty))
          {
            links.Add(new EnvironmentLinkItem
            {
              Category = "Handler",
              Title = step.HandlerId,
              Detail = "не найден в handlers-catalog.json",
              HasGap = true
            });
          }
        }
      }

      return links;
    }

    private static List<int> FindAdaptiveActionIds(GeneticReflexesSystem geneticReflexes, int influenceActionId)
    {
      var result = new List<int>();
      if (geneticReflexes == null || influenceActionId <= 0)
        return result;

      foreach (GeneticReflexesSystem.GeneticReflex reflex in geneticReflexes.GetAllGeneticReflexes())
      {
        if (reflex?.Level3 == null || !reflex.Level3.Contains(influenceActionId))
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
          && r.RecommendedTriggerInfluenceIds != null
          && r.RecommendedTriggerInfluenceIds.Contains(trigger.InfluenceActionId));
    }

    private static string BuildDetailText(
        EnvironmentTriggerData trigger,
        EnvironmentRecipeData recipe,
        List<int> adaptiveIds)
    {
      string eventText = trigger.EventKind ?? "?";
      string eaText = "EA " + trigger.InfluenceActionId.ToString(CultureInfo.InvariantCulture);
      if (recipe != null)
        return eventText + " → " + eaText + " → G_AD " + recipe.AdaptiveActionId + " → " + recipe.Id;

      if (adaptiveIds.Count > 0)
        return eventText + " → " + eaText + " → G_AD " + adaptiveIds[0] + " → (нет рецепта)";

      return eventText + " → " + eaText + " → (нет рефлекса/рецепта)";
    }
  }
}
