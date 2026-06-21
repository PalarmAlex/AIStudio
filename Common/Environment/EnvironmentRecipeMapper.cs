using System;
using System.Collections.Generic;
using System.Linq;
using AIStudio.Common.Adapters;
using AIStudio.ViewModels.SymbiontEnv;
using ISIDA.SymbiontEnv.Contract;

namespace AIStudio.Common.SymbiontEnv
{
  /// <summary>
  /// Преобразование моделей редактора и данных YAML.
  /// </summary>
  public static class EnvironmentRecipeMapper
  {
    /// <summary>Строка реестра.</summary>
    public static EnvironmentRecipeListItem ToListItem(EnvironmentRecipeData recipe, AdapterEnvironmentSchema schema)
    {
      if (recipe == null)
        return null;

      int stepCount = recipe.Steps?.Count ?? 0;
      int warnings = 0;
      if (schema != null && recipe.Steps != null)
      {
        foreach (EnvironmentRecipeStepData step in recipe.Steps)
        {
          if (step == null)
            continue;
          if (string.Equals(step.Type, EnvironmentRecipeStepSchemaHelper.StepTypeComment, StringComparison.OrdinalIgnoreCase))
            continue;
          if (string.IsNullOrWhiteSpace(step.Handler))
            warnings++;
          else if (!schema.Handlers.Any(h => string.Equals(h?.Id, step.Handler, StringComparison.OrdinalIgnoreCase)))
            warnings++;
        }
      }

      return new EnvironmentRecipeListItem
      {
        Id = recipe.Id,
        DisplayName = recipe.DisplayName,
        AdaptiveActionId = recipe.AdaptiveActionId,
        StepCount = stepCount,
        WarningCount = warnings
      };
    }

    /// <summary>Модель редактора.</summary>
    public static EnvironmentRecipeEditorModel ToEditorModel(EnvironmentRecipeData recipe)
    {
      if (recipe == null)
        return new EnvironmentRecipeEditorModel();
      var model = new EnvironmentRecipeEditorModel
      {
        Id = recipe.Id,
        DisplayName = recipe.DisplayName,
        Description = recipe.Description,
        AdaptiveActionId = recipe.AdaptiveActionId,
        ReactiveEligible = recipe.ReactiveEligible
      };
      foreach (EnvironmentRecipeStepData step in recipe.Steps)
      {
        var row = new EnvironmentRecipeStepRow();
        EnvironmentRecipeStepSchemaHelper.ApplyFromStepData(row, step);
        model.Steps.Add(row);
      }
      return model;
    }

    /// <summary>Данные для YAML.</summary>
    public static EnvironmentRecipeData ToData(EnvironmentRecipeEditorModel model)
    {
      if (model == null)
        throw new ArgumentNullException(nameof(model));
      var data = new EnvironmentRecipeData
      {
        Id = model.Id?.Trim() ?? string.Empty,
        AdaptiveActionId = model.AdaptiveActionId,
        DisplayName = model.DisplayName ?? string.Empty,
        Description = model.Description ?? string.Empty,
        ReactiveEligible = model.ReactiveEligible
      };
      foreach (EnvironmentRecipeStepRow row in model.Steps)
        data.Steps.Add(EnvironmentRecipeStepSchemaHelper.ToStepData(row));
      return data;
    }
  }
}
