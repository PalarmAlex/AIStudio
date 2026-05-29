using ISIDA.Common;
using ISIDA.Niche;
using System;
using System.IO;

namespace AIStudio.Common
{
  /// <summary>
  /// Пути каталогов триады Creature↔Niche относительно корня проекта данных.
  /// </summary>
  public static class TriadProjectPaths
  {
    /// <summary>
    /// Возвращает корень проекта по каталогу Settings или null.
    /// </summary>
    /// <param name="projectRoot">Корень проекта при успехе.</param>
    /// <returns>True, если корень определён.</returns>
    public static bool TryGetProjectRoot(out string projectRoot)
    {
      return SettingsValidator.TryGetProjectRootFromSettingsPath(AppConfig.SettingsPath, out projectRoot);
    }

    /// <summary>
    /// Каталог Environment (coupling, triad_config) для текущего проекта.
    /// </summary>
    /// <returns>Путь к Environment.</returns>
    public static string GetEnvironmentFolder()
    {
      if (TryGetProjectRoot(out string projectRoot))
        return Path.Combine(projectRoot, "Environment");

      return Path.Combine(
          System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData),
          "ISIDA",
          "Environment");
    }

    /// <summary>
    /// Каталог Data/Niche (рефлексы Niche) для текущего проекта.
    /// </summary>
    /// <returns>Путь к Data/Niche.</returns>
    public static string GetNicheDataFolder()
    {
      if (TryGetProjectRoot(out string projectRoot))
        return Path.Combine(projectRoot, "Data", "Niche");

      return Path.Combine(
          System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData),
          "ISIDA",
          "Data",
          "Niche");
    }

    /// <summary>
    /// Создаёт каталоги и шаблоны конфигурации триады, если их ещё нет.
    /// </summary>
    public static void EnsureTriadDataFolders()
    {
      EnsureTriadDataFoldersForRoot(null);
    }

    /// <summary>
    /// Создаёт Environment и Data/Niche в указанном корне проекта.
    /// </summary>
    /// <param name="projectRoot">Корень проекта или null (текущий проект).</param>
    public static void EnsureTriadDataFoldersForRoot(string projectRoot)
    {
      string env;
      string niche;
      if (!string.IsNullOrWhiteSpace(projectRoot))
      {
        env = Path.Combine(projectRoot, "Environment");
        niche = Path.Combine(projectRoot, "Data", "Niche");
      }
      else
      {
        env = GetEnvironmentFolder();
        niche = GetNicheDataFolder();
      }

      Directory.CreateDirectory(env);
      Directory.CreateDirectory(niche);
      CouplingMappingLoader.EnsureTemplateFiles(env);
      NicheSymbiontBootstrap.EnsureSymbiontLayout(niche);
    }

    /// <summary>Data/Niche/Gomeostas для текущего проекта.</summary>
    public static string GetNicheGomeostasFolder()
    {
      return NicheSymbiontBootstrap.GetGomeostasFolder(GetNicheDataFolder());
    }

    /// <summary>Data/Niche/Reflexes для текущего проекта.</summary>
    public static string GetNicheReflexesFolder()
    {
      return NicheSymbiontBootstrap.GetReflexesFolder(GetNicheDataFolder());
    }
  }
}
