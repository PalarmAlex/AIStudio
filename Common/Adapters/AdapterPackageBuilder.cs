using ISIDA.SymbiontEnv.Contract;
using System;
using System.IO;

namespace AIStudio.Common.Adapters
{
  /// <summary>
  /// Сборка пакета адаптера из demo + runtime и регистрация в Adapters.
  /// </summary>
  public static class AdapterPackageBuilder
  {
    /// <summary>
    /// Копирует demo, подставляет стартовый SDK и опционально DLL host, регистрирует в Adapters.
    /// </summary>
    /// <param name="hostBinDirectoryOptional">Каталог bin\Debug host; <c>null</c> — только стартовый SDK из <c>AdapterPackageTemplates\demo\runtime</c>.</param>
    public static bool TryCreateAndRegisterFromDemo(
        AdapterManifest manifest,
        string hostBinDirectoryOptional,
        out string installedPath,
        out string errorMessage,
        out string warningMessage)
    {
      installedPath = null;
      errorMessage = null;
      warningMessage = null;
      if (!AdapterPackageManager.TryValidateManifest(manifest, isCreate: true, originalId: null, out errorMessage))
        return false;
      string demoPath = AdapterPaths.GetDemoTemplatePath();
      if (!Directory.Exists(demoPath))
      {
        errorMessage = "Каталог каркаса demo не найден:\n" + demoPath
            + "\n\nОн должен быть создан установщиком AIStudio.";
        return false;
      }
      string tempRoot = Path.Combine(Path.GetTempPath(), "AIStudio.AdapterBuild", Guid.NewGuid().ToString("N"));
      try
      {
        CopyDirectoryRecursive(demoPath, tempRoot);
        string runtimeTarget = Path.Combine(tempRoot, "runtime");
        AdapterSdkRuntime.EnsureSdk(runtimeTarget, out string sdkWarning);
        AdapterSdkRuntime.MergeHostBin(hostBinDirectoryOptional, runtimeTarget);
        if (!AdapterPackageManager.TryWriteManifest(tempRoot, manifest, out errorMessage))
          return false;
        warningMessage = sdkWarning;
        AdapterPaths.EnsureAdaptersRoot();
        string targetDir = AdapterPaths.GetAdapterDirectory(manifest.Id);
        if (Directory.Exists(targetDir))
        {
          errorMessage = "Адаптер с id «" + manifest.Id + "» уже зарегистрирован:\n" + targetDir;
          return false;
        }
        CopyDirectoryRecursive(tempRoot, targetDir);
        installedPath = targetDir;
        manifest.PackageRootPath = targetDir;
        AdapterRegistry.InvalidateCache();
        return true;
      }
      catch (Exception ex)
      {
        errorMessage = "Не удалось собрать пакет: " + ex.Message;
        return false;
      }
      finally
      {
        TryDeleteDirectory(tempRoot);
      }
    }

    private static void CopyDirectoryRecursive(string sourceDir, string targetDir)
    {
      Directory.CreateDirectory(targetDir);
      foreach (string file in Directory.GetFiles(sourceDir))
        File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), overwrite: true);
      foreach (string subDir in Directory.GetDirectories(sourceDir))
      {
        string name = Path.GetFileName(subDir);
        CopyDirectoryRecursive(subDir, Path.Combine(targetDir, name));
      }
    }

    private static void TryDeleteDirectory(string path)
    {
      if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        return;
      try
      {
        Directory.Delete(path, recursive: true);
      }
      catch
      {
        // best effort
      }
    }
  }
}
