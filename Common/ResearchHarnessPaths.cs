using System.IO;

namespace AIStudio.Common
{
  /// <summary>Каталог данных исследовательских прогонов (задаётся в настройках, см. <see cref="AppConfig.ResearchHarnessOutputFolderPath"/>).</summary>
  public static class ResearchHarnessPaths
  {
    /// <summary>Корневой каталог выгрузки прогонов и отчётов.</summary>
    public static string RootFolder => AppConfig.ResearchHarnessOutputFolderPath;

    /// <summary>Подкаталог для одного прогона.</summary>
    public static string RunFolder(string runStamp) =>
        Path.Combine(RootFolder, "Runs", runStamp);

    /// <summary>Корень сохранённых сценариев: …\Scenarios.</summary>
    public static string ScenariosRootFolder => Path.Combine(RootFolder, "Scenarios");
  }
}
