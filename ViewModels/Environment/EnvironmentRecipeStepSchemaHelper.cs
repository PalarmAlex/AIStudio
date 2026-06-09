using AIStudio.Common.Adapters;
using ISIDA.SymbiontEnv.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>
  /// Шаги рецепта: <c>invoke</c> (flat args) и <c>comment</c>.
  /// </summary>
  public static class EnvironmentRecipeStepSchemaHelper
  {
    public const string StepTypeInvoke = "invoke";
    public const string StepTypeComment = "comment";

    public static void InitializeAllSteps(
        IEnumerable<EnvironmentRecipeStepRow> steps,
        AdapterEnvironmentSchema schema)
    {
      if (steps == null)
        return;
      foreach (EnvironmentRecipeStepRow step in steps)
        RefreshSummary(step, schema);
    }

    public static EnvironmentRecipeStepRow CreateDefaultInvokeStep(AdapterEnvironmentSchema schema)
    {
      var row = new EnvironmentRecipeStepRow { StepKind = StepTypeInvoke };
      AdapterSchemaHandler firstHandler = schema?.Handlers?.FirstOrDefault(h => !string.IsNullOrWhiteSpace(h?.Id));
      if (firstHandler != null)
        row.HandlerId = firstHandler.Id;
      RefreshSummary(row, schema);
      return row;
    }

    public static EnvironmentRecipeStepRow CreateDefaultCommentStep()
    {
      return new EnvironmentRecipeStepRow
      {
        StepKind = StepTypeComment,
        CommentText = "Комментарий"
      };
    }

    public static void ApplyFromStepData(EnvironmentRecipeStepRow row, EnvironmentRecipeStepData step)
    {
      if (row == null)
        return;

      string type = (step?.Type ?? StepTypeInvoke).Trim().ToLowerInvariant();
      if (string.Equals(type, StepTypeComment, StringComparison.Ordinal))
      {
        row.StepKind = StepTypeComment;
        row.CommentText = step?.Text ?? string.Empty;
        row.HandlerId = string.Empty;
        row.Args.Clear();
        return;
      }

      row.StepKind = StepTypeInvoke;
      row.HandlerId = step?.Handler ?? string.Empty;
      row.CommentText = string.Empty;
      row.Args.Clear();
      if (step?.Args != null)
      {
        foreach (KeyValuePair<string, string> kv in step.Args)
          row.Args[kv.Key] = kv.Value;
      }
    }

    public static EnvironmentRecipeStepData ToStepData(EnvironmentRecipeStepRow row)
    {
      if (row == null)
        return new EnvironmentRecipeStepData { Type = StepTypeInvoke };

      if (row.IsComment)
      {
        return new EnvironmentRecipeStepData
        {
          Type = StepTypeComment,
          Text = row.CommentText ?? string.Empty
        };
      }

      var data = new EnvironmentRecipeStepData
      {
        Type = StepTypeInvoke,
        Handler = row.HandlerId ?? string.Empty
      };
      foreach (KeyValuePair<string, string> kv in row.Args)
        data.Args[kv.Key] = kv.Value;
      return data;
    }

    public static void ApplyArgsFromDictionary(EnvironmentRecipeStepRow row, IDictionary<string, string> values)
    {
      if (row == null)
        return;
      row.Args.Clear();
      if (values == null)
        return;
      foreach (KeyValuePair<string, string> kv in values)
        row.Args[kv.Key] = kv.Value ?? string.Empty;
    }

    public static void RefreshSummary(EnvironmentRecipeStepRow row, AdapterEnvironmentSchema schema)
    {
      if (row == null)
        return;

      if (row.IsComment)
      {
        row.Summary = string.IsNullOrWhiteSpace(row.CommentText) ? "(комментарий)" : row.CommentText;
        return;
      }

      string handlerId = (row.HandlerId ?? string.Empty).Trim();
      if (handlerId.Length == 0)
      {
        row.Summary = string.Empty;
        return;
      }

      AdapterSchemaHandler handler = schema?.Handlers?
          .FirstOrDefault(h => string.Equals(h?.Id, handlerId, StringComparison.OrdinalIgnoreCase));

      var parts = new List<string>();
      if (handler?.ArgsSchema != null)
      {
        foreach (AdapterSchemaHandlerArg arg in handler.ArgsSchema.Take(3))
        {
          if (arg == null || string.IsNullOrWhiteSpace(arg.Key))
            continue;
          if (row.Args.TryGetValue(arg.Key, out string value) && !string.IsNullOrWhiteSpace(value))
          {
            string label = string.IsNullOrWhiteSpace(arg.Label) ? arg.Key : arg.Label;
            parts.Add(label + ": " + value);
          }
        }
      }

      if (parts.Count > 0)
      {
        row.Summary = string.Join("; ", parts);
        return;
      }

      if (handler != null && !string.IsNullOrWhiteSpace(handler.Label))
      {
        row.Summary = handler.Label;
        return;
      }

      row.Summary = handlerId;
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
        if (step == null)
          return "Шаг " + index + ": пустая строка.";

        if (step.IsComment)
          continue;

        if (string.IsNullOrWhiteSpace(step.HandlerId))
          return "Шаг " + index + ": укажите handler.";

        if (!IsKnownHandler(schema, step.HandlerId))
          return "Шаг " + index + ": неизвестный handler \"" + step.HandlerId + "\".";

        string argsError = ValidateHandlerArgs(schema, step.HandlerId, step.Args);
        if (!string.IsNullOrWhiteSpace(argsError))
          return "Шаг " + index + " (" + step.HandlerId + "): " + argsError;
      }

      return string.Empty;
    }

    public static int CountValidationIssues(
        IEnumerable<EnvironmentRecipeStepRow> steps,
        AdapterEnvironmentSchema schema)
    {
      if (steps == null)
        return 0;
      int count = 0;
      foreach (EnvironmentRecipeStepRow step in steps)
      {
        if (step == null)
        {
          count++;
          continue;
        }
        if (step.IsComment)
          continue;
        if (string.IsNullOrWhiteSpace(step.HandlerId) || !IsKnownHandler(schema, step.HandlerId))
          count++;
        else if (!string.IsNullOrWhiteSpace(ValidateHandlerArgs(schema, step.HandlerId, step.Args)))
          count++;
      }
      return count;
    }

    public static IEnumerable<AdapterSchemaHandler> GetHandlers(AdapterEnvironmentSchema schema)
    {
      if (schema?.Handlers == null || schema.Handlers.Count == 0)
        return Enumerable.Empty<AdapterSchemaHandler>();
      return schema.Handlers.Where(h => !string.IsNullOrWhiteSpace(h?.Id));
    }

    private static bool IsKnownHandler(AdapterEnvironmentSchema schema, string handlerId)
    {
      if (string.IsNullOrWhiteSpace(handlerId) || schema?.Handlers == null)
        return false;
      return schema.Handlers.Any(h => string.Equals(h?.Id, handlerId.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string ValidateHandlerArgs(
        AdapterEnvironmentSchema schema,
        string handlerId,
        IDictionary<string, string> args)
    {
      AdapterSchemaHandler handler = schema?.Handlers?
          .FirstOrDefault(h => string.Equals(h?.Id, handlerId?.Trim(), StringComparison.OrdinalIgnoreCase));
      if (handler?.ArgsSchema == null || handler.ArgsSchema.Count == 0)
        return string.Empty;

      var argValues = args ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      foreach (AdapterSchemaHandlerArg arg in handler.ArgsSchema)
      {
        if (arg == null || !arg.Required || string.IsNullOrWhiteSpace(arg.Key))
          continue;
        if (!argValues.TryGetValue(arg.Key, out string value) || string.IsNullOrWhiteSpace(value))
        {
          string label = string.IsNullOrWhiteSpace(arg.Label) ? arg.Key : arg.Label;
          return "заполните аргумент \"" + label + "\".";
        }
      }

      return string.Empty;
    }
  }
}
