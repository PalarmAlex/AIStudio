using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>Строка параметра schema (handler args или event parameters).</summary>
  public sealed class SchemaParamRow : INotifyPropertyChanged
  {
    private string _value = string.Empty;
    private string _validationError = string.Empty;

    public string Key { get; set; }
    public string Label { get; set; }
    public string Type { get; set; }
    public bool Required { get; set; }
    public IList<string> AllowedValues { get; set; }

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
        OnPropertyChanged(nameof(BoolValue));
      }
    }

    public bool BoolValue
    {
      get
      {
        if (bool.TryParse(_value, out bool parsed))
          return parsed;
        return _value == "1";
      }
      set
      {
        string normalized = value ? "true" : "false";
        if (_value == normalized)
          return;
        _value = normalized;
        OnPropertyChanged();
        OnPropertyChanged(nameof(Value));
      }
    }

    public string ValidationError
    {
      get => _validationError;
      set
      {
        string normalized = value ?? string.Empty;
        if (_validationError == normalized)
          return;
        _validationError = normalized;
        OnPropertyChanged();
      }
    }

    public bool IsBool =>
        string.Equals(Type, "bool", System.StringComparison.OrdinalIgnoreCase);

    public bool IsEnum => AllowedValues != null && AllowedValues.Count > 0;

    public bool IsStringEditor => !IsBool && !IsEnum;

    public event PropertyChangedEventHandler PropertyChanged;

    internal void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
