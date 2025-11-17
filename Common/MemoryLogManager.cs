using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace AIStudio.Common
{
  /// <summary>
  /// Менеджер логов в памяти для реального времени (клиентская часть)
  /// </summary>
  public sealed class MemoryLogManager : IDisposable, ISIDA.Common.ILogWriter
  {
    private static MemoryLogManager _instance;

    public static MemoryLogManager Instance
    {
      get
      {
        if (_instance == null)
        {
          _instance = new MemoryLogManager();
        }
        return _instance;
      }
    }

    private readonly ObservableCollection<LogEntry> _logEntries = new ObservableCollection<LogEntry>();
    private readonly int _maxLogEntries = 1000;
    private readonly object _lock = new object();
    private bool _disposed = false;

    public ReadOnlyObservableCollection<LogEntry> LogEntries { get; }
    private readonly ObservableCollection<StyleLogEntry> _styleLogEntries = new ObservableCollection<StyleLogEntry>();
    private readonly ObservableCollection<ParameterLogEntry> _parameterLogEntries = new ObservableCollection<ParameterLogEntry>();
    public ReadOnlyObservableCollection<StyleLogEntry> StyleLogEntries { get; }
    public ReadOnlyObservableCollection<ParameterLogEntry> ParameterLogEntries { get; }

    private MemoryLogManager()
    {
      LogEntries = new ReadOnlyObservableCollection<LogEntry>(_logEntries);
      StyleLogEntries = new ReadOnlyObservableCollection<StyleLogEntry>(_styleLogEntries);
      ParameterLogEntries = new ReadOnlyObservableCollection<ParameterLogEntry>(_parameterLogEntries);
    }

    /// <summary>
    /// Запись лога стилей
    /// </summary>
    public void WriteStyleLog(int pulse, string stage, int styleId, string styleName,
                             int weight)
    {
      if (_disposed) return;

      var entry = new StyleLogEntry
      {
        Pulse = pulse,
        Stage = stage,
        StyleId = styleId,
        StyleName = styleName,
        Weight = weight,
        Timestamp = DateTime.Now
      };

      AddStyleLogEntry(entry);
    }

    /// <summary>
    /// Запись лога параметров
    /// </summary>
    public void WriteParameterLog(int pulse, int paramId, string paramName, int weight,
                                 int normaWell, int speed, float value, float urgencyFunction,
                                 string parameterState, string activationZone)
    {
      if (_disposed) return;

      var entry = new ParameterLogEntry
      {
        Pulse = pulse,
        ParamId = paramId,
        ParamName = paramName,
        Weight = weight,
        NormaWell = normaWell,
        Speed = speed,
        Value = value,
        UrgencyFunction = urgencyFunction,
        ParameterState = parameterState,
        ActivationZone = activationZone,
        Timestamp = DateTime.Now
      };

      AddParameterLogEntry(entry);
    }

    /// <summary>
    /// Реализация интерфейса ILogWriter - запись лога из библиотеки
    /// </summary>
    public void WriteLog(string className, string method, int? pulse, int? baseId,
                       int? baseStyleId, int? triggerStimulusId, int? hasCriticalChanges, 
                       int? geneticReflexId, int? conditionedReflexId)
    {
      if (_disposed) return;

      var entry = new LogEntry
      {
        ClassName = className ?? string.Empty,
        Method = method ?? string.Empty,
        Pulse = pulse,
        BaseID = baseId,
        BaseStyleID = baseStyleId == 0 ? null : baseStyleId,
        TriggerStimulusID = triggerStimulusId == 0 ? null : triggerStimulusId,
        HasCriticalChanges = hasCriticalChanges == 0 ? null : hasCriticalChanges,
        GeneticReflexID = geneticReflexId == 0 ? null : geneticReflexId,
        ConditionReflexID = conditionedReflexId == 0 ? null : conditionedReflexId
      };

      AddLogEntry(entry);
    }

    private void AddLogEntry(LogEntry entry)
    {
      if (Application.Current != null && Application.Current.Dispatcher.CheckAccess())
      {
        AddLogEntryInternal(entry);
      }
      else if (Application.Current != null)
      {
        Application.Current.Dispatcher.BeginInvoke(new Action<LogEntry>(AddLogEntryInternal),
            DispatcherPriority.Background, entry);
      }
      else
      {
        AddLogEntryInternal(entry);
      }
    }

    private void AddLogEntryInternal(LogEntry entry)
    {
      lock (_lock)
      {
        _logEntries.Insert(0, entry);

        while (_logEntries.Count > _maxLogEntries)
        {
          _logEntries.RemoveAt(_logEntries.Count - 1);
        }
      }
    }

    private void AddStyleLogEntry(StyleLogEntry entry)
    {
      if (Application.Current != null && Application.Current.Dispatcher.CheckAccess())
      {
        AddStyleLogEntryInternal(entry);
      }
      else if (Application.Current != null)
      {
        Application.Current.Dispatcher.BeginInvoke(new Action<StyleLogEntry>(AddStyleLogEntryInternal),
            DispatcherPriority.Background, entry);
      }
      else
      {
        AddStyleLogEntryInternal(entry);
      }
    }

    private void AddStyleLogEntryInternal(StyleLogEntry entry)
    {
      lock (_lock)
      {
        _styleLogEntries.Insert(0, entry);

        while (_styleLogEntries.Count > _maxLogEntries)
        {
          _styleLogEntries.RemoveAt(_styleLogEntries.Count - 1);
        }
      }
    }

    private void AddParameterLogEntry(ParameterLogEntry entry)
    {
      if (Application.Current != null && Application.Current.Dispatcher.CheckAccess())
      {
        AddParameterLogEntryInternal(entry);
      }
      else if (Application.Current != null)
      {
        Application.Current.Dispatcher.BeginInvoke(new Action<ParameterLogEntry>(AddParameterLogEntryInternal),
            DispatcherPriority.Background, entry);
      }
      else
      {
        AddParameterLogEntryInternal(entry);
      }
    }

    private void AddParameterLogEntryInternal(ParameterLogEntry entry)
    {
      lock (_lock)
      {
        _parameterLogEntries.Insert(0, entry);

        while (_parameterLogEntries.Count > _maxLogEntries)
        {
          _parameterLogEntries.RemoveAt(_parameterLogEntries.Count - 1);
        }
      }
    }

    public void Clear()
    {
      if (_disposed) return;

      if (Application.Current != null && Application.Current.Dispatcher.CheckAccess())
      {
        ClearInternal();
      }
      else if (Application.Current != null)
      {
        Application.Current.Dispatcher.BeginInvoke(new Action(ClearInternal),
            DispatcherPriority.Background);
      }
      else
      {
        ClearInternal();
      }
    }

    private void ClearInternal()
    {
      lock (_lock)
      {
        _logEntries.Clear();
        _styleLogEntries.Clear();
        _parameterLogEntries.Clear();
      }
    }

    public void ClearStyleLogs()
    {
      if (Application.Current != null && Application.Current.Dispatcher.CheckAccess())
      {
        ClearStyleLogsInternal();
      }
      else if (Application.Current != null)
      {
        Application.Current.Dispatcher.BeginInvoke(new Action(ClearStyleLogsInternal),
            DispatcherPriority.Background);
      }
      else
      {
        ClearStyleLogsInternal();
      }
    }

    private void ClearStyleLogsInternal()
    {
      lock (_lock)
      {
        _styleLogEntries.Clear();
      }
    }

    public void ClearParameterLogs()
    {
      if (Application.Current != null && Application.Current.Dispatcher.CheckAccess())
      {
        ClearParameterLogsInternal();
      }
      else if (Application.Current != null)
      {
        Application.Current.Dispatcher.BeginInvoke(new Action(ClearParameterLogsInternal),
            DispatcherPriority.Background);
      }
      else
      {
        ClearParameterLogsInternal();
      }
    }

    private void ClearParameterLogsInternal()
    {
      lock (_lock)
      {
        _parameterLogEntries.Clear();
      }
    }

    public void Dispose()
    {
      if (_disposed) return;

      if (Application.Current != null && Application.Current.Dispatcher.CheckAccess())
      {
        DisposeInternal();
      }
      else if (Application.Current != null)
      {
        Application.Current.Dispatcher.Invoke(new Action(DisposeInternal));
      }
      else
      {
        DisposeInternal();
      }
    }

    private void DisposeInternal()
    {
      lock (_lock)
      {
        _logEntries.Clear();
        _styleLogEntries.Clear();
        _parameterLogEntries.Clear();
        _disposed = true;
      }
    }
  }

  public class LogEntry
  {
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string ClassName { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int? BaseID { get; set; }
    public int? Pulse { get; set; }
    public int? BaseStyleID { get; set; }
    public int? TriggerStimulusID { get; set; }
    public int? HasCriticalChanges { get; set; }
    public int? GeneticReflexID { get; set; }
    public int? ConditionReflexID { get; set; }

    public string DisplayTime => Timestamp.ToString("HH:mm:ss");
    public string DisplayPulse => Pulse?.ToString() ?? "-";
    public string DisplayBaseID => BaseID?.ToString() ?? "-";
    public string DisplayBaseStyleID => BaseStyleID?.ToString() ?? "-";
    public string DisplayTriggerStimulusID => TriggerStimulusID?.ToString() ?? "-";
    public string DisplayHasCriticalChanges => HasCriticalChanges?.ToString() ?? "-";
    public string DisplayGeneticReflexID => GeneticReflexID?.ToString() ?? "-";
    public string DisplayConditionReflexID => ConditionReflexID?.ToString() ?? "-";
  }

  public class StyleLogEntry
  {
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public int Pulse { get; set; }
    public string Stage { get; set; } = string.Empty;
    public int StyleId { get; set; }
    public string StyleName { get; set; } = string.Empty;
    public int Weight { get; set; }

    public string DisplayTime => Timestamp.ToString("HH:mm:ss");
    public string DisplayPulse => Pulse.ToString();
    public string DisplayStage => Stage;
    public string DisplayStyleId => StyleId.ToString();
    public string DisplayStyleName => StyleName;
    public string DisplayWeight => Weight.ToString();
  }

  public class ParameterLogEntry
  {
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public int Pulse { get; set; }
    public int ParamId { get; set; }
    public string ParamName { get; set; } = string.Empty;
    public int Weight { get; set; }
    public int NormaWell { get; set; }
    public int Speed { get; set; }
    public float Value { get; set; }
    public float UrgencyFunction { get; set; }
    public string ParameterState { get; set; } = string.Empty;
    public string ActivationZone { get; set; } = string.Empty;

    public string DisplayTime => Timestamp.ToString("HH:mm:ss");
    public string DisplayPulse => Pulse.ToString();
    public string DisplayParamId => ParamId.ToString();
    public string DisplayParamName => ParamName;
    public string DisplayWeight => Weight.ToString();
    public string DisplayNormaWell => NormaWell.ToString();
    public string DisplaySpeed => Speed.ToString();
    public string DisplayValue => Value.ToString("F2");
    public string DisplayUrgencyFunction => UrgencyFunction.ToString("F4");
    public string DisplayParameterState => ParameterState;
    public string DisplayActivationZone => ActivationZone;
  }
}