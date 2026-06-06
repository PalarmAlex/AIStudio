using System;
using System.IO;
using ISIDA.SymbiontEnv.Contract;

namespace AIStudio.Common.Adapters
{
  /// <summary>
  /// Пути каталога зарегистрированных адаптеров (<c>%ProgramData%\ISIDA\Adapters</c>).
  /// </summary>
  public static class AdapterPaths
  {
    /// <summary>Корень: <c>%ProgramData%\ISIDA\Adapters</c>.</summary>
    public static readonly string AdaptersRootPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "ISIDA",
        "Adapters");
    /// <summary>Каркасы пакетов (не зарегистрированные адаптеры): <c>%ProgramData%\ISIDA\AdapterPackageTemplates</c>.</summary>
    public static readonly string PackageTemplatesRootPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "ISIDA",
        "AdapterPackageTemplates");
    /// <summary>Имя каталога каркаса пакета по умолчанию.</summary>
    public const string DemoTemplateFolderName = "demo";
    /// <summary>
    /// Путь к шаблону <c>demo</c>: <c>AdapterPackageTemplates\demo\</c> (кладёт установщик студии).
    /// </summary>
    public static string GetDemoTemplatePath()
    {
      return Path.Combine(PackageTemplatesRootPath, DemoTemplateFolderName);
    }

    /// <summary>
    /// Каталог установленного адаптера: <c>Adapters\{adapterId}\</c>.
    /// </summary>
    public static string GetAdapterDirectory(string adapterId)
    {
      if (string.IsNullOrWhiteSpace(adapterId))
        throw new ArgumentException("adapterId");
      return Path.Combine(AdaptersRootPath, adapterId.Trim());
    }

    /// <summary>
    /// Путь к <c>manifest.json</c> в каталоге адаптера.
    /// </summary>
    public static string GetManifestPath(string adapterDirectory)
    {
      return AdapterPackageLayout.GetManifestPath(adapterDirectory);
    }

    /// <summary>
    /// Каталог BootData пакета: <c>{adapterDir}\BootData\</c> или путь из manifest.
    /// </summary>
    public static string GetBootDataPath(AdapterManifest manifest)
    {
      return AdapterPackageLayout.GetBootDataPath(manifest);
    }

    /// <summary>
    /// <c>BootData\Environment\EnvironmentRecipes.yaml</c>.
    /// </summary>
    public static string GetBootDataRecipesPath(AdapterManifest manifest)
    {
      return AdapterPackageLayout.GetBootDataRecipesPath(manifest);
    }

    /// <summary>
    /// <c>BootData\Environment\EnvironmentTriggers.yaml</c>.
    /// </summary>
    public static string GetBootDataTriggersPath(AdapterManifest manifest)
    {
      return AdapterPackageLayout.GetBootDataTriggersPath(manifest);
    }

    /// <summary>
    /// Каталог <c>runtime\</c> пакета.
    /// </summary>
    public static string GetRuntimePath(string adapterDirectory)
    {
      return AdapterPackageLayout.GetRuntimePath(adapterDirectory);
    }

    /// <summary>
    /// Создаёт корень Adapters, если его ещё нет.
    /// </summary>
    public static string EnsureAdaptersRoot()
    {
      Directory.CreateDirectory(AdaptersRootPath);
      return AdaptersRootPath;
    }
  }
}
