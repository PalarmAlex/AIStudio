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

      LoadHandlersCatalog(Path.Combine(schemaDir, "handlers-catalog.json"), schema.Handlers);
      LoadDetectKinds(Path.Combine(schemaDir, "trigger-detect.json"), schema.TriggerDetectKinds);
      LoadMetricProbes(Path.Combine(schemaDir, "metric-probes.json"), schema.MetricProbes);
      LoadRecipeCatalog(Path.Combine(schemaDir, "recipe-catalog.json"), schema.RecipeCatalog);
      LoadTriggerCatalog(Path.Combine(schemaDir, "trigger-catalog.json"), schema.TriggerCatalog);
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
    /// Каталог триггеров из <c>schema\trigger-catalog.json</c> для текущего проекта.
    /// </summary>
    public static IReadOnlyList<AdapterSchemaTriggerCatalogEntry> LoadTriggerCatalogForCurrentProject()
    {
      if (!SymbiontProjectAdapterSettings.TryGetValidatedCurrentAdapterId(out string adapterId))
        return new AdapterSchemaTriggerCatalogEntry[0];
      return LoadTriggerCatalogForAdapter(adapterId);
    }

    /// <summary>
    /// Каталог триггеров из <c>schema\trigger-catalog.json</c> зарегистрированного пакета.
    /// </summary>
    public static IReadOnlyList<AdapterSchemaTriggerCatalogEntry> LoadTriggerCatalogForAdapter(string adapterId)
    {
      if (string.IsNullOrWhiteSpace(adapterId))
        return new AdapterSchemaTriggerCatalogEntry[0];
      AdapterManifest manifest = AdapterRegistry.TryGetById(adapterId);
      if (manifest == null || string.IsNullOrWhiteSpace(manifest.PackageRootPath))
        return new AdapterSchemaTriggerCatalogEntry[0];
      string path = Path.Combine(manifest.PackageRootPath, "schema", "trigger-catalog.json");
      var entries = new List<AdapterSchemaTriggerCatalogEntry>();
      LoadTriggerCatalog(path, entries);
      return entries;
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

    private static void LoadHandlersCatalog(string path, IList<AdapterSchemaHandler> target)
    {
      if (!File.Exists(path))
        return;
      try
      {
        JObject jo = JObject.Parse(File.ReadAllText(path));
        JArray arr = jo["handlers"] as JArray;
        if (arr == null)
          return;
        foreach (JToken token in arr)
        {
          if (!(token is JObject item))
            continue;
          string id = item["id"]?.ToString();
          if (string.IsNullOrWhiteSpace(id))
            continue;
          var handler = new AdapterSchemaHandler
          {
            Id = id.Trim(),
            Label = item["label"]?.ToString() ?? id,
            Description = item["description"]?.ToString()
          };
          if (item["argsSchema"] is JArray argsArr)
          {
            foreach (JToken argToken in argsArr)
            {
              if (!(argToken is JObject argItem))
                continue;
              string argKey = argItem["key"]?.ToString();
              if (string.IsNullOrWhiteSpace(argKey))
                continue;
              var arg = new AdapterSchemaHandlerArg
              {
                Key = argKey.Trim(),
                Label = argItem["label"]?.ToString(),
                Type = argItem["type"]?.ToString(),
                Required = argItem["required"]?.Value<bool>() ?? false
              };
              if (argItem["values"] is JArray valuesArr)
              {
                var values = new List<string>();
                foreach (JToken valueToken in valuesArr)
                {
                  string value = valueToken?.ToString();
                  if (!string.IsNullOrWhiteSpace(value))
                    values.Add(value);
                }
                arg.Values = values;
              }
              handler.ArgsSchema.Add(arg);
            }
          }
          target.Add(handler);
        }
      }
      catch
      {
        // ignore broken schema
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

    private static void LoadTriggerCatalog(string path, IList<AdapterSchemaTriggerCatalogEntry> target)
    {
      if (!File.Exists(path))
        return;
      try
      {
        JObject jo = JObject.Parse(File.ReadAllText(path));
        JArray arr = jo["triggers"] as JArray;
        if (arr == null)
          return;
        foreach (JToken token in arr)
        {
          if (!(token is JObject item))
            continue;
          string id = item["id"]?.ToString();
          if (string.IsNullOrWhiteSpace(id))
            continue;
          target.Add(new AdapterSchemaTriggerCatalogEntry
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
          var detectKind = new AdapterSchemaDetectKind
          {
            Kind = kind,
            Label = item["label"]?.ToString() ?? kind
          };
          if (item["parameters"] is JArray paramsArr)
          {
            foreach (JToken paramToken in paramsArr)
            {
              if (!(paramToken is JObject paramItem))
                continue;
              string key = paramItem["key"]?.ToString();
              if (string.IsNullOrWhiteSpace(key))
                continue;
              var param = new AdapterSchemaEventParameter
              {
                Key = key.Trim(),
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
                param.Values = values;
              }
              detectKind.Parameters.Add(param);
            }
          }
          target.Add(detectKind);
        }
      }
      catch
      {
        // ignore
      }
    }
  }
}
