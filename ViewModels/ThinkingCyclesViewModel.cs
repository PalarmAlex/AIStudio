using ISIDA.Psychic;
using ISIDA.Psychic.Thinking;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace AIStudio.ViewModels
{
  /// <summary>
  /// Модель представления страницы циклов осмысления: матрица и детали по выбору.
  /// </summary>
  public sealed class ThinkingCyclesViewModel : INotifyPropertyChanged, IDisposable
  {
    public event PropertyChangedEventHandler PropertyChanged;

    private readonly PsychicSystem _psychicSystem;
    private readonly DispatcherTimer _refreshTimer;
    private int _tickCounter;
    private bool _disposed;

    private readonly int _detailLogLines;

    private static readonly Color BgLow = Color.FromRgb(0xE8, 0xF5, 0xE9);
    private static readonly Color BgHigh = Color.FromRgb(0x43, 0xA0, 0x47);
    private static readonly Color MainTileBg = Color.FromRgb(0x2E, 0x7D, 0x32);

    private int _selectedCycleId;

    public ThinkingCyclesViewModel(
      PsychicSystem psychicSystem,
      int detailLogLines = 80)
    {
      _psychicSystem = psychicSystem ?? throw new ArgumentNullException(nameof(psychicSystem));
      _detailLogLines = Math.Max(0, detailLogLines);

      BackgroundLimitOptions = new List<KeyValuePair<int, string>>
      {
        new KeyValuePair<int, string>(50, "50"),
        new KeyValuePair<int, string>(100, "100"),
        new KeyValuePair<int, string>(200, "200"),
        new KeyValuePair<int, string>(500, "500"),
      };
      _selectedBackgroundLimit = 100;

      MainTile = null;
      BackgroundRows = new ObservableCollection<ThinkingCycleWeightRowViewModel>();
      SelectCycleCommand = new RelayCommand(OnSelectCycle);

      BackgroundCountText = string.Empty;
      DetailHeaderText = "Выберите цикл на матрице";

      _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
      _refreshTimer.Tick += (_, __) =>
      {
        if (_disposed) return;
        _tickCounter++;
        RefreshFromEngine();
      };
      _refreshTimer.Start();
      RefreshFromEngine();
    }

    public List<KeyValuePair<int, string>> BackgroundLimitOptions { get; }

    private int _selectedBackgroundLimit = 100;
    public int SelectedBackgroundLimit
    {
      get => _selectedBackgroundLimit;
      set
      {
        if (_selectedBackgroundLimit == value) return;
        _selectedBackgroundLimit = value;
        OnPropertyChanged(nameof(SelectedBackgroundLimit));
        RebuildMatrixFromLastSnapshot();
      }
    }

    public ICommand SelectCycleCommand { get; }

    public ThinkingCycleTileViewModel MainTile { get; private set; }

    public ObservableCollection<ThinkingCycleWeightRowViewModel> BackgroundRows { get; }

    private string _backgroundCountText;
    public string BackgroundCountText
    {
      get => _backgroundCountText;
      private set
      {
        if (_backgroundCountText == value) return;
        _backgroundCountText = value;
        OnPropertyChanged(nameof(BackgroundCountText));
      }
    }

    private bool _hasBackgroundRows;
    public bool HasBackgroundRows
    {
      get => _hasBackgroundRows;
      private set
      {
        if (_hasBackgroundRows == value) return;
        _hasBackgroundRows = value;
        OnPropertyChanged(nameof(HasBackgroundRows));
      }
    }

    private bool _hasMainCycle;
    public bool HasMainCycle
    {
      get => _hasMainCycle;
      private set
      {
        if (_hasMainCycle == value) return;
        _hasMainCycle = value;
        OnPropertyChanged(nameof(HasMainCycle));
      }
    }

    private string _detailHeaderText;
    public string DetailHeaderText
    {
      get => _detailHeaderText;
      private set
      {
        if (_detailHeaderText == value) return;
        _detailHeaderText = value;
        OnPropertyChanged(nameof(DetailHeaderText));
      }
    }

    private string _detailIdText;
    public string DetailIdText
    {
      get => _detailIdText;
      private set
      {
        if (_detailIdText == value) return;
        _detailIdText = value;
        OnPropertyChanged(nameof(DetailIdText));
      }
    }

    private string _detailOrderText;
    public string DetailOrderText
    {
      get => _detailOrderText;
      private set
      {
        if (_detailOrderText == value) return;
        _detailOrderText = value;
        OnPropertyChanged(nameof(DetailOrderText));
      }
    }

    private string _detailMainText;
    public string DetailMainText
    {
      get => _detailMainText;
      private set
      {
        if (_detailMainText == value) return;
        _detailMainText = value;
        OnPropertyChanged(nameof(DetailMainText));
      }
    }

    private string _detailWeightText;
    public string DetailWeightText
    {
      get => _detailWeightText;
      private set
      {
        if (_detailWeightText == value) return;
        _detailWeightText = value;
        OnPropertyChanged(nameof(DetailWeightText));
      }
    }

    private string _detailStepsText;
    public string DetailStepsText
    {
      get => _detailStepsText;
      private set
      {
        if (_detailStepsText == value) return;
        _detailStepsText = value;
        OnPropertyChanged(nameof(DetailStepsText));
      }
    }

    private string _detailPulseText;
    public string DetailPulseText
    {
      get => _detailPulseText;
      private set
      {
        if (_detailPulseText == value) return;
        _detailPulseText = value;
        OnPropertyChanged(nameof(DetailPulseText));
      }
    }

    private string _detailUpdatedText;
    public string DetailUpdatedText
    {
      get => _detailUpdatedText;
      private set
      {
        if (_detailUpdatedText == value) return;
        _detailUpdatedText = value;
        OnPropertyChanged(nameof(DetailUpdatedText));
      }
    }

    private string _detailPendingAtmzText;
    public string DetailPendingAtmzText
    {
      get => _detailPendingAtmzText;
      private set
      {
        if (_detailPendingAtmzText == value) return;
        _detailPendingAtmzText = value;
        OnPropertyChanged(nameof(DetailPendingAtmzText));
      }
    }

    private string _detailAwaitingText;
    public string DetailAwaitingText
    {
      get => _detailAwaitingText;
      private set
      {
        if (_detailAwaitingText == value) return;
        _detailAwaitingText = value;
        OnPropertyChanged(nameof(DetailAwaitingText));
      }
    }

    private string _detailNodeText;
    public string DetailNodeText
    {
      get => _detailNodeText;
      private set
      {
        if (_detailNodeText == value) return;
        _detailNodeText = value;
        OnPropertyChanged(nameof(DetailNodeText));
      }
    }

    private string _detailProblemText;
    public string DetailProblemText
    {
      get => _detailProblemText;
      private set
      {
        if (_detailProblemText == value) return;
        _detailProblemText = value;
        OnPropertyChanged(nameof(DetailProblemText));
      }
    }

    private string _detailThemeText;
    public string DetailThemeText
    {
      get => _detailThemeText;
      private set
      {
        if (_detailThemeText == value) return;
        _detailThemeText = value;
        OnPropertyChanged(nameof(DetailThemeText));
      }
    }

    private string _detailPurposeText;
    public string DetailPurposeText
    {
      get => _detailPurposeText;
      private set
      {
        if (_detailPurposeText == value) return;
        _detailPurposeText = value;
        OnPropertyChanged(nameof(DetailPurposeText));
      }
    }

    private string _detailContextLine;
    public string DetailContextLine
    {
      get => _detailContextLine;
      private set
      {
        if (_detailContextLine == value) return;
        _detailContextLine = value;
        OnPropertyChanged(nameof(DetailContextLine));
      }
    }

    private string _detailStrategyText;
    public string DetailStrategyText
    {
      get => _detailStrategyText;
      private set
      {
        if (_detailStrategyText == value) return;
        _detailStrategyText = value;
        OnPropertyChanged(nameof(DetailStrategyText));
      }
    }

    private string _detailLogText;
    public string DetailLogText
    {
      get => _detailLogText;
      private set
      {
        if (_detailLogText == value) return;
        _detailLogText = value;
        OnPropertyChanged(nameof(DetailLogText));
      }
    }

    private IReadOnlyList<ThinkingCycleListItem> _lastList = Array.Empty<ThinkingCycleListItem>();

    private void OnSelectCycle(object parameter)
    {
      if (parameter == null) return;
      if (!int.TryParse(parameter.ToString(), out var id) || id <= 0) return;
      _selectedCycleId = id;
      RefreshDetail();
      RebuildMatrixFromLastSnapshot();
    }

    private void RefreshFromEngine()
    {
      try
      {
        _lastList = _psychicSystem.GetThinkingCyclesListSnapshot();
        RebuildMatrixFromLastSnapshot();

        if (_tickCounter % 3 == 0 && _selectedCycleId > 0)
          RefreshDetail();
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine(ex);
      }
    }

    private void RebuildMatrixFromLastSnapshot()
    {
      var list = _lastList?.ToList() ?? new List<ThinkingCycleListItem>();
      var main = list.FirstOrDefault(x => x.IsMainCycle);
      HasMainCycle = main != null;

      if (main != null)
      {
        MainTile = BuildMainTile(main);
        OnPropertyChanged(nameof(MainTile));
      }
      else
      {
        MainTile = null;
        OnPropertyChanged(nameof(MainTile));
      }

      var bgAll = list.Where(x => !x.IsMainCycle)
        .OrderByDescending(x => x.Weight)
        .ThenBy(x => x.Order)
        .ToList();

      int totalBg = bgAll.Count;
      if (totalBg == 0)
      {
        BackgroundCountText = "Фоновых циклов: 0";
        BackgroundRows.Clear();
        HasBackgroundRows = false;
        OnPropertyChanged(nameof(BackgroundRows));
        return;
      }

      int take = Math.Min(SelectedBackgroundLimit, totalBg);
      var bg = bgAll.Take(take).ToList();
      BackgroundCountText = take >= totalBg
        ? $"Показано фоновых: {take}"
        : $"Показано фоновых: {take} из {totalBg}";

      int minW = bg.Min(x => x.Weight);
      int maxW = bg.Max(x => x.Weight);

      BackgroundRows.Clear();
      foreach (var g in bg
                 .GroupBy(x => GetWeightDecadeBucket(x.Weight))
                 .OrderByDescending(x => x.Key))
      {
        var label = FormatWeightRangeLabel(g.Key);
        var row = new ThinkingCycleWeightRowViewModel { Weight = g.Key, WeightLabel = label };
        foreach (var item in g.OrderByDescending(x => x.Weight).ThenBy(x => x.Order))
        {
          row.Tiles.Add(BuildBackgroundTile(item, minW, maxW));
        }
        BackgroundRows.Add(row);
      }
      HasBackgroundRows = BackgroundRows.Count > 0;
      OnPropertyChanged(nameof(BackgroundRows));
    }

    private ThinkingCycleTileViewModel BuildMainTile(ThinkingCycleListItem m)
    {
      var brush = new SolidColorBrush(MainTileBg);
      brush.Freeze();
      var (borderBrush, th) = GetTileBorderVisual(m);
      var sel = new SolidColorBrush(Color.FromRgb(0x15, 0x65, 0xC0));
      sel.Freeze();
      return new ThinkingCycleTileViewModel
      {
        SelectCycleCommand = SelectCycleCommand,
        CycleId = m.Id,
        OrderLabel = m.Order.ToString(),
        TooltipText = BuildTileTooltip(m),
        TileBackground = brush,
        BorderBrush = borderBrush,
        BorderThickness = th,
        SelectionBorderBrush = _selectedCycleId == m.Id ? sel : Brushes.Transparent,
        SelectionBorderThickness = _selectedCycleId == m.Id ? 2 : 0,
        MarkerText = BuildMarker(m),
        IsMainStyle = true,
        IsSelected = _selectedCycleId == m.Id
      };
    }

    private ThinkingCycleTileViewModel BuildBackgroundTile(ThinkingCycleListItem item, int minW, int maxW)
    {
      double t = maxW == minW ? 1.0 : (item.Weight - minW) / (double)(maxW - minW);
      var c = LerpRgb(BgLow, BgHigh, t);
      var brush = new SolidColorBrush(c);
      brush.Freeze();
      var (borderBrush, th) = GetTileBorderVisual(item);
      return new ThinkingCycleTileViewModel
      {
        SelectCycleCommand = SelectCycleCommand,
        CycleId = item.Id,
        OrderLabel = item.Order.ToString(),
        TooltipText = BuildTileTooltip(item),
        TileBackground = brush,
        BorderBrush = borderBrush,
        BorderThickness = th,
        SelectionBorderBrush = _selectedCycleId == item.Id ? new SolidColorBrush(Color.FromRgb(0x15, 0x65, 0xC0)) : Brushes.Transparent,
        SelectionBorderThickness = _selectedCycleId == item.Id ? 2 : 0,
        MarkerText = BuildMarker(item),
        IsMainStyle = false,
        IsSelected = _selectedCycleId == item.Id
      };
    }

    private static string BuildMarker(ThinkingCycleListItem m)
    {
      var parts = new List<string>();
      if (m.Dreaming) parts.Add("D");
      if (m.IsIdle) parts.Add("I");
      return parts.Count == 0 ? string.Empty : string.Join(",", parts);
    }

    private static string BuildTileTooltip(ThinkingCycleListItem m)
    {
      return
        $"Order={m.Order}, Id={m.Id}, Weight={m.Weight}" + Environment.NewLine +
        $"AwaitingEval={m.AwaitingEvaluation}, PendingAtmz={m.PendingSolutionAutomatizmId}" + Environment.NewLine +
        $"Idle={m.IsIdle}, Dreaming={m.Dreaming}, Steps={m.StepCount}";
    }

    private static (Brush BorderBrush, double Thickness) GetTileBorderVisual(ThinkingCycleListItem m)
    {
      if (m.ShowAwaitingEvaluationBorder)
      {
        var b = new SolidColorBrush(Color.FromRgb(0x1B, 0x5E, 0x20));
        b.Freeze();
        return (b, 3.0);
      }
      if (m.ShowNoSolutionBorder)
      {
        var b = new SolidColorBrush(Color.FromRgb(0xB7, 0x1C, 0x1C));
        b.Freeze();
        return (b, 3.0);
      }
      var n = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
      n.Freeze();
      return (n, 1.0);
    }

    private static Color LerpRgb(Color a, Color b, double t)
    {
      t = Math.Max(0, Math.Min(1, t));
      return Color.FromRgb(
        (byte)(a.R + (b.R - a.R) * t),
        (byte)(a.G + (b.G - a.G) * t),
        (byte)(a.B + (b.B - a.B) * t));
    }

    /// <summary>Десяток веса для группировки: 0 = 0–9, …, 9 = 90–99, 10 = 100.</summary>
    private static int GetWeightDecadeBucket(int weight)
    {
      if (weight >= 100) return 10;
      if (weight < 0) return 0;
      return weight / 10;
    }

    private static string FormatWeightRangeLabel(int bucket)
    {
      if (bucket == 10)
        return "100";
      int lo = bucket * 10;
      int hi = lo + 9;
      return $"{hi}-{lo}";
    }

    private void RefreshDetail()
    {
      if (_selectedCycleId <= 0)
      {
        DetailHeaderText = "Выберите цикл на матрице";
        ClearDetailFields();
        return;
      }

      try
      {
        var snap = _psychicSystem.GetThinkingCycleSnapshotById(_selectedCycleId, _detailLogLines);
        if (snap == null)
        {
          DetailHeaderText = "Цикл не найден (возможно, уже удалён)";
          ClearDetailFields();
          return;
        }

        DetailHeaderText = snap.IsMainCycle ? "Главный цикл (детали)" : "Фоновый цикл (детали)";
        DetailIdText = snap.Id.ToString();
        DetailOrderText = snap.Order.ToString();
        DetailMainText = snap.IsMainCycle ? "да" : "нет";
        DetailWeightText = snap.Weight.ToString();
        DetailStepsText = snap.StepCount.ToString();
        DetailPulseText = snap.CreatedPulse.ToString();
        DetailUpdatedText = snap.LastUpdatedUtc.ToString("dd.MM.yyyy HH:mm:ss 'UTC'");
        DetailPendingAtmzText = snap.PendingSolutionAutomatizmId.ToString();
        DetailAwaitingText = snap.AwaitingEvaluation ? "да" : "нет";
        DetailNodeText = snap.UnresolvedNodeId.ToString();
        DetailProblemText = snap.ProblemNodeId.ToString();
        DetailThemeText = snap.ThemeId.ToString();
        DetailPurposeText = snap.PurposeId.ToString();
        DetailContextLine =
          $"{snap.UnresolvedNodeId} / {snap.ProblemNodeId} / {snap.ThemeId} / {snap.PurposeId}";
        DetailStrategyText = string.IsNullOrWhiteSpace(snap.LastStrategyId) ? "—" : snap.LastStrategyId;
        DetailLogText = (snap.Log != null && snap.Log.Count > 0)
          ? string.Join(Environment.NewLine, snap.Log)
          : "—";
      }
      catch (Exception ex)
      {
        DetailHeaderText = "Ошибка загрузки деталей";
        DetailLogText = ex.Message;
      }
    }

    private void ClearDetailFields()
    {
      DetailIdText = "—";
      DetailOrderText = "—";
      DetailMainText = "—";
      DetailWeightText = "—";
      DetailStepsText = "—";
      DetailPulseText = "—";
      DetailUpdatedText = "—";
      DetailPendingAtmzText = "—";
      DetailAwaitingText = "—";
      DetailNodeText = "—";
      DetailProblemText = "—";
      DetailThemeText = "—";
      DetailPurposeText = "—";
      DetailContextLine = "—";
      DetailStrategyText = "—";
      DetailLogText = "—";
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

  /// <summary>Строка матрицы: один вес, несколько плашек.</summary>
  public sealed class ThinkingCycleWeightRowViewModel
  {
    public int Weight { get; set; }
    public string WeightLabel { get; set; }
    public ObservableCollection<ThinkingCycleTileViewModel> Tiles { get; } = new ObservableCollection<ThinkingCycleTileViewModel>();
  }

  /// <summary>Плашка цикла на матрице.</summary>
  public sealed class ThinkingCycleTileViewModel
  {
    /// <summary>Та же команда выбора, что у страницы (без RelativeSource через вложенные ItemsControl).</summary>
    public ICommand SelectCycleCommand { get; set; }

    public int CycleId { get; set; }
    public string OrderLabel { get; set; }
    public string TooltipText { get; set; }
    public Brush TileBackground { get; set; }
    public Brush BorderBrush { get; set; }
    public double BorderThickness { get; set; }
    public Brush SelectionBorderBrush { get; set; }
    public double SelectionBorderThickness { get; set; }
    public string MarkerText { get; set; }
    public bool IsMainStyle { get; set; }
    public bool IsSelected { get; set; }
  }
}
