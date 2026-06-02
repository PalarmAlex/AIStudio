using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AIStudio.Common.SymbiontEnv
{
  public static partial class EnvironmentYamlCodec
  {
    /// <summary>Читает триггеры из YAML.</summary>
    public static List<EnvironmentTriggerData> ReadTriggers(string filePath, IList<string> errors)
    {
      if (errors == null)
        throw new ArgumentNullException(nameof(errors));

      if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
      {
        errors.Add("Файл триггеров не найден: " + filePath);
        return new List<EnvironmentTriggerData>();
      }

      try
      {
        string text = File.ReadAllText(filePath, Encoding.UTF8);
        YamlMap root = ParseDocument(text);
        YamlSequence triggersSeq = GetSequence(root, "triggers");
        if (triggersSeq == null)
        {
          errors.Add("В файле нет секции triggers: " + filePath);
          return new List<EnvironmentTriggerData>();
        }

        if (triggersSeq.Items.Count == 0)
          return new List<EnvironmentTriggerData>();

        var list = new List<EnvironmentTriggerData>();
        foreach (YamlNode node in triggersSeq.Items)
        {
          YamlMap map = node as YamlMap;
          if (map == null)
            continue;

          EnvironmentTriggerData trigger = MapTrigger(map, filePath, errors);
          if (trigger != null)
            list.Add(trigger);
        }

        return list;
      }
      catch (Exception ex)
      {
        errors.Add(filePath + ": " + ex.Message);
        return new List<EnvironmentTriggerData>();
      }
    }

    private static EnvironmentTriggerData MapTrigger(YamlMap map, string filePath, IList<string> errors)
    {
      string key = GetScalar(map, "id");
      if (string.IsNullOrWhiteSpace(key))
        key = GetScalar(map, "trigger_key");

      if (string.IsNullOrWhiteSpace(key))
      {
        errors.Add("Пропущен id в " + filePath);
        return null;
      }

      if (!TryGetInt(map, "influence_action_id", out int influenceId) || influenceId <= 0)
      {
        errors.Add("Некорректный influence_action_id для " + key + " в " + filePath);
        return null;
      }

      YamlSequence kindsSeq = GetSequence(map, "document_kinds");
      if (kindsSeq == null)
        kindsSeq = GetSequence(map, "document_filter");

      return new EnvironmentTriggerData
      {
        Id = key.Trim(),
        DisplayName = GetScalar(map, "display_name"),
        InfluenceActionId = influenceId,
        DocumentKinds = ParseDocumentKinds(kindsSeq),
        DetectRules = ParseTriggerDetectRules(GetSequence(map, "detect"))
      };
    }

    private static List<EnvironmentTriggerDetectData> ParseTriggerDetectRules(YamlSequence seq)
    {
      if (seq == null || seq.Items.Count == 0)
        return new List<EnvironmentTriggerDetectData>();

      var list = new List<EnvironmentTriggerDetectData>();
      foreach (YamlNode node in seq.Items)
      {
        YamlMap map = node as YamlMap;
        if (map == null)
          continue;

        string kind = GetScalar(map, "kind");
        if (string.IsNullOrWhiteSpace(kind))
          continue;

        list.Add(new EnvironmentTriggerDetectData
        {
          Kind = kind.Trim(),
          Enabled = TryGetBool(map, "enabled", defaultValue: true),
          Environment = GetScalar(map, "environment"),
          CommandIds = ParseIntList(map, "command_ids")
        });
      }

      return list;
    }
  }
}
