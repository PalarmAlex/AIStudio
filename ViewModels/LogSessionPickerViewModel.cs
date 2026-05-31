using AIStudio.Common;
using ISIDA.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace AIStudio.ViewModels
{
  public enum LogSessionPickerKind
  {
    Agent,
    Style,
    Parameter
  }

  public sealed class LogSessionPickerViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    public ObservableCollection<LiveLogSessionPickerItem> Items { get; } = new ObservableCollection<LiveLogSessionPickerItem>();

    private string _filterText = string.Empty;

    public string FilterText
    {
      get => _filterText;
      set
      {
        if (_filterText == value)
          return;
        _filterText = value ?? string.Empty;
        OnPropertyChanged(nameof(FilterText));
        ApplyFilter();
      }
    }

    public ICommand SelectAllCommand { get; }
    public ICommand ClearAllCommand { get; }

    private readonly LogSessionPickerKind _kind;
    private readonly ResearchLogger _researchLogger;

    public LogSessionPickerViewModel(
        LogSessionPickerKind kind,
        ResearchLogger researchLogger,
        IEnumerable<string> initiallySelectedKeys)
    {
      _kind = kind;
      _researchLogger = researchLogger;
      var selected = new HashSet<string>(initiallySelectedKeys ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
      LoadItems(selected);

      SelectAllCommand = new RelayCommand(_ =>
      {
        foreach (var item in Items.Where(i => i.IsVisible))
          item.IsChecked = true;
      });

      ClearAllCommand = new RelayCommand(_ =>
      {
        foreach (var item in Items)
          item.IsChecked = false;
      });
    }

    public bool TryDeleteSelectedSessions(Window owner)
    {
      return LogSessionPickerDeleteOperations.TryDeleteSelected(
          Items,
          DeleteSessionsByIndex,
          () => LoadItems(new HashSet<string>(new[] { LogFileSessionInfo.CurrentSessionKey }, StringComparer.Ordinal)),
          owner);
    }

    private (bool ok, string error) DeleteSessionsByIndex(IEnumerable<int> indices)
    {
      bool ok = LogFileSessionDeletion.TryDeleteSessions(_researchLogger, _kind, indices, out string error);
      return (ok, error);
    }

    private void LoadItems(HashSet<string> selected)
    {
      Items.Clear();

      int liveCount = GetLiveEntryCount(_kind);
      string currentSuffix = liveCount > 0 ? " (" + liveCount + ")" : " (пусто)";
      Items.Add(new LiveLogSessionPickerItem(
          LogFileSessionInfo.CurrentSessionKey,
          true,
          "Текущая сессия" + currentSuffix,
          selected.Contains(LogFileSessionInfo.CurrentSessionKey)));

      foreach (var fileSession in ListFileSessions(_kind))
      {
        Items.Add(new LiveLogSessionPickerItem(
            fileSession.SessionKey,
            false,
            fileSession.BuildDisplayLabel(),
            selected.Contains(fileSession.SessionKey)));
      }

      ApplyFilter();
    }

    public HashSet<string> GetSelectedKeys() =>
        new HashSet<string>(Items.Where(i => i.IsChecked).Select(i => i.SessionKey), StringComparer.Ordinal);

    private static int GetLiveEntryCount(LogSessionPickerKind kind)
    {
      switch (kind)
      {
        case LogSessionPickerKind.Agent:
          return MemoryLogManager.Instance.AgentDisplayLogEntries.Count;
        case LogSessionPickerKind.Style:
          return MemoryLogManager.Instance.StyleLogEntries.Count
                 + MemoryLogManager.Instance.StyleParameterActivationEntries.Count;
        case LogSessionPickerKind.Parameter:
          return MemoryLogManager.Instance.ParameterLogEntries.Count;
        default:
          return 0;
      }
    }

    private static IReadOnlyList<LogFileSessionInfo> ListFileSessions(LogSessionPickerKind kind)
    {
      switch (kind)
      {
        case LogSessionPickerKind.Agent:
          return AgentLogFileSessions.ListFileSessions();
        case LogSessionPickerKind.Style:
          return StyleLogFileSessions.ListFileSessions();
        case LogSessionPickerKind.Parameter:
          return ParameterLogFileSessions.ListFileSessions();
        default:
          return Array.Empty<LogFileSessionInfo>();
      }
    }

    private void ApplyFilter()
    {
      string f = _filterText;
      foreach (var item in Items)
        item.IsVisible = item.MatchesFilter(f);
    }

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
  }
}
