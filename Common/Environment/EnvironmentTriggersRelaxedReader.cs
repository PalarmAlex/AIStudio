using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ISIDA.SymbiontEnv.Contract;

namespace AIStudio.Common.SymbiontEnv
{
  /// <summary>
  /// Мягкое чтение EnvironmentTriggers.yaml для UI-редактора.
  /// Нужен, чтобы "битые" триггеры (без homeostasis_deltas и без reflex_trigger_command_pattern_id)
  /// не исчезали из списка из-за строгой валидации contract-кодека, а могли быть исправлены в UI.
  /// </summary>
  public static class EnvironmentTriggersRelaxedReader
  {
    public static List<EnvironmentTriggerData> Read(string filePath)
    {
      var list = new List<EnvironmentTriggerData>();
      if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        return list;

      string[] lines;
      try
      {
        lines = File.ReadAllLines(filePath);
      }
      catch
      {
        return list;
      }

      EnvironmentTriggerData current = null;
      int pendingParamId = 0;
      foreach (string rawLine in lines)
      {
        string line = rawLine ?? string.Empty;

        // New trigger item.
        if (line.StartsWith("  - id:", StringComparison.Ordinal))
        {
          if (current != null && !string.IsNullOrWhiteSpace(current.Id))
            list.Add(current);

          current = new EnvironmentTriggerData();
          pendingParamId = 0;

          current.Id = Unquote(line.Substring("  - id:".Length).Trim());
          continue;
        }

        if (current == null)
          continue;

        // Scalars at indent 4.
        if (line.StartsWith("    display_name:", StringComparison.Ordinal))
        {
          current.DisplayName = Unquote(line.Substring("    display_name:".Length).Trim());
          continue;
        }

        if (line.StartsWith("    event_kind:", StringComparison.Ordinal))
        {
          current.EventKind = Unquote(line.Substring("    event_kind:".Length).Trim());
          continue;
        }

        if (line.StartsWith("    reflex_trigger_command_pattern_id:", StringComparison.Ordinal))
        {
          string v = line.Substring("    reflex_trigger_command_pattern_id:".Length).Trim();
          if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
            current.ReflexTriggerCommandPatternId = id;
          continue;
        }

        // homeostasis_deltas list items.
        if (line.StartsWith("      - param_id:", StringComparison.Ordinal))
        {
          string v = line.Substring("      - param_id:".Length).Trim();
          if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int pid))
            pendingParamId = pid;
          continue;
        }

        if (line.StartsWith("        delta:", StringComparison.Ordinal))
        {
          if (pendingParamId <= 0)
            continue;

          string v = line.Substring("        delta:".Length).Trim();
          if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float delta))
          {
            // В row-слое это int; здесь оставляем float, как в contract.
            if (delta != 0)
              current.HomeostasisDeltas.Add(new HomeostasisDeltaEntry { ParameterId = pendingParamId, Delta = delta });
          }

          pendingParamId = 0;
          continue;
        }
      }

      if (current != null && !string.IsNullOrWhiteSpace(current.Id))
        list.Add(current);

      return list;
    }

    private static string Unquote(string text)
    {
      if (string.IsNullOrEmpty(text))
        return string.Empty;

      string t = text.Trim();
      if (t.Length >= 2 && t[0] == '"' && t[t.Length - 1] == '"')
        return t.Substring(1, t.Length - 2).Replace("\\\"", "\"").Replace("\\\\", "\\");

      return t;
    }
  }
}

