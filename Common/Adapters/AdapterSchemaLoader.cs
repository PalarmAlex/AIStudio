using ISIDA.SymbiontEnv.Contract;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;

namespace AIStudio.Common.Adapters
{
  /// <summary>
  /// Загрузка schema JSON из зарегистрированного пакета адаптера.
  /// </summary>
  public static class AdapterSchemaLoader
  {
    /// <summary>
    /// Загружает schema для адаптера. Отсутствующие или пустые файлы — пустые списки в schema.
    /// </summary>
    public static AdapterEnvironmentSchema LoadForAdapter(string adapterId)
    {
      var schema = new AdapterEnvironmentSchema();
      if (string.IsNullOrWhiteSpace(adapterId))
        return schema;

      AdapterManifest manifest = AdapterRegistry.TryGetById(adapterId);
      if (manifest == null || string.IsNullOrWhiteSpace(manifest.PackageRootPath))
        return schema;

      string schemaDir = Path.Combine(manifest.PackageRootPath, "schema");
      if (!Directory.Exists(schemaDir))
        return schema;

      LoadFields(Path.Combine(schemaDir, "recipe-preconditions.json"), "fields", schema.RecipePreconditions);
      LoadStepTypes(Path.Combine(schemaDir, "recipe-steps.json"), schema.RecipeStepTypes);
      LoadFields(Path.Combine(schemaDir, "trigger-filter.json"), "fields", schema.TriggerFilterFields);
      LoadDetectKinds(Path.Combine(schemaDir, "trigger-detect.json"), schema.TriggerDetectKinds);
      LoadMetricProbes(Path.Combine(schemaDir, "metric-probes.json"), schema.MetricProbes);
      LoadRecipeCatalog(Path.Combine(schemaDir, "recipe-catalog.json"), schema.RecipeCatalog);
      return schema;
    }

    /// <summary>Schema для текущего AdapterId в AgentProperties.dat.</summary>
    public static AdapterEnvironmentSchema LoadForCurrentProject()
    {
      if (!SymbiontProjectAdapterSettings.TryGetValidatedCurrentAdapterId(out string adapterId))
        return new AdapterEnvironmentSchema();
      return LoadForAdapter(adapterId);
    }

    /// <summary>
    /// Ключи проб метрик среды для текущего проекта.
    /// Без подключённого адаптера — пустой список.
    /// </summary>
    public static IReadOnlyList<AdapterSchemaMetricProbe> LoadMetricProbesForCurrentProject()
    {
      if (!SymbiontProjectAdapterSettings.TryGetValidatedCurrentAdapterId(out string adapterId))
        return new AdapterSchemaMetricProbe[0];
      return LoadMetricProbesForAdapter(adapterId);
    }

    /// <summary>
    /// Каталог рецептов из <c>schema\recipe-catalog.json</c> для текущего проекта.
    /// </summary>
    public static IReadOnlyList<AdapterSchemaRecipeCatalogEntry> LoadRecipeCatalogForCurrentProject()
    {
      if (!SymbiontProjectAdapterSettings.TryGetValidatedCurrentAdapterId(out string adapterId))
        return new AdapterSchemaRecipeCatalogEntry[0];
      return LoadRecipeCatalogForAdapter(adapterId);
    }

    /// <summary>
    /// Каталог рецептов из <c>schema\recipe-catalog.json</c> зарегистрированного пакета.
    /// </summary>
    public static IReadOnlyList<AdapterSchemaRecipeCatalogEntry> LoadRecipeCatalogForAdapter(string adapterId)
    {
      if (string.IsNullOrWhiteSpace(adapterId))
        return new AdapterSchemaRecipeCatalogEntry[0];
      AdapterManifest manifest = AdapterRegistry.TryGetById(adapterId);
      if (manifest == null || string.IsNullOrWhiteSpace(manifest.PackageRootPath))
        return new AdapterSchemaRecipeCatalogEntry[0];
      string path = Path.Combine(manifest.PackageRootPath, "schema", "recipe-catalog.json");
      var entries = new List<AdapterSchemaRecipeCatalogEntry>();
      LoadRecipeCatalog(path, entries);
      return entries;
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
        // ignore broken schema
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
          var stepType = new AdapterSchemaStepType
          {
            Type = type,
            Label = item["label"]?.ToString() ?? type,
            RuntimeType = item["runtimeType"]?.ToString()
          };
          if (item["parameters"] is JArray paramArr)
          {
            foreach (JToken paramToken in paramArr)
            {
              if (!(paramToken is JObject paramItem))
                continue;
              string paramKey = paramItem["key"]?.ToString();
              if (string.IsNullOrWhiteSpace(paramKey))
                continue;
              var parameter = new AdapterSchemaStepParameter
              {
                Key = paramKey,
                Label = paramItem["label"]?.ToString(),
                Type = paramItem["type"]?.ToString(),
                Required = paramItem["required"]?.Value<bool>() ?? false
              };
              if (paramItem["values"] is JArray valuesArr)
              {
                var values = new List<string>();
                foreach (JToken valueToken in valuesArr)
                {
                  string value = valueToken?.ToString();
                  if (!string.IsNullOrWhiteSpace(value))
                    values.Add(value);
                }
                parameter.Values = values;
              }
              stepType.Parameters.Add(parameter);
            }
          }
          target.Add(stepType);
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

    private static void LoadRecipeCatalog(string path, IList<AdapterSchemaRecipeCatalogEntry> target)
    {
      if (!File.Exists(path))
        return;
      try
      {
        JObject jo = JObject.Parse(File.ReadAllText(path));
        JArray arr = jo["recipes"] as JArray;
        if (arr == null)
          return;
        foreach (JToken token in arr)
        {
          if (!(token is JObject item))
            continue;
          string id = item["id"]?.ToString();
          if (string.IsNullOrWhiteSpace(id))
            continue;
          target.Add(new AdapterSchemaRecipeCatalogEntry
          {
            Id = id.Trim(),
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
  }
}
