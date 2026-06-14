using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>Строка таблицы триггеров среды.</summary>
  public sealed class EnvironmentTriggerRow : INotifyPropertyChanged
  {
    private string _id = string.Empty;
    private string _displayName = string.Empty;
    private int _influenceActionId;
    private string _eventKind = string.Empty;
    private string _eventSummary = string.Empty;

    /// <summary>
    /// Создаёт пустую строку.
    /// </summary>
    public EnvironmentTriggerRow()
    {
      EventParameters = new Dictionary<string, string>();
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

    /// <summary>ID воздействия (InfluenceAction).</summary>
    public int InfluenceActionId
    {
      get => _influenceActionId;
      set
      {
        if (_influenceActionId == value)
          return;
        _influenceActionId = value;
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
