using ISIDA.Common;
using ISIDA.Niche;
using ISIDA.Scenarios;
using static ISIDA.Common.FileValidator;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace AIStudio.Common
{
  /// <summary>
  /// Установка шаблонов сценариев и группы валидации триады (§6.14): фазы A/B/C.
  /// </summary>
  public static class TriadValidationScenarioBootstrap
  {
    /// <summary>Префикс названий сценариев валидации триады.</summary>
    public const string ScenarioTitlePrefix = "[Triad ";

    /// <summary>Название группы сценариев валидации.</summary>
    public const string GroupTitle = "[Triad] Валидация §6.14";

    private const int ReservedIdStart = 9001;

    /// <summary>
    /// Устанавливает сценарии, группу и preset Environment для валидации триады.
    /// </summary>
    /// <param name="overwriteExisting">Перезаписывать существующие сценарии с префиксом <see cref="ScenarioTitlePrefix"/>.</param>
    /// <param name="environmentFolder">Каталог Environment проекта.</param>
    /// <param name="message">Итоговое сообщение для пользователя.</param>
    /// <returns>True, если установка прошла без критических ошибок.</returns>
    public static bool TryInstall(bool overwriteExisting, string environmentFolder, out string message)
    {
      var log = new StringBuilder();
      try
      {
        ScenarioStorage.EnsureFolder();
        EnsureValidationEnvironmentPreset(environmentFolder);
        EnsureProbeInfluenceAction();

        var registry = ScenarioStorage.LoadRegistry();
        var groupRegistry = ScenarioGroupStorage.LoadGroupRegistry();

        int idA = EnsureScenario(registry, BuildPhaseAScenario(), overwriteExisting, log);
        int idB = EnsureScenario(registry, BuildPhaseBScenario(), overwriteExisting, log);
        int idC = EnsureScenario(registry, BuildPhaseCScenario(), overwriteExisting, log);

        registry = ScenarioStorage.LoadRegistry();
        var saveReg = ScenarioStorage.SaveRegistry(registry);
        if (!saveReg.Success)
        {
          message = "Ошибка реестра сценариев: " + saveReg.Error;
          return false;
        }

        int groupId = EnsureValidationGroup(groupRegistry, idA, idB, idC, overwriteExisting, log);

        string phaseError = null;
        if (!string.IsNullOrWhiteSpace(environmentFolder)
            && CouplingMappingLoader.TrySaveTriadPhase(environmentFolder, TriadPhase.A, out phaseError))
        {
          log.AppendLine("Фаза A записана в triad_config.dat.");
        }
        else if (!string.IsNullOrWhiteSpace(phaseError))
        {
          log.AppendLine("Фаза A не применена: " + phaseError);
        }

        log.AppendLine();
        log.AppendLine("Запуск: меню «Исследования» → «Группы сценариев», группа «" + GroupTitle + "» (id "
            + groupId.ToString(CultureInfo.InvariantCulture) + ").");
        log.AppendLine("Перед каждым сценарием примените соответствующую фазу на панели «Триада» (A/B/C).");
        log.AppendLine("Метрики: A — УР или automatizm с AOE>0; B — Belief=2, StimulusOrigin в логе диады;");
        log.AppendLine("C — episodic rule, изменение Niche через Operator→Niche (AgentLogs_Dyad.jsonl).");
        log.AppendLine("Contour §6.8: воздействие «Подогреть среду» (probe warm) + UseProbeContour=1 → contourInputDelta в логе диады.");

        message = log.ToString().Trim();
        return true;
      }
      catch (Exception ex)
      {
        message = "Ошибка установки: " + ex.Message;
        return false;
      }
    }

    /// <summary>
    /// Возвращает true, если в реестре уже есть сценарии валидации триады.
    /// </summary>
    public static bool IsValidationPackInstalled()
    {
      return ScenarioStorage.LoadRegistry()
          .Any(h => h.Title != null && h.Title.StartsWith(ScenarioTitlePrefix, StringComparison.Ordinal));
    }

    private static void EnsureProbeInfluenceAction()
    {
      if (!TriadProjectPaths.TryGetProjectRoot(out string projectRoot))
        return;

      string actionsFolder = SettingsValidator.GetExpectedFolderPathForSetting(projectRoot, "DataActionsFolderPath");
      string path = Path.Combine(actionsFolder, "InfluenceActions.dat");
      if (!File.Exists(path))
        return;

      string text = File.ReadAllText(path, Encoding.UTF8);
      if (text.IndexOf("|warm", StringComparison.OrdinalIgnoreCase) >= 0)
        return;

      const string probeLine = "4|Подогреть среду|Contour probe → Niche (§6.8)|1:0|0|warm\r\n";
      if (!text.EndsWith("\n", StringComparison.Ordinal))
        text += "\r\n";
      File.WriteAllText(path, text + probeLine, Encoding.UTF8);
    }

    private static void EnsureValidationEnvironmentPreset(string environmentFolder)
    {
      if (string.IsNullOrWhiteSpace(environmentFolder))
        return;

      TriadProjectPaths.EnsureTriadDataFoldersForRoot(null);
      CouplingMappingLoader.EnsureTemplateFiles(environmentFolder);

      WriteIfMissingOrTemplateOnly(
          Path.Combine(environmentFolder, "triad_config.dat"),
          isTemplate: text => text.Contains("static_mvp") || text.Contains("|B|"),
          ValidationTriadConfigBody);

      WriteIfMissingOrTemplateOnly(
          Path.Combine(environmentFolder, "operator_niche_coupling.dat"),
          isTemplate: text => CountDataLines(text) <= 1,
          FileHeaders.OperatorNicheCouplingFormat + "\r\n"
          + "# Валидация §6.14: воздействия пульта → Niche 101\r\n"
          + "1|101|5.0|1.0\r\n"
          + "2|101|-5.0|1.0\r\n"
          + "3|101|2.0|1.0\r\n");

      WriteIfMissingOrTemplateOnly(
          Path.Combine(environmentFolder, "action_coupling.dat"),
          isTemplate: text => CountDataLines(text) <= 1,
          FileHeaders.ActionCouplingFormat + "\r\n"
          + "# Валидация §6.14: действие Creature 1 → Niche 101\r\n"
          + "1|101|5.0|1.0\r\n");

      WriteIfMissingOrTemplateOnly(
          Path.Combine(environmentFolder, "niche_creature_mapping.dat"),
          isTemplate: text => CountDataLines(text) <= 1,
          FileHeaders.NicheCreatureMappingFormat + "\r\n"
          + "101|3|1.0|0\r\n");

      WriteIfMissingOrTemplateOnly(
          Path.Combine(environmentFolder, "niche_params.dat"),
          isTemplate: text => CountDataLines(text) <= 1,
          FileHeaders.NicheParamsFormat + "\r\n"
          + "101|50.0|0\r\n");

      WriteIfMissingOrTemplateOnly(
          Path.Combine(environmentFolder, "contour_probes.dat"),
          isTemplate: text => CountDataLines(text) <= 1,
          FileHeaders.ContourProbesFormat + "\r\n"
          + "warm|101|2.0\r\n");

      string readmePath = Path.Combine(environmentFolder, "TRIAD_VALIDATION_README.txt");
      if (!File.Exists(readmePath))
      {
        File.WriteAllText(readmePath, ValidationReadmeText, Encoding.UTF8);
      }
    }

    private static int CountDataLines(string text)
    {
      if (string.IsNullOrEmpty(text))
        return 0;
      return text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
          .Count(line =>
          {
            var t = line.Trim();
            return t.Length > 0 && !t.StartsWith("#", StringComparison.Ordinal);
          });
    }

    private static void WriteIfMissingOrTemplateOnly(string path, Func<string, bool> isTemplate, string content)
    {
      if (!File.Exists(path))
      {
        File.WriteAllText(path, content, Encoding.UTF8);
        return;
      }

      string existing = File.ReadAllText(path, Encoding.UTF8);
      if (isTemplate(existing))
        File.WriteAllText(path, content, Encoding.UTF8);
    }

    private const string ValidationTriadConfigBody =
        FileHeaders.TriadConfigFormat + "\r\n"
        + FileHeaders.TriadConfigAoeParams + "\r\n"
        + FileHeaders.TriadConfigEngineParams + "\r\n"
        + "A|triad_validation|0|2|20|0.5|3|30|1|niche_reactive|1\r\n";

    private const string ValidationReadmeText =
        "Валидация триады §6.14\r\n"
        + "========================\r\n\r\n"
        + "Перед прогоном сценариев [Triad A/B/C] примените фазу на панели «Триада (Creature↔Niche)».\r\n\r\n"
        + "  A — CS/US через Operator (стадия 2); успех = УР или automatizm с AOE>0\r\n"
        + "  B — ритуал + coupling Niche (стадия 3); успех = Belief=2, StimulusOrigin в логе\r\n"
        + "  C — Operator только через Niche (стадия 4); успех = episodic rule, Δ Niche в AgentLogs_Dyad.jsonl\r\n\r\n"
        + "Contour §6.8: воздействие id=4 «Подогреть среду» (EnvironmentMetricProbeKey=warm) при UseProbeContour=1\r\n"
        + "  → InputSnapshot → Δ Niche; проверка: contourProbeKey в AgentLogs_Dyad.jsonl.\r\n\r\n"
        + "Группа сценариев: «[Triad] Валидация §6.14» в реестре групп (Исследования).\r\n";

    private static int EnsureScenario(
        List<ScenarioHeader> registry,
        ScenarioDocument doc,
        bool overwriteExisting,
        StringBuilder log)
    {
      var existing = registry.FirstOrDefault(h =>
          string.Equals(h.Title, doc.Header.Title, StringComparison.Ordinal));

      if (existing != null && !overwriteExisting)
      {
        log.AppendLine("Сценарий «" + doc.Header.Title + "» уже есть (id "
            + existing.Id.ToString(CultureInfo.InvariantCulture) + "), пропуск.");
        return existing.Id;
      }

      int id = existing?.Id ?? AllocateScenarioId(registry);
      doc.Header.Id = id;

      var saveLines = ScenarioStorage.SaveScenarioLines(doc);
      if (!saveLines.Success)
        throw new InvalidOperationException("Сценарий «" + doc.Header.Title + "»: " + saveLines.Error);

      UpsertRegistryHeader(registry, doc.Header);

      log.AppendLine((existing != null ? "Обновлён" : "Создан") + " сценарий «" + doc.Header.Title + "» (id "
          + id.ToString(CultureInfo.InvariantCulture) + ").");
      return id;
    }

    private static void UpsertRegistryHeader(List<ScenarioHeader> registry, ScenarioHeader header)
    {
      registry.RemoveAll(h => h.Id == header.Id);
      registry.Add(header.Clone());
    }

    private static int EnsureValidationGroup(
        List<ScenarioGroupHeader> groupRegistry,
        int idA,
        int idB,
        int idC,
        bool overwriteExisting,
        StringBuilder log)
    {
      var existing = groupRegistry.FirstOrDefault(g =>
          string.Equals(g.Title, GroupTitle, StringComparison.Ordinal));

      if (existing != null && !overwriteExisting)
      {
        log.AppendLine("Группа «" + GroupTitle + "» уже есть (id "
            + existing.Id.ToString(CultureInfo.InvariantCulture) + "), пропуск.");
        return existing.Id;
      }

      int groupId = existing?.Id ?? AllocateGroupId(groupRegistry);
      var doc = new ScenarioGroupDocument
      {
        Id = groupId,
        Title = GroupTitle,
        Description =
            "Последовательная валидация триады §6.14. Перед каждым сценарием примените фазу A/B/C на панели «Триада». "
            + "Проверяйте AgentLogs_Dyad.jsonl и отчёт сценария.",
        RunPulseTimingCoefficient = 10,
        ReportFormat = ScenarioGroupReportFormat.Detailed,
        Members = new List<ScenarioGroupMemberRow>
        {
          Member(1, idA, 2),
          Member(2, idB, 3),
          Member(3, idC, 4)
        }
      };

      var saveGroup = ScenarioGroupStorage.SaveGroup(doc);
      if (!saveGroup.Success)
        throw new InvalidOperationException("Группа: " + saveGroup.Error);

      UpsertGroupHeader(groupRegistry, new ScenarioGroupHeader
      {
        Id = groupId,
        Title = doc.Title,
        Description = doc.Description
      });

      var saveReg = ScenarioGroupStorage.SaveGroupRegistry(groupRegistry);
      if (!saveReg.Success)
        throw new InvalidOperationException("Реестр групп: " + saveReg.Error);

      log.AppendLine((existing != null ? "Обновлена" : "Создана") + " группа «" + GroupTitle + "» (id "
          + groupId.ToString(CultureInfo.InvariantCulture) + ").");
      return groupId;
    }

    private static ScenarioGroupMemberRow Member(int sort, int scenarioId, int stage)
    {
      return new ScenarioGroupMemberRow
      {
        SortOrderInGroup = sort,
        ScenarioId = scenarioId,
        PreRunTargetStage = stage,
        PreRunClearAgentData = true,
        PreRunNormalHomeostasisState = true,
        ScenarioObservationMode = false,
        ScenarioAuthoritativeRecording = true
      };
    }

    private static void UpsertGroupHeader(List<ScenarioGroupHeader> registry, ScenarioGroupHeader header)
    {
      registry.RemoveAll(h => h.Id == header.Id);
      registry.Add(new ScenarioGroupHeader
      {
        Id = header.Id,
        Title = header.Title,
        Description = header.Description
      });
    }

    private static int AllocateScenarioId(List<ScenarioHeader> registry)
    {
      var used = new HashSet<int>(registry.Select(h => h.Id));
      for (int id = ReservedIdStart; id < ReservedIdStart + 100; id++)
      {
        if (!used.Contains(id))
          return id;
      }

      return registry.Count == 0 ? 1 : registry.Max(h => h.Id) + 1;
    }

    private static int AllocateGroupId(List<ScenarioGroupHeader> registry)
    {
      var used = new HashSet<int>(registry.Select(h => h.Id));
      for (int id = ReservedIdStart; id < ReservedIdStart + 100; id++)
      {
        if (!used.Contains(id))
          return id;
      }

      return registry.Count == 0 ? 1 : registry.Max(h => h.Id) + 1;
    }

    private static ScenarioDocument BuildPhaseAScenario()
    {
      var header = BaseHeader(
          "[Triad A] CS/US через Operator",
          "Фаза A, стадия 2. Условный стимул (фраза) + безусловное подкрепление (Поощрить). "
          + "Перед запуском: фаза A на панели «Триада». Метрика: УР или automatizm с AOE>0 после повторного CS.",
          2);

      var doc = new ScenarioDocument { Header = header };
      doc.Lines.Add(Pult(1, 1, "триада_а", null));
      doc.Lines.Add(Pult(2, 2, null, 2));
      doc.Lines.Add(Wait(3, 3));
      doc.Lines.Add(Pult(4, 4, "триада_а", null));
      doc.Lines.Add(Wait(5, 5));

      doc.LogExpectationColumnSkips = SkipMostColumns();
      doc.LogExpectations = new List<ScenarioLogExpectationRow>
      {
        Expect(4, 4, conditionReflex: "1")
      };
      return doc;
    }

    private static ScenarioDocument BuildPhaseBScenario()
    {
      var header = BaseHeader(
          "[Triad B] Ритуал + coupling Niche",
          "Фаза B, стадия 3. Ритуальная фраза + подкрепление; coupling Creature→Niche активен. "
          + "Перед запуском: фаза B на панели «Триада», перезагрузка конфига. "
          + "Метрика: Belief=2 (эхо-автоматизм), StimulusOrigin=Niche в AgentLogs_Dyad.jsonl.",
          3);

      var doc = new ScenarioDocument { Header = header };
      doc.Lines.Add(Pult(1, 1, "привет", null));
      doc.Lines.Add(Pult(2, 2, null, 2));
      doc.Lines.Add(Wait(3, 3));
      doc.Lines.Add(Pult(4, 4, "привет", null));
      doc.Lines.Add(Wait(5, 5));

      doc.LogExpectationColumnSkips = SkipMostColumns();
      doc.LogExpectations = new List<ScenarioLogExpectationRow>
      {
        Expect(4, 4, automatizm: "2", usefulness: "2")
      };
      return doc;
    }

    private static ScenarioDocument BuildPhaseCScenario()
    {
      var header = BaseHeader(
          "[Triad C] Operator только через Niche",
          "Фаза C, стадия 4. Воздействия пульта маршрутизируются в Niche (operator_niche_coupling.dat). "
          + "Перед запуском: фаза C на панели «Триада». "
          + "Метрика: изменение параметра Niche 101 в AgentLogs_Dyad.jsonl; episodic rule при AOE Niche.",
          4);

      var doc = new ScenarioDocument { Header = header };
      doc.Lines.Add(Pult(1, 1, null, 1));
      doc.Lines.Add(Wait(2, 2));
      doc.Lines.Add(Pult(3, 3, null, 2));
      doc.Lines.Add(Wait(4, 4));
      doc.Lines.Add(Pult(5, 5, null, 3));
      doc.Lines.Add(Wait(6, 6));

      doc.LogExpectationColumnSkips = SkipMostColumns();
      return doc;
    }

    private static ScenarioHeader BaseHeader(string title, string description, int stage)
    {
      return new ScenarioHeader
      {
        Title = title,
        Description = description,
        PreRunTargetStage = stage,
        PreRunClearAgentData = true,
        PreRunNormalHomeostasisState = true,
        ScenarioObservationMode = false,
        ScenarioAuthoritativeRecording = true,
        PulseStepIncrement = (int)ScenarioPulseStepIncrement.ActionHoldPlusOne,
        RunPulseTimingCoefficient = 10
      };
    }

    private static ScenarioLineRow Pult(int step, int pulse, string phrase, int? actionId)
    {
      var row = new ScenarioLineRow
      {
        StepIndex = step,
        PulseWithinScenario = pulse,
        Kind = ScenarioLineKind.Pult,
        Phrase = phrase ?? string.Empty
      };
      if (actionId.HasValue)
        row.ActionIds.Add(actionId.Value);
      return row;
    }

    private static ScenarioLineRow Wait(int step, int pulse)
    {
      return new ScenarioLineRow
      {
        StepIndex = step,
        PulseWithinScenario = pulse,
        Kind = ScenarioLineKind.WaitClick
      };
    }

    private static ScenarioLogExpectationColumnSkips SkipMostColumns()
    {
      return new ScenarioLogExpectationColumnSkips
      {
        SkipState = true,
        SkipStyle = true,
        SkipTheme = true,
        SkipTrigger = true,
        SkipOrUm = true,
        SkipDanger = true,
        SkipVeryActual = true,
        SkipGeneticReflex = true,
        SkipReflexChain = true,
        SkipAutomatizmChain = true,
        SkipMainCycle = true,
        SkipBackgroundCycles = true
      };
    }

    private static ScenarioLogExpectationRow Expect(
        int step,
        int pulse,
        string conditionReflex = null,
        string automatizm = null,
        string usefulness = null)
    {
      return new ScenarioLogExpectationRow
      {
        StepIndex = step,
        PulseWithinScenario = pulse,
        ConditionReflexText = conditionReflex ?? "-",
        AutomatizmText = automatizm ?? "-",
        AutomatizmUsefulnessText = usefulness ?? "-"
      };
    }
  }
}
