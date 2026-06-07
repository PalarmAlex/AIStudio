using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>
  /// Одно поле параметра шага рецепта (из schema/recipe-steps.json).
  /// </summary>
  public sealed class EnvironmentRecipeStepParameterField : INotifyPropertyChanged
  {
    private string _value = string.Empty;

    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string FieldType { get; set; } = "string";
    public bool Required { get; set; }
    public IReadOnlyList<string> EnumValues { get; set; }

    public string Value
    {
      get => _value;
      set
      {
        string normalized = value ?? string.Empty;
        if (_value == normalized)
          return;
        _value = normalized;
        OnPropertyChanged();
      }
    }

    public bool HasEnumValues => EnumValues != null && EnumValues.Count > 0;

    public bool IsTemplateField =>
        string.Equals(Key, "template", System.StringComparison.OrdinalIgnoreCase);

    public bool IsPropertyNameField =>
        string.Equals(Key, "name", System.StringComparison.OrdinalIgnoreCase);

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
