using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>
  /// Поле schema среды (preconditions рецепта, фильтр триггера).
  /// </summary>
  public sealed class EnvironmentRecipePreconditionField : INotifyPropertyChanged
  {
    private bool _isChecked;

    public EnvironmentRecipePreconditionField()
    {
      ListItems = new ObservableCollection<EnvironmentRecipePreconditionListItem>();
    }

    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string FieldType { get; set; } = "bool";

    public bool IsBoolType =>
        string.Equals(FieldType, "bool", System.StringComparison.OrdinalIgnoreCase);

    public bool IsStringListType =>
        string.Equals(FieldType, "stringList", System.StringComparison.OrdinalIgnoreCase);

    public bool IsChecked
    {
      get => _isChecked;
      set
      {
        if (_isChecked == value)
          return;
        _isChecked = value;
        OnPropertyChanged();
      }
    }

    public ObservableCollection<EnvironmentRecipePreconditionListItem> ListItems { get; }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
