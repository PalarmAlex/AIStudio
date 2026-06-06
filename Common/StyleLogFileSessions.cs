using System;
using System.Collections.Generic;
using System.Linq;
using static AIStudio.Common.MemoryLogManager;

namespace AIStudio.Common
{
  /// <summary>Сессии лога стилей (AgentLogs_Styles.csv).</summary>
  public static class StyleLogFileSessions
  {
    private const string CsvFileName = "AgentLogs_Styles.csv";
    public static IReadOnlyList<LogFileSessionInfo> ListFileSessions() =>
        CsvLogFileSessionReader.ListSessions(CsvFileName, IsHeaderRow, "Time");
    public static StyleLogSessionData LoadSessionData(int sessionIndex)
    {
      var rows = CsvLogFileSessionReader.ReadSessionRows(CsvFileName, sessionIndex, IsHeaderRow, "Time");
      return ParseRows(rows);
    }

    public static StyleLogSessionData LoadMergedSessions(IEnumerable<int> sessionIndices)
    {
      var data = new StyleLogSessionData();
      if (sessionIndices == null)
        return data;
      foreach (int ix in sessionIndices)
      {
        var part = LoadSessionData(ix);
        data.StyleEntries.AddRange(part.StyleEntries);
        data.Activations.AddRange(part.Activations);
      }
      return data;
    }

    internal static bool IsHeaderRow(string line)
    {
      line = line ?? string.Empty;
      return line.IndexOf("ActivationDetails", StringComparison.Ordinal) >= 0
             && line.IndexOf("Time", StringComparison.Ordinal) >= 0
             && line.IndexOf("Pulse", StringComparison.Ordinal) >= 0
             && line.IndexOf(';') >= 0;
    }

    private static StyleLogSessionData ParseRows(List<Dictionary<string, string>> rows)
    {
      var data = new StyleLogSessionData();
      foreach (var row in rows)
      {
        if (!CsvLogFileSessionReader.TryParseTimestamp(row["Time"], out DateTime ts))
          continue;
        int pulse = CsvLogFileSessionReader.ParseInt(row.TryGetValue("Pulse", out string p) ? p : "0");
        string stage = row.TryGetValue("Stage", out string st) ? st : "";
        if (string.Equals(stage, "Final", StringComparison.OrdinalIgnoreCase))
        {
          data.StyleEntries.Add(new StyleLogEntry
          {
            Timestamp = ts,
            Pulse = pulse,
            Stage = stage,
            StyleId = CsvLogFileSessionReader.ParseInt(row.TryGetValue("StyleId", out string sid) ? sid : "0"),
            StyleName = row.TryGetValue("StyleName", out string sn) ? sn : ""
          });
          continue;
        }
        if (string.Equals(stage, "ParameterActivation", StringComparison.OrdinalIgnoreCase))
        {
          data.Activations.Add(new StyleParameterActivationEntry
          {
            Timestamp = ts,
            Pulse = pulse,
            Stage = stage,
            ParameterId = CsvLogFileSessionReader.ParseInt(row.TryGetValue("ParameterId", out string pid) ? pid : "0"),
            ParameterName = row.TryGetValue("ParameterName", out string pn) ? pn : "",
            ZoneId = CsvLogFileSessionReader.ParseInt(row.TryGetValue("ZoneId", out string zid) ? zid : "0"),
            ZoneDescription = row.TryGetValue("ZoneDescription", out string zd) ? zd : "",
            StyleId = CsvLogFileSessionReader.ParseInt(row.TryGetValue("StyleId", out string sid) ? sid : "0"),
            StyleName = row.TryGetValue("StyleName", out string sn) ? sn : ""
          });
        }
      }
      return data;
    }

    public sealed class StyleLogSessionData
    {
      public List<StyleLogEntry> StyleEntries { get; } = new List<StyleLogEntry>();
      public List<StyleParameterActivationEntry> Activations { get; } = new List<StyleParameterActivationEntry>();
    }
  }
}
