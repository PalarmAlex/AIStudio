using System.ComponentModel;

namespace AIStudio.ViewModels
{
  /// <summary>Пункт чек-списка сессий логов.</summary>
  public sealed class LiveLogSessionPickerItem : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    public string SessionKey { get; }
    public bool IsCurrent { get; }
    public string DisplayLabel { get; }

    private bool _isChecked;
    private bool _isVisible = true;

    public bool IsChecked
    {
      get => _isChecked;
      set
      {
        if (_isChecked == value)
          return;
        _isChecked = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
      }
    }

    public bool IsVisible
    {
      get => _isVisible;
      set
      {
        if (_isVisible == value)
          return;
        _isVisible = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
      }
    }

    public LiveLogSessionPickerItem(string sessionKey, bool isCurrent, string displayLabel, bool isChecked)
    {
      SessionKey = sessionKey;
      IsCurrent = isCurrent;
      DisplayLabel = displayLabel;
      _isChecked = isChecked;
    }

    public bool MatchesFilter(string filter)
    {
      if (string.IsNullOrWhiteSpace(filter))
        return true;
      return DisplayLabel != null
             && DisplayLabel.IndexOf(filter.Trim(), System.StringComparison.CurrentCultureIgnoreCase) >= 0;
    }
  }
}
