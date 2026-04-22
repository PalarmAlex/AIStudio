using AIStudio.Common;
using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Gomeostas;
using ISIDA.Psychic;
using ISIDA.Psychic.Automatism;
using ISIDA.Reflexes;
using ISIDA.Sensors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using static AIStudio.Common.MemoryLogManager;

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
    private bool _disposed = false;

    private readonly AgentLogCellTooltipProvider _tooltipProvider;

    /// <summary>
    /// Полный агентный лог (для отчётов сценариев и отладки).
    /// </summary>
    public ReadOnlyObservableCollection<LogEntry> LogEntries => MemoryLogManager.Instance.LogEntries;

    /// <summary>
    /// Агентный лог для таблицы: одна строка на глобальный пульс после слияния снимков в движке.
    /// </summary>
    public ReadOnlyObservableCollection<LogEntry> AgentDisplayLogEntries => MemoryLogManager.Instance.AgentDisplayLogEntries;

    /// <summary>
    /// Коллекция записей логов цепочек рефлексов и автоматизмов только для чтения
    /// </summary>
    public ReadOnlyObservableCollection<MemoryLogManager.ChainLogEntry> ChainLogEntries => MemoryLogManager.Instance.ChainLogEntries;

    /// <summary>
    /// Команда очистки логов
    /// </summary>
    public ICommand ClearLogsCommand { get; }

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
      _tooltipProvider = new AgentLogCellTooltipProvider(
          gomeostas ?? throw new ArgumentNullException(nameof(gomeostas)),
          perceptionImagesSystem ?? throw new ArgumentNullException(nameof(perceptionImagesSystem)),
          influenceActionSystem ?? throw new ArgumentNullException(nameof(influenceActionSystem)),
          verbalSensor ?? throw new ArgumentNullException(nameof(verbalSensor)),
          adaptiveActionsSystem ?? throw new ArgumentNullException(nameof(adaptiveActionsSystem)),
          geneticReflexesSystem ?? throw new ArgumentNullException(nameof(geneticReflexesSystem)),
          conditionedReflexesSystem ?? throw new ArgumentNullException(nameof(conditionedReflexesSystem)),
          automatizmSystem ?? throw new ArgumentNullException(nameof(automatizmSystem)),
          actionsImagesSystem ?? throw new ArgumentNullException(nameof(actionsImagesSystem)));

      ClearLogsCommand = new RelayCommand(_ => ClearLogs());

      // Таймер для обновления интерфейса (автообновление всегда включено)
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

      // Принудительно обновляем привязку (автообновление всегда включено)
      OnPropertyChanged(nameof(LogEntries));
      OnPropertyChanged(nameof(AgentDisplayLogEntries));
      OnPropertyChanged(nameof(ChainLogEntries));
    }

    /// <summary>
    /// Очищает все логи
    /// </summary>
    private void ClearLogs()
    {
      if (_disposed) return;

      MemoryLogManager.Instance.Clear();
      OnPropertyChanged(nameof(LogEntries));
      OnPropertyChanged(nameof(AgentDisplayLogEntries));
      OnPropertyChanged(nameof(ChainLogEntries));
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
    public string GetReflexChainTooltip(string chainInfo) => _tooltipProvider.GetReflexChainTooltip(chainInfo);

    /// <summary>
    /// Получает текст подсказки для цепочки автоматизмов (формат "ChainId:ActionId")
    /// </summary>
    public string GetAutomatizmChainTooltip(string chainInfo) => _tooltipProvider.GetAutomatizmChainTooltip(chainInfo);

    /// <summary>
    /// Получает текст подсказки для стиля поведения
    /// </summary>
    public string GetStyleTooltip(string displayBaseStyleID) => _tooltipProvider.GetStyleTooltip(displayBaseStyleID);

    /// <summary>
    /// Получает текст подсказки для триггера
    /// </summary>
    public string GetTriggerTooltip(string displayTriggerStimulusID) => _tooltipProvider.GetTriggerTooltip(displayTriggerStimulusID);

    /// <summary>
    /// Получает текст подсказки для безусловного рефлекса
    /// </summary>
    public string GetActionsForGeneticReflex(string displayReflexID) => _tooltipProvider.GetActionsForGeneticReflex(displayReflexID);

    /// <summary>
    /// Получает текст подсказки для условного рефлекса
    /// </summary>
    public string GetActionsForConditionReflex(string displayReflexID) => _tooltipProvider.GetActionsForConditionReflex(displayReflexID);

    /// <summary>
    /// Получает список действий для безусловного рефлекса
    /// </summary>
    public List<int> GetActionsForGeneticReflexes(int reflexId) => _tooltipProvider.GetActionsForGeneticReflexes(reflexId);

    /// <summary>
    /// Получает образ действия автоматизма
    /// </summary>
    public AutomatizmsViewModel.ActionsImageDisplay GetActionsForAutomatizm(string displayAutomatizmID)
    {
      var d = _tooltipProvider.TryGetAutomatizmActionsImageData(displayAutomatizmID);
      if (d == null)
        return null;
      return new AutomatizmsViewModel.ActionsImageDisplay
      {
        ActIdList = d.ActIdList ?? new List<int>(),
        PhraseIdList = d.PhraseIdList ?? new List<int>(),
        ToneId = d.ToneId,
        MoodId = d.MoodId,
        Usefulness = d.Usefulness
      };
    }

    /// <summary>
    /// Получает текст подсказки для ориентировочного рефлекса
    /// </summary>
    public string GetOrientationReflexTooltip(string displayOrientationReflexType) =>
        _tooltipProvider.GetOrientationReflexTooltip(displayOrientationReflexType);

    /// <summary>
    /// Получает текст подсказки для уровня мышления (уровни 1 и 2) с результатом (успех/неудача)
    /// </summary>
    public string GetThinkingLevelTooltip(string displayThinkingLevel, bool? thinkingLevelSuccess) =>
        _tooltipProvider.GetThinkingLevelTooltip(displayThinkingLevel, thinkingLevelSuccess);

    #endregion
  }
}