using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>
  /// Один элемент списка stringList в предусловии (например, тип документа).
  /// </summary>
  public sealed class EnvironmentRecipePreconditionListItem : INotifyPropertyChanged
  {
    private bool _isSelected;

    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;

    public bool IsSelected
    {
      get => _isSelected;
      set
      {
        if (_isSelected == value)
          return;
        _isSelected = value;
        OnPropertyChanged();
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
