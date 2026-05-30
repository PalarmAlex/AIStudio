using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using static AIStudio.Common.MemoryLogManager;

namespace AIStudio.Common
{
  /// <summary>Сохранение и загрузка архивов симбионтного лога по сессиям запуска студии.</summary>
  public static class AgentLogSessionStorage
  {
    private const string SubfolderName = "LiveSessions";
    private const int MaxArchivedSessions = 30;

    public static string GetSessionsDirectory()
    {
      var logsRoot = AppConfig.LogsFolderPath;
      if (string.IsNullOrWhiteSpace(logsRoot))
      {
        logsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "ISIDA", "Logs");
      }

      return Path.Combine(logsRoot.Trim(), SubfolderName);
    }

    public static IReadOnlyList<AgentLogSessionInfo> ListArchivedSessions()
    {
      var dir = GetSessionsDirectory();
      if (!Directory.Exists(dir))
        return Array.Empty<AgentLogSessionInfo>();

      var list = new List<AgentLogSessionInfo>();
      foreach (var path in Directory.EnumerateFiles(dir, "*.json"))
      {
        try
        {
          var info = TryReadSessionInfo(path);
          if (info != null)
            list.Add(info);
        }
        catch
        {
        }
      }

      return list
          .OrderByDescending(s => s.EndedUtc)
          .ThenByDescending(s => s.StartedUtc)
          .ToList();
    }

    public static void ArchiveCurrentSessionIfNotEmpty(
        IEnumerable<LogEntry> liveEntries,
        DateTime sessionStartedUtc)
    {
      if (liveEntries == null)
        return;

      var snapshot = liveEntries.ToList();
      if (snapshot.Count == 0)
        return;

      var endedUtc = DateTime.UtcNow;
      var sessionId = endedUtc.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)
                      + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);

      var file = new AgentLogSessionFile
      {
        SessionId = sessionId,
        StartedUtc = sessionStartedUtc,
        EndedUtc = endedUtc,
        Entries = snapshot.Select(ToSnapshot).ToList()
      };

      var dir = GetSessionsDirectory();
      Directory.CreateDirectory(dir);
      var path = Path.Combine(dir, sessionId + ".json");
      File.WriteAllText(path, JsonConvert.SerializeObject(file, Formatting.Indented));

      TrimOldSessions(dir);
    }

    public static IList<LogEntry> LoadSessionEntries(string sessionId)
    {
      if (string.IsNullOrWhiteSpace(sessionId))
        return new List<LogEntry>();

      var path = Path.Combine(GetSessionsDirectory(), sessionId + ".json");
      if (!File.Exists(path))
        return new List<LogEntry>();

      var file = JsonConvert.DeserializeObject<AgentLogSessionFile>(File.ReadAllText(path));
      if (file?.Entries == null || file.Entries.Count == 0)
        return new List<LogEntry>();

      var entries = file.Entries.Select(FromSnapshot).ToList();
      MemoryLogManager.ApplyDisplayAggregatesToEntries(entries);
      return entries;
    }

    private static AgentLogSessionInfo TryReadSessionInfo(string path)
    {
      var file = JsonConvert.DeserializeObject<AgentLogSessionFile>(File.ReadAllText(path));
      if (file == null || string.IsNullOrWhiteSpace(file.SessionId))
        return null;

      int count = file.Entries?.Count ?? 0;
      if (count == 0)
      {
        try
        {
          var fi = new FileInfo(path);
          if (fi.Length < 64)
            return null;
        }
        catch
        {
          return null;
        }
      }

      return new AgentLogSessionInfo
      {
        SessionId = file.SessionId,
        StartedUtc = file.StartedUtc,
        EndedUtc = file.EndedUtc,
        EntryCount = count,
        FilePath = path
      };
    }

    private static void TrimOldSessions(string dir)
    {
      var files = Directory.EnumerateFiles(dir, "*.json")
          .Select(p => new FileInfo(p))
          .OrderByDescending(f => f.LastWriteTimeUtc)
          .ToList();

      for (int i = MaxArchivedSessions; i < files.Count; i++)
      {
        try
        {
          files[i].Delete();
        }
        catch
        {
        }
      }
    }

    private static AgentLogEntrySnapshot ToSnapshot(LogEntry e)
    {
      if (e == null)
        return null;

      return new AgentLogEntrySnapshot
      {
        Timestamp = e.Timestamp,
        ClassName = e.ClassName,
        Method = e.Method,
        Pulse = e.Pulse,
        BaseID = e.BaseID,
        BaseStyleID = e.BaseStyleID,
        TriggerStimulusID = e.TriggerStimulusID,
        HasCriticalChanges = e.HasCriticalChanges,
        OrientationReflexType = e.OrientationReflexType,
        GeneticReflexID = e.GeneticReflexID,
        ConditionReflexID = e.ConditionReflexID,
        AutomatizmID = e.AutomatizmID,
        ReflexChainInfo = e.ReflexChainInfo,
        AutomatizmChainInfo = e.AutomatizmChainInfo,
        ThinkingLevel = e.ThinkingLevel,
        ThinkingLevelSuccess = e.ThinkingLevelSuccess,
        ThinkingThemeTypeId = e.ThinkingThemeTypeId,
        ThinkingThemeTooltip = e.ThinkingThemeTooltip,
        MainThinkingCycleId = e.MainThinkingCycleId,
        MainThinkingCycleTooltip = e.MainThinkingCycleTooltip,
        MainThinkingCycleTaskStatus = e.MainThinkingCycleTaskStatus,
        BackgroundThinkingCyclesJson = e.BackgroundThinkingCyclesJson,
        InformationEnvironmentDanger = e.InformationEnvironmentDanger,
        InformationEnvironmentVeryActual = e.InformationEnvironmentVeryActual,
        AutomatizmUsefulnessAtSnapshot = e.AutomatizmUsefulnessAtSnapshot
      };
    }

    private static LogEntry FromSnapshot(AgentLogEntrySnapshot s)
    {
      if (s == null)
        return new LogEntry();

      return new LogEntry
      {
        Timestamp = s.Timestamp,
        ClassName = s.ClassName ?? string.Empty,
        Method = s.Method ?? string.Empty,
        Pulse = s.Pulse,
        BaseID = s.BaseID,
        BaseStyleID = s.BaseStyleID,
        TriggerStimulusID = s.TriggerStimulusID,
        HasCriticalChanges = s.HasCriticalChanges,
        OrientationReflexType = s.OrientationReflexType,
        GeneticReflexID = s.GeneticReflexID,
        ConditionReflexID = s.ConditionReflexID,
        AutomatizmID = s.AutomatizmID,
        ReflexChainInfo = s.ReflexChainInfo ?? string.Empty,
        AutomatizmChainInfo = s.AutomatizmChainInfo ?? string.Empty,
        ThinkingLevel = s.ThinkingLevel,
        ThinkingLevelSuccess = s.ThinkingLevelSuccess,
        ThinkingThemeTypeId = s.ThinkingThemeTypeId,
        ThinkingThemeTooltip = s.ThinkingThemeTooltip,
        MainThinkingCycleId = s.MainThinkingCycleId,
        MainThinkingCycleTooltip = s.MainThinkingCycleTooltip,
        MainThinkingCycleTaskStatus = s.MainThinkingCycleTaskStatus,
        BackgroundThinkingCyclesJson = s.BackgroundThinkingCyclesJson,
        InformationEnvironmentDanger = s.InformationEnvironmentDanger,
        InformationEnvironmentVeryActual = s.InformationEnvironmentVeryActual,
        AutomatizmUsefulnessAtSnapshot = s.AutomatizmUsefulnessAtSnapshot
      };
    }

    public sealed class AgentLogSessionInfo
    {
      public string SessionId { get; set; }
      public DateTime StartedUtc { get; set; }
      public DateTime EndedUtc { get; set; }
      public int EntryCount { get; set; }
      public string FilePath { get; set; }

      public string BuildDisplayLabel()
      {
        var startLocal = StartedUtc.ToLocalTime();
        var endLocal = EndedUtc.ToLocalTime();
        return startLocal.ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture)
               + "–" + endLocal.ToString("HH:mm", CultureInfo.CurrentCulture)
               + " (" + EntryCount + ")";
      }
    }

    private sealed class AgentLogSessionFile
    {
      public string SessionId { get; set; }
      public DateTime StartedUtc { get; set; }
      public DateTime EndedUtc { get; set; }
      public List<AgentLogEntrySnapshot> Entries { get; set; }
    }

    private sealed class AgentLogEntrySnapshot
    {
      public DateTime Timestamp { get; set; }
      public string ClassName { get; set; }
      public string Method { get; set; }
      public int? Pulse { get; set; }
      public int? BaseID { get; set; }
      public int? BaseStyleID { get; set; }
      public int? TriggerStimulusID { get; set; }
      public int? HasCriticalChanges { get; set; }
      public int? OrientationReflexType { get; set; }
      public int? GeneticReflexID { get; set; }
      public int? ConditionReflexID { get; set; }
      public int? AutomatizmID { get; set; }
      public string ReflexChainInfo { get; set; }
      public string AutomatizmChainInfo { get; set; }
      public int? ThinkingLevel { get; set; }
      public bool? ThinkingLevelSuccess { get; set; }
      public int? ThinkingThemeTypeId { get; set; }
      public string ThinkingThemeTooltip { get; set; }
      public int? MainThinkingCycleId { get; set; }
      public string MainThinkingCycleTooltip { get; set; }
      public string MainThinkingCycleTaskStatus { get; set; }
      public string BackgroundThinkingCyclesJson { get; set; }
      public bool InformationEnvironmentDanger { get; set; }
      public bool InformationEnvironmentVeryActual { get; set; }
      public int? AutomatizmUsefulnessAtSnapshot { get; set; }
    }
  }
}
