using System;
using System.Collections.Generic;
using System.Linq;
using AIStudio.ViewModels.SymbiontEnv;
using ISIDA.SymbiontEnv.Contract;

namespace AIStudio.Common.SymbiontEnv
{
  /// <summary>
  /// Преобразование моделей редактора и данных YAML.
  /// </summary>
  public static class EnvironmentRecipeMapper
  {
    private const string KindPart = "part";
    private const string KindAssembly = "assembly";
    private const string KindDrawing = "drawing";

    /// <summary>Строка реестра.</summary>
    public static EnvironmentRecipeListItem ToListItem(EnvironmentRecipeData recipe)
    {
      if (recipe == null)
        return null;
      return new EnvironmentRecipeListItem
      {
        Id = recipe.Id,
        DisplayName = recipe.DisplayName,
        AdaptiveActionId = recipe.AdaptiveActionId,
        RiskTier = recipe.RiskTier.ToString()
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
        RiskTier = recipe.RiskTier == EnvironmentRecipeRiskTier.Unknown
            ? EnvironmentRecipeRiskTier.B
            : recipe.RiskTier,
        ReactiveEligible = recipe.ReactiveEligible,
        PostconditionLog = recipe.PostconditionLog,
        TestNotes = recipe.TestNotes,
        RecommendedTriggerInfluenceIds = new List<int>(recipe.RecommendedTriggerInfluenceIds),
        DocumentKindPart = HasKind(recipe, KindPart),
        DocumentKindAssembly = HasKind(recipe, KindAssembly),
        DocumentKindDrawing = HasKind(recipe, KindDrawing),
        NotSketchEdit = GetPreconditionBool(recipe.Preconditions, "not_sketch_edit"),
        NotReadOnly = GetPreconditionBool(recipe.Preconditions, "not_read_only"),
        PdmCheckoutRequired = GetPreconditionBool(recipe.Preconditions, "pdm_checkout_required")
      };
      foreach (EnvironmentRecipeStepData step in recipe.Steps)
      {
        model.Steps.Add(new EnvironmentRecipeStepRow
        {
          StepType = step?.Type ?? string.Empty,
          ParametersText = FormatParameters(step?.Parameters)
        });
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
        RiskTier = model.RiskTier,
        ReactiveEligible = model.ReactiveEligible,
        PostconditionLog = model.PostconditionLog ?? string.Empty,
        TestNotes = model.TestNotes ?? string.Empty,
        RecommendedTriggerInfluenceIds = model.RecommendedTriggerInfluenceIds?.ToList() ?? new List<int>()
      };
      if (model.NotSketchEdit)
        data.Preconditions["not_sketch_edit"] = true;
      if (model.NotReadOnly)
        data.Preconditions["not_read_only"] = true;
      if (model.PdmCheckoutRequired)
        data.Preconditions["pdm_checkout_required"] = true;
      if (model.DocumentKindPart)
        data.DocumentKinds.Add(KindPart);
      if (model.DocumentKindAssembly)
        data.DocumentKinds.Add(KindAssembly);
      if (model.DocumentKindDrawing)
        data.DocumentKinds.Add(KindDrawing);
      foreach (EnvironmentRecipeStepRow row in model.Steps)
      {
        data.Steps.Add(new EnvironmentRecipeStepData
        {
          Type = row?.StepType ?? string.Empty,
          Parameters = new Dictionary<string, string>(ParseParameters(row?.ParametersText), StringComparer.OrdinalIgnoreCase)
        });
      }
      return data;
    }

    private static bool HasKind(EnvironmentRecipeData recipe, string kind)
    {
      if (recipe.DocumentKinds == null || recipe.DocumentKinds.Count == 0)
        return string.Equals(kind, KindPart, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(kind, KindAssembly, StringComparison.OrdinalIgnoreCase);
      return recipe.DocumentKinds.Any(
          x => string.Equals(x, kind, StringComparison.OrdinalIgnoreCase));
    }

    private static bool GetPreconditionBool(IDictionary<string, bool> preconditions, string key)
    {
      if (preconditions == null)
        return false;
      return preconditions.TryGetValue(key, out bool value) && value;
    }

    private static string FormatParameters(Dictionary<string, string> parameters)
    {
      if (parameters == null || parameters.Count == 0)
        return string.Empty;
      return string.Join(
          System.Environment.NewLine,
          parameters.Select(kv => kv.Key + "=" + kv.Value));
    }

    private static Dictionary<string, string> ParseParameters(string text)
    {
      var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      if (string.IsNullOrWhiteSpace(text))
        return dict;
      foreach (string line in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
      {
        int eq = line.IndexOf('=');
        if (eq <= 0)
          continue;
        string key = line.Substring(0, eq).Trim();
        string value = line.Substring(eq + 1).Trim();
        if (key.Length > 0)
          dict[key] = value;
      }
      return dict;
    }
  }
}
