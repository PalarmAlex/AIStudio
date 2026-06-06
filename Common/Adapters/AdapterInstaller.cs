using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ISIDA.SymbiontEnv.Contract;

namespace AIStudio.Common.Adapters
{
  /// <summary>
  /// Установка пакета адаптера в <see cref="AdapterPaths.AdaptersRootPath"/>.
  /// </summary>
  public static class AdapterInstaller
  {
    /// <summary>
    /// Устанавливает пакет из каталога (полное копирование в <c>Adapters\{id}\</c>).
    /// </summary>
    public static bool TryInstallFromDirectory(
        string sourceDirectory,
        bool replaceExisting,
        out string installedPath,
        out string errorMessage)
    {
      installedPath = null;
      errorMessage = null;
      if (string.IsNullOrWhiteSpace(sourceDirectory) || !Directory.Exists(sourceDirectory))
      {
        errorMessage = "Каталог пакета не найден.";
        return false;
      }
      string sourceRoot = Path.GetFullPath(sourceDirectory);
      if (!AdapterManifest.TryLoad(sourceRoot, out AdapterManifest manifest, out string manifestError))
      {
        errorMessage = manifestError;
        return false;
      }
      return TryInstallValidatedPackage(sourceRoot, manifest, replaceExisting, out installedPath, out errorMessage);
    }

    /// <summary>
    /// Устанавливает пакет из ZIP (распаковка во временный каталог, затем копирование).
    /// </summary>
    public static bool TryInstallFromZip(
        string zipFilePath,
        bool replaceExisting,
        out string installedPath,
        out string errorMessage)
    {
      installedPath = null;
      errorMessage = null;
      if (string.IsNullOrWhiteSpace(zipFilePath) || !File.Exists(zipFilePath))
      {
        errorMessage = "ZIP-файл не найден.";
        return false;
      }
      string tempRoot = Path.Combine(Path.GetTempPath(), "AIStudio.AdapterInstall", Guid.NewGuid().ToString("N"));
      try
      {
        Directory.CreateDirectory(tempRoot);
        ExtractZipSafe(zipFilePath, tempRoot);
        string packageRoot = ResolvePackageRootAfterExtract(tempRoot);
        if (packageRoot == null)
        {
          errorMessage = "В архиве не найден manifest.json (корень пакета).";
          return false;
        }
        return TryInstallFromDirectory(packageRoot, replaceExisting, out installedPath, out errorMessage);
      }
      catch (Exception ex)
      {
        errorMessage = "Ошибка распаковки ZIP: " + ex.Message;
        return false;
      }
      finally
      {
        TryDeleteDirectory(tempRoot);
      }
    }

    private static bool TryInstallValidatedPackage(
        string sourceRoot,
        AdapterManifest manifest,
        bool replaceExisting,
        out string installedPath,
        out string errorMessage)
    {
      installedPath = null;
      errorMessage = null;
      IReadOnlyList<AdapterValidationMessage> validation = AdapterValidator.Validate(sourceRoot);
      if (AdapterValidator.HasErrors(validation))
      {
        errorMessage = "Пакет не прошёл проверку. Исправьте ошибки и повторите установку.";
        return false;
      }
      AdapterPaths.EnsureAdaptersRoot();
      string targetDir = AdapterPaths.GetAdapterDirectory(manifest.Id);
      if (Directory.Exists(targetDir))
      {
        if (!replaceExisting)
        {
          errorMessage = "Адаптер «" + manifest.Id + "» уже установлен. Подтвердите замену.";
          return false;
        }
        TryDeleteDirectory(targetDir);
      }
      try
      {
        CopyDirectoryRecursive(sourceRoot, targetDir);
        installedPath = targetDir;
        AdapterRegistry.InvalidateCache();
        return true;
      }
      catch (Exception ex)
      {
        errorMessage = "Ошибка копирования пакета: " + ex.Message;
        TryDeleteDirectory(targetDir);
        return false;
      }
    }

    private static string ResolvePackageRootAfterExtract(string extractRoot)
    {
      if (File.Exists(AdapterPaths.GetManifestPath(extractRoot)))
        return extractRoot;
      string[] subDirs = Directory.GetDirectories(extractRoot);
      foreach (string dir in subDirs)
      {
        if (File.Exists(AdapterPaths.GetManifestPath(dir)))
          return dir;
      }
      return null;
    }

    private static void ExtractZipSafe(string zipFilePath, string destinationDirectory)
    {
      string destFull = Path.GetFullPath(destinationDirectory);
      if (!destFull.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        destFull += Path.DirectorySeparatorChar;
      using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
      {
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
          if (string.IsNullOrEmpty(entry.FullName))
            continue;
          string relative = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
          if (relative.Contains(".."))
            throw new InvalidDataException("Недопустимый путь в ZIP: " + entry.FullName);
          string targetPath = Path.GetFullPath(Path.Combine(destFull, relative));
          if (!targetPath.StartsWith(destFull, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Недопустимый путь в ZIP: " + entry.FullName);
          if (entry.FullName.EndsWith("/", StringComparison.Ordinal) ||
              entry.FullName.EndsWith("\\", StringComparison.Ordinal))
          {
            Directory.CreateDirectory(targetPath);
            continue;
          }
          string targetDir = Path.GetDirectoryName(targetPath);
          if (!string.IsNullOrEmpty(targetDir))
            Directory.CreateDirectory(targetDir);
          entry.ExtractToFile(targetPath, overwrite: true);
        }
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
