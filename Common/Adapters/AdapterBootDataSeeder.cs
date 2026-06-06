using System;
using System.IO;
using ISIDA.SymbiontEnv.Contract;

namespace AIStudio.Common.Adapters
{
  /// <summary>
  /// Копирование BootData из зарегистрированного пакета в проект симбионта (seed, без перезаписи).
  /// </summary>
  public static class AdapterBootDataSeeder
  {
    /// <summary>
    /// Копирует <c>Adapters\{adapterId}\BootData\</c> → <c>{projectBootDataPath}\</c>; существующие файлы не перезаписываются.
    /// </summary>
    public static bool TrySeedFromInstalledAdapter(
        string adapterId,
        string projectBootDataPath,
        out string errorMessage)
    {
      errorMessage = null;
      if (string.IsNullOrWhiteSpace(adapterId))
      {
        errorMessage = "Не указан адаптер.";
        return false;
      }
      if (string.IsNullOrWhiteSpace(projectBootDataPath))
      {
        errorMessage = "Не указан каталог BootData проекта.";
        return false;
      }
      AdapterManifest manifest = AdapterRegistry.TryGetById(adapterId);
      if (manifest == null)
      {
        errorMessage = "Адаптер «" + adapterId + "» не установлен в " + AdapterPaths.AdaptersRootPath + ".";
        return false;
      }
      string sourceBoot = AdapterPaths.GetBootDataPath(manifest);
      if (!Directory.Exists(sourceBoot))
      {
        errorMessage = "В пакете адаптера нет BootData: " + sourceBoot;
        return false;
      }
      try
      {
        CopyDirectoryPreserveExisting(sourceBoot, projectBootDataPath);
        return true;
      }
      catch (Exception ex)
      {
        errorMessage = "Ошибка копирования BootData: " + ex.Message;
        return false;
      }
    }

    private static void CopyDirectoryPreserveExisting(string sourceDir, string targetDir)
    {
      Directory.CreateDirectory(targetDir);
      foreach (string file in Directory.GetFiles(sourceDir))
      {
        string dest = Path.Combine(targetDir, Path.GetFileName(file));
        if (!File.Exists(dest))
          File.Copy(file, dest, overwrite: false);
      }
      foreach (string sub in Directory.GetDirectories(sourceDir))
      {
        string name = Path.GetFileName(sub);
        CopyDirectoryPreserveExisting(sub, Path.Combine(targetDir, name));
      }
    }
  }
}
