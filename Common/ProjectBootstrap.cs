using AIStudio.Common.SymbiontEnv;
using ISIDA.Common;
using System;
using System.IO;
using System.Text;
using System.Xml.Linq;
namespace AIStudio.Common
{
  /// <summary>
  /// Создание нового проекта данных ISIDA: каталоги по шаблону и минимальный набор файлов гомеостаза.
  /// </summary>
  public static class ProjectBootstrap
  {
    /// <summary>Каталог по умолчанию для поиска и создания проектов.</summary>
    public static readonly string DefaultProjectsParentPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "ISIDA", "Projects");

    /// <summary>
    /// Гарантирует существование <see cref="DefaultProjectsParentPath"/> и возвращает его.
    /// </summary>
    public static string EnsureDefaultProjectsParentPath()
    {
      Directory.CreateDirectory(DefaultProjectsParentPath);
      return DefaultProjectsParentPath;
    }

    /// <summary>
    /// Путь для <c>SelectedPath</c> в <c>VistaFolderBrowserDialog</c>: с завершающим «\», иначе Ookii открывает родительский каталог.
    /// </summary>
    public static string ToFolderDialogInitialPath(string folderPath)
    {
      if (string.IsNullOrWhiteSpace(folderPath))
        return folderPath;

      string full = Path.GetFullPath(folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
      if (Directory.Exists(full))
        return full + Path.DirectorySeparatorChar;

      return full + Path.DirectorySeparatorChar;
    }

    /// <summary>Начальный каталог диалога создания/выбора проекта: <see cref="DefaultProjectsParentPath"/>.</summary>
    public static string GetDefaultProjectsFolderDialogPath()
    {
      EnsureDefaultProjectsParentPath();
      return ToFolderDialogInitialPath(DefaultProjectsParentPath);
    }

    /// <summary>
    /// Нормализует путь из диалога и создаёт каталог, если его ещё нет (в т.ч. имя из поля «Папка»).
    /// </summary>
    public static bool TryEnsureFolderFromDialogSelection(
        string dialogSelectedPath,
        out string projectRoot,
        out string errorMessage)
    {
      projectRoot = null;
      errorMessage = null;

      if (string.IsNullOrWhiteSpace(dialogSelectedPath))
      {
        errorMessage = "Не указан каталог проекта.";
        return false;
      }

      try
      {
        string path = dialogSelectedPath.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (!Path.IsPathRooted(path))
          path = Path.Combine(DefaultProjectsParentPath, path);

        projectRoot = Path.GetFullPath(path);

        if (!Directory.Exists(projectRoot))
          Directory.CreateDirectory(projectRoot);

        return true;
      }
      catch (Exception ex)
      {
        errorMessage = "Не удалось создать каталог: " + ex.Message;
        return false;
      }
    }

    /// <summary>
    /// Создаёт структуру каталогов и минимальные данные в указанном корне проекта.
    /// </summary>
    /// <param name="projectRoot">Корневой каталог проекта (будет создан при отсутствии).</param>
    /// <param name="errorMessage">Текст ошибки при неуспехе.</param>
    /// <returns>True, если проект создан или дополнен без конфликтов.</returns>
    public static bool TryCreateProject(string projectRoot, out string errorMessage)
    {
      errorMessage = null;
      if (string.IsNullOrWhiteSpace(projectRoot))
      {
        errorMessage = "Не указан каталог проекта.";
        return false;
      }

      try
      {
        string rootFull = Path.GetFullPath(projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        if (ProjectAlreadyContainsSeedData(rootFull))
        {
          errorMessage = "В выбранном каталоге уже есть файлы данных проекта (например VitalParameters.dat). Выберите пустой каталог или другой путь.";
          return false;
        }

        EnsureProjectDirectoryStructure(rootFull);
        WriteMinimalSeedData(rootFull);
        WriteProjectSettingsXml(rootFull);
        return true;
      }
      catch (Exception ex)
      {
        errorMessage = ex.Message;
        return false;
      }
    }

    /// <summary>
    /// Создаёт каталоги по дереву <see cref="SettingsValidator.GetProjectDirectoryTemplateRoot"/> (узлы-файлы пропускаются).
    /// </summary>
    private static void EnsureProjectDirectoryStructure(string projectRoot)
    {
      string rootFull = Path.GetFullPath(projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
      Directory.CreateDirectory(rootFull);

      ProjectDirectoryTemplateNode templateRoot = SettingsValidator.GetProjectDirectoryTemplateRoot();
      for (int i = 0; i < templateRoot.Children.Count; i++)
        EnsureProjectDirectoryNode(rootFull, templateRoot.Children[i], null);
    }

    private static void EnsureProjectDirectoryNode(string projectRootFull, ProjectDirectoryTemplateNode node, string relativePath)
    {
      if (TemplateNodeLooksLikeFile(node.Name))
        return;

      string directoryPath = string.IsNullOrEmpty(relativePath)
          ? Path.Combine(projectRootFull, node.Name)
          : Path.Combine(projectRootFull, relativePath, node.Name);

      Directory.CreateDirectory(directoryPath);

      string childRelative = string.IsNullOrEmpty(relativePath)
          ? node.Name
          : Path.Combine(relativePath, node.Name);

      for (int i = 0; i < node.Children.Count; i++)
        EnsureProjectDirectoryNode(projectRootFull, node.Children[i], childRelative);
    }

    private static bool TemplateNodeLooksLikeFile(string name)
    {
      if (string.IsNullOrEmpty(name))
        return true;

      int dot = name.LastIndexOf('.');
      return dot > 0 && dot < name.Length - 1;
    }

    private static bool ProjectAlreadyContainsSeedData(string projectRootFull)
    {
      try
      {
        string gomeostasPath = SettingsValidator.GetExpectedFolderPathForSetting(projectRootFull, "DataGomeostasFolderPath");
        if (File.Exists(Path.Combine(gomeostasPath, "VitalParameters.dat")))
          return true;
        if (File.Exists(Path.Combine(gomeostasPath, "BehaviorStyles.dat")))
          return true;

        string actionsPath = SettingsValidator.GetExpectedFolderPathForSetting(projectRootFull, "DataActionsFolderPath");
        if (File.Exists(Path.Combine(actionsPath, "AdaptiveActions.dat")))
          return true;
        if (File.Exists(Path.Combine(actionsPath, "InfluenceActions.dat")))
          return true;
      }
      catch
      {
        // ignore — считаем, что конфликта нет
      }

      return false;
    }

    private static void WriteMinimalSeedData(string projectRootFull)
    {
      string gomeostasPath = SettingsValidator.GetExpectedFolderPathForSetting(projectRootFull, "DataGomeostasFolderPath");
      string actionsPath = SettingsValidator.GetExpectedFolderPathForSetting(projectRootFull, "DataActionsFolderPath");

      string sensorsPath = SettingsValidator.GetExpectedFolderPathForSetting(projectRootFull, "SensorsFolderPath");

      WriteFileIfMissing(Path.Combine(gomeostasPath, "VitalParameters.dat"), MinimalVitalParametersContent);
      WriteFileIfMissing(Path.Combine(gomeostasPath, "BehaviorStyles.dat"), MinimalBehaviorStylesContent);
      WriteFileIfMissing(Path.Combine(actionsPath, "AdaptiveActions.dat"), MinimalAdaptiveActionsContent);
      WriteFileIfMissing(Path.Combine(actionsPath, "InfluenceActions.dat"), MinimalInfluenceActionsContent);
      WriteFileIfMissing(Path.Combine(sensorsPath, "DefaultVerbalPrimaries.tmp"), MinimalDefaultVerbalPrimariesContent);

      string bootDataPath = SettingsValidator.GetExpectedFolderPathForSetting(projectRootFull, "BootDataFolderPath");
      EnvironmentCatalogStorage.EnsureCatalogAt(bootDataPath);
    }

    private static void WriteFileIfMissing(string path, string content)
    {
      if (File.Exists(path))
        return;

      string dir = Path.GetDirectoryName(path);
      if (!string.IsNullOrEmpty(dir))
        Directory.CreateDirectory(dir);

      File.WriteAllText(path, content, Encoding.UTF8);
    }

    private static void WriteProjectSettingsXml(string projectRootFull)
    {
      string settingsDir = SettingsValidator.GetExpectedFolderPathForSetting(projectRootFull, "SettingsPath");
      Directory.CreateDirectory(settingsDir);

      string settingsFile = Path.Combine(settingsDir, AppConfig.StudioSettingsFileName);
      if (File.Exists(settingsFile))
        return;

      var doc = new XDocument(
          new XElement("Configuration",
              new XElement("AppSettings",
                  new XElement("DataGomeostasFolderPath",
                      SettingsValidator.GetExpectedFolderPathForSetting(projectRootFull, "DataGomeostasFolderPath")),
                  new XElement("DataActionsFolderPath",
                      SettingsValidator.GetExpectedFolderPathForSetting(projectRootFull, "DataActionsFolderPath")),
                  new XElement("SensorsFolderPath",
                      SettingsValidator.GetExpectedFolderPathForSetting(projectRootFull, "SensorsFolderPath")),
                  new XElement("ReflexesFolderPath",
                      SettingsValidator.GetExpectedFolderPathForSetting(projectRootFull, "ReflexesFolderPath")),
                  new XElement("PsychicDataFolderPath",
                      SettingsValidator.GetExpectedFolderPathForSetting(projectRootFull, "PsychicDataFolderPath")),
                  new XElement("SettingsPath", settingsDir),
                  new XElement("LogsFolderPath",
                      SettingsValidator.GetExpectedFolderPathForSetting(projectRootFull, "LogsFolderPath")),
                  new XElement("BootDataFolderPath",
                      SettingsValidator.GetExpectedFolderPathForSetting(projectRootFull, "BootDataFolderPath")),
                  new XElement("ScenarioReportsFolderPath",
                      SettingsValidator.GetExpectedFolderPathForSetting(projectRootFull, "ScenarioReportsFolderPath")),
                  new XElement("DefaultStileId", 1),
                  new XElement("DefaultAdaptiveActionId", 1),
                  new XElement("DefaultThemeTypeId", 0),
                  new XElement("RecognitionThreshold", 3),
                  new XElement("CompareLevel", 30),
                  new XElement("DifSensorPar", 0.02),
                  new XElement("DynamicTime", 7),
                  new XElement("ReflexActionDisplayDuration", 2),
                  new XElement("WaitingPeriodForActionsVal", 30),
                  new XElement("ThinkingCycleDecayAgeDivisor", 100),
                  new XElement("ThinkingCycleDecayBase", 1),
                  new XElement("ThinkingCycleMainMaxAgePulses", 1000),
                  new XElement("NoOperatorStimulusSilencePulses", 30),
                  new XElement("FirstRun", 1),
                  new XElement("LogEnabled", false),
                  new XElement("DefaultFormatLog", "All"),
                  new XElement("HomeostasisPulseSpeedDriftEnabled", true))));

      doc.Save(settingsFile);
    }

    private const string MinimalVitalParametersContent =
@"# Формат: ID|Название|Описание|Значение|Вес|Норма|Скорость|Активации стилей|Критический|Мин.значение|Макс.значение
# Активации стилей: id1,id2,id3
1|Голод|Дефицит ориентированный параметр|50|50|70|-1|0:1,2;1:1;2:2|True|10|100
2|Стресс|Избыток ориентированный параметр|0|50|50|1|0:2,3;1:3;2:1|True|0|90
";

    private const string MinimalBehaviorStylesContent =
@"# Формат: ID|Имя|Описание|Антагонисты
# Антагонисты: id1,id2,id3
1|Поиск|Стратегия поиска решения|2,3
2|Ступор|Непонимание — бездействие|1,3
3|Расслабление|Спокойное состояние|1,2
";

    private const string MinimalAdaptiveActionsContent =
@"# Формат: ID|Имя|Описание|Интенсивность|Антагонисты|Target параметры|InfluenceActionId
# Антагонисты: id1,id2,id3
# Target параметры: id1,id2,id3
# InfluenceActionId: 0=нет связи, иначе ID действия с пульта
1|Ступор|Ничего не делать|5|2,3|1,2|0
2|Исследует|Изучение нового|5|1,3|1|0
3|Спит|Пассивный отдых|5|1,2|2|0
";

    private const string MinimalInfluenceActionsContent =
@"# Формат: ID|Имя|Описание|Воздействие|Антагонисты|EnvironmentMetricProbeKey
# Воздействие: paramId1:effect1;paramId2:effect2
# Антагонисты: id1,id2,id3
1|Наказать|Отрицательное подкрепление|1:3;2:2|2,3|
2|Поощрить|Положительное подкрепление|1:-3;2:-2|1,3|
3|Напугать|Повышение стресса|2:3|1,2|
";

    private const string MinimalDefaultVerbalPrimariesContent =
@"# Формат: Символ|#|ID
 |#|0
";
  }
}
