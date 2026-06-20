using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>Строка таблицы триггеров среды (contract 3.1).</summary>
  public sealed class EnvironmentTriggerRow : INotifyPropertyChanged
  {
    private string _id = string.Empty;
    private string _displayName = string.Empty;
    private string _eventKind = string.Empty;
    private string _eventSummary = string.Empty;
    private int _reflexTriggerCommandPatternId;
    private string _reflexTriggerCommandPatternText = string.Empty;

    /// <summary>
    /// Создаёт пустую строку.
    /// </summary>
    public EnvironmentTriggerRow()
    {
      EventParameters = new Dictionary<string, string>();
      HomeostasisDeltas = new Dictionary<int, int>();
    }

    /// <summary>Уникальный ID триггера.</summary>
    public string Id
    {
      get => _id;
      set
      {
        string normalized = value ?? string.Empty;
        if (_id == normalized)
          return;
        _id = normalized;
        OnPropertyChanged();
      }
    }

    /// <summary>Отображаемое имя.</summary>
    public string DisplayName
    {
      get => _displayName;
      set
      {
        string normalized = value ?? string.Empty;
        if (_displayName == normalized)
          return;
        _displayName = normalized;
        OnPropertyChanged();
      }
    }

    /// <summary>Mechanical path: дельты параметров гомеostasis (<c>homeostasis_deltas</c>).</summary>
    public Dictionary<int, int> HomeostasisDeltas { get; }

    public void NotifyHomeostasisDeltasChanged()
    {
      OnPropertyChanged(nameof(HomeostasisDeltas));
    }

    /// <summary>
    /// Command pattern для genetic reflex (справочно; 0 = не задан).
    /// </summary>
    public int ReflexTriggerCommandPatternId
    {
      get => _reflexTriggerCommandPatternId;
      set
      {
        if (_reflexTriggerCommandPatternId == value)
          return;
        _reflexTriggerCommandPatternId = value;
        OnPropertyChanged();
      }
    }

    /// <summary>Текст паттерна Command (для UI).</summary>
    public string ReflexTriggerCommandPatternText
    {
      get => _reflexTriggerCommandPatternText;
      set
      {
        string normalized = value ?? string.Empty;
        if (_reflexTriggerCommandPatternText == normalized)
          return;
        _reflexTriggerCommandPatternText = normalized;
        OnPropertyChanged();
      }
    }

    /// <summary>Тип события среды (schema/trigger-detect.json).</summary>
    public string EventKind
    {
      get => _eventKind;
      set
      {
        string normalized = value ?? string.Empty;
        if (_eventKind == normalized)
          return;
        _eventKind = normalized;
        OnPropertyChanged();
      }
    }

    /// <summary>Параметры события.</summary>
    public Dictionary<string, string> EventParameters { get; }

    /// <summary>Краткое описание события для таблицы.</summary>
    public string EventSummary
    {
      get => _eventSummary;
      set
      {
        string normalized = value ?? string.Empty;
        if (_eventSummary == normalized)
          return;
        _eventSummary = normalized;
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
