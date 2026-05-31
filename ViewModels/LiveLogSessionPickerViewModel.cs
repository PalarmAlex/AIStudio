using AIStudio.Common;
using System;
using static AIStudio.Common.MemoryLogManager;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace AIStudio.ViewModels
{
  public sealed class LiveLogSessionPickerViewModel : INotifyPropertyChanged
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

    public LiveLogSessionPickerViewModel(IEnumerable<string> initiallySelectedKeys)
    {
      var selected = new HashSet<string>(initiallySelectedKeys ?? Enumerable.Empty<string>(), StringComparer.Ordinal);

      int liveCount = MemoryLogManager.Instance.AgentDisplayLogEntries.Count;
      string currentSuffix = liveCount > 0 ? " (" + liveCount + ")" : " (пусто)";
      Items.Add(new LiveLogSessionPickerItem(
          AgentLogFileSessions.CurrentSessionKey,
          true,
          "Текущая сессия" + currentSuffix,
          selected.Contains(AgentLogFileSessions.CurrentSessionKey)));

      foreach (var fileSession in AgentLogFileSessions.ListFileSessions())
      {
        Items.Add(new LiveLogSessionPickerItem(
            fileSession.SessionKey,
            false,
            fileSession.BuildDisplayLabel(),
            selected.Contains(fileSession.SessionKey)));
      }

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

    public HashSet<string> GetSelectedKeys()
    {
      return new HashSet<string>(
          Items.Where(i => i.IsChecked).Select(i => i.SessionKey),
          StringComparer.Ordinal);
    }

    private void ApplyFilter()
    {
      string f = _filterText;
      foreach (var item in Items)
        item.IsVisible = item.MatchesFilter(f);
    }

    private void OnPropertyChanged(string name)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
  }
}
