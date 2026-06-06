using ISIDA.SymbiontEnv.Contract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AIStudio.Common.Adapters
{
  /// <summary>
  /// Сборка пакета адаптера из demo + runtime и регистрация в Adapters.
  /// </summary>
  public static class AdapterPackageBuilder
  {
    /// <summary>
    /// Копирует demo, подставляет стартовый SDK и опционально DLL host, проверяет пакет и регистрирует в Adapters.
    /// </summary>
    /// <param name="hostBinDirectoryOptional">Каталог bin\Debug host; <c>null</c> — только стартовый SDK из <c>AdapterPackageTemplates\demo\runtime</c>.</param>
    public static bool TryCreateAndRegisterFromDemo(
        AdapterManifest manifest,
        string hostBinDirectoryOptional,
        out string installedPath,
        out string errorMessage)
    {
      installedPath = null;
      errorMessage = null;
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
        Directory.CreateDirectory(runtimeTarget);
        if (!AdapterSdkRuntime.TryEnsureSdk(runtimeTarget, out errorMessage))
          return false;
        AdapterSdkRuntime.MergeHostBin(hostBinDirectoryOptional, runtimeTarget);
        if (!AdapterPackageManager.TryWriteManifest(tempRoot, manifest, out errorMessage))
          return false;
        IReadOnlyList<AdapterValidationMessage> validation = AdapterValidator.Validate(tempRoot);
        if (AdapterValidator.HasErrors(validation))
        {
          errorMessage = "Пакет не прошёл проверку. Исправьте ошибки и повторите.\n\n"
              + FormatValidation(validation);
          return false;
        }
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

    private static string FormatValidation(IReadOnlyList<AdapterValidationMessage> messages)
    {
      if (messages == null || messages.Count == 0)
        return string.Empty;
      return string.Join(
          Environment.NewLine,
          messages.Select(m => "[" + m.Severity + "] " + m.Text));
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
