using System.IO;

namespace AIStudio.Common
{
  /// <summary>Каталог данных исследовательских прогонов (рядом со сценариями).</summary>
  public static class ResearchHarnessPaths
  {
    public const string RootFolderName = "ResearchHarness";

    /// <summary>Корень: …/Data/ResearchHarness.</summary>
    public static string RootFolder =>
        Path.Combine(Path.GetDirectoryName(AppConfig.DataActionsFolderPath) ?? "", RootFolderName);

    /// <summary>Подкаталог для одного прогона.</summary>
    public static string RunFolder(string runStamp) =>
        Path.Combine(RootFolder, "Runs", runStamp);
  }
}
