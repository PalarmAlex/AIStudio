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
        DocumentKindPart = HasKind(recipe, EnvironmentDocumentKind.Part),
        DocumentKindAssembly = HasKind(recipe, EnvironmentDocumentKind.Assembly),
        DocumentKindDrawing = HasKind(recipe, EnvironmentDocumentKind.Drawing),
        NotSketchEdit = recipe.NotSketchEdit,
        NotReadOnly = recipe.NotReadOnly,
        PdmCheckoutRequired = recipe.PdmCheckoutRequired
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
        NotSketchEdit = model.NotSketchEdit,
        NotReadOnly = model.NotReadOnly,
        PdmCheckoutRequired = model.PdmCheckoutRequired,
        PostconditionLog = model.PostconditionLog ?? string.Empty,
        TestNotes = model.TestNotes ?? string.Empty,
        RecommendedTriggerInfluenceIds = model.RecommendedTriggerInfluenceIds?.ToList() ?? new List<int>()
      };
      if (model.DocumentKindPart)
        data.DocumentKinds.Add(EnvironmentDocumentKind.Part);
      if (model.DocumentKindAssembly)
        data.DocumentKinds.Add(EnvironmentDocumentKind.Assembly);
      if (model.DocumentKindDrawing)
        data.DocumentKinds.Add(EnvironmentDocumentKind.Drawing);
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

    private static bool HasKind(EnvironmentRecipeData recipe, EnvironmentDocumentKind kind)
    {
      if (recipe.DocumentKinds == null || recipe.DocumentKinds.Count == 0)
        return kind == EnvironmentDocumentKind.Part || kind == EnvironmentDocumentKind.Assembly;
      return recipe.DocumentKinds.Contains(kind);
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
