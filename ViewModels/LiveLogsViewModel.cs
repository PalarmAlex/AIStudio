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
using System.Linq;
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
    private readonly ObservableCollection<LogEntry> _archivedDisplayEntries = new ObservableCollection<LogEntry>();
    private AgentLogSessionListItem _selectedSession;
    private string _loadedArchiveSessionId;

    /// <summary>
    /// Полный симбионтный лог (для отчётов сценариев и отладки).
    /// </summary>
    public ReadOnlyObservableCollection<LogEntry> LogEntries => MemoryLogManager.Instance.LogEntries;

    /// <summary>
    /// Список сессий: текущая и архивы прошлых запусков.
    /// </summary>
    public ObservableCollection<AgentLogSessionListItem> LogSessions { get; } = new ObservableCollection<AgentLogSessionListItem>();

    /// <summary>
    /// Выбранная сессия в комбобоксе.
    /// </summary>
    public AgentLogSessionListItem SelectedSession
    {
      get => _selectedSession;
      set
      {
        if (_selectedSession == value)
          return;
        _selectedSession = value;
        OnPropertyChanged(nameof(SelectedSession));
        OnPropertyChanged(nameof(IsViewingCurrentSession));
        OnPropertyChanged(nameof(CanClearLogs));
        (ClearLogsCommand as RelayCommand)?.RaiseCanExecuteChanged();
        ApplySelectedSessionView();
      }
    }

    /// <summary>Просматривается ли сейчас живая сессия (а не архив).</summary>
    public bool IsViewingCurrentSession => SelectedSession == null || SelectedSession.IsCurrent;

    /// <summary>Можно ли очистить лог (только для текущей сессии).</summary>
    public bool CanClearLogs => IsViewingCurrentSession;

    /// <summary>
    /// Симбионтный лог для таблицы: текущая сессия или загруженный архив.
    /// </summary>
    public object DisplayedAgentLogEntries =>
        IsViewingCurrentSession
            ? (object)MemoryLogManager.Instance.AgentDisplayLogEntries
            : _archivedDisplayEntries;

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

      ClearLogsCommand = new RelayCommand(_ => ClearLogs(), _ => CanClearLogs);

      ReloadSessionList(selectCurrent: true);

      _refreshTimer = new DispatcherTimer
      {
        Interval = TimeSpan.FromMilliseconds(100)
      };
      _refreshTimer.Tick += (s, e) => RefreshDisplay();
      _refreshTimer.Start();
    }

    private void ReloadSessionList(bool selectCurrent)
    {
      var keepId = selectCurrent ? null : SelectedSession?.SessionId;

      LogSessions.Clear();
      int liveCount = MemoryLogManager.Instance.AgentDisplayLogEntries.Count;
      LogSessions.Add(AgentLogSessionListItem.CreateCurrent(liveCount));

      foreach (var archived in AgentLogSessionStorage.ListArchivedSessions())
        LogSessions.Add(AgentLogSessionListItem.FromArchived(archived));

      AgentLogSessionListItem pick = LogSessions.FirstOrDefault(s =>
          (keepId == null && s.IsCurrent) ||
          (!s.IsCurrent && s.SessionId == keepId));

      if (pick == null)
        pick = LogSessions.FirstOrDefault();

      SelectedSession = pick;
    }

    private void UpdateCurrentSessionListItemLabel()
    {
      var current = LogSessions.FirstOrDefault(s => s.IsCurrent);
      if (current == null)
        return;

      int count = MemoryLogManager.Instance.AgentDisplayLogEntries.Count;
      current.UpdateCurrentEntryCount(count);
    }

    private void ApplySelectedSessionView()
    {
      if (IsViewingCurrentSession)
      {
        _loadedArchiveSessionId = null;
        _archivedDisplayEntries.Clear();
      }
      else
      {
        LoadArchivedSession(SelectedSession.SessionId);
      }

      OnPropertyChanged(nameof(DisplayedAgentLogEntries));
    }

    private void LoadArchivedSession(string sessionId)
    {
      if (_loadedArchiveSessionId == sessionId && _archivedDisplayEntries.Count > 0)
        return;

      _archivedDisplayEntries.Clear();
      _loadedArchiveSessionId = sessionId;

      foreach (var entry in AgentLogSessionStorage.LoadSessionEntries(sessionId))
        _archivedDisplayEntries.Add(entry);
    }

    /// <summary>
    /// Обновляет отображение логов
    /// </summary>
    private void RefreshDisplay()
    {
      if (_disposed) return;

      UpdateCurrentSessionListItemLabel();

      if (IsViewingCurrentSession)
      {
        OnPropertyChanged(nameof(DisplayedAgentLogEntries));
        OnPropertyChanged(nameof(LogEntries));
        OnPropertyChanged(nameof(ChainLogEntries));
      }
    }

    /// <summary>
    /// Очищает все логи текущей сессии
    /// </summary>
    private void ClearLogs()
    {
      if (_disposed || !CanClearLogs) return;

      MemoryLogManager.Instance.Clear();
      ReloadSessionList(selectCurrent: true);
      OnPropertyChanged(nameof(LogEntries));
      OnPropertyChanged(nameof(DisplayedAgentLogEntries));
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
    /// <param name="usefulnessAtSnapshot">Из строки лога; если null — берётся текущая из справочника (как в редакторе).</param>
    public AutomatizmsViewModel.ActionsImageDisplay GetActionsForAutomatizm(string displayAutomatizmID, int? usefulnessAtSnapshot = null)
    {
      var d = _tooltipProvider.TryGetAutomatizmActionsImageData(displayAutomatizmID);
      if (d == null && !usefulnessAtSnapshot.HasValue)
        return null;
      if (d == null)
      {
        return new AutomatizmsViewModel.ActionsImageDisplay
        {
          ActIdList = new List<int>(),
          PhraseIdList = new List<int>(),
          ToneId = 0,
          MoodId = 0,
          Usefulness = usefulnessAtSnapshot
        };
      }

      return new AutomatizmsViewModel.ActionsImageDisplay
      {
        ActIdList = d.ActIdList ?? new List<int>(),
        PhraseIdList = d.PhraseIdList ?? new List<int>(),
        ToneId = d.ToneId,
        MoodId = d.MoodId,
        Usefulness = usefulnessAtSnapshot ?? d.Usefulness
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
