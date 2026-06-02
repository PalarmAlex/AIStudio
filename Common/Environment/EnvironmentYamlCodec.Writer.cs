using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace AIStudio.Common.SymbiontEnv
{
  public static partial class EnvironmentYamlCodec
  {
    /// <summary>Сохраняет рецепты в YAML.</summary>
    public static void WriteRecipes(string filePath, IReadOnlyList<EnvironmentRecipeData> recipes)
    {
      if (string.IsNullOrWhiteSpace(filePath))
        throw new ArgumentException("filePath");

      var sb = new StringBuilder();
      sb.AppendLine("# Рецепты среды.");
      sb.AppendLine("recipes:");

      if (recipes != null)
      {
        foreach (EnvironmentRecipeData recipe in recipes)
        {
          if (recipe == null || string.IsNullOrWhiteSpace(recipe.Id))
            continue;

          sb.AppendLine("  - id: " + FormatYamlScalar(recipe.Id));
          AppendScalarLine(sb, 4, "display_name", recipe.DisplayName);
          AppendScalarLine(sb, 4, "description", recipe.Description);
          sb.AppendLine("    adaptive_action_id: " +
              recipe.AdaptiveActionId.ToString(CultureInfo.InvariantCulture));
          sb.AppendLine("    risk_tier: " + FormatRiskTier(recipe.RiskTier));
          sb.AppendLine("    reactive_eligible: " + (recipe.ReactiveEligible ? "true" : "false"));
          AppendIntList(sb, 4, "recommended_trigger_influence_ids", recipe.RecommendedTriggerInfluenceIds);

          sb.AppendLine("    preconditions:");
          AppendDocumentKinds(sb, 6, recipe.DocumentKinds);
          sb.AppendLine("      not_sketch_edit: " + (recipe.NotSketchEdit ? "true" : "false"));
          sb.AppendLine("      not_read_only: " + (recipe.NotReadOnly ? "true" : "false"));
          sb.AppendLine("      pdm_checkout_required: " + (recipe.PdmCheckoutRequired ? "true" : "false"));

          sb.AppendLine("    steps:");
          if (recipe.Steps != null)
          {
            foreach (EnvironmentRecipeStepData step in recipe.Steps)
            {
              string stepType = FormatStepTypeForFile(step?.Type);
              sb.AppendLine("      - type: " + FormatYamlScalar(stepType));
              if (step?.Parameters != null)
              {
                foreach (KeyValuePair<string, string> kv in step.Parameters)
                  AppendScalarLine(sb, 8, kv.Key, kv.Value);
              }
            }
          }

          AppendScalarLine(sb, 4, "postcondition_log", recipe.PostconditionLog);
          AppendScalarLine(sb, 4, "test_notes", recipe.TestNotes);
          sb.AppendLine();
        }
      }

      WriteUtf8(filePath, sb.ToString());
    }

    /// <summary>Сохраняет триггеры в YAML.</summary>
    public static void WriteTriggers(string filePath, IReadOnlyList<EnvironmentTriggerData> triggers)
    {
      if (string.IsNullOrWhiteSpace(filePath))
        throw new ArgumentException("filePath");

      var sb = new StringBuilder();
      sb.AppendLine("# Триггеры среды.");
      sb.AppendLine("triggers:");

      if (triggers != null)
      {
        foreach (EnvironmentTriggerData trigger in triggers)
        {
          if (trigger == null || string.IsNullOrWhiteSpace(trigger.Id))
            continue;

          sb.AppendLine("  - id: " + FormatYamlScalar(trigger.Id));
          AppendScalarLine(sb, 4, "display_name", trigger.DisplayName);
          sb.AppendLine("    influence_action_id: " +
              trigger.InfluenceActionId.ToString(CultureInfo.InvariantCulture));
          AppendDocumentKinds(sb, 4, trigger.DocumentKinds);
          sb.AppendLine("    detect:");

          if (trigger.DetectRules != null)
          {
            foreach (EnvironmentTriggerDetectData rule in trigger.DetectRules)
            {
              string kind = FormatDetectKindForFile(rule?.Kind);
              sb.AppendLine("      - kind: " + FormatYamlScalar(kind));
              if (!string.IsNullOrWhiteSpace(rule?.Environment))
                AppendScalarLine(sb, 8, "environment", rule.Environment);
              sb.AppendLine("        enabled: " + (rule != null && rule.Enabled ? "true" : "false"));
              if (rule?.CommandIds != null && rule.CommandIds.Count > 0)
                AppendIntList(sb, 8, "command_ids", rule.CommandIds);
            }
          }
        }
      }

      WriteUtf8(filePath, sb.ToString());
    }

    private static string FormatStepTypeForFile(string type)
    {
      if (string.Equals(type, "set_custom_property", StringComparison.OrdinalIgnoreCase))
        return "set_property";
      return type ?? string.Empty;
    }

    private static string FormatDetectKindForFile(string kind)
    {
      if (string.Equals(kind, "sw_command_pre", StringComparison.OrdinalIgnoreCase))
        return "command_before";
      if (string.Equals(kind, "file_save_post", StringComparison.OrdinalIgnoreCase))
        return "document_saved";
      return kind ?? string.Empty;
    }

    private static string FormatRiskTier(EnvironmentRecipeRiskTier tier)
    {
      switch (tier)
      {
        case EnvironmentRecipeRiskTier.A: return "A";
        case EnvironmentRecipeRiskTier.C: return "C";
        default: return "B";
      }
    }

    private static void AppendDocumentKinds(StringBuilder sb, int indent, IList<EnvironmentDocumentKind> kinds)
    {
      string pad = new string(' ', indent);
      if (kinds == null || kinds.Count == 0)
      {
        sb.AppendLine(pad + "document_kinds: []");
        return;
      }

      sb.Append(pad + "document_kinds: [");
      for (int i = 0; i < kinds.Count; i++)
      {
        if (i > 0)
          sb.Append(", ");
        switch (kinds[i])
        {
          case EnvironmentDocumentKind.Assembly: sb.Append("assembly"); break;
          case EnvironmentDocumentKind.Drawing: sb.Append("drawing"); break;
          default: sb.Append("part"); break;
        }
      }

      sb.AppendLine("]");
    }

    private static void AppendIntList(StringBuilder sb, int indent, string key, IList<int> ids)
    {
      string pad = new string(' ', indent);
      if (ids == null || ids.Count == 0)
      {
        sb.AppendLine(pad + key + ": []");
        return;
      }

      sb.Append(pad + key + ": [");
      for (int i = 0; i < ids.Count; i++)
      {
        if (i > 0)
          sb.Append(", ");
        sb.Append(ids[i].ToString(CultureInfo.InvariantCulture));
      }

      sb.AppendLine("]");
    }

    private static void AppendScalarLine(StringBuilder sb, int indent, string key, string value)
    {
      if (string.IsNullOrEmpty(value))
        return;
      sb.AppendLine(new string(' ', indent) + key + ": " + FormatYamlScalar(value));
    }

    private static string FormatYamlScalar(string value)
    {
      if (string.IsNullOrEmpty(value))
        return "\"\"";
      if (value.IndexOfAny(new[] { ':', '#', '[', ']', '{', '}', ',', '\n', '\r' }) >= 0)
        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
      return value;
    }

    private static void WriteUtf8(string filePath, string content)
    {
      string dir = Path.GetDirectoryName(filePath);
      if (!string.IsNullOrEmpty(dir))
        Directory.CreateDirectory(dir);
      File.WriteAllText(filePath, content, new UTF8Encoding(false));
    }
  }
}
