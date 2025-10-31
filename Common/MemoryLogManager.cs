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

    private MemoryLogManager()
    {
      LogEntries = new ReadOnlyObservableCollection<LogEntry>(_logEntries);
    }

    /// <summary>
    /// Реализация интерфейса ILogWriter - запись лога из библиотеки
    /// </summary>
    public void WriteLog(string className, string method, int? pulse, int? baseId,
                       int? baseStyleId, int? triggerStimulusId,
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
    public int? GeneticReflexID { get; set; }
    public int? ConditionReflexID { get; set; }

    public string DisplayTime => Timestamp.ToString("HH:mm:ss");
    public string DisplayPulse => Pulse?.ToString() ?? "-";
    public string DisplayBaseID => BaseID?.ToString() ?? "-";
    public string DisplayBaseStyleID => BaseStyleID?.ToString() ?? "-";
    public string DisplayTriggerStimulusID => TriggerStimulusID?.ToString() ?? "-";
    public string DisplayGeneticReflexID => GeneticReflexID?.ToString() ?? "-";
    public string DisplayConditionReflexID => ConditionReflexID?.ToString() ?? "-";
  }
}