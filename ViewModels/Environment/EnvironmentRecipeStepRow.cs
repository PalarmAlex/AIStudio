using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>
  /// Строка шага рецепта в редакторе.
  /// </summary>
  public sealed class EnvironmentRecipeStepRow : INotifyPropertyChanged
  {
    private string _stepType = string.Empty;
    private string _parametersText = string.Empty;
    private string _summary = string.Empty;

    public EnvironmentRecipeStepRow()
    {
      ParameterFields = new ObservableCollection<EnvironmentRecipeStepParameterField>();
      ParameterFields.CollectionChanged += (_, __) => OnPropertyChanged(nameof(ParameterFields));
    }

    /// <summary>Тип шага.</summary>
    public string StepType
    {
      get => _stepType;
      set
      {
        string normalized = value ?? string.Empty;
        if (_stepType == normalized)
          return;
        _stepType = normalized;
        OnPropertyChanged();
      }
    }

    /// <summary>Устаревший текстовый формат; используется при загрузке до инициализации полей.</summary>
    public string ParametersText
    {
      get => _parametersText;
      set
      {
        string normalized = value ?? string.Empty;
        if (_parametersText == normalized)
          return;
        _parametersText = normalized;
        OnPropertyChanged();
      }
    }

    /// <summary>Краткое описание параметров для списка шагов.</summary>
    public string Summary
    {
      get => _summary;
      set
      {
        string normalized = value ?? string.Empty;
        if (_summary == normalized)
          return;
        _summary = normalized;
        OnPropertyChanged();
      }
    }

    /// <summary>Структурированные поля параметров по schema.</summary>
    public ObservableCollection<EnvironmentRecipeStepParameterField> ParameterFields { get; }

    public event PropertyChangedEventHandler PropertyChanged;

    internal void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
