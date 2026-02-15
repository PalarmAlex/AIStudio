using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace AIStudio.Common
{
  /// <summary>
  /// Менеджер логов в памяти для отображения в реальном времени в пользовательском интерфейсе
  /// </summary>
  public sealed class MemoryLogManager : IDisposable, ISIDA.Common.ILogWriter
  {
    #region Singleton Implementation

    private static MemoryLogManager _instance;

    /// <summary>
    /// Единственный экземпляр менеджера логов в памяти
    /// </summary>
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

    #endregion

    #region Private Fields

    private readonly ObservableCollection<LogEntry> _logEntries = new ObservableCollection<LogEntry>();
    private readonly ObservableCollection<StyleLogEntry> _styleLogEntries = new ObservableCollection<StyleLogEntry>();
    private readonly ObservableCollection<ParameterLogEntry> _parameterLogEntries = new ObservableCollection<ParameterLogEntry>();
    private readonly ObservableCollection<StyleParameterActivationEntry> _styleParameterActivationEntries = new ObservableCollection<StyleParameterActivationEntry>();
    private readonly ObservableCollection<ChainLogEntry> _chainLogEntries = new ObservableCollection<ChainLogEntry>();
    private readonly int _maxLogEntries = 1000;
    private readonly object _lock = new object();
    private bool _disposed = false;

    #endregion

    #region Public Properties

    /// <summary>
    /// Коллекция записей системных логов в режиме только для чтения
    /// </summary>
    public ReadOnlyObservableCollection<LogEntry> LogEntries { get; }

    /// <summary>
    /// Коллекция записей логов стилей поведения в режиме только для чтения
    /// </summary>
    public ReadOnlyObservableCollection<StyleLogEntry> StyleLogEntries { get; }

    /// <summary>
    /// Коллекция записей логов параметров гомеостаза в режиме только для чтения
    /// </summary>
    public ReadOnlyObservableCollection<ParameterLogEntry> ParameterLogEntries { get; }

    /// <summary>
    /// Коллекция записей активации стилей от параметров в режиме только для чтения
    /// </summary>
    public ReadOnlyObservableCollection<StyleParameterActivationEntry> StyleParameterActivationEntries { get; }

    /// <summary>
    /// Коллекция записей выполнения цепочек рефлексов и автоматизмов в режиме только для чтения
    /// </summary>
    public ReadOnlyObservableCollection<ChainLogEntry> ChainLogEntries { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Приватный конструктор для реализации singleton паттерна
    /// </summary>
    private MemoryLogManager()
    {
      LogEntries = new ReadOnlyObservableCollection<LogEntry>(_logEntries);
      StyleLogEntries = new ReadOnlyObservableCollection<StyleLogEntry>(_styleLogEntries);
      ParameterLogEntries = new ReadOnlyObservableCollection<ParameterLogEntry>(_parameterLogEntries);
      StyleParameterActivationEntries = new ReadOnlyObservableCollection<StyleParameterActivationEntry>(_styleParameterActivationEntries);
      ChainLogEntries = new ReadOnlyObservableCollection<ChainLogEntry>(_chainLogEntries);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Записывает лог стилей поведения в память
    /// </summary>
    /// <param name="pulse">Номер текущего пульса системы</param>
    /// <param name="stage">Стадия процесса активации стилей</param>
    /// <param name="styleId">Идентификатор стиля поведения</param>
    /// <param name="styleName">Наименование стиля поведения</param>
    /// <param name="weight">Вес стиля в текущей стадии</param>
    public void WriteStyleLog(int pulse, string stage, int styleId, string styleName)
    {
      if (_disposed) return;

      var entry = new StyleLogEntry
      {
        Pulse = pulse,
        Stage = stage,
        StyleId = styleId,
        StyleName = styleName,
        Timestamp = DateTime.Now
      };

      AddStyleLogEntry(entry);
    }

    /// <summary>
    /// Записывает лог параметров гомеостаза в память
    /// </summary>
    /// <param name="pulse">Номер текущего пульса системы</param>
    /// <param name="paramId">Идентификатор параметра гомеостаза</param>
    /// <param name="paramName">Наименование параметра гомеостаза</param>
    /// <param name="weight">Вес параметра в системе гомеостаза</param>
    /// <param name="normaWell">Значение нормы параметра</param>
    /// <param name="speed">Скорость изменения параметра</param>
    /// <param name="value">Текущее значение параметра</param>
    /// <param name="urgencyFunction">Значение функции срочности</param>
    /// <param name="parameterState">Текущее состояние параметра</param>
    /// <param name="activationZone">Зона активации параметра</param>
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
    /// Реализация интерфейса ILogWriter - записывает системный лог из библиотеки ISIDA
    /// </summary>
    /// <param name="className">Имя класса, в котором произошло событие</param>
    /// <param name="method">Имя метода, в котором произошло событие</param>
    /// <param name="pulse">Номер пульса системы</param>
    /// <param name="baseId">Идентификатор базового состояния</param>
    /// <param name="baseStyleId">Идентификатор базового стиля поведения</param>
    /// <param name="triggerStimulusId">Идентификатор триггерного стимула</param>
    /// <param name="hasCriticalChanges">Флаг наличия критических изменений</param>
    /// <param name="geneticReflexId">Идентификатор безусловного рефлекса</param>
    /// <param name="conditionedReflexId">Идентификатор условного рефлекса</param>
    public void WriteLog(string className, string method, int? pulse, int? baseId,
                       int? baseStyleId, int? triggerStimulusId, int? orientationReflexType,
                       int? geneticReflexId, int? conditionedReflexId, int? automatizmId,
                       string reflexChainInfo = null, string automatizmChainInfo = null)
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
        OrientationReflexType = orientationReflexType == 0 ? null : orientationReflexType,
        GeneticReflexID = geneticReflexId == 0 ? null : geneticReflexId,
        ConditionReflexID = conditionedReflexId == 0 ? null : conditionedReflexId,
        AutomatizmID = automatizmId == 0 ? null : automatizmId,
        ReflexChainInfo = reflexChainInfo ?? string.Empty,
        AutomatizmChainInfo = automatizmChainInfo ?? string.Empty,
        Timestamp = DateTime.Now
      };

      AddLogEntry(entry);
    }

    /// <summary>
    /// Записывает лог активации стилей от параметров гомеостаза
    /// </summary>
    /// <param name="pulse">Номер текущего пульса системы</param>
    /// <param name="stage">Стадия процесса активации</param>
    /// <param name="parameterId">Идентификатор параметра гомеостаза</param>
    /// <param name="parameterName">Наименование параметра гомеостаза</param>
    /// <param name="zoneId">Идентификатор зоны активации (0-6)</param>
    /// <param name="zoneDescription">Описание зоны активации</param>
    /// <param name="styleId">Идентификатор стиля поведения</param>
    /// <param name="styleName">Наименование стиля поведения</param>
    /// <param name="weight">Вес стиля при активации</param>
    /// <param name="activationDetails">Детали процесса активации</param>
    public void WriteStyleParameterActivation(int pulse, string stage, int parameterId, string parameterName,
                                             int zoneId, string zoneDescription, int styleId, string styleName,
                                             string activationDetails)
    {
      if (_disposed) return;

      var entry = new StyleParameterActivationEntry
      {
        Pulse = pulse,
        Stage = stage,
        ParameterId = parameterId,
        ParameterName = parameterName,
        ZoneId = zoneId,
        ZoneDescription = zoneDescription,
        StyleId = styleId,
        StyleName = styleName,
        ActivationDetails = activationDetails,
        Timestamp = DateTime.Now
      };

      AddStyleParameterActivationEntry(entry);
    }

    /// <summary>
    /// Записывает лог события выполнения цепочки рефлексов или автоматизмов
    /// </summary>
    /// <param name="pulse">Номер текущего пульса системы</param>
    /// <param name="chainType">Тип цепочки: "Reflex" или "Automatizm"</param>
    /// <param name="chainId">Идентификатор цепочки</param>
    /// <param name="chainName">Наименование цепочки</param>
    /// <param name="eventType">Тип события: ChainStart, LinkExecute, Evaluation, BranchDecision, ChainComplete</param>
    /// <param name="linkId">Идентификатор звена (если применимо)</param>
    /// <param name="actionId">Идентификатор действия (если применимо)</param>
    /// <param name="operatorEvaluation">Оценка оператора (true/false/null)</param>
    /// <param name="nextLinkId">Идентификатор следующего звена после ветвления</param>
    /// <param name="branchType">Тип выбранной ветви: Success или Failure</param>
    /// <param name="details">Текстовое описание события</param>
    public void WriteChainLog(int pulse, string chainType, int chainId, string chainName, string eventType,
                             int? linkId = null, int? actionId = null, bool? operatorEvaluation = null,
                             int? nextLinkId = null, string branchType = null, string details = null)
    {
      if (_disposed) return;

      var entry = new ChainLogEntry
      {
        Pulse = pulse,
        ChainType = chainType,
        ChainId = chainId,
        ChainName = chainName,
        EventType = eventType,
        LinkId = linkId,
        ActionId = actionId,
        OperatorEvaluation = operatorEvaluation,
        NextLinkId = nextLinkId,
        BranchType = branchType,
        Details = details,
        Timestamp = DateTime.Now
      };

      AddChainLogEntry(entry);
    }

    /// <summary>
    /// Полностью очищает все коллекции логов в памяти
    /// </summary>
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

    /// <summary>
    /// Очищает коллекцию логов стилей поведения
    /// </summary>
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

    /// <summary>
    /// Очищает коллекцию логов параметров гомеостаза
    /// </summary>
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

    /// <summary>
    /// Очищает коллекцию записей активации стилей от параметров
    /// </summary>
    public void ClearStyleParameterActivations()
    {
      if (Application.Current != null && Application.Current.Dispatcher.CheckAccess())
      {
        ClearStyleParameterActivationsInternal();
      }
      else if (Application.Current != null)
      {
        Application.Current.Dispatcher.BeginInvoke(new Action(ClearStyleParameterActivationsInternal),
            DispatcherPriority.Background);
      }
      else
      {
        ClearStyleParameterActivationsInternal();
      }
    }

    /// <summary>
    /// Очищает коллекцию логов выполнения цепочек рефлексов и автоматизмов
    /// </summary>
    public void ClearChainLogs()
    {
      if (Application.Current != null && Application.Current.Dispatcher.CheckAccess())
      {
        ClearChainLogsInternal();
      }
      else if (Application.Current != null)
      {
        Application.Current.Dispatcher.BeginInvoke(new Action(ClearChainLogsInternal),
            DispatcherPriority.Background);
      }
      else
      {
        ClearChainLogsInternal();
      }
    }

    /// <summary>
    /// Освобождает все ресурсы, используемые менеджером логов
    /// </summary>
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

    #endregion

    #region Private Methods

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

    private void AddStyleParameterActivationEntry(StyleParameterActivationEntry entry)
    {
      if (Application.Current != null && Application.Current.Dispatcher.CheckAccess())
      {
        AddStyleParameterActivationEntryInternal(entry);
      }
      else if (Application.Current != null)
      {
        Application.Current.Dispatcher.BeginInvoke(new Action<StyleParameterActivationEntry>(AddStyleParameterActivationEntryInternal),
            DispatcherPriority.Background, entry);
      }
      else
      {
        AddStyleParameterActivationEntryInternal(entry);
      }
    }

    private void AddStyleParameterActivationEntryInternal(StyleParameterActivationEntry entry)
    {
      lock (_lock)
      {
        _styleParameterActivationEntries.Insert(0, entry);

        while (_styleParameterActivationEntries.Count > _maxLogEntries)
        {
          _styleParameterActivationEntries.RemoveAt(_styleParameterActivationEntries.Count - 1);
        }
      }
    }

    private void AddChainLogEntry(ChainLogEntry entry)
    {
      if (Application.Current != null && Application.Current.Dispatcher.CheckAccess())
      {
        AddChainLogEntryInternal(entry);
      }
      else if (Application.Current != null)
      {
        Application.Current.Dispatcher.BeginInvoke(new Action<ChainLogEntry>(AddChainLogEntryInternal),
            DispatcherPriority.Background, entry);
      }
      else
      {
        AddChainLogEntryInternal(entry);
      }
    }

    private void AddChainLogEntryInternal(ChainLogEntry entry)
    {
      lock (_lock)
      {
        _chainLogEntries.Insert(0, entry);

        while (_chainLogEntries.Count > _maxLogEntries)
        {
          _chainLogEntries.RemoveAt(_chainLogEntries.Count - 1);
        }
      }
    }

    private void ClearInternal()
    {
      lock (_lock)
      {
        _logEntries.Clear();
        _styleLogEntries.Clear();
        _parameterLogEntries.Clear();
        _styleParameterActivationEntries.Clear();
        _chainLogEntries.Clear();
      }
    }

    private void ClearStyleLogsInternal()
    {
      lock (_lock)
      {
        _styleLogEntries.Clear();
      }
    }

    private void ClearParameterLogsInternal()
    {
      lock (_lock)
      {
        _parameterLogEntries.Clear();
      }
    }

    private void ClearStyleParameterActivationsInternal()
    {
      lock (_lock)
      {
        _styleParameterActivationEntries.Clear();
      }
    }

    private void ClearChainLogsInternal()
    {
      lock (_lock)
      {
        _chainLogEntries.Clear();
      }
    }

    private void DisposeInternal()
    {
      lock (_lock)
      {
        _logEntries.Clear();
        _styleLogEntries.Clear();
        _parameterLogEntries.Clear();
        _styleParameterActivationEntries.Clear();
        _disposed = true;
      }
    }

    #endregion

    #region Log Entry Classes

    /// <summary>
    /// Запись системного лога для отображения в пользовательском интерфейсе
    /// </summary>
    public class LogEntry
    {
      /// <summary>
      /// Временная метка создания записи
      /// </summary>
      public DateTime Timestamp { get; set; } = DateTime.Now;

      /// <summary>
      /// Имя класса, в котором произошло событие
      /// </summary>
      public string ClassName { get; set; } = string.Empty;

      /// <summary>
      /// Имя метода, в котором произошло событие
      /// </summary>
      public string Method { get; set; } = string.Empty;

      /// <summary>
      /// Идентификатор базового состояния системы
      /// </summary>
      public int? BaseID { get; set; }

      /// <summary>
      /// Номер текущего пульса системы
      /// </summary>
      public int? Pulse { get; set; }

      /// <summary>
      /// Идентификатор базового стиля поведения
      /// </summary>
      public int? BaseStyleID { get; set; }

      /// <summary>
      /// Идентификатор триггерного стимула
      /// </summary>
      public int? TriggerStimulusID { get; set; }

      /// <summary>
      /// Флаг наличия критических изменений в системе
      /// </summary>
      public int? HasCriticalChanges { get; set; }

      /// <summary>
      /// Идентификатор ориентировочного рефлекса
      /// </summary>
      public int? OrientationReflexType { get; set; }

      /// <summary>
      /// Идентификатор активного безусловного рефлекса
      /// </summary>
      public int? GeneticReflexID { get; set; }

      /// <summary>
      /// Идентификатор активного условного рефлекса
      /// </summary>
      public int? ConditionReflexID { get; set; }

      /// <summary>
      /// Идентификатор активного автоматизма
      /// </summary>
      public int? AutomatizmID { get; set; }

      /// <summary>
      /// Отформатированное время для отображения в UI
      /// </summary>
      public string DisplayTime => Timestamp.ToString("HH:mm:ss");

      /// <summary>
      /// Отформатированный номер пульса для отображения в UI
      /// </summary>
      public string DisplayPulse => Pulse?.ToString() ?? "-";

      /// <summary>
      /// Отформатированный идентификатор базового состояния для отображения в UI
      /// </summary>
      public string DisplayBaseID => BaseID?.ToString() ?? "-";

      /// <summary>
      /// Отформатированный идентификатор базового стиля для отображения в UI
      /// </summary>
      public string DisplayBaseStyleID => BaseStyleID?.ToString() ?? "-";

      /// <summary>
      /// Отформатированный идентификатор триггерного стимула для отображения в UI
      /// </summary>
      public string DisplayTriggerStimulusID => TriggerStimulusID?.ToString() ?? "-";

      /// <summary>
      /// Отформатированный флаг критических изменений для отображения в UI
      /// </summary>
      public string DisplayHasCriticalChanges => HasCriticalChanges?.ToString() ?? "-";

      public string DisplayOrientationReflexType
      {
        get
        {
          if (!OrientationReflexType.HasValue)
            return "-";

          string result;
          switch (OrientationReflexType.Value)
          {
            case 1:
              result = "ОР1";
              break;
            case 2:
              result = "ОР2";
              break;
            default:
              result = OrientationReflexType.Value.ToString();
              break;
          }
          return result;
        }
      }

      /// <summary>
      /// Отформатированный идентификатор безусловного рефлекса для отображения в UI
      /// </summary>
      public string DisplayGeneticReflexID => GeneticReflexID?.ToString() ?? "-";

      /// <summary>
      /// Отформатированный идентификатор условного рефлекса для отображения в UI
      /// </summary>
      public string DisplayConditionReflexID => ConditionReflexID?.ToString() ?? "-";

      /// <summary>
      /// Отформатированный идентификатор автоматизма для отображения в UI
      /// </summary>
      public string DisplayAutomatizmID => AutomatizmID?.ToString() ?? "-";

      /// <summary>
      /// ID цепочки рефлекса и ID действия в формате "ChainId:ActionId"
      /// </summary>
      public string ReflexChainInfo { get; set; } = string.Empty;

      /// <summary>
      /// ID цепочки автоматизма и ID действия в формате "ChainId:ActionId"
      /// </summary>
      public string AutomatizmChainInfo { get; set; } = string.Empty;

      /// <summary>
      /// Форматированная информация о цепочке рефлекса для отображения в UI (используется с tooltip)
      /// </summary>
      public string DisplayReflexChainInfo => !string.IsNullOrEmpty(ReflexChainInfo) ? ReflexChainInfo : "-";

      /// <summary>
      /// Форматированная информация о цепочке автоматизма для отображения в UI (используется с tooltip)
      /// </summary>
      public string DisplayAutomatizmChainInfo => !string.IsNullOrEmpty(AutomatizmChainInfo) ? AutomatizmChainInfo : "-";
    }

    /// <summary>
    /// Запись лога стилей поведения для отображения в пользовательском интерфейсе
    /// </summary>
    public class StyleLogEntry
    {
      /// <summary>
      /// Временная метка создания записи
      /// </summary>
      public DateTime Timestamp { get; set; } = DateTime.Now;

      /// <summary>
      /// Номер текущего пульса системы
      /// </summary>
      public int Pulse { get; set; }

      /// <summary>
      /// Стадия процесса активации стилей
      /// </summary>
      public string Stage { get; set; } = string.Empty;

      /// <summary>
      /// Идентификатор стиля поведения
      /// </summary>
      public int StyleId { get; set; }

      /// <summary>
      /// Наименование стиля поведения
      /// </summary>
      public string StyleName { get; set; } = string.Empty;

      /// <summary>
      /// Отформатированное время для отображения в UI
      /// </summary>
      public string DisplayTime => Timestamp.ToString("HH:mm:ss");

      /// <summary>
      /// Отформатированный номер пульса для отображения в UI
      /// </summary>
      public string DisplayPulse => Pulse.ToString();

      /// <summary>
      /// Стадия процесса для отображения в UI
      /// </summary>
      public string DisplayStage => Stage;

      /// <summary>
      /// Отформатированный идентификатор стиля для отображения в UI
      /// </summary>
      public string DisplayStyleId => StyleId.ToString();

      /// <summary>
      /// Наименование стиля для отображения в UI
      /// </summary>
      public string DisplayStyleName => StyleName;
    }

    /// <summary>
    /// Запись лога параметров гомеостаза для отображения в пользовательском интерфейсе
    /// </summary>
    public class ParameterLogEntry
    {
      /// <summary>
      /// Временная метка создания записи
      /// </summary>
      public DateTime Timestamp { get; set; } = DateTime.Now;

      /// <summary>
      /// Номер текущего пульса системы
      /// </summary>
      public int Pulse { get; set; }

      /// <summary>
      /// Идентификатор параметра гомеостаза
      /// </summary>
      public int ParamId { get; set; }

      /// <summary>
      /// Наименование параметра гомеостаза
      /// </summary>
      public string ParamName { get; set; } = string.Empty;

      /// <summary>
      /// Вес параметра в системе гомеостаза
      /// </summary>
      public int Weight { get; set; }

      /// <summary>
      /// Значение нормы параметра
      /// </summary>
      public int NormaWell { get; set; }

      /// <summary>
      /// Скорость изменения параметра
      /// </summary>
      public int Speed { get; set; }

      /// <summary>
      /// Текущее значение параметра
      /// </summary>
      public float Value { get; set; }

      /// <summary>
      /// Значение функции срочности
      /// </summary>
      public float UrgencyFunction { get; set; }

      /// <summary>
      /// Текущее состояние параметра
      /// </summary>
      public string ParameterState { get; set; } = string.Empty;

      /// <summary>
      /// Зона активации параметра
      /// </summary>
      public string ActivationZone { get; set; } = string.Empty;

      /// <summary>
      /// Отформатированное время для отображения в UI
      /// </summary>
      public string DisplayTime => Timestamp.ToString("HH:mm:ss");

      /// <summary>
      /// Отформатированный номер пульса для отображения в UI
      /// </summary>
      public string DisplayPulse => Pulse.ToString();

      /// <summary>
      /// Отформатированный идентификатор параметра для отображения в UI
      /// </summary>
      public string DisplayParamId => ParamId.ToString();

      /// <summary>
      /// Наименование параметра для отображения в UI
      /// </summary>
      public string DisplayParamName => ParamName;

      /// <summary>
      /// Отформатированный вес параметра для отображения в UI
      /// </summary>
      public string DisplayWeight => Weight.ToString();

      /// <summary>
      /// Отформатированное значение нормы для отображения в UI
      /// </summary>
      public string DisplayNormaWell => NormaWell.ToString();

      /// <summary>
      /// Отформатированная скорость изменения для отображения в UI
      /// </summary>
      public string DisplaySpeed => Speed.ToString();

      /// <summary>
      /// Отформатированное текущее значение параметра для отображения в UI
      /// </summary>
      public string DisplayValue => Value.ToString("F2");

      /// <summary>
      /// Отформатированное значение функции срочности для отображения в UI
      /// </summary>
      public string DisplayUrgencyFunction => UrgencyFunction.ToString("F4");

      /// <summary>
      /// Состояние параметра для отображения в UI
      /// </summary>
      public string DisplayParameterState => ParameterState;

      /// <summary>
      /// Зона активации для отображения в UI
      /// </summary>
      public string DisplayActivationZone => ActivationZone;
    }

    /// <summary>
    /// Запись активации стилей поведения от параметров гомеостаза для отображения в пользовательском интерфейсе
    /// </summary>
    public class StyleParameterActivationEntry
    {
      /// <summary>
      /// Временная метка создания записи
      /// </summary>
      public DateTime Timestamp { get; set; } = DateTime.Now;

      /// <summary>
      /// Номер текущего пульса системы
      /// </summary>
      public int Pulse { get; set; }

      /// <summary>
      /// Стадия процесса активации
      /// </summary>
      public string Stage { get; set; } = string.Empty;

      /// <summary>
      /// Идентификатор параметра гомеостаза
      /// </summary>
      public int ParameterId { get; set; }

      /// <summary>
      /// Наименование параметра гомеостаза
      /// </summary>
      public string ParameterName { get; set; } = string.Empty;

      /// <summary>
      /// Идентификатор зоны активации (0-6)
      /// </summary>
      public int ZoneId { get; set; }

      /// <summary>
      /// Описание зоны активации
      /// </summary>
      public string ZoneDescription { get; set; } = string.Empty;

      /// <summary>
      /// Идентификатор стиля поведения
      /// </summary>
      public int StyleId { get; set; }

      /// <summary>
      /// Наименование стиля поведения
      /// </summary>
      public string StyleName { get; set; } = string.Empty;

      /// <summary>
      /// Вес стиля при активации
      /// </summary>
      public int Weight { get; set; }

      /// <summary>
      /// Детали процесса активации
      /// </summary>
      public string ActivationDetails { get; set; } = string.Empty;

      /// <summary>
      /// Отформатированное время для отображения в UI
      /// </summary>
      public string DisplayTime => Timestamp.ToString("HH:mm:ss");

      /// <summary>
      /// Отформатированный номер пульса для отображения в UI
      /// </summary>
      public string DisplayPulse => Pulse.ToString();

      /// <summary>
      /// Стадия процесса для отображения в UI
      /// </summary>
      public string DisplayStage => Stage;

      /// <summary>
      /// Отформатированный идентификатор параметра для отображения в UI
      /// </summary>
      public string DisplayParameterId => ParameterId.ToString();

      /// <summary>
      /// Наименование параметра для отображения в UI
      /// </summary>
      public string DisplayParameterName => ParameterName;

      /// <summary>
      /// Отформатированный идентификатор зоны для отображения в UI
      /// </summary>
      public string DisplayZoneId => ZoneId.ToString();

      /// <summary>
      /// Описание зоны для отображения в UI
      /// </summary>
      public string DisplayZoneDescription => ZoneDescription;

      /// <summary>
      /// Отформатированный идентификатор стиля для отображения в UI
      /// </summary>
      public string DisplayStyleId => StyleId.ToString();

      /// <summary>
      /// Наименование стиля для отображения в UI
      /// </summary>
      public string DisplayStyleName => StyleName;

      /// <summary>
      /// Отформатированный вес стиля для отображения в UI
      /// </summary>
      public string DisplayWeight => Weight.ToString();

      /// <summary>
      /// Детали активации для отображения в UI
      /// </summary>
      public string DisplayActivationDetails => ActivationDetails;
    }

    /// <summary>
    /// Запись лога события выполнения цепочки рефлексов или автоматизмов
    /// </summary>
    public class ChainLogEntry
    {
      /// <summary>
      /// Временная метка создания записи
      /// </summary>
      public DateTime Timestamp { get; set; } = DateTime.Now;

      /// <summary>
      /// Номер текущего пульса системы
      /// </summary>
      public int Pulse { get; set; }

      /// <summary>
      /// Тип цепочки: "Reflex" или "Automatizm"
      /// </summary>
      public string ChainType { get; set; } = string.Empty;

      /// <summary>
      /// Идентификатор цепочки
      /// </summary>
      public int ChainId { get; set; }

      /// <summary>
      /// Наименование цепочки
      /// </summary>
      public string ChainName { get; set; } = string.Empty;

      /// <summary>
      /// Тип события: ChainStart, LinkExecute, Evaluation, BranchDecision, ChainComplete
      /// </summary>
      public string EventType { get; set; } = string.Empty;

      /// <summary>
      /// Идентификатор звена цепочки (для событий выполнения)
      /// </summary>
      public int? LinkId { get; set; }

      /// <summary>
      /// Идентификатор действия (для событий выполнения звена)
      /// </summary>
      public int? ActionId { get; set; }

      /// <summary>
      /// Оценка оператора (true=успех, false=неудача, null=ожидание)
      /// </summary>
      public bool? OperatorEvaluation { get; set; }

      /// <summary>
      /// Идентификатор следующего звена после ветвления
      /// </summary>
      public int? NextLinkId { get; set; }

      /// <summary>
      /// Тип выбранной ветви: Success или Failure
      /// </summary>
      public string BranchType { get; set; } = string.Empty;

      /// <summary>
      /// Описание события
      /// </summary>
      public string Details { get; set; } = string.Empty;

      // Display properties for UI

      /// <summary>
      /// Отформатированное время для отображения в UI
      /// </summary>
      public string DisplayTime => Timestamp.ToString("HH:mm:ss");

      /// <summary>
      /// Отформатированный номер пульса для отображения в UI
      /// </summary>
      public string DisplayPulse => Pulse.ToString();

      /// <summary>
      /// Тип цепочки для отображения в UI
      /// </summary>
      public string DisplayChainType => ChainType;

      /// <summary>
      /// Идентификатор цепочки для отображения в UI
      /// </summary>
      public string DisplayChainId => ChainId.ToString();

      /// <summary>
      /// Наименование цепочки для отображения в UI
      /// </summary>
      public string DisplayChainName => ChainName;

      /// <summary>
      /// Тип события для отображения в UI
      /// </summary>
      public string DisplayEventType => EventType;

      /// <summary>
      /// Идентификатор звена для отображения в UI
      /// </summary>
      public string DisplayLinkId => LinkId?.ToString() ?? "-";

      /// <summary>
      /// Идентификатор действия для отображения в UI
      /// </summary>
      public string DisplayActionId => ActionId?.ToString() ?? "-";

      /// <summary>
      /// Оценка оператора для отображения в UI
      /// </summary>
      public string DisplayOperatorEvaluation
      {
        get
        {
          if (OperatorEvaluation == null)
            return "—";
          return OperatorEvaluation.Value ? "✓ Успех" : "✗ Неудача";
        }
      }

      /// <summary>
      /// Идентификатор следующего звена для отображения в UI
      /// </summary>
      public string DisplayNextLinkId => NextLinkId?.ToString() ?? "-";

      /// <summary>
      /// Тип ветви для отображения в UI
      /// </summary>
      public string DisplayBranchType => BranchType;

      /// <summary>
      /// Описание события для отображения в UI
      /// </summary>
      public string DisplayDetails => Details;
    }

    #endregion
  }
}