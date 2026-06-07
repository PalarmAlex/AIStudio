using AIStudio.Common.Adapters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>
  /// Синхронизация шагов рецепта со schema/recipe-steps.json.
  /// </summary>
  public static class EnvironmentRecipeStepSchemaHelper
  {
    public static readonly IReadOnlyList<string> KnownTemplatePlaceholders = new[]
    {
      "{DESCRIPTION}",
      "{PROJECT}",
      "{DISCIPLINE}",
      "{SEQ}",
      "{SEQ:4}",
      "{DOCUMENT_PATH}",
      "{FILE_NAME}",
      "{FILE_NAME_WITHOUT_EXT}",
      "{DOCUMENT_TITLE}"
    };

    public static readonly IReadOnlyList<string> KnownPropertyNames = new[]
    {
      "Обозначение",
      "Наименование",
      "Материал",
      "Разработал",
      "Проверил"
    };

    public static void InitializeAllSteps(
        IEnumerable<EnvironmentRecipeStepRow> steps,
        AdapterEnvironmentSchema schema)
    {
      if (steps == null)
        return;
      foreach (EnvironmentRecipeStepRow step in steps)
        InitializeStepRow(step, schema);
    }

    public static void InitializeStepRow(EnvironmentRecipeStepRow step, AdapterEnvironmentSchema schema)
    {
      if (step == null)
        return;
      Dictionary<string, string> existing = ParseParametersText(step.ParametersText);
      ApplyStepType(step, step.StepType, schema, existing);
    }

    public static void ApplyStepType(
        EnvironmentRecipeStepRow step,
        string stepType,
        AdapterEnvironmentSchema schema,
        IDictionary<string, string> preservedValues = null)
    {
      if (step == null)
        return;

      step.StepType = stepType ?? string.Empty;
      step.ParameterFields.Clear();

      AdapterSchemaStepType schemaType = FindStepType(schema, step.StepType);
      if (schemaType?.Parameters == null || schemaType.Parameters.Count == 0)
      {
        step.Summary = BuildSummary(step.StepType, preservedValues);
        return;
      }

      var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      if (preservedValues != null)
      {
        foreach (KeyValuePair<string, string> kv in preservedValues)
          values[kv.Key] = kv.Value ?? string.Empty;
      }

      foreach (AdapterSchemaStepParameter parameter in schemaType.Parameters)
      {
        if (parameter == null || string.IsNullOrWhiteSpace(parameter.Key))
          continue;

        string key = parameter.Key.Trim();
        if (!values.TryGetValue(key, out string currentValue))
          currentValue = string.Empty;

        var field = new EnvironmentRecipeStepParameterField
        {
          Key = key,
          Label = string.IsNullOrWhiteSpace(parameter.Label) ? key : parameter.Label,
          FieldType = parameter.Type ?? "string",
          Required = parameter.Required,
          EnumValues = parameter.Values?.ToList() ?? new List<string>(),
          Value = currentValue
        };
        field.PropertyChanged += (_, args) =>
        {
          if (args.PropertyName == nameof(EnvironmentRecipeStepParameterField.Value))
            RefreshSummary(step);
        };
        step.ParameterFields.Add(field);
      }

      step.Summary = BuildSummary(step.StepType, ToParametersDictionary(step));
    }

    public static EnvironmentRecipeStepRow CreateDefaultStep(AdapterEnvironmentSchema schema)
    {
      AdapterSchemaStepType first = GetStepTypes(schema).FirstOrDefault();
      if (first == null || string.IsNullOrWhiteSpace(first.Type))
        return new EnvironmentRecipeStepRow();
      var row = new EnvironmentRecipeStepRow { StepType = first.Type };
      ApplyStepType(row, first.Type, schema);
      return row;
    }

    public static Dictionary<string, string> ToParametersDictionary(EnvironmentRecipeStepRow step)
    {
      var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      if (step?.ParameterFields == null)
        return dict;
      foreach (EnvironmentRecipeStepParameterField field in step.ParameterFields)
      {
        if (field == null || string.IsNullOrWhiteSpace(field.Key))
          continue;
        dict[field.Key] = field.Value ?? string.Empty;
      }
      return dict;
    }

    public static void RefreshSummary(EnvironmentRecipeStepRow step)
    {
      if (step == null)
        return;
      step.Summary = BuildSummary(step.StepType, ToParametersDictionary(step));
    }

    public static string ValidateSteps(
        IEnumerable<EnvironmentRecipeStepRow> steps,
        AdapterEnvironmentSchema schema)
    {
      if (steps == null)
        return string.Empty;

      int index = 0;
      foreach (EnvironmentRecipeStepRow step in steps)
      {
        index++;
        if (step == null || string.IsNullOrWhiteSpace(step.StepType))
          return "Шаг " + index + ": укажите тип шага.";

        AdapterSchemaStepType schemaType = FindStepType(schema, step.StepType);
        if (schemaType == null)
          return "Шаг " + index + ": неизвестный тип \"" + step.StepType +
                 "\". Укажите тип из schema/recipe-steps.json пакета адаптера.";
        if (schemaType.Parameters == null)
          continue;

        foreach (AdapterSchemaStepParameter parameter in schemaType.Parameters)
        {
          if (parameter == null || !parameter.Required || string.IsNullOrWhiteSpace(parameter.Key))
            continue;

          EnvironmentRecipeStepParameterField field = step.ParameterFields
              .FirstOrDefault(f => string.Equals(f?.Key, parameter.Key, StringComparison.OrdinalIgnoreCase));
          if (field == null || string.IsNullOrWhiteSpace(field.Value))
          {
            string label = string.IsNullOrWhiteSpace(parameter.Label) ? parameter.Key : parameter.Label;
            return "Шаг " + index + " (" + step.StepType + "): заполните поле \"" + label + "\".";
          }
        }
      }

      return string.Empty;
    }

    public static IEnumerable<AdapterSchemaStepType> GetStepTypes(AdapterEnvironmentSchema schema)
    {
      if (schema?.RecipeStepTypes == null || schema.RecipeStepTypes.Count == 0)
        return Enumerable.Empty<AdapterSchemaStepType>();
      return schema.RecipeStepTypes.Where(s => !string.IsNullOrWhiteSpace(s?.Type));
    }

    private static AdapterSchemaStepType FindStepType(AdapterEnvironmentSchema schema, string stepType)
    {
      if (string.IsNullOrWhiteSpace(stepType))
        return null;
      return GetStepTypes(schema)
          .FirstOrDefault(s => string.Equals(s.Type, stepType, StringComparison.OrdinalIgnoreCase));
    }

    private static Dictionary<string, string> ParseParametersText(string text)
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

    private static string BuildSummary(string stepType, IDictionary<string, string> parameters)
    {
      if (parameters == null || parameters.Count == 0)
        return stepType ?? string.Empty;

      var parts = new List<string>();
      foreach (KeyValuePair<string, string> kv in parameters.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
      {
        if (string.IsNullOrWhiteSpace(kv.Value))
          continue;
        parts.Add(kv.Key + "=" + kv.Value);
      }

      if (parts.Count == 0)
        return stepType ?? string.Empty;

      string joined = string.Join(", ", parts);
      if (joined.Length > 120)
        joined = joined.Substring(0, 117) + "...";
      return joined;
    }
  }
}
