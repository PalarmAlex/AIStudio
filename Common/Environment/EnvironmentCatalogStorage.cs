using System.Collections.Generic;
using System.Linq;
using ISIDA.SymbiontEnv.Contract;

namespace AIStudio.Common.SymbiontEnv
{
  /// <summary>
  /// Загрузка и сохранение YAML каталогов среды (только isida + файлы проекта).
  /// </summary>
  public static class EnvironmentCatalogStorage
  {
    /// <summary>
    /// Создаёт <c>BootData\Environment</c> и пустые YAML-файлы, если их ещё нет.
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
      EnvironmentCatalogSeed.EnsureCatalogAt(bootDataFolder);
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

    /// <summary>
    /// Возвращает множество ID стимулов (<see cref="EnvironmentTriggerData.InfluenceActionId"/>), на которые ссылаются триггеры YAML.
    /// </summary>
    public static HashSet<int> GetReferencedInfluenceActionIds()
    {
      var errors = new List<string>();
      List<EnvironmentTriggerData> triggers = LoadTriggers(errors);
      var ids = new HashSet<int>();
      foreach (EnvironmentTriggerData trigger in triggers)
      {
        if (trigger != null && trigger.InfluenceActionId > 0)
          ids.Add(trigger.InfluenceActionId);
      }
      return ids;
    }
  }
}
