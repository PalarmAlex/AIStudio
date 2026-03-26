using System.IO;

namespace AIStudio.Common
{
  /// <summary>Пути к файлам сценариев в каталоге данных ISIDA.</summary>
  public static class ScenarioPaths
  {
    public const string RegistryFileName = "ScenarioRegistry.dat";
    public const string ScenarioLinesFilePrefix = "Scenario_";
    public const string ScenarioLinesFileSuffix = ".dat";

    public static string RootFolder =>
        Path.Combine(Path.GetDirectoryName(AppConfig.DataActionsFolderPath) ?? "", "Scenarios");

    public static string RegistryPath => Path.Combine(RootFolder, RegistryFileName);

    public static string LinesPath(int scenarioId) =>
        Path.Combine(RootFolder, $"{ScenarioLinesFilePrefix}{scenarioId}{ScenarioLinesFileSuffix}");
  }
}
