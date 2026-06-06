using System;
using System.IO;
using System.Text.RegularExpressions;
using ISIDA.SymbiontEnv.Contract;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AIStudio.Common.Adapters
{
  /// <summary>
  /// Создание, изменение и удаление пакетов адаптеров в каталоге Adapters.
  /// </summary>
  public static class AdapterPackageManager
  {
    private static readonly Regex IdPattern = new Regex(
        @"^[a-z0-9_-]+$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);
    /// <summary>
    /// Проверяет поля manifest перед сохранением.
    /// </summary>
    public static bool TryValidateManifest(
        AdapterManifest manifest,
        bool isCreate,
        string originalId,
        out string errorMessage)
    {
      errorMessage = null;
      if (manifest == null)
      {
        errorMessage = "Не заданы данные пакета.";
        return false;
      }
      string id = (manifest.Id ?? string.Empty).Trim();
      if (string.IsNullOrEmpty(id))
      {
        errorMessage = "Укажите идентификатор адаптера (id).";
        return false;
      }
      if (!IdPattern.IsMatch(id))
      {
        errorMessage = "Идентификатор допускает только латинские буквы, цифры, «_» и «-» ([a-z0-9_-]+).";
        return false;
      }
      manifest.Id = id;
      if (string.IsNullOrWhiteSpace(manifest.DisplayName))
      {
        errorMessage = "Укажите отображаемое имя адаптера.";
        return false;
      }
      manifest.DisplayName = manifest.DisplayName.Trim();
      if (string.IsNullOrWhiteSpace(manifest.Version))
        manifest.Version = "0.1.0";
      else
        manifest.Version = manifest.Version.Trim();
      if (string.IsNullOrWhiteSpace(manifest.ContractVersion))
        manifest.ContractVersion = AdapterManifest.SupportedContractVersion;
      else if (!string.Equals(manifest.ContractVersion, AdapterManifest.SupportedContractVersion, StringComparison.Ordinal))
      {
        errorMessage = "Поддерживается только contractVersion «" + AdapterManifest.SupportedContractVersion + "».";
        return false;
      }
      if (string.IsNullOrWhiteSpace(manifest.BootDataRelativePath))
        manifest.BootDataRelativePath = "BootData";
      else
        manifest.BootDataRelativePath = manifest.BootDataRelativePath.Trim();
      if (!IsIdAvailable(id, isCreate ? null : originalId))
      {
        errorMessage = "Адаптер с id «" + id + "» уже зарегистрирован. Выберите уникальный идентификатор.";
        return false;
      }
      return true;
    }

    /// <summary>
    /// Проверяет, свободен ли идентификатор (кроме <paramref name="exceptId"/> при редактировании).
    /// </summary>
    public static bool IsIdAvailable(string id, string exceptId)
    {
      if (string.IsNullOrWhiteSpace(id))
        return false;
      string targetDir = AdapterPaths.GetAdapterDirectory(id.Trim());
      if (!Directory.Exists(targetDir))
        return true;
      if (!string.IsNullOrWhiteSpace(exceptId) &&
          string.Equals(id.Trim(), exceptId.Trim(), StringComparison.OrdinalIgnoreCase))
        return true;
      return false;
    }

    /// <summary>
    /// Копирует каркас demo в <c>Adapters\{id}\</c> и записывает manifest.
    /// </summary>
    public static bool TryCreateFromDemoTemplate(AdapterManifest manifest, out string installedPath, out string errorMessage)
    {
      installedPath = null;
      errorMessage = null;
      if (!TryValidateManifest(manifest, isCreate: true, originalId: null, out errorMessage))
        return false;
      string demoPath = AdapterPaths.GetDemoTemplatePath();
      if (!Directory.Exists(demoPath))
      {
        errorMessage = "Каталог каркаса demo не найден:\n" + demoPath
            + "\n\nОн должен быть создан установщиком AIStudio.";
        return false;
      }
      if (!AdapterManifest.TryLoad(demoPath, out _, out string demoManifestError))
      {
        errorMessage = "Каркас demo повреждён: " + demoManifestError;
        return false;
      }
      AdapterPaths.EnsureAdaptersRoot();
      string targetDir = AdapterPaths.GetAdapterDirectory(manifest.Id);
      if (Directory.Exists(targetDir))
      {
        errorMessage = "Каталог адаптера уже существует: " + targetDir;
        return false;
      }
      try
      {
        CopyDirectoryRecursive(demoPath, targetDir);
        if (!TryWriteManifest(targetDir, manifest, out errorMessage))
        {
          TryDeleteDirectory(targetDir);
          return false;
        }
        installedPath = targetDir;
        manifest.PackageRootPath = targetDir;
        AdapterRegistry.InvalidateCache();
        return true;
      }
      catch (Exception ex)
      {
        TryDeleteDirectory(targetDir);
        errorMessage = "Не удалось создать пакет: " + ex.Message;
        return false;
      }
    }

    /// <summary>
    /// Обновляет manifest.json зарегистрированного пакета; при смене id переименовывает каталог.
    /// </summary>
    public static bool TryUpdatePackage(
        AdapterManifest manifest,
        string packageRoot,
        string originalId,
        out string updatedPath,
        out string errorMessage)
    {
      updatedPath = packageRoot;
      errorMessage = null;
      if (string.IsNullOrWhiteSpace(packageRoot) || !Directory.Exists(packageRoot))
      {
        errorMessage = "Каталог пакета не найден.";
        return false;
      }
      if (!TryValidateManifest(manifest, isCreate: false, originalId: originalId, out errorMessage))
        return false;
      string root = Path.GetFullPath(packageRoot);
      string newDir = AdapterPaths.GetAdapterDirectory(manifest.Id);
      try
      {
        if (!string.Equals(manifest.Id, originalId, StringComparison.OrdinalIgnoreCase))
        {
          if (Directory.Exists(newDir))
          {
            errorMessage = "Адаптер с id «" + manifest.Id + "» уже зарегистрирован.";
            return false;
          }
          MoveDirectory(root, newDir);
          root = newDir;
        }
        if (!TryWriteManifest(root, manifest, out errorMessage))
          return false;
        manifest.PackageRootPath = root;
        updatedPath = root;
        AdapterRegistry.InvalidateCache();
        return true;
      }
      catch (Exception ex)
      {
        errorMessage = "Не удалось сохранить пакет: " + ex.Message;
        return false;
      }
    }

    /// <summary>
    /// Удаляет каталог зарегистрированного адаптера.
    /// </summary>
    public static bool TryDeletePackage(string adapterId, out string errorMessage)
    {
      errorMessage = null;
      if (string.IsNullOrWhiteSpace(adapterId))
      {
        errorMessage = "Не указан идентификатор адаптера.";
        return false;
      }
      string targetDir = AdapterPaths.GetAdapterDirectory(adapterId.Trim());
      if (!Directory.Exists(targetDir))
      {
        errorMessage = "Каталог адаптера не найден:\n" + targetDir;
        return false;
      }
      try
      {
        Directory.Delete(targetDir, recursive: true);
        AdapterRegistry.InvalidateCache();
        return true;
      }
      catch (Exception ex)
      {
        errorMessage = "Не удалось удалить каталог: " + ex.Message;
        return false;
      }
    }

    /// <summary>
    /// Записывает <c>manifest.json</c> в корень пакета.
    /// </summary>
    public static bool TryWriteManifest(string packageRoot, AdapterManifest manifest, out string errorMessage)
    {
      errorMessage = null;
      if (manifest == null)
      {
        errorMessage = "Не заданы данные manifest.";
        return false;
      }
      try
      {
        string manifestPath = AdapterPaths.GetManifestPath(packageRoot);
        var jo = new JObject
        {
          ["id"] = manifest.Id,
          ["displayName"] = manifest.DisplayName,
          ["version"] = manifest.Version,
          ["contractVersion"] = manifest.ContractVersion ?? AdapterManifest.SupportedContractVersion,
          ["author"] = manifest.Author ?? string.Empty,
          ["bootDataRelativePath"] = string.IsNullOrWhiteSpace(manifest.BootDataRelativePath)
              ? "BootData"
              : manifest.BootDataRelativePath
        };
        if (!string.IsNullOrWhiteSpace(manifest.SchemaVersion))
          jo["schemaVersion"] = manifest.SchemaVersion;
        if (!string.IsNullOrWhiteSpace(manifest.InstallerTemplateRelativePath))
          jo["installerTemplateRelativePath"] = manifest.InstallerTemplateRelativePath;
        if (!string.IsNullOrWhiteSpace(manifest.AdapterSettingsRelativePath))
          jo["adapterSettingsRelativePath"] = manifest.AdapterSettingsRelativePath;
        if (!string.IsNullOrWhiteSpace(manifest.Description))
          jo["description"] = manifest.Description;
        File.WriteAllText(manifestPath, jo.ToString(Formatting.Indented));
        return true;
      }
      catch (Exception ex)
      {
        errorMessage = "Ошибка записи manifest.json: " + ex.Message;
        return false;
      }
    }

    private static void MoveDirectory(string sourceDir, string targetDir)
    {
      if (string.Equals(
          Path.GetPathRoot(Path.GetFullPath(sourceDir)),
          Path.GetPathRoot(Path.GetFullPath(targetDir)),
          StringComparison.OrdinalIgnoreCase))
      {
        Directory.Move(sourceDir, targetDir);
        return;
      }
      CopyDirectoryRecursive(sourceDir, targetDir);
      TryDeleteDirectory(sourceDir);
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
