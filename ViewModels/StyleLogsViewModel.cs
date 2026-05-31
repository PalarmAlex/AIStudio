using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using AIStudio.Common;
using AIStudio.Windows;
using ISIDA.Gomeostas;
using static AIStudio.Common.MemoryLogManager;

namespace AIStudio.ViewModels
{
  public class StyleLogsViewModel : INotifyPropertyChanged, IDisposable
  {
    public event PropertyChangedEventHandler PropertyChanged;

    private readonly DispatcherTimer _refreshTimer;
    private bool _disposed = false;
    private readonly GomeostasSystem _gomeostas;
    private string _currentAgentName;
    private int _currentAgentStage;
    private StyleLogGroup _selectedRow;
    private HashSet<string> _selectedSessionKeys = new HashSet<string>(StringComparer.Ordinal)
    {
      LogFileSessionInfo.CurrentSessionKey
    };

    public ObservableCollection<StyleLogGroup> StyleLogGroups { get; } = new ObservableCollection<StyleLogGroup>();
    public ICommand ClearLogsCommand { get; }
    public ICommand OpenSessionsPickerCommand { get; }

    public string SessionsButtonLabel => LogSessionsUiHelper.BuildButtonLabel(_selectedSessionKeys);
    public bool IsLiveOnlyView => LogSessionsUiHelper.UsesOnlyCurrentSession(_selectedSessionKeys);

    public string CurrentAgentTitle =>
        SymbiontPageTitleFormatter.Format("ЛОГИ СТИЛЕЙ ПОВЕДЕНИЯ", _currentAgentName, _currentAgentStage);

    private bool _suppressFileSessionLoad;

    public StyleLogGroup SelectedRow
    {
      get => _selectedRow;
      set
      {
        _selectedRow = value;
        OnPropertyChanged(nameof(SelectedRow));
      }
    }

    public StyleLogsViewModel(GomeostasSystem gomeostas = null)
    {
      _gomeostas = gomeostas;
      RefreshAgentTitleContext();
      ClearLogsCommand = new RelayCommand(_ => ClearLogs());
      OpenSessionsPickerCommand = new RelayCommand(_ => OpenSessionsPicker());

      _refreshTimer = new DispatcherTimer
      {
        Interval = TimeSpan.FromMilliseconds(500)
      };
      _refreshTimer.Tick += (s, e) => RefreshDisplay();
      _refreshTimer.Start();
    }

    private void OpenSessionsPicker()
    {
      var dlg = new LogSessionPickerWindow(
          "Сессии логов стилей",
          "ВЫБОР СЕССИЙ — СТИЛИ",
          LogSessionPickerKind.Style,
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
      RefreshDisplay();
    }

    private void RefreshAgentTitleContext()
    {
      SymbiontPageTitleFormatter.ReadAgentContext(_gomeostas, out _currentAgentName, out _currentAgentStage);
      OnPropertyChanged(nameof(CurrentAgentTitle));
    }

    private void RefreshDisplay()
    {
      if (_disposed) return;

      var previouslySelectedPulse = SelectedRow?.Pulse;
      var groupedData = BuildGroups(CollectEntriesForDisplay());

      Application.Current.Dispatcher.Invoke(() =>
      {
        StyleLogGroup rowToSelect = null;
        StyleLogGroups.Clear();
        foreach (var group in groupedData)
        {
          group.RowHeight = CalculateRowHeight(group);
          StyleLogGroups.Add(group);
          if (previouslySelectedPulse.HasValue && group.Pulse == previouslySelectedPulse.Value)
            rowToSelect = group;
        }

        if (rowToSelect != null)
          SelectedRow = rowToSelect;

        if (IsLiveOnlyView)
          OnPropertyChanged(nameof(SessionsButtonLabel));
      });
    }

    private StyleLogFileSessions.StyleLogSessionData CollectEntriesForDisplay()
    {
      var data = new StyleLogFileSessions.StyleLogSessionData();

      if (_selectedSessionKeys.Contains(LogFileSessionInfo.CurrentSessionKey))
      {
        foreach (var e in MemoryLogManager.Instance.StyleLogEntries)
          data.StyleEntries.Add(e);
        foreach (var e in MemoryLogManager.Instance.StyleParameterActivationEntries)
          data.Activations.Add(e);
      }

      var fileIndices = _selectedSessionKeys
          .Where(k => k != LogFileSessionInfo.CurrentSessionKey)
          .Select(k => int.TryParse(k, out int ix) ? ix : -1)
          .Where(ix => ix >= 0)
          .ToList();

      if (fileIndices.Count > 0)
      {
        var fromFile = StyleLogFileSessions.LoadMergedSessions(fileIndices);
        data.StyleEntries.AddRange(fromFile.StyleEntries);
        data.Activations.AddRange(fromFile.Activations);
      }

      return data;
    }

    private static List<StyleLogGroup> BuildGroups(StyleLogFileSessions.StyleLogSessionData data)
    {
      if (data == null)
        return new List<StyleLogGroup>();

      return data.StyleEntries
          .GroupBy(entry => entry.Pulse)
          .OrderByDescending(g => g.Key)
          .Select(g => new StyleLogGroup
          {
            Pulse = g.Key,
            Timestamp = g.First().Timestamp,
            FinalStyles = g.Where(e => e.Stage == "Final")
                .Select(e => new StyleInfo { StyleId = e.StyleId, StyleName = e.StyleName })
                .ToList(),
            ParameterActivations = data.Activations
                .Where(e => e.Pulse == g.Key)
                .Select(e => new ParameterActivationInfo
                {
                  ParameterId = e.ParameterId,
                  ParameterName = e.ParameterName,
                  ZoneId = e.ZoneId,
                  ZoneDescription = e.ZoneDescription,
                  StyleId = e.StyleId,
                  StyleName = e.StyleName
                })
                .ToList()
          })
          .ToList();
    }

    private int CalculateRowHeight(StyleLogGroup group)
    {
      const int baseHeightPerLine = 20;
      var linesInParameters = string.IsNullOrEmpty(group.DisplayParameters) ? 1 : group.DisplayParameters.Split('\n').Length;
      var linesInZones = string.IsNullOrEmpty(group.DisplayZones) ? 1 : group.DisplayZones.Split('\n').Length;
      var linesInActiveStyles = string.IsNullOrEmpty(group.DisplayActiveStyles) ? 1 : group.DisplayActiveStyles.Split('\n').Length;
      var maxLines = Math.Max(linesInParameters, Math.Max(linesInZones, linesInActiveStyles));
      return Math.Max(baseHeightPerLine, maxLines * baseHeightPerLine);
    }

    private void ClearLogs()
    {
      if (_disposed) return;

      _suppressFileSessionLoad = _selectedSessionKeys.Any(k => k != LogFileSessionInfo.CurrentSessionKey);

      Application.Current.Dispatcher.Invoke(() =>
      {
        StyleLogGroups.Clear();
        SelectedRow = null;
      });

      MemoryLogManager.Instance.ClearStyleLogs();
      MemoryLogManager.Instance.ClearStyleParameterActivations();
      OnPropertyChanged(nameof(SessionsButtonLabel));
    }

    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public void Dispose()
    {
      if (_disposed) return;
      _refreshTimer?.Stop();
      _disposed = true;
    }

    public class StyleLogGroup
    {
      public DateTime Timestamp { get; set; }
      public int Pulse { get; set; }
      public int RowHeight { get; set; } = 30;
      public List<StyleInfo> FinalStyles { get; set; } = new List<StyleInfo>();
      public List<ParameterActivationInfo> ParameterActivations { get; set; } = new List<ParameterActivationInfo>();

      public string DisplayTime => Timestamp.ToString("HH:mm:ss");
      public string DisplayPulse => Pulse.ToString();
      public string DisplayActiveStyles => string.Join(" | ", FinalStyles.Select(s => s.ToString()));
      public string DisplayParameters => GetParametersDisplay();
      public string DisplayZones => GetZonesDisplay();

      private string GetParametersDisplay()
      {
        var uniqueParameters = ParameterActivations
            .GroupBy(pa => new { pa.ParameterId, pa.ParameterName })
            .OrderBy(g => g.Key.ParameterName)
            .Select(g => $"{g.Key.ParameterName} (ID:{g.Key.ParameterId})")
            .ToList();
        return string.Join("\n", uniqueParameters);
      }

      private string GetZonesDisplay()
      {
        var uniqueZones = ParameterActivations
            .GroupBy(pa => new { pa.ZoneId, pa.ZoneDescription })
            .OrderBy(g => g.Key.ZoneId)
            .Select(g => $"{g.Key.ZoneDescription} (ID:{g.Key.ZoneId})")
            .ToList();
        return string.Join("\n", uniqueZones);
      }
    }

    public class StyleInfo
    {
      public int StyleId { get; set; }
      public string StyleName { get; set; } = string.Empty;
      public override string ToString() => $"{StyleName} (ID:{StyleId})";
    }

    public class ParameterActivationInfo
    {
      public int ParameterId { get; set; }
      public string ParameterName { get; set; } = string.Empty;
      public int ZoneId { get; set; }
      public string ZoneDescription { get; set; } = string.Empty;
      public int StyleId { get; set; }
      public string StyleName { get; set; } = string.Empty;
    }
  }
}
