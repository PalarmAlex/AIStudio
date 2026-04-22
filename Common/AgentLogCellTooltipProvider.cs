using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Gomeostas;
using ISIDA.Psychic;
using ISIDA.Psychic.Automatism;
using ISIDA.Psychic.Understanding;
using ISIDA.Reflexes;
using ISIDA.Sensors;

namespace AIStudio.Common
{
  /// <summary>Текстовые подсказки для ячеек агентного лога (живые логи, HTML-отчёты сценариев).</summary>
  public sealed class AgentLogCellTooltipProvider
  {
    private readonly GomeostasSystem _gomeostas;
    private readonly PerceptionImagesSystem _perceptionImagesSystem;
    private readonly InfluenceActionSystem _influenceActionSystem;
    private readonly VerbalSensorChannel _verbalSensor;
    private readonly AdaptiveActionsSystem _adaptiveActionsSystem;
    private readonly GeneticReflexesSystem _geneticReflexesSystem;
    private readonly ConditionedReflexesSystem _conditionedReflexesSystem;
    private readonly AutomatizmSystem _automatizmSystem;
    private readonly ActionsImagesSystem _actionsImagesSystem;

    public AgentLogCellTooltipProvider(
        GomeostasSystem gomeostas,
        PerceptionImagesSystem perceptionImagesSystem,
        InfluenceActionSystem influenceActionSystem,
        VerbalSensorChannel verbalSensor,
        AdaptiveActionsSystem adaptiveActionsSystem,
        GeneticReflexesSystem geneticReflexesSystem,
        ConditionedReflexesSystem conditionedReflexesSystem,
        AutomatizmSystem automatizmSystem,
        ActionsImagesSystem actionsImagesSystem)
    {
      _gomeostas = gomeostas ?? throw new ArgumentNullException(nameof(gomeostas));
      _perceptionImagesSystem = perceptionImagesSystem ?? throw new ArgumentNullException(nameof(perceptionImagesSystem));
      _influenceActionSystem = influenceActionSystem ?? throw new ArgumentNullException(nameof(influenceActionSystem));
      _verbalSensor = verbalSensor;
      _adaptiveActionsSystem = adaptiveActionsSystem ?? throw new ArgumentNullException(nameof(adaptiveActionsSystem));
      _geneticReflexesSystem = geneticReflexesSystem ?? throw new ArgumentNullException(nameof(geneticReflexesSystem));
      _conditionedReflexesSystem = conditionedReflexesSystem ?? throw new ArgumentNullException(nameof(conditionedReflexesSystem));
      _automatizmSystem = automatizmSystem ?? throw new ArgumentNullException(nameof(automatizmSystem));
      _actionsImagesSystem = actionsImagesSystem ?? throw new ArgumentNullException(nameof(actionsImagesSystem));
    }

    public string GetReflexChainTooltip(string chainInfo)
    {
      if (string.IsNullOrEmpty(chainInfo) || chainInfo == "-")
        return "Нет активных цепочек рефлексов";

      var parts = chainInfo.Split(':');
      if (parts.Length != 2 || !int.TryParse(parts[1], out int actionId) || actionId <= 0)
        return "Неверный формат цепочки рефлекса";

      try
      {
        var allActions = _adaptiveActionsSystem.GetAllAdaptiveActions();
        var action = allActions.FirstOrDefault(a => a.Id == actionId);
        return action != null ? action.Name : $"Действие {actionId}";
      }
      catch (Exception ex)
      {
        return $"Ошибка загрузки действия: {ex.Message}";
      }
    }

    public string GetAutomatizmChainTooltip(string chainInfo)
    {
      if (string.IsNullOrEmpty(chainInfo) || chainInfo == "-")
        return "Нет активных цепочек автоматизмов";

      var parts = chainInfo.Split(':');
      if (parts.Length != 2 || !int.TryParse(parts[1], out int actionId) || actionId <= 0)
        return "Неверный формат цепочки автоматизма";

      try
      {
        var allActions = _adaptiveActionsSystem.GetAllAdaptiveActions();
        var action = allActions.FirstOrDefault(a => a.Id == actionId);
        return action != null ? action.Name : $"Действие {actionId}";
      }
      catch (Exception ex)
      {
        return $"Ошибка загрузки действия: {ex.Message}";
      }
    }

    /// <summary>ID образа стиля (одно число) → названия стилей.</summary>
    public string GetStyleTooltip(string displayBaseStyleID)
    {
      if (string.IsNullOrEmpty(displayBaseStyleID) || !int.TryParse(displayBaseStyleID, out int imageId) || imageId <= 0)
        return "Нет данных о стилях";

      try
      {
        var styleImages = _perceptionImagesSystem.GetAllBehaviorStyleImagesList();
        var styleImage = styleImages.FirstOrDefault(img => img.Id == imageId);

        if (styleImage != null && styleImage.BehaviorStylesList.Any())
        {
          var allStyles = _gomeostas.GetAllBehaviorStyles();

          var styleNames = styleImage.BehaviorStylesList
              .Select(styleId => allStyles.ContainsKey(styleId) ? allStyles[styleId].Name : $"Стиль {styleId}")
              .Where(name => !string.IsNullOrEmpty(name));

          return string.Join(", ", styleNames);
        }
      }
      catch (Exception ex)
      {
        return $"Ошибка загрузки стилей: {ex.Message}";
      }

      return "Нет данных о стилях";
    }

    /// <summary>Ячейка «Стиль» в отчёте: коды комбинации «1,2,3» или одно число.</summary>
    public string GetStyleCellTooltipForReport(string cellRaw)
    {
      if (string.IsNullOrWhiteSpace(cellRaw))
        return "Нет данных о стилях";
      var t = cellRaw.Trim();
      if (t == "-")
        return "Нет данных о стилях";

      if (t.IndexOf(',') >= 0)
        return GetStyleCombinationNamesFromCodes(t);

      if (int.TryParse(t, out int id) && id > 0)
      {
        var byImage = GetStyleTooltip(t);
        if (byImage != "Нет данных о стилях")
          return byImage;
        return GetSingleBehaviorStyleName(id);
      }

      return "Нет данных о стилях";
    }

    private string GetStyleCombinationNamesFromCodes(string commaSeparated)
    {
      try
      {
        var parts = commaSeparated.Split(',');
        var ids = new List<int>();
        foreach (var p in parts)
        {
          if (int.TryParse(p.Trim(), out int sid) && sid > 0)
            ids.Add(sid);
        }
        if (!ids.Any())
          return "Нет данных о стилях";

        var allStyles = _gomeostas.GetAllBehaviorStyles();
        var names = ids
            .OrderBy(x => x)
            .Select(styleId => allStyles.ContainsKey(styleId) ? allStyles[styleId].Name : $"Стиль {styleId}")
            .Where(name => !string.IsNullOrEmpty(name));
        var list = names.ToList();
        return list.Any() ? string.Join(", ", list) : "Нет данных о стилях";
      }
      catch (Exception ex)
      {
        return $"Ошибка загрузки стилей: {ex.Message}";
      }
    }

    private string GetSingleBehaviorStyleName(int styleId)
    {
      try
      {
        var allStyles = _gomeostas.GetAllBehaviorStyles();
        if (allStyles.ContainsKey(styleId))
          return allStyles[styleId].Name ?? $"Стиль {styleId}";
      }
      catch
      {
        // ignored
      }
      return $"Стиль {styleId}";
    }

    public string GetTriggerTooltip(string displayTriggerStimulusID)
    {
      if (string.IsNullOrEmpty(displayTriggerStimulusID) || !int.TryParse(displayTriggerStimulusID, out int imageId) || imageId <= 0)
        return "Нет данных о триггере";

      try
      {
        var perceptionImages = _perceptionImagesSystem.GetAllPerceptionImagesList();
        var perceptionImage = perceptionImages.FirstOrDefault(img => img.Id == imageId);

        if (perceptionImage != null)
        {
          string influenceLine;
          if (perceptionImage.InfluenceActionsList?.Any() == true)
          {
            var allInfluences = _influenceActionSystem.GetAllInfluenceActions();
            var influenceNames = perceptionImage.InfluenceActionsList
                .Select(actionId => allInfluences.FirstOrDefault(a => a.Id == actionId)?.Name ?? $"Воздействие {actionId}")
                .Where(name => !string.IsNullOrEmpty(name));
            influenceLine = influenceNames.Any() ? string.Join(", ", influenceNames) : "нет";
          }
          else
            influenceLine = "нет";

          string phrasesLine;
          if (perceptionImage.PhraseIdList?.Any() == true)
          {
            var phraseNames = perceptionImage.PhraseIdList
                .Select(phraseId => _verbalSensor?.GetPhraseFromPhraseId(phraseId) ?? $"Фраза {phraseId}")
                .Where(phrase => !string.IsNullOrEmpty(phrase));
            phrasesLine = phraseNames.Any() ? string.Join(", ", phraseNames) : "нет";
          }
          else
            phrasesLine = "нет";

          string toneLine = "—";
          string moodLine = "—";
          if (VerbalBrocaImagesSystem.IsInitialized && perceptionImage.PhraseIdList?.Any() == true)
          {
            var broca = VerbalBrocaImagesSystem.Instance.GetAllVerbalBrocaImagesList()
                .FirstOrDefault(v => AddUtils.AreListsEqual(v.PhraseIdList, perceptionImage.PhraseIdList));
            if (broca != null)
            {
              var t = ActionsImagesSystem.GetToneText(broca.ToneId);
              toneLine = string.IsNullOrEmpty(t) ? broca.ToneId.ToString() : $"{t} ({broca.ToneId})";
              var m = ActionsImagesSystem.GetMoodText(broca.MoodId);
              moodLine = string.IsNullOrEmpty(m) ? broca.MoodId.ToString() : $"{m} ({broca.MoodId})";
            }
          }

          int colorCode = AgentVisualColor.IsValidCode(perceptionImage.VisualColorId)
              ? perceptionImage.VisualColorId
              : AgentVisualColor.White;
          string colorLine = AgentVisualColor.GetDisplayName(colorCode);

          return "Воздействие: " + influenceLine
              + "\nФразы: " + phrasesLine
              + "\nТон: " + toneLine
              + "\nНастроение: " + moodLine
              + "\nЦветовой фон: " + colorLine;
        }
      }
      catch (Exception ex)
      {
        return $"Ошибка загрузки триггера: {ex.Message}";
      }

      return "Нет данных о триггере";
    }

    public string GetActionsForGeneticReflex(string displayReflexID)
    {
      if (string.IsNullOrEmpty(displayReflexID) || !int.TryParse(displayReflexID, out int reflexId) || reflexId <= 0)
        return "Нет данных о действиях рефлекса";

      try
      {
        var reflex = _geneticReflexesSystem.GetAllGeneticReflexesList()
            .FirstOrDefault(r => r.Id == reflexId);

        if (reflex != null)
        {
          var tooltipParts = new List<string>();
          var allActions = _adaptiveActionsSystem.GetAllAdaptiveActions();

          var actionNames = reflex.AdaptiveActions
              .Select(actionId => allActions.FirstOrDefault(a => a.Id == actionId)?.Name ?? $"Действие {actionId}")
              .Where(name => !string.IsNullOrEmpty(name));

          if (actionNames.Any())
            tooltipParts.Add($"Действия: {string.Join(", ", actionNames)}");

          return tooltipParts.Any() ? string.Join("\n", tooltipParts) : "Пустой образ действий рефлекса";
        }
      }
      catch (Exception ex)
      {
        return $"Ошибка загрузки действий рефлекса: {ex.Message}";
      }

      return "Нет данных о действиях рефлекса";
    }

    public string GetActionsForConditionReflex(string displayReflexID)
    {
      if (string.IsNullOrEmpty(displayReflexID) || !int.TryParse(displayReflexID, out int reflexId) || reflexId <= 0)
        return "Нет данных о действиях рефлекса";

      try
      {
        var conditionedReflex = _conditionedReflexesSystem.GetAllConditionedReflexes()
          .FirstOrDefault(r => r.Id == reflexId);

        if (conditionedReflex == null)
          return "Нет данных о действиях рефлекса";

        var conditionReflexesActions = GetActionsForGeneticReflexes(conditionedReflex.SourceGeneticReflexId);

        var tooltipParts = new List<string>();
        var allActions = _adaptiveActionsSystem.GetAllAdaptiveActions();
        var actionNames = conditionReflexesActions
            .Select(actionId => allActions.FirstOrDefault(a => a.Id == actionId)?.Name ?? $"Действие {actionId}")
            .Where(name => !string.IsNullOrEmpty(name));

        if (actionNames.Any())
          tooltipParts.Add($"Действия: {string.Join(", ", actionNames)}");

        return tooltipParts.Any() ? string.Join("\n", tooltipParts) : "Пустой образ действий рефлекса";
      }
      catch (Exception ex)
      {
        return $"Ошибка загрузки действий рефлекса: {ex.Message}";
      }
    }

    public List<int> GetActionsForGeneticReflexes(int reflexId)
    {
      try
      {
        var reflex = _geneticReflexesSystem.GetAllGeneticReflexesList()
            .FirstOrDefault(r => r.Id == reflexId);

        if (reflex == null)
          return new List<int>();

        return reflex.AdaptiveActions?.ToList() ?? new List<int>();
      }
      catch
      {
        return new List<int>();
      }
    }

    public sealed class AutomatizmActionsImageData
    {
      public List<int> ActIdList { get; set; }
      public List<int> PhraseIdList { get; set; }
      public int ToneId { get; set; }
      public int MoodId { get; set; }
      /// <summary>Свойство <see cref="Automatizm.Usefulness"/> из справочника автоматизмов; null если автоматизм не найден.</summary>
      public int? Usefulness { get; set; }
    }

    public AutomatizmActionsImageData TryGetAutomatizmActionsImageData(string displayAutomatizmID)
    {
      if (string.IsNullOrEmpty(displayAutomatizmID) || !int.TryParse(displayAutomatizmID, out int atmzId) || atmzId <= 0)
        return null;

      try
      {
        var atmz = _automatizmSystem.GetAutomatizmById(atmzId);
        if (atmz != null)
        {
          int atmzImg = atmz.ActionsImageID;
          var actionsImage = _actionsImagesSystem.GetActionsImage(atmzImg);

          if (actionsImage != null)
          {
            return new AutomatizmActionsImageData
            {
              ActIdList = actionsImage.ActIdList?.ToList() ?? new List<int>(),
              PhraseIdList = actionsImage.PhraseIdList?.ToList() ?? new List<int>(),
              ToneId = actionsImage.ToneId,
              MoodId = actionsImage.MoodId,
              Usefulness = atmz.Usefulness
            };
          }

          return new AutomatizmActionsImageData
          {
            ActIdList = new List<int>(),
            PhraseIdList = new List<int>(),
            ToneId = 0,
            MoodId = 0,
            Usefulness = atmz.Usefulness
          };
        }
      }
      catch (Exception ex)
      {
        Logger.Error(ex.Message);
        return new AutomatizmActionsImageData
        {
          ActIdList = new List<int>(),
          PhraseIdList = new List<int>(),
          ToneId = 0,
          MoodId = 0
        };
      }

      return null;
    }

    public string GetAutomatizmTooltip(string displayAutomatizmID)
    {
      var img = TryGetAutomatizmActionsImageData(displayAutomatizmID);
      if (img == null)
        return "Нет данных о действиях автоматизма";
      return FormatAutomatizmActionsImageTooltip(img);
    }

    private string FormatAutomatizmActionsImageTooltip(AutomatizmActionsImageData actionsImage)
    {
      var sb = new StringBuilder();

      if (actionsImage.ActIdList != null && actionsImage.ActIdList.Any())
      {
        var allActions = _adaptiveActionsSystem.GetAllAdaptiveActions();
        var names = actionsImage.ActIdList
            .Where(id => allActions.Any(a => a.Id == id))
            .Select(id => allActions.First(a => a.Id == id).Name)
            .ToList();

        sb.AppendLine($"Действия ({actionsImage.ActIdList.Count}): {string.Join(", ", names)}");
      }
      else
        sb.AppendLine("Действия: нет");

      if (actionsImage.PhraseIdList != null && actionsImage.PhraseIdList.Any())
      {
        if (_verbalSensor != null)
        {
          var phraseTexts = new List<string>();

          foreach (var phraseId in actionsImage.PhraseIdList)
          {
            string phraseText = _verbalSensor.GetPhraseFromPhraseId(phraseId);

            if (!string.IsNullOrEmpty(phraseText))
              phraseTexts.Add($"\"{phraseText}\" (ID: {phraseId})");
            else
              phraseTexts.Add($"ID: {phraseId} (фраза не найдена)");
          }

          if (phraseTexts.Any())
            sb.AppendLine($"Фразы ({actionsImage.PhraseIdList.Count}): {string.Join(", ", phraseTexts)}");
        }
        else
          sb.AppendLine($"Фразы: {string.Join(", ", actionsImage.PhraseIdList)}");
      }
      else
        sb.AppendLine("Фразы: нет");

      string toneText = ActionsImagesSystem.GetToneText(actionsImage.ToneId);
      sb.AppendLine(string.IsNullOrEmpty(toneText) ? "Тон: —" : $"Тон: {toneText}");

      string moodText = ActionsImagesSystem.GetMoodText(actionsImage.MoodId);
      sb.AppendLine(string.IsNullOrEmpty(moodText) ? "Настроение: —" : $"Настроение: {moodText}");

      if (actionsImage.Usefulness.HasValue)
        sb.AppendLine($"Полезность: {actionsImage.Usefulness.Value}");

      return sb.ToString().TrimEnd();
    }

    public string GetOrientationReflexTooltip(string displayOrientationReflexType)
    {
      string or1 = "Нет автоматизма, нужно быстро создать его по гомеостатическим целям";
      string or2 = "Автоматизм есть, надо его проверить в текущих условиях";

      if (string.IsNullOrEmpty(displayOrientationReflexType))
        return "Нет ориентировочного рефлекса";

      var orValue = displayOrientationReflexType.Trim();

      if (orValue == "ОР1")
        return or1;

      if (orValue == "ОР2")
        return or2;

      if (int.TryParse(orValue, out int orType))
      {
        return orType == 1
            ? or1
            : orType == 2
                ? or2
                : $"Ориентировочный рефлекс типа {orType}";
      }

      return $"Ориентировочный рефлекс: {orValue}";
    }

    public string GetThinkingLevelTooltip(string displayThinkingLevel, bool? thinkingLevelSuccess)
    {
      if (string.IsNullOrEmpty(displayThinkingLevel) || displayThinkingLevel == "-")
        return "Уровень мышления не активирован";

      var value = displayThinkingLevel.Trim();
      bool isUm1 = value == "1" || value == "УМ1";
      bool isUm2 = value == "2" || value == "УМ2";
      string levelDesc = isUm1
          ? "Уровень мышления 1: решение за счёт штатного автоматизма узла дерева (без правил эпизодической памяти)"
          : isUm2
              ? "Уровень мышления 2: поиск или создание автоматизма по правилам эпизодической памяти"
              : $"Уровень мышления: {value}";
      string resultLine = thinkingLevelSuccess.HasValue
          ? (thinkingLevelSuccess.Value ? "Результат: Успех" : "Результат: Неудача")
          : "";
      return string.IsNullOrEmpty(resultLine) ? levelDesc : levelDesc + "\n" + resultLine;
    }

    /// <summary>Как в ResearchLogger.BuildThinkingThemeTooltip для UI.</summary>
    public string GetThinkingThemeTypeTooltip(string themeCell)
    {
      if (string.IsNullOrWhiteSpace(themeCell) || themeCell.Trim() == "-")
        return null;
      if (!int.TryParse(themeCell.Trim(), out int themeTypeId) || themeTypeId <= 0)
        return null;
      if (!ThemeImageSystem.IsInitialized)
        return null;
      var name = ThemeImageSystem.Instance.GetThemeTypeDescription(themeTypeId) ?? "";
      int w = ThemeImageSystem.Instance.GetDefaultWeightForThemeType(themeTypeId);
      return string.IsNullOrEmpty(name) ? $"({w})" : $"{name} ({w})";
    }

    public string GetOrUmTooltip(string displayOrUm, bool? thinkingLevelSuccessForUm)
    {
      if (string.IsNullOrWhiteSpace(displayOrUm))
        return "ОР/УМ";

      var s = displayOrUm.Trim();
      if (s == "1" || s == "2" || s == "УМ1" || s == "УМ2")
        return GetThinkingLevelTooltip(s, thinkingLevelSuccessForUm);

      if (s == "-" || string.IsNullOrEmpty(s))
        return "Нет активации ОР или уровня мышления";

      return GetOrientationReflexTooltip(s);
    }

    public static string GetStateCodeTooltip(string rawStateCell)
    {
      if (string.IsNullOrWhiteSpace(rawStateCell))
        return null;
      var a = rawStateCell.Trim();
      if (a == "-" || a.Length == 0)
        return null;
      switch (a)
      {
        case "-1":
          return "ПЛОХО";
        case "0":
          return "НОРМА";
        case "1":
          return "ХОРОШО";
        default:
          return "Состояние: " + a;
      }
    }
  }
}
