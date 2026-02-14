using AIStudio.Common;
using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Gomeostas;
using ISIDA.Psychic.Automatism;
using ISIDA.Reflexes;
using ISIDA.Sensors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static AIStudio.Common.MemoryLogManager;
using static ISIDA.Reflexes.ConditionedReflexesSystem;
using static ISIDA.Reflexes.PerceptionImagesSystem;

namespace AIStudio.ViewModels
{
  /// <summary>
  /// Модель представления для страницы живых логов
  /// </summary>
  public class LiveLogsViewModel : INotifyPropertyChanged, IDisposable
  {
    /// <summary>
    /// Событие изменения свойства
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    private readonly DispatcherTimer _refreshTimer;
    private bool _isAutoRefreshEnabled = true;
    private bool _disposed = false;

    // Зависимости
    private readonly GomeostasSystem _gomeostas;
    private readonly PerceptionImagesSystem _perceptionImagesSystem;
    private readonly InfluenceActionSystem _influenceActionSystem;
    private readonly VerbalSensorChannel _verbalSensor;
    private readonly AdaptiveActionsSystem _adaptiveActionsSystem;
    private readonly GeneticReflexesSystem _geneticReflexesSystem;
    private readonly ConditionedReflexesSystem _conditionedReflexesSystem;
    private readonly AutomatizmSystem _automatizmSystem;
    private readonly ActionsImagesSystem _actionsImagesSystem;

    /// <summary>
    /// Коллекция записей логов только для чтения
    /// </summary>
    public ReadOnlyObservableCollection<LogEntry> LogEntries => MemoryLogManager.Instance.LogEntries;

    /// <summary>
    /// Коллекция записей логов цепочек рефлексов и автоматизмов только для чтения
    /// </summary>
    public ReadOnlyObservableCollection<MemoryLogManager.ChainLogEntry> ChainLogEntries => MemoryLogManager.Instance.ChainLogEntries;

    /// <summary>
    /// Команда очистки логов
    /// </summary>
    public ICommand ClearLogsCommand { get; }

    /// <summary>
    /// Команда переключения автообновления
    /// </summary>
    public ICommand ToggleAutoRefreshCommand { get; }

    /// <summary>
    /// Статус автообновления
    /// </summary>
    public string AutoRefreshStatus => _isAutoRefreshEnabled ? "Автообновление: ВКЛ" : "Автообновление: ВЫКЛ";

    /// <summary>
    /// Цвет индикатора автообновления
    /// </summary>
    public Brush AutoRefreshColor => _isAutoRefreshEnabled ? Brushes.Green : Brushes.Red;

    /// <summary>
    /// Конструктор модели представления живых логов
    /// </summary>
    public LiveLogsViewModel(
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
      _verbalSensor = verbalSensor ?? throw new ArgumentNullException(nameof(verbalSensor));
      _adaptiveActionsSystem = adaptiveActionsSystem ?? throw new ArgumentNullException(nameof(adaptiveActionsSystem));
      _geneticReflexesSystem = geneticReflexesSystem ?? throw new ArgumentNullException(nameof(geneticReflexesSystem));
      _conditionedReflexesSystem = conditionedReflexesSystem ?? throw new ArgumentNullException(nameof(conditionedReflexesSystem));
      _automatizmSystem = automatizmSystem ?? throw new ArgumentNullException(nameof(automatizmSystem));
      _actionsImagesSystem = actionsImagesSystem ?? throw new ArgumentNullException(nameof(actionsImagesSystem));

      ClearLogsCommand = new RelayCommand(_ => ClearLogs());
      ToggleAutoRefreshCommand = new RelayCommand(_ => ToggleAutoRefresh());

      // Таймер для обновления интерфейса
      _refreshTimer = new DispatcherTimer
      {
        Interval = TimeSpan.FromMilliseconds(100) // 10 FPS для плавной анимации
      };
      _refreshTimer.Tick += (s, e) => RefreshDisplay();
      _refreshTimer.Start();
    }

    /// <summary>
    /// Обновляет отображение логов
    /// </summary>
    private void RefreshDisplay()
    {
      if (_disposed) return;

      if (_isAutoRefreshEnabled)
      {
        // Принудительно обновляем привязку
        OnPropertyChanged(nameof(LogEntries));
        OnPropertyChanged(nameof(ChainLogEntries));
      }
    }

    /// <summary>
    /// Очищает все логи
    /// </summary>
    private void ClearLogs()
    {
      if (_disposed) return;

      MemoryLogManager.Instance.Clear();
      OnPropertyChanged(nameof(LogEntries));
      OnPropertyChanged(nameof(ChainLogEntries));
    }

    /// <summary>
    /// Переключает режим автообновления
    /// </summary>
    private void ToggleAutoRefresh()
    {
      if (_disposed) return;

      _isAutoRefreshEnabled = !_isAutoRefreshEnabled;
      OnPropertyChanged(nameof(AutoRefreshStatus));
      OnPropertyChanged(nameof(AutoRefreshColor));
    }

    /// <summary>
    /// Вызывает событие изменения свойства
    /// </summary>
    /// <param name="propertyName">Имя измененного свойства</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Освобождает ресурсы модели представления
    /// </summary>
    public void Dispose()
    {
      if (_disposed) return;

      _refreshTimer?.Stop();
      _disposed = true;
    }

    #region Конвертеры для ToolTip'ов

    /// <summary>
    /// Получает текст подсказки для цепочки рефлексов (формат "ChainId:ActionId")
    /// </summary>
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

    /// <summary>
    /// Получает текст подсказки для цепочки автоматизмов (формат "ChainId:ActionId")
    /// </summary>
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

    /// <summary>
    /// Получает текст подсказки для стиля поведения
    /// </summary>
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

    /// <summary>
    /// Получает текст подсказки для триггера
    /// </summary>
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
          var tooltipParts = new List<string>();

          // Получаем названия внешних воздействий
          if (perceptionImage.InfluenceActionsList.Any())
          {
            var allInfluences = _influenceActionSystem.GetAllInfluenceActions();

            var influenceNames = perceptionImage.InfluenceActionsList
                .Select(actionId => allInfluences.FirstOrDefault(a => a.Id == actionId)?.Name ?? $"Воздействие {actionId}")
                .Where(name => !string.IsNullOrEmpty(name));

            if (influenceNames.Any())
              tooltipParts.Add($"Воздействия: {string.Join(", ", influenceNames)}");
          }

          // Получаем фразы
          if (perceptionImage.PhraseIdList.Any())
          {
            var phraseNames = perceptionImage.PhraseIdList
                .Select(phraseId => _verbalSensor?.GetPhraseFromPhraseId(phraseId) ?? $"Фраза {phraseId}")
                .Where(phrase => !string.IsNullOrEmpty(phrase));

            if (phraseNames.Any())
              tooltipParts.Add($"Фразы: {string.Join(", ", phraseNames)}");
          }

          return tooltipParts.Any() ? string.Join("\n", tooltipParts) : "Пустой образ восприятия";
        }
      }
      catch (Exception ex)
      {
        return $"Ошибка загрузки триггера: {ex.Message}";
      }

      return "Нет данных о триггере";
    }

    /// <summary>
    /// Получает текст подсказки для безусловного рефлекса
    /// </summary>
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

    /// <summary>
    /// Получает текст подсказки для условного рефлекса
    /// </summary>
    public string GetActionsForConditionReflex(string displayReflexID)
    {
      if (string.IsNullOrEmpty(displayReflexID) || !int.TryParse(displayReflexID, out int reflexId) || reflexId <= 0)
        return "Нет данных о действиях рефлекса";

      try
      {
        var conditionedReflex = _conditionedReflexesSystem.GetAllConditionedReflexes()
          .FirstOrDefault(r => r.Id == reflexId);

        var conditionReflexesActions = GetActionsForGeneticReflexes(conditionedReflex.SourceGeneticReflexId);

        if (conditionedReflex != null)
        {
          var tooltipParts = new List<string>();
          var allActions = _adaptiveActionsSystem.GetAllAdaptiveActions();
          var actionNames = conditionReflexesActions
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

    /// <summary>
    /// Получает список действий для безусловного рефлекса
    /// </summary>
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

    /// <summary>
    /// Получает образ действия автоматизма
    /// </summary>
    public AutomatizmsViewModel.ActionsImageDisplay GetActionsForAutomatizm(string displayAutomatizmID)
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
            return new AutomatizmsViewModel.ActionsImageDisplay
            {
              ActIdList = actionsImage.ActIdList?.ToList() ?? new List<int>(),
              PhraseIdList = actionsImage.PhraseIdList?.ToList() ?? new List<int>(),
              ToneId = actionsImage.ToneId,
              MoodId = actionsImage.MoodId
            };
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Error(ex.Message);

        return new AutomatizmsViewModel.ActionsImageDisplay
        {
          ActIdList = new List<int>(),
          PhraseIdList = new List<int>(),
          ToneId = 0,
          MoodId = 0
        };
      }

      return null;
    }

    /// <summary>
    /// Получает текст подсказки для ориентировочного рефлекса
    /// </summary>
    public string GetOrientationReflexTooltip(string displayOrientationReflexType)
    {
      string or1 = "Нет автоматизма, нужно быстро создать его по гомеостатическим целям";
      string or2 = "Автоматизм есть, надо его проверить в текущих условиях";

      if (string.IsNullOrEmpty(displayOrientationReflexType))
        return "Нет ориентировочного рефлекса";

      // Убираем возможные пробелы и преобразуем в строку
      var orValue = displayOrientationReflexType.Trim();

      if (orValue == "ОР1")
        return or1;

      if (orValue == "ОР2")
        return or2;

      // Если передано число вместо строки
      if (int.TryParse(orValue, out int orType))
      {
        return orType == 1 ?
            or1 :
            orType == 2 ?
            or2 :
            $"Ориентировочный рефлекс типа {orType}";
      }

      return $"Ориентировочный рефлекс: {orValue}";
    }

    #endregion
  }
}