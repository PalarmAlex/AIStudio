using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AIStudio.Common.SymbiontEnv
{
  /// <summary>
  /// Загрузка и сохранение YAML каталогов среды (только isida + файлы проекта).
  /// </summary>
  public static class EnvironmentCatalogStorage
  {
    private const string EmptyRecipesSeed =
        "# Рецепты среды (исполняемая моторика; привязка к G_AD — adaptive_action_id).\r\n" +
        "recipes: []\r\n";

    private const string EmptyTriggersSeed =
        "# Триггеры среды: событие → influence_action_id.\r\n" +
        "triggers: []\r\n";

    /// <summary>
    /// Создаёт <c>BootData\Environment</c>, при необходимости переносит данные из <c>ReactiveCore</c>
    /// и создаёт пустые каталоги YAML.
    /// </summary>
    public static void EnsureCatalog()
    {
      string boot = EnvironmentPaths.GetBootDataFolder();
      if (string.IsNullOrWhiteSpace(boot))
        return;

      EnsureCatalogAt(boot);
    }

    /// <summary>Создаёт каталог среды под указанным BootData (для нового проекта).</summary>
    public static void EnsureCatalogAt(string bootDataFolder)
    {
      if (string.IsNullOrWhiteSpace(bootDataFolder))
        return;

      Directory.CreateDirectory(bootDataFolder);
      string envDir = Path.Combine(bootDataFolder, "Environment");
      Directory.CreateDirectory(envDir);

      string recipesPath = Path.Combine(envDir, "EnvironmentRecipes.yaml");
      string triggersPath = Path.Combine(envDir, "EnvironmentTriggers.yaml");

      if (!File.Exists(triggersPath))
        TryMigrateLegacyTriggers(bootDataFolder, triggersPath);

      if (!File.Exists(recipesPath))
        TryMigrateLegacyRecipes(bootDataFolder, recipesPath);

      WriteSeedIfMissing(recipesPath, EmptyRecipesSeed);
      WriteSeedIfMissing(triggersPath, EmptyTriggersSeed);
    }

    /// <summary>Загружает рецепты.</summary>
    public static List<EnvironmentRecipeData> LoadRecipes(IList<string> errors)
    {
      EnsureCatalog();
      return EnvironmentYamlCodec.ReadRecipes(EnvironmentPaths.RecipesFilePath, errors);
    }

    /// <summary>Сохраняет рецепты.</summary>
    public static void SaveRecipes(IReadOnlyList<EnvironmentRecipeData> recipes)
    {
      EnsureCatalog();
      EnvironmentYamlCodec.WriteRecipes(EnvironmentPaths.RecipesFilePath, recipes);
    }

    /// <summary>Загружает триггеры.</summary>
    public static List<EnvironmentTriggerData> LoadTriggers(IList<string> errors)
    {
      EnsureCatalog();
      return EnvironmentYamlCodec.ReadTriggers(EnvironmentPaths.TriggersFilePath, errors);
    }

    /// <summary>Сохраняет триггеры.</summary>
    public static void SaveTriggers(IReadOnlyList<EnvironmentTriggerData> triggers)
    {
      EnsureCatalog();
      EnvironmentYamlCodec.WriteTriggers(EnvironmentPaths.TriggersFilePath, triggers);
    }

    private static void WriteSeedIfMissing(string path, string content)
    {
      if (File.Exists(path))
        return;

      string dir = Path.GetDirectoryName(path);
      if (!string.IsNullOrEmpty(dir))
        Directory.CreateDirectory(dir);

      File.WriteAllText(path, content, new UTF8Encoding(false));
    }

    private static void TryMigrateLegacyTriggers(string bootDataFolder, string targetPath)
    {
      string legacy = Path.Combine(bootDataFolder, "ReactiveCore", "SwUserTriggers.yaml");
      if (!File.Exists(legacy))
        return;

      try
      {
        File.Copy(legacy, targetPath, overwrite: false);
      }
      catch
      {
        // ignore — пользователь может скопировать вручную
      }
    }

    private static void TryMigrateLegacyRecipes(string bootDataFolder, string targetPath)
    {
      string legacyDir = Path.Combine(bootDataFolder, "ReactiveCore", "Recipes");
      if (!Directory.Exists(legacyDir))
        return;

      var merged = new List<EnvironmentRecipeData>();
      var readErrors = new List<string>();

      foreach (string file in Directory.GetFiles(legacyDir, "*.yaml", SearchOption.TopDirectoryOnly))
      {
        List<EnvironmentRecipeData> batch = EnvironmentYamlCodec.ReadRecipes(file, readErrors);
        if (batch == null || batch.Count == 0)
          continue;

        foreach (EnvironmentRecipeData recipe in batch)
        {
          if (recipe == null || string.IsNullOrWhiteSpace(recipe.Id))
            continue;

          if (merged.Exists(r => string.Equals(r.Id, recipe.Id, StringComparison.OrdinalIgnoreCase)))
            continue;

          merged.Add(recipe);
        }
      }

      if (merged.Count == 0)
        return;

      try
      {
        EnvironmentYamlCodec.WriteRecipes(targetPath, merged);
      }
      catch
      {
        // ignore
      }
    }
  }
}
