using ISIDA.SymbiontEnv.Contract;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace AIStudio.Common.Adapters
{
  /// <summary>
  /// Загрузка schema JSON из зарегистрированного пакета адаптера.
  /// </summary>
  public static class AdapterSchemaLoader
  {
    private static readonly string[] DefaultStepTypes =
    {
      "set_property",
      "run_sw_command",
      "rebuild",
      "log"
    };
    private static readonly string[] DefaultDetectKinds =
    {
      "command_before",
      "document_saved"
    };
    /// <summary>
    /// Загружает schema для адаптера; при отсутствии файлов — fallback Velum-like defaults.
    /// </summary>
    public static AdapterEnvironmentSchema LoadForAdapter(string adapterId)
    {
      if (string.IsNullOrWhiteSpace(adapterId))
        return CreateVelumLikeFallback();
      AdapterManifest manifest = AdapterRegistry.TryGetById(adapterId);
      if (manifest == null || string.IsNullOrWhiteSpace(manifest.PackageRootPath))
        return CreateVelumLikeFallback();
      string schemaDir = Path.Combine(manifest.PackageRootPath, "schema");
      if (!Directory.Exists(schemaDir))
        return CreateVelumLikeFallback();
      var schema = new AdapterEnvironmentSchema();
      LoadFields(Path.Combine(schemaDir, "recipe-preconditions.json"), "fields", schema.RecipePreconditions);
      LoadStepTypes(Path.Combine(schemaDir, "recipe-steps.json"), schema.RecipeStepTypes);
      LoadFields(Path.Combine(schemaDir, "trigger-filter.json"), "fields", schema.TriggerFilterFields);
      LoadDetectKinds(Path.Combine(schemaDir, "trigger-detect.json"), schema.TriggerDetectKinds);
      LoadMetricProbes(Path.Combine(schemaDir, "metric-probes.json"), schema.MetricProbes);
      if (schema.RecipeStepTypes.Count == 0)
        FillDefaultStepTypes(schema.RecipeStepTypes);
      if (schema.RecipePreconditions.Count == 0)
        FillDefaultRecipePreconditions(schema.RecipePreconditions);
      if (schema.TriggerFilterFields.Count == 0)
        FillDefaultTriggerFilter(schema.TriggerFilterFields);
      if (schema.TriggerDetectKinds.Count == 0)
        FillDefaultDetectKinds(schema.TriggerDetectKinds);
      return schema;
    }

    /// <summary>Schema для текущего AdapterId в AgentProperties.dat.</summary>
    public static AdapterEnvironmentSchema LoadForCurrentProject()
    {
      if (!SymbiontProjectAdapterSettings.TryGetValidatedCurrentAdapterId(out string adapterId))
        return CreateVelumLikeFallback();
      return LoadForAdapter(adapterId);
    }

    /// <summary>
    /// Ключи проб метрик среды для текущего проекта.
    /// Без подключённого адаптера — пустой список (без fallback).
    /// </summary>
    public static IReadOnlyList<AdapterSchemaMetricProbe> LoadMetricProbesForCurrentProject()
    {
      if (!SymbiontProjectAdapterSettings.TryGetValidatedCurrentAdapterId(out string adapterId))
        return new AdapterSchemaMetricProbe[0];
      return LoadMetricProbesForAdapter(adapterId);
    }

    /// <summary>
    /// Ключи проб из <c>schema\metric-probes.json</c> зарегистрированного пакета.
    /// </summary>
    public static IReadOnlyList<AdapterSchemaMetricProbe> LoadMetricProbesForAdapter(string adapterId)
    {
      if (string.IsNullOrWhiteSpace(adapterId))
        return new AdapterSchemaMetricProbe[0];
      AdapterManifest manifest = AdapterRegistry.TryGetById(adapterId);
      if (manifest == null || string.IsNullOrWhiteSpace(manifest.PackageRootPath))
        return new AdapterSchemaMetricProbe[0];
      string path = Path.Combine(manifest.PackageRootPath, "schema", "metric-probes.json");
      var probes = new List<AdapterSchemaMetricProbe>();
      LoadMetricProbes(path, probes);
      return probes;
    }

    private static void LoadFields(string path, string arrayName, IList<AdapterSchemaField> target)
    {
      if (!File.Exists(path))
        return;
      try
      {
        JObject jo = JObject.Parse(File.ReadAllText(path));
        JArray arr = jo[arrayName] as JArray;
        if (arr == null)
          return;
        foreach (JToken token in arr)
        {
          if (!(token is JObject item))
            continue;
          var field = new AdapterSchemaField
          {
            Key = item["key"]?.ToString(),
            Label = item["label"]?.ToString(),
            Type = item["type"]?.ToString(),
            Required = item["required"]?.Value<bool>() ?? false
          };
          if (item["enumValues"] is JArray enumArr)
          {
            var values = new List<string>();
            foreach (JToken ev in enumArr)
            {
              string s = ev?.ToString();
              if (!string.IsNullOrWhiteSpace(s))
                values.Add(s);
            }
            field.EnumValues = values;
          }
          if (!string.IsNullOrWhiteSpace(field.Key))
            target.Add(field);
        }
      }
      catch
      {
        // ignore broken schema — fallback applied by caller
      }
    }

    private static void LoadStepTypes(string path, IList<AdapterSchemaStepType> target)
    {
      if (!File.Exists(path))
        return;
      try
      {
        JObject jo = JObject.Parse(File.ReadAllText(path));
        JArray arr = jo["stepTypes"] as JArray;
        if (arr == null)
          return;
        foreach (JToken token in arr)
        {
          if (!(token is JObject item))
            continue;
          string type = item["type"]?.ToString();
          if (string.IsNullOrWhiteSpace(type))
            continue;
          target.Add(new AdapterSchemaStepType
          {
            Type = type,
            Label = item["label"]?.ToString() ?? type
          });
        }
      }
      catch
      {
        // ignore
      }
    }

    private static void LoadMetricProbes(string path, IList<AdapterSchemaMetricProbe> target)
    {
      if (!File.Exists(path))
        return;
      try
      {
        JObject jo = JObject.Parse(File.ReadAllText(path));
        JArray arr = jo["probes"] as JArray;
        if (arr == null)
          return;
        foreach (JToken token in arr)
        {
          if (!(token is JObject item))
            continue;
          string key = item["key"]?.ToString();
          if (string.IsNullOrWhiteSpace(key))
            continue;
          target.Add(new AdapterSchemaMetricProbe
          {
            Key = key.Trim(),
            Label = item["label"]?.ToString(),
            Description = item["description"]?.ToString()
          });
        }
      }
      catch
      {
        // ignore broken schema
      }
    }

    private static void LoadDetectKinds(string path, IList<AdapterSchemaDetectKind> target)
    {
      if (!File.Exists(path))
        return;
      try
      {
        JObject jo = JObject.Parse(File.ReadAllText(path));
        JArray arr = jo["detectKinds"] as JArray;
        if (arr == null)
          return;
        foreach (JToken token in arr)
        {
          if (!(token is JObject item))
            continue;
          string kind = item["kind"]?.ToString();
          if (string.IsNullOrWhiteSpace(kind))
            continue;
          target.Add(new AdapterSchemaDetectKind
          {
            Kind = kind,
            Label = item["label"]?.ToString() ?? kind
          });
        }
      }
      catch
      {
        // ignore
      }
    }

    private static AdapterEnvironmentSchema CreateVelumLikeFallback()
    {
      var schema = new AdapterEnvironmentSchema();
      FillDefaultRecipePreconditions(schema.RecipePreconditions);
      FillDefaultStepTypes(schema.RecipeStepTypes);
      FillDefaultTriggerFilter(schema.TriggerFilterFields);
      FillDefaultDetectKinds(schema.TriggerDetectKinds);
      return schema;
    }

    private static void FillDefaultRecipePreconditions(IList<AdapterSchemaField> target)
    {
      target.Add(new AdapterSchemaField
      {
        Key = "document_kinds",
        Label = "Типы документа",
        Type = "stringList",
        EnumValues = new List<string> { "part", "assembly", "drawing" }
      });
      target.Add(new AdapterSchemaField { Key = "not_sketch_edit", Label = "Не в режиме эскиза", Type = "bool" });
      target.Add(new AdapterSchemaField { Key = "not_read_only", Label = "Не read-only", Type = "bool" });
      target.Add(new AdapterSchemaField { Key = "pdm_checkout_required", Label = "Требуется checkout PDM", Type = "bool" });
    }

    private static void FillDefaultStepTypes(IList<AdapterSchemaStepType> target)
    {
      for (int i = 0; i < DefaultStepTypes.Length; i++)
      {
        target.Add(new AdapterSchemaStepType
        {
          Type = DefaultStepTypes[i],
          Label = DefaultStepTypes[i]
        });
      }
    }

    private static void FillDefaultTriggerFilter(IList<AdapterSchemaField> target)
    {
      target.Add(new AdapterSchemaField
      {
        Key = "document_kinds",
        Label = "Типы документа",
        Type = "stringList",
        EnumValues = new List<string> { "part", "assembly", "drawing" }
      });
    }

    private static void FillDefaultDetectKinds(IList<AdapterSchemaDetectKind> target)
    {
      for (int i = 0; i < DefaultDetectKinds.Length; i++)
      {
        target.Add(new AdapterSchemaDetectKind
        {
          Kind = DefaultDetectKinds[i],
          Label = DefaultDetectKinds[i]
        });
      }
    }
  }
}
