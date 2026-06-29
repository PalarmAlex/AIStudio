using ISIDA.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using static AIStudio.Common.MemoryLogManager;

namespace AIStudio.Common
{
  /// <summary>Сессии симбионтного лога в файле AgentLogs.csv (блоки между повторяющимися строками заголовка).</summary>
  public static class AgentLogFileSessions
  {
    public const string CurrentSessionKey = LogFileSessionInfo.CurrentSessionKey;
    private const string AgentLogCsvFileName = "AgentLogs.csv";
    private static readonly string[] TimeFormats =
    {
      "yyyy-MM-dd HH:mm:ss",
      "dd.MM.yyyy HH:mm:ss"
    };
    public static string GetAgentLogCsvPath() => ResolveAgentLogCsvPath();
    /// <summary>Путь к AgentLogs.csv: каталог из настроек проекта или ProgramData\ISIDA\Logs.</summary>
    public static string ResolveAgentLogCsvPath() =>
        LogFilePaths.ResolveLogFile(AgentLogCsvFileName);
    /// <summary>Список сессий из файла (без текущей в памяти), от новых к старым.</summary>
    public static IReadOnlyList<LogFileSessionInfo> ListFileSessions()
    {
      var path = ResolveAgentLogCsvPath();
      if (!File.Exists(path))
        return Array.Empty<LogFileSessionInfo>();
      try
      {
        var blocks = ReadSessionBlocks(path);
        var list = new List<LogFileSessionInfo>();
        for (int i = 0; i < blocks.Count; i++)
        {
          var block = blocks[i];
          if (block.DataRowCount == 0)
            continue;
          list.Add(new LogFileSessionInfo
          {
            SessionKey = i.ToString(CultureInfo.InvariantCulture),
            SessionIndex = i,
            StartedLocal = block.StartedLocal,
            EndedLocal = block.EndedLocal,
            EntryCount = block.DataRowCount
          });
        }
        if (list.Count > 0)
        {
          return list
              .OrderByDescending(s => s.StartedLocal)
              .ToList();
        }

        // CSV есть, но блоки пустые — пробуем jsonl (тот же каталог, те же границы по пульсу 1)
        var jsonlPath = Path.Combine(Path.GetDirectoryName(path) ?? "", "AgentLogs.jsonl");
        if (File.Exists(jsonlPath))
          return ListFileSessionsFromJsonl(jsonlPath);
        return list;
      }
      catch (Exception ex)
      {
        Logger.Error("Сессии AgentLogs.csv: " + ex.Message);
        return Array.Empty<LogFileSessionInfo>();
      }
    }

    public static IList<LogEntry> LoadSessionDisplayEntries(int sessionIndex)
    {
      var raw = LoadSessionRawEntries(sessionIndex);
      return MemoryLogManager.BuildAgentDisplayEntriesFromRaw(raw);
    }

    public static IList<LogEntry> LoadSessionRawEntries(int sessionIndex)
    {
      var path = ResolveAgentLogCsvPath();
      if (!File.Exists(path))
        return new List<LogEntry>();
      var blocks = ReadSessionBlocks(path);
      if (sessionIndex >= 0 && sessionIndex < blocks.Count && blocks[sessionIndex].DataRowCount > 0)
        return blocks[sessionIndex].Entries;
      var jsonlPath = Path.Combine(Path.GetDirectoryName(path) ?? "", "AgentLogs.jsonl");
      if (File.Exists(jsonlPath))
        return LoadSessionRawEntriesFromJsonl(jsonlPath, sessionIndex);
      return new List<LogEntry>();
    }

    private sealed class SessionBlock
    {
      public List<LogEntry> Entries { get; } = new List<LogEntry>();
      public int DataRowCount { get; set; }
      public DateTime StartedLocal { get; set; }
      public DateTime EndedLocal { get; set; }
    }

    private static List<SessionBlock> ReadSessionBlocks(string path)
    {
      var blocks = new List<SessionBlock>();
      SessionBlock current = null;
      Dictionary<string, int> columns = null;
      foreach (var line in ReadLinesShared(path))
      {
        if (string.IsNullOrWhiteSpace(line))
          continue;
        if (IsHeaderRow(line))
        {
          current = new SessionBlock();
          blocks.Add(current);
          columns = ParseHeaderColumns(line);
          continue;
        }
        if (current == null || columns == null)
          continue;
        var entry = TryParseDataRow(line, columns);
        if (entry == null)
          continue;
        current.Entries.Add(entry);
        current.DataRowCount++;
        if (current.DataRowCount == 1)
          current.StartedLocal = entry.Timestamp;
        current.EndedLocal = entry.Timestamp;
      }
      return blocks;
    }

    private static IEnumerable<string> ReadLinesShared(string path)
    {
      using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
      using (var reader = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
      {
        string line;
        while ((line = reader.ReadLine()) != null)
          yield return StripBom(line);
      }
    }

    private static string StripBom(string line)
    {
      if (string.IsNullOrEmpty(line))
        return line;
      if (line[0] == '\uFEFF')
        return line.Substring(1);
      return line;
    }

    private static bool IsHeaderRow(string line)
    {
      line = StripBom(line ?? string.Empty);
      return line.IndexOf("Автоматизм", StringComparison.Ordinal) >= 0
             && line.IndexOf("Время", StringComparison.Ordinal) >= 0
             && line.IndexOf("Пульс", StringComparison.Ordinal) >= 0
             && line.IndexOf(';') >= 0;
    }

    private static Dictionary<string, int> ParseHeaderColumns(string headerLine)
    {
      var parts = StripBom(headerLine ?? string.Empty).Split(';');
      var map = new Dictionary<string, int>(StringComparer.Ordinal);
      for (int i = 0; i < parts.Length; i++)
      {
        var name = parts[i].Trim();
        if (string.IsNullOrEmpty(name) || map.ContainsKey(name))
          continue;
        map[name] = i;
      }
      return map;
    }

    private static IReadOnlyList<LogFileSessionInfo> ListFileSessionsFromJsonl(string jsonlPath)
    {
      var blocks = ReadJsonlSessionBlocks(jsonlPath);
      var list = new List<LogFileSessionInfo>();
      for (int i = 0; i < blocks.Count; i++)
      {
        if (blocks[i].DataRowCount == 0)
          continue;
        list.Add(new LogFileSessionInfo
        {
          SessionKey = i.ToString(CultureInfo.InvariantCulture),
          SessionIndex = i,
          StartedLocal = blocks[i].StartedLocal,
          EndedLocal = blocks[i].EndedLocal,
          EntryCount = blocks[i].DataRowCount
        });
      }
      return list.OrderByDescending(s => s.StartedLocal).ToList();
    }

    private static IList<LogEntry> LoadSessionRawEntriesFromJsonl(string jsonlPath, int sessionIndex)
    {
      var blocks = ReadJsonlSessionBlocks(jsonlPath);
      if (sessionIndex < 0 || sessionIndex >= blocks.Count)
        return new List<LogEntry>();
      return blocks[sessionIndex].Entries;
    }

    private static List<SessionBlock> ReadJsonlSessionBlocks(string jsonlPath)
    {
      var blocks = new List<SessionBlock>();
      SessionBlock current = null;
      int? lastPulse = null;
      foreach (var line in ReadLinesShared(jsonlPath))
      {
        if (string.IsNullOrWhiteSpace(line) || line[0] != '{')
          continue;
        var entry = TryParseJsonlRow(line);
        if (entry == null)
          continue;
        int pulse = entry.Pulse ?? 0;
        bool newSession = current == null
                          || (pulse == 1 && lastPulse.HasValue && lastPulse.Value > 1);
        if (newSession)
        {
          current = new SessionBlock();
          blocks.Add(current);
          lastPulse = null;
        }
        current.Entries.Add(entry);
        current.DataRowCount++;
        if (current.DataRowCount == 1)
          current.StartedLocal = entry.Timestamp;
        current.EndedLocal = entry.Timestamp;
        lastPulse = pulse;
      }
      return blocks;
    }

    private static LogEntry TryParseJsonlRow(string line)
    {
      try
      {
        var jo = Newtonsoft.Json.Linq.JObject.Parse(line);
        string timeRaw = (string)jo["Время"];
        if (!TryParseTimestamp(timeRaw, out DateTime timestamp))
          return null;
        string or = (string)jo["ОР"] ?? "";
        string um = (string)jo["УМ"] ?? "";
        string umOk = null;
        var umOkToken = jo["УМ_успех"];
        if (umOkToken != null && umOkToken.Type != Newtonsoft.Json.Linq.JTokenType.Null)
          umOk = umOkToken.Type == Newtonsoft.Json.Linq.JTokenType.Boolean
              ? ((bool)umOkToken).ToString()
              : (string)umOkToken;
        ParseOrUm(or, um, umOk, out int? ort, out int? tl, out bool? tls);
        var entry = new LogEntry
        {
          Timestamp = timestamp,
          ClassName = (string)jo["Объект"] ?? "ResearchLogger",
          Method = (string)jo["Метод"] ?? "LogSystemState",
          Pulse = ParseNullableInt((string)jo["Пульс"]),
          BaseID = ParseNullableInt((string)jo["Состояние"]),
          BaseStyleID = ParseNullableInt((string)jo["Стили"]),
          TriggerStimulusID = ParseNullableInt((string)jo["Триггер"]),
          OrientationReflexType = ort,
          GeneticReflexID = ParseNullableInt((string)jo["Б/у рефлекс"]),
          ConditionReflexID = ParseNullableInt((string)jo["Усл. рефлекс"]),
          AutomatizmID = ParseNullableInt((string)jo["Автоматизм"]),
          ReflexChainInfo = (string)jo["Цепочка РФ"] ?? "",
          AutomatizmChainInfo = (string)jo["Цепочка АВ"] ?? "",
          ThinkingLevel = tl,
          ThinkingLevelSuccess = tls,
          ThinkingThemeTypeId = ParseNullableInt((string)jo["Тема"]),
          MainThinkingCycleId = ParseNullableInt((string)jo["Цикл М"]),
          BackgroundThinkingCyclesJson = (string)jo["ЦиклыФ_json"],
          InformationEnvironmentDanger = (string)jo["Опасно"] == "1",
          InformationEnvironmentVeryActual = (string)jo["Актуально"] == "1",
          AutomatizmUsefulnessAtSnapshot = ParseNullableInt((string)jo["Полезность"]),
          EnvironmentPressureCell = NullIfEmpty((string)jo["Среда"]),
          EnvironmentPressureTooltip = NullIfEmpty((string)jo["Среда_подсказка"])
        };
        entry.RefreshEnvironmentPressureSegments();
        return entry;
      }
      catch
      {
        return null;
      }
    }

    private static LogEntry TryParseDataRow(string line, Dictionary<string, int> columns)
    {
      var parts = line.Split(';');
      if (parts.Length < 5)
        return null;
      string Get(string name)
      {
        if (!columns.TryGetValue(name, out int ix) || ix >= parts.Length)
          return string.Empty;
        return parts[ix]?.Trim() ?? string.Empty;
      }
      if (!TryParseTimestamp(Get("Время"), out DateTime timestamp))
        return null;
      ParseOrUm(Get("ОР"), Get("УМ"), Get("УМ_успех"),
          out int? orientationReflexType, out int? thinkingLevel, out bool? thinkingLevelSuccess);
      var entry = new LogEntry
      {
        Timestamp = timestamp,
        ClassName = Get("Объект"),
        Method = Get("Метод"),
        Pulse = ParseNullableInt(Get("Пульс")),
        BaseID = ParseNullableInt(Get("Состояние")),
        BaseStyleID = ParseNullableInt(Get("Стили")),
        TriggerStimulusID = ParseNullableInt(Get("Триггер")),
        OrientationReflexType = orientationReflexType,
        GeneticReflexID = ParseNullableInt(Get("Б/у рефлекс")),
        ConditionReflexID = ParseNullableInt(Get("Усл. рефлекс")),
        AutomatizmID = ParseNullableInt(Get("Автоматизм")),
        ReflexChainInfo = Get("Цепочка РФ"),
        AutomatizmChainInfo = Get("Цепочка АВ"),
        ThinkingLevel = thinkingLevel,
        ThinkingLevelSuccess = thinkingLevelSuccess,
        ThinkingThemeTypeId = ParseNullableInt(Get("Тема")),
        MainThinkingCycleId = ParseNullableInt(Get("Цикл М")),
        MainThinkingCycleTooltip = NullIfEmpty(Get("ЦиклМ_тема")),
        MainThinkingCycleTaskStatus = NullIfEmpty(Get("ЦиклМ_задача")),
        BackgroundThinkingCyclesJson = NullIfEmpty(Get("ЦиклыФ_json")),
        InformationEnvironmentDanger = Get("Опасно") == "1",
        InformationEnvironmentVeryActual = Get("Актуально") == "1",
        AutomatizmUsefulnessAtSnapshot = ParseNullableInt(Get("Полезность")),
        EnvironmentPressureCell = NullIfEmpty(Get("Среда")),
        EnvironmentPressureTooltip = NullIfEmpty(Get("Среда_подсказка"))
      };
      if (string.IsNullOrEmpty(entry.ClassName))
        entry.ClassName = "ResearchLogger";
      if (string.IsNullOrEmpty(entry.Method))
        entry.Method = "LogSystemState";
      entry.RefreshEnvironmentPressureSegments();
      return entry;
    }

    private static void ParseOrUm(string orCol, string umCol, string umSuccessCol,
        out int? orientationReflexType, out int? thinkingLevel, out bool? thinkingLevelSuccess)
    {
      orientationReflexType = null;
      thinkingLevel = null;
      thinkingLevelSuccess = null;
      var um = (umCol ?? string.Empty).Trim();
      if (um == "УМ1" || um == "1")
      {
        thinkingLevel = 1;
        thinkingLevelSuccess = ParseNullableBool(umSuccessCol);
        return;
      }
      if (um == "УМ2" || um == "2")
      {
        thinkingLevel = 2;
        thinkingLevelSuccess = ParseNullableBool(umSuccessCol);
        return;
      }
      var or = (orCol ?? string.Empty).Trim();
      if (or == "ОР1" || or == "1")
        orientationReflexType = 1;
      else if (or == "ОР2" || or == "2")
        orientationReflexType = 2;
      else if (int.TryParse(or, NumberStyles.Integer, CultureInfo.InvariantCulture, out int orNum) && orNum > 0)
        orientationReflexType = orNum;
    }

    private static bool TryParseTimestamp(string raw, out DateTime timestamp)
    {
      timestamp = default;
      if (string.IsNullOrWhiteSpace(raw))
        return false;
      if (DateTime.TryParseExact(raw, TimeFormats, CultureInfo.InvariantCulture,
              DateTimeStyles.AssumeLocal, out timestamp))
        return true;
      return DateTime.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out timestamp);
    }

    private static int? ParseNullableInt(string raw)
    {
      if (string.IsNullOrWhiteSpace(raw))
        return null;
      if (int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
        return v;
      return null;
    }

    private static bool? ParseNullableBool(string raw)
    {
      if (string.IsNullOrWhiteSpace(raw))
        return null;
      raw = raw.Trim();
      if (raw == "1" || string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase))
        return true;
      if (raw == "0" || string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase))
        return false;
      return null;
    }

    private static string NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    /// <summary>Удаляет сохранённые сессии из AgentLogs.csv и при наличии — из AgentLogs.jsonl.</summary>
    public static bool TryDeleteFileSessions(IEnumerable<int> blockIndicesToDelete, out string errorMessage)
    {
      errorMessage = null;
      var toDelete = new HashSet<int>(blockIndicesToDelete ?? Enumerable.Empty<int>());
      if (toDelete.Count == 0)
        return true;
      var path = ResolveAgentLogCsvPath();
      if (!File.Exists(path))
      {
        errorMessage = "Файл логов не найден.";
        return false;
      }
      try
      {
        var csvBlocks = ReadRawCsvBlocks(path);
        var remainingCsv = csvBlocks.Where((_, i) => !toDelete.Contains(i)).ToList();
        WriteRawCsvBlocks(path, remainingCsv);
        var jsonlPath = Path.Combine(Path.GetDirectoryName(path) ?? "", "AgentLogs.jsonl");
        if (File.Exists(jsonlPath))
        {
          var jsonlBlocks = ReadRawJsonlBlocks(jsonlPath);
          var remainingJsonl = jsonlBlocks.Where((_, i) => !toDelete.Contains(i)).ToList();
          WriteRawJsonlBlocks(jsonlPath, remainingJsonl);
        }
        return true;
      }
      catch (Exception ex)
      {
        errorMessage = ex.Message;
        Logger.Error("Удаление сессий AgentLogs: " + ex.Message);
        return false;
      }
    }

    private sealed class RawCsvBlock
    {
      public string HeaderLine { get; set; }
      public List<string> DataLines { get; } = new List<string>();
    }

    private sealed class RawJsonlBlock
    {
      public List<string> Lines { get; } = new List<string>();
    }

    private static List<RawCsvBlock> ReadRawCsvBlocks(string path)
    {
      var blocks = new List<RawCsvBlock>();
      RawCsvBlock current = null;
      foreach (var line in ReadLinesShared(path))
      {
        if (string.IsNullOrWhiteSpace(line))
          continue;
        if (IsHeaderRow(line))
        {
          current = new RawCsvBlock { HeaderLine = line };
          blocks.Add(current);
          continue;
        }
        current?.DataLines.Add(line);
      }
      return blocks;
    }

    private static void WriteRawCsvBlocks(string path, IReadOnlyList<RawCsvBlock> blocks)
    {
      var sb = new StringBuilder();
      foreach (var block in blocks)
      {
        sb.AppendLine(block.HeaderLine);
        foreach (var line in block.DataLines)
          sb.AppendLine(line);
      }
      WriteFileAtomically(path, sb.ToString());
    }

    private static List<RawJsonlBlock> ReadRawJsonlBlocks(string jsonlPath)
    {
      var blocks = new List<RawJsonlBlock>();
      RawJsonlBlock current = null;
      int? lastPulse = null;
      foreach (var line in ReadLinesShared(jsonlPath))
      {
        if (string.IsNullOrWhiteSpace(line) || line[0] != '{')
          continue;
        var entry = TryParseJsonlRow(line);
        if (entry == null)
          continue;
        int pulse = entry.Pulse ?? 0;
        bool newSession = current == null
                          || (pulse == 1 && lastPulse.HasValue && lastPulse.Value > 1);
        if (newSession)
        {
          current = new RawJsonlBlock();
          blocks.Add(current);
          lastPulse = null;
        }
        current.Lines.Add(line);
        lastPulse = pulse;
      }
      return blocks;
    }

    private static void WriteRawJsonlBlocks(string jsonlPath, IReadOnlyList<RawJsonlBlock> blocks)
    {
      var sb = new StringBuilder();
      foreach (var block in blocks)
      {
        foreach (var line in block.Lines)
          sb.AppendLine(line);
      }
      WriteFileAtomically(jsonlPath, sb.ToString());
    }

    private static void WriteFileAtomically(string path, string content)
    {
      var tempPath = path + ".tmp";
      File.WriteAllText(tempPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
      File.Copy(tempPath, path, overwrite: true);
      File.Delete(tempPath);
    }
  }
}
