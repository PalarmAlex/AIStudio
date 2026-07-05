using ISIDA.SymbiontEnv.Contract;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
      LoadMetricProbes(Path.Combine(schemaDir, "metric-probes.json"), schema.MetricProbes);
      LoadRecipeCatalog(Path.Combine(schemaDir, "recipe-catalog.json"), schema.RecipeCatalog);
      LoadRecipeTemplateCatalog(Path.Combine(schemaDir, "recipe-template-catalog.json"), schema.RecipeTemplateCatalog);
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
                Required = argItem["required"]?.Value<bool>() ?? false,
                DefaultValue = argItem["defaultValue"]?.ToString(),
                EditorHint = argItem["editorHint"]?.ToString(),
                Values = ParseArgValueOptions(argItem)
              };
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

    private static void LoadRecipeTemplateCatalog(string path, AdapterSchemaRecipeTemplateCatalog target)
    {
      if (target == null || !File.Exists(path))
        return;
      try
      {
        JObject jo = JObject.Parse(File.ReadAllText(path));
        if (jo["placeholders"] is JArray placeholdersArr)
        {
          foreach (JToken token in placeholdersArr)
          {
            if (!(token is JObject item))
              continue;
            string placeholderToken = ReadJsonString(item["token"]);
            if (string.IsNullOrWhiteSpace(placeholderToken))
              continue;
            target.Placeholders.Add(new AdapterSchemaTemplatePlaceholder
            {
              Token = RecipeTemplateTokenNormalizer.NormalizeForSolidWorks(placeholderToken.Trim()),
              Label = item["label"]?.ToString(),
              Description = item["description"]?.ToString()
            });
          }
        }

        if (jo["propertyNames"] is JArray propertyNamesArr)
        {
          foreach (JToken token in propertyNamesArr)
          {
            if (!(token is JObject item))
              continue;
            string name = item["name"]?.ToString();
            if (string.IsNullOrWhiteSpace(name))
              continue;
            target.PropertyNames.Add(new AdapterSchemaPropertyNameEntry
            {
              Name = name.Trim(),
              Label = item["label"]?.ToString(),
              Description = item["description"]?.ToString()
            });
          }
        }
      }
      catch
      {
        // ignore broken schema
      }
    }

    /// <summary>Токены плейсхолдеров из schema пакета (пусто, если каталог не задан).</summary>
    public static IReadOnlyList<string> GetTemplatePlaceholderTokens(AdapterEnvironmentSchema schema)
    {
      if (schema?.RecipeTemplateCatalog?.Placeholders == null || schema.RecipeTemplateCatalog.Placeholders.Count == 0)
        return Array.Empty<string>();
      return schema.RecipeTemplateCatalog.Placeholders
          .Where(p => p != null && !string.IsNullOrWhiteSpace(p.Token))
          .Select(p => p.Token.Trim())
          .Distinct(StringComparer.OrdinalIgnoreCase)
          .ToList();
    }

    /// <summary>Имена свойств документа из schema пакета (пусто, если каталог не задан).</summary>
    public static IReadOnlyList<string> GetPropertyNameValues(AdapterEnvironmentSchema schema)
    {
      if (schema?.RecipeTemplateCatalog?.PropertyNames == null || schema.RecipeTemplateCatalog.PropertyNames.Count == 0)
        return Array.Empty<string>();
      return schema.RecipeTemplateCatalog.PropertyNames
          .Where(p => p != null && !string.IsNullOrWhiteSpace(p.Name))
          .Select(p => p.Name.Trim())
          .Distinct(StringComparer.OrdinalIgnoreCase)
          .ToList();
    }

    private static IList<AdapterSchemaArgValueOption> ParseArgValueOptions(JObject item)
    {
      if (item == null)
        return null;

      Dictionary<string, string> valueLabels = null;
      if (item["valueLabels"] is JObject labelsObj)
      {
        valueLabels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (JProperty property in labelsObj.Properties())
        {
          string label = property.Value?.ToString();
          if (!string.IsNullOrWhiteSpace(property.Name) && !string.IsNullOrWhiteSpace(label))
            valueLabels[property.Name.Trim()] = label.Trim();
        }
        if (valueLabels.Count == 0)
          valueLabels = null;
      }

      if (!(item["values"] is JArray valuesArr) || valuesArr.Count == 0)
        return null;

      var options = new List<AdapterSchemaArgValueOption>();
      foreach (JToken valueToken in valuesArr)
      {
        if (valueToken is JObject valueObj)
        {
          string key = valueObj["key"]?.ToString() ?? valueObj["value"]?.ToString();
          if (string.IsNullOrWhiteSpace(key))
            continue;
          string label = valueObj["label"]?.ToString();
          if (string.IsNullOrWhiteSpace(label) && valueLabels != null)
            valueLabels.TryGetValue(key.Trim(), out label);
          options.Add(new AdapterSchemaArgValueOption
          {
            Key = key.Trim(),
            Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim()
          });
          continue;
        }

        string scalarKey = valueToken?.ToString();
        if (string.IsNullOrWhiteSpace(scalarKey))
          continue;
        string scalarLabel = null;
        if (valueLabels != null)
          valueLabels.TryGetValue(scalarKey.Trim(), out scalarLabel);
        options.Add(new AdapterSchemaArgValueOption
        {
          Key = scalarKey.Trim(),
          Label = string.IsNullOrWhiteSpace(scalarLabel) ? null : scalarLabel.Trim()
        });
      }

      return options.Count > 0 ? options : null;
    }

    private static string ReadJsonString(JToken token)
    {
      if (token == null)
        return null;
      if (token.Type == JTokenType.String)
        return token.Value<string>();
      return token.ToString();
    }
  }
}
