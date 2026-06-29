using AIStudio.Common;
using AIStudio.Windows;
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
    public event PropertyChangedEventHandler PropertyChanged;
    private readonly DispatcherTimer _refreshTimer;
    private bool _disposed = false;
    private readonly AgentLogCellTooltipProvider _tooltipProvider;
    private readonly GomeostasSystem _gomeostas;
    private readonly ResearchLogger _researchLogger;
    private string _currentAgentName;
    private int _currentAgentStage;
    private readonly ObservableCollection<LogEntry> _mergedDisplayEntries = new ObservableCollection<LogEntry>();
    private HashSet<string> _selectedSessionKeys = new HashSet<string>(StringComparer.Ordinal)
    {
      LogFileSessionInfo.CurrentSessionKey
    };
    public ReadOnlyObservableCollection<LogEntry> LogEntries => MemoryLogManager.Instance.LogEntries;
    public string SessionsButtonLabel => LogSessionsUiHelper.BuildButtonLabel(_selectedSessionKeys);
    public bool IsLiveOnlyView => LogSessionsUiHelper.UsesOnlyCurrentSession(_selectedSessionKeys);
    private bool _suppressFileSessionLoad;
    public object DisplayedAgentLogEntries =>
        IsLiveOnlyView
            ? (object)MemoryLogManager.Instance.AgentDisplayLogEntries
            : _mergedDisplayEntries;
    public ReadOnlyObservableCollection<MemoryLogManager.ChainLogEntry> ChainLogEntries =>
        MemoryLogManager.Instance.ChainLogEntries;
    public ICommand ClearLogsCommand { get; }
    public ICommand OpenSessionsPickerCommand { get; }
    public string CurrentAgentTitle =>
        SymbiontPageTitleFormatter.Format("СИСТЕМНЫЕ ЛОГИ", _currentAgentName, _currentAgentStage);
    public LiveLogsViewModel(
        GomeostasSystem gomeostas,
        ResearchLogger researchLogger,
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
      _researchLogger = researchLogger ?? throw new ArgumentNullException(nameof(researchLogger));
      _tooltipProvider = new AgentLogCellTooltipProvider(
          _gomeostas,
          perceptionImagesSystem ?? throw new ArgumentNullException(nameof(perceptionImagesSystem)),
          influenceActionSystem ?? throw new ArgumentNullException(nameof(influenceActionSystem)),
          verbalSensor ?? throw new ArgumentNullException(nameof(verbalSensor)),
          adaptiveActionsSystem ?? throw new ArgumentNullException(nameof(adaptiveActionsSystem)),
          geneticReflexesSystem ?? throw new ArgumentNullException(nameof(geneticReflexesSystem)),
          conditionedReflexesSystem ?? throw new ArgumentNullException(nameof(conditionedReflexesSystem)),
          automatizmSystem ?? throw new ArgumentNullException(nameof(automatizmSystem)),
          actionsImagesSystem ?? throw new ArgumentNullException(nameof(actionsImagesSystem)));
      ClearLogsCommand = new RelayCommand(_ => ClearLogs());
      OpenSessionsPickerCommand = new RelayCommand(_ => OpenSessionsPicker());
      RefreshAgentTitleContext();
      RebuildDisplayedEntries();
      _refreshTimer = new DispatcherTimer
      {
        Interval = TimeSpan.FromMilliseconds(100)
      };
      _refreshTimer.Tick += (s, e) => RefreshDisplay();
      _refreshTimer.Start();
    }

    private void OpenSessionsPicker()
    {
      if (!LogSessionPickerGate.EnsurePulsationStopped(Application.Current?.MainWindow))
        return;
      var dlg = new LogSessionPickerWindow(
          "Сессии системных логов",
          "ВЫБОР СЕССИЙ ЛОГОВ",
          LogSessionPickerKind.Agent,
          _researchLogger,
          _selectedSessionKeys)
      {
        Owner = Application.Current?.MainWindow
      };
      if (dlg.ShowDialog() != true)
        return;
      _selectedSessionKeys = dlg.ViewModel.GetSelectedKeys();
      if (_selectedSessionKeys.Count == 0)
        _selectedSessionKeys.Add(LogFileSessionInfo.CurrentSessionKey);
      _suppressFileSessionLoad = false;
      OnPropertyChanged(nameof(SessionsButtonLabel));
      OnPropertyChanged(nameof(IsLiveOnlyView));
      RebuildDisplayedEntries();
    }

    private void RebuildDisplayedEntries()
    {
      if (LogSessionsUiHelper.UsesOnlyCurrentSession(_selectedSessionKeys))
      {
        _mergedDisplayEntries.Clear();
        OnPropertyChanged(nameof(DisplayedAgentLogEntries));
        return;
      }
      var combined = new List<LogEntry>();
      if (_selectedSessionKeys.Contains(LogFileSessionInfo.CurrentSessionKey))
      {
        foreach (var e in MemoryLogManager.Instance.AgentDisplayLogEntries)
          combined.Add(e);
      }
      if (!_suppressFileSessionLoad)
      {
        foreach (var key in _selectedSessionKeys.Where(k => k != LogFileSessionInfo.CurrentSessionKey))
        {
          if (!int.TryParse(key, out int sessionIndex))
            continue;
          try
          {
            combined.AddRange(AgentLogFileSessions.LoadSessionDisplayEntries(sessionIndex));
          }
          catch
          {
          }
        }
      }
      _mergedDisplayEntries.Clear();
      foreach (var e in combined.OrderByDescending(x => x.Timestamp))
        _mergedDisplayEntries.Add(e);
      OnPropertyChanged(nameof(DisplayedAgentLogEntries));
    }

    private void RefreshAgentTitleContext()
    {
      SymbiontPageTitleFormatter.ReadAgentContext(_gomeostas, out _currentAgentName, out _currentAgentStage);
      OnPropertyChanged(nameof(CurrentAgentTitle));
    }

    private void RefreshDisplay()
    {
      if (_disposed) return;
      if (IsLiveOnlyView)
      {
        OnPropertyChanged(nameof(DisplayedAgentLogEntries));
        OnPropertyChanged(nameof(LogEntries));
        OnPropertyChanged(nameof(ChainLogEntries));
        OnPropertyChanged(nameof(SessionsButtonLabel));
      }
      else if (_selectedSessionKeys.Contains(LogFileSessionInfo.CurrentSessionKey))
      {
        RebuildDisplayedEntries();
        OnPropertyChanged(nameof(SessionsButtonLabel));
      }
    }

    private void ClearLogs()
    {
      if (_disposed) return;
      _suppressFileSessionLoad = _selectedSessionKeys.Any(k => k != LogFileSessionInfo.CurrentSessionKey);
      MemoryLogManager.Instance.Clear();
      _mergedDisplayEntries.Clear();
      OnPropertyChanged(nameof(LogEntries));
      OnPropertyChanged(nameof(DisplayedAgentLogEntries));
      OnPropertyChanged(nameof(ChainLogEntries));
      OnPropertyChanged(nameof(SessionsButtonLabel));
      if (!IsLiveOnlyView)
        RebuildDisplayedEntries();
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
      if (_disposed) return;
      _refreshTimer?.Stop();
      _disposed = true;
    }
    #region Конвертеры для ToolTip'ов
    public string GetReflexChainTooltip(string chainInfo) => _tooltipProvider.GetReflexChainTooltip(chainInfo);
    public string GetAutomatizmChainTooltip(string chainInfo) => _tooltipProvider.GetAutomatizmChainTooltip(chainInfo);
    public string GetStyleTooltip(string displayBaseStyleID) => _tooltipProvider.GetStyleTooltip(displayBaseStyleID);
    public string GetTriggerTooltip(string displayTriggerStimulusID) =>
        _tooltipProvider.GetTriggerTooltip(displayTriggerStimulusID);
    public string GetEnvironmentPressureTooltip(string cellRaw, string storedTooltip) =>
        _tooltipProvider.GetEnvironmentPressureTooltip(cellRaw, storedTooltip);
    public string GetActionsForGeneticReflex(string displayReflexID) =>
        _tooltipProvider.GetActionsForGeneticReflex(displayReflexID);
    public string GetActionsForConditionReflex(string displayReflexID) =>
        _tooltipProvider.GetActionsForConditionReflex(displayReflexID);
    public List<int> GetActionsForGeneticReflexes(int reflexId) => _tooltipProvider.GetActionsForGeneticReflexes(reflexId);
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

    public string GetOrientationReflexTooltip(string displayOrientationReflexType) =>
        _tooltipProvider.GetOrientationReflexTooltip(displayOrientationReflexType);
    public string GetThinkingLevelTooltip(string displayThinkingLevel, bool? thinkingLevelSuccess) =>
        _tooltipProvider.GetThinkingLevelTooltip(displayThinkingLevel, thinkingLevelSuccess);
    #endregion
  }
}
