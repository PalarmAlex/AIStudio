using System;
using System.IO;

namespace AIStudio.Common.SymbiontEnv
{
  /// <summary>
  /// Пути к файлам среды в каталоге BootData проекта (см. <see cref="AppConfig.BootDataFolderPath"/>).
  /// </summary>
  public static class EnvironmentPaths
  {
    /// <summary>
    /// Каталог BootData из настроек проекта (<c>%ProgramData%\…</c> разворачивается).
    /// </summary>
    public static string GetBootDataFolder()
    {
      string raw = AppConfig.BootDataFolderPath;
      if (string.IsNullOrWhiteSpace(raw))
      {
        raw = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "ISIDA",
            "BootData");
      }
      else
      {
        raw = Environment.ExpandEnvironmentVariables(raw.Trim());
      }
      try
      {
        return Path.GetFullPath(raw);
      }
      catch
      {
        return raw;
      }
    }

    /// <summary><c>BootData\Environment</c>.</summary>
    public static string EnvironmentFolder => Path.Combine(GetBootDataFolder(), "Environment");
    /// <summary>Файл рецептов среды.</summary>
    public static string RecipesFilePath => Path.Combine(EnvironmentFolder, "EnvironmentRecipes.yaml");
    /// <summary>Файл триггеров среды.</summary>
    public static string TriggersFilePath => Path.Combine(EnvironmentFolder, "EnvironmentTriggers.yaml");
  }
}
