using ISIDA.Psychic;
using ISIDA.Psychic.Thinking;
using System;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Threading;

namespace AIStudio.ViewModels
{
  /// <summary>
  /// Read-only модель представления для визуализации циклов осмысления (3-й уровень).
  /// </summary>
  public sealed class ThinkingCyclesViewModel : INotifyPropertyChanged, IDisposable
  {
    public event PropertyChangedEventHandler PropertyChanged;

    private readonly PsychicSystem _psychicSystem;
    private readonly DispatcherTimer _refreshTimer;
    private int _tickCounter;
    private bool _disposed;

    private readonly int _mainLogLinesToShow;
    private readonly int _debugMaxLogLinesPerCycle;

    private readonly Brush _statusNeutralBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120));
    private readonly Brush _statusWaitingBrush = new SolidColorBrush(Color.FromRgb(217, 133, 0)); // оранжевый
    private readonly Brush _statusDreamingBrush = new SolidColorBrush(Color.FromRgb(124, 77, 255)); // фиолетовый
    private readonly Brush _statusActiveBrush = new SolidColorBrush(Color.FromRgb(0, 128, 0)); // зелёный
    private readonly Brush _statusIdleBrush = new SolidColorBrush(Color.FromRgb(150, 150, 150)); // серый

    private bool _isMainCycleAvailable;
    public bool IsMainCycleAvailable
    {
      get => _isMainCycleAvailable;
      private set
      {
        if (_isMainCycleAvailable == value) return;
        _isMainCycleAvailable = value;
        OnPropertyChanged(nameof(IsMainCycleAvailable));
      }
    }

    private Brush _mainCycleStatusBrush;
    public Brush MainCycleStatusBrush
    {
      get => _mainCycleStatusBrush;
      private set
      {
        if (_mainCycleStatusBrush == value) return;
        _mainCycleStatusBrush = value;
        OnPropertyChanged(nameof(MainCycleStatusBrush));
      }
    }

    private string _mainCycleStatusText;
    public string MainCycleStatusText
    {
      get => _mainCycleStatusText;
      private set
      {
        if (_mainCycleStatusText == value) return;
        _mainCycleStatusText = value;
        OnPropertyChanged(nameof(MainCycleStatusText));
      }
    }

    private string _mainCycleIdText;
    public string MainCycleIdText
    {
      get => _mainCycleIdText;
      private set
      {
        if (_mainCycleIdText == value) return;
        _mainCycleIdText = value;
        OnPropertyChanged(nameof(MainCycleIdText));
      }
    }

    private string _mainCycleOrderText;
    public string MainCycleOrderText
    {
      get => _mainCycleOrderText;
      private set
      {
        if (_mainCycleOrderText == value) return;
        _mainCycleOrderText = value;
        OnPropertyChanged(nameof(MainCycleOrderText));
      }
    }

    private string _mainCycleCreatedPulseText;
    public string MainCycleCreatedPulseText
    {
      get => _mainCycleCreatedPulseText;
      private set
      {
        if (_mainCycleCreatedPulseText == value) return;
        _mainCycleCreatedPulseText = value;
        OnPropertyChanged(nameof(MainCycleCreatedPulseText));
      }
    }

    private string _mainCycleStepCountText;
    public string MainCycleStepCountText
    {
      get => _mainCycleStepCountText;
      private set
      {
        if (_mainCycleStepCountText == value) return;
        _mainCycleStepCountText = value;
        OnPropertyChanged(nameof(MainCycleStepCountText));
      }
    }

    private string _mainCycleWeightText;
    public string MainCycleWeightText
    {
      get => _mainCycleWeightText;
      private set
      {
        if (_mainCycleWeightText == value) return;
        _mainCycleWeightText = value;
        OnPropertyChanged(nameof(MainCycleWeightText));
      }
    }

    private string _mainCycleLastUpdatedText;
    public string MainCycleLastUpdatedText
    {
      get => _mainCycleLastUpdatedText;
      private set
      {
        if (_mainCycleLastUpdatedText == value) return;
        _mainCycleLastUpdatedText = value;
        OnPropertyChanged(nameof(MainCycleLastUpdatedText));
      }
    }

    private string _mainCycleUnresolvedNodeIdText;
    public string MainCycleUnresolvedNodeIdText
    {
      get => _mainCycleUnresolvedNodeIdText;
      private set
      {
        if (_mainCycleUnresolvedNodeIdText == value) return;
        _mainCycleUnresolvedNodeIdText = value;
        OnPropertyChanged(nameof(MainCycleUnresolvedNodeIdText));
      }
    }

    private string _mainCycleUnresolvedActionsImageIdText;
    public string MainCycleUnresolvedActionsImageIdText
    {
      get => _mainCycleUnresolvedActionsImageIdText;
      private set
      {
        if (_mainCycleUnresolvedActionsImageIdText == value) return;
        _mainCycleUnresolvedActionsImageIdText = value;
        OnPropertyChanged(nameof(MainCycleUnresolvedActionsImageIdText));
      }
    }

    private string _mainCycleProblemNodeIdText;
    public string MainCycleProblemNodeIdText
    {
      get => _mainCycleProblemNodeIdText;
      private set
      {
        if (_mainCycleProblemNodeIdText == value) return;
        _mainCycleProblemNodeIdText = value;
        OnPropertyChanged(nameof(MainCycleProblemNodeIdText));
      }
    }

    private string _mainCycleThemeIdText;
    public string MainCycleThemeIdText
    {
      get => _mainCycleThemeIdText;
      private set
      {
        if (_mainCycleThemeIdText == value) return;
        _mainCycleThemeIdText = value;
        OnPropertyChanged(nameof(MainCycleThemeIdText));
      }
    }

    private string _mainCyclePurposeIdText;
    public string MainCyclePurposeIdText
    {
      get => _mainCyclePurposeIdText;
      private set
      {
        if (_mainCyclePurposeIdText == value) return;
        _mainCyclePurposeIdText = value;
        OnPropertyChanged(nameof(MainCyclePurposeIdText));
      }
    }

    private string _mainCycleLastStrategyIdText;
    public string MainCycleLastStrategyIdText
    {
      get => _mainCycleLastStrategyIdText;
      private set
      {
        if (_mainCycleLastStrategyIdText == value) return;
        _mainCycleLastStrategyIdText = value;
        OnPropertyChanged(nameof(MainCycleLastStrategyIdText));
      }
    }

    private string _mainCycleLogText;
    public string MainCycleLogText
    {
      get => _mainCycleLogText;
      private set
      {
        if (_mainCycleLogText == value) return;
        _mainCycleLogText = value;
        OnPropertyChanged(nameof(MainCycleLogText));
      }
    }

    private string _debugSnapshotText;
    public string DebugSnapshotText
    {
      get => _debugSnapshotText;
      private set
      {
        if (_debugSnapshotText == value) return;
        _debugSnapshotText = value;
        OnPropertyChanged(nameof(DebugSnapshotText));
      }
    }

    public ThinkingCyclesViewModel(
      PsychicSystem psychicSystem,
      int mainLogLinesToShow = 40,
      int debugMaxLogLinesPerCycle = 2)
    {
      _psychicSystem = psychicSystem ?? throw new ArgumentNullException(nameof(psychicSystem));
      _mainLogLinesToShow = Math.Max(0, mainLogLinesToShow);
      _debugMaxLogLinesPerCycle = Math.Max(0, debugMaxLogLinesPerCycle);

      // По UX: обновляем состояние относительно часто, но отладочный дамп реже.
      _refreshTimer = new DispatcherTimer
      {
        Interval = TimeSpan.FromMilliseconds(200)
      };
      _refreshTimer.Tick += (_, __) =>
      {
        if (_disposed) return;

        _tickCounter++;
        RefreshMainCycle();

        if (_tickCounter % 5 == 0) // примерно раз в 1 сек
          RefreshDebugSnapshot();
      };

      // инициализация
      MainCycleStatusBrush = _statusNeutralBrush;
      MainCycleStatusText = "Инициализация...";
      MainCycleLogText = "-";
      DebugSnapshotText = "Подготовка...";

      _refreshTimer.Start();
    }

    private void RefreshMainCycle()
    {
      try
      {
        ThinkingCycleInfo snap = _psychicSystem.GetThinkingCyclesMainSnapshot(_mainLogLinesToShow);
        if (snap == null)
        {
          IsMainCycleAvailable = false;
          MainCycleStatusBrush = _statusNeutralBrush;
          MainCycleStatusText = "Нет активного главного цикла";
          MainCycleIdText = "-";
          MainCycleOrderText = "-";
          MainCycleCreatedPulseText = "-";
          MainCycleStepCountText = "-";
          MainCycleWeightText = "-";
          MainCycleLastUpdatedText = "-";
          MainCycleUnresolvedNodeIdText = "-";
          MainCycleUnresolvedActionsImageIdText = "-";
          MainCycleProblemNodeIdText = "-";
          MainCycleThemeIdText = "-";
          MainCyclePurposeIdText = "-";
          MainCycleLastStrategyIdText = "-";
          MainCycleLogText = "ThinkingCycles: none";
          return;
        }

        IsMainCycleAvailable = true;
        MainCycleIdText = snap.Id.ToString();
        MainCycleOrderText = snap.Order.ToString();
        MainCycleCreatedPulseText = snap.CreatedPulse.ToString();
        MainCycleStepCountText = snap.StepCount.ToString();
        MainCycleWeightText = snap.Weight.ToString();
        MainCycleLastUpdatedText = snap.LastUpdatedUtc.ToString("dd.MM.yyyy HH:mm:ss 'UTC'");
        MainCycleUnresolvedNodeIdText = snap.UnresolvedNodeId.ToString();
        MainCycleUnresolvedActionsImageIdText = snap.UnresolvedActionsImageId.ToString();
        MainCycleProblemNodeIdText = snap.ProblemNodeId.ToString();
        MainCycleThemeIdText = snap.ThemeId.ToString();
        MainCyclePurposeIdText = snap.PurposeId.ToString();
        MainCycleLastStrategyIdText = string.IsNullOrWhiteSpace(snap.LastStrategyId) ? "-" : snap.LastStrategyId;

        MainCycleStatusText = GetStatusText(snap);
        MainCycleStatusBrush = GetStatusBrush(snap);
        MainCycleLogText = (snap.Log != null && snap.Log.Count > 0) ? string.Join(Environment.NewLine, snap.Log) : "-";
      }
      catch (Exception ex)
      {
        // Не валимся из UI при редких проблемах синхронизации/данных.
        IsMainCycleAvailable = false;
        MainCycleStatusBrush = _statusNeutralBrush;
        MainCycleStatusText = $"Ошибка: {ex.Message}";
        MainCycleLogText = "-";
      }
    }

    private void RefreshDebugSnapshot()
    {
      try
      {
        DebugSnapshotText = _psychicSystem.GetThinkingCyclesDebugSnapshot(_debugMaxLogLinesPerCycle);
      }
      catch (Exception ex)
      {
        DebugSnapshotText = $"Ошибка получения debug-снимка: {ex.Message}";
      }
    }

    private static string GetStatusText(ThinkingCycleInfo snap)
    {
      if (snap.IsWaitingPeriod)
        return "Ожидание оценки оператора";
      if (snap.Dreaming)
        return "Мечтание (dreaming)";
      if (snap.IsIdle)
        return "Idle (ожидает/не выполняет шаги)";
      return "Активен (выполняет шаги)";
    }

    private Brush GetStatusBrush(ThinkingCycleInfo snap)
    {
      if (snap.IsWaitingPeriod) return _statusWaitingBrush;
      if (snap.Dreaming) return _statusDreamingBrush;
      if (snap.IsIdle) return _statusIdleBrush;
      return _statusActiveBrush;
    }

    private void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
      if (_disposed) return;
      _disposed = true;
      _refreshTimer?.Stop();
    }
  }
}

