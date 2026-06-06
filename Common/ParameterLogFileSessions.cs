using System;
using System.Collections.Generic;
using System.Linq;
using static AIStudio.Common.MemoryLogManager;

namespace AIStudio.Common
{
  /// <summary>Сессии лога параметров (AgentLogs_Parameters.csv).</summary>
  public static class ParameterLogFileSessions
  {
    private const string CsvFileName = "AgentLogs_Parameters.csv";
    public static IReadOnlyList<LogFileSessionInfo> ListFileSessions() =>
        CsvLogFileSessionReader.ListSessions(CsvFileName, IsHeaderRow, "Time");
    public static List<ParameterLogEntry> LoadSessionEntries(int sessionIndex)
    {
      var rows = CsvLogFileSessionReader.ReadSessionRows(CsvFileName, sessionIndex, IsHeaderRow, "Time");
      return ParseRows(rows);
    }

    public static List<ParameterLogEntry> LoadMergedSessions(IEnumerable<int> sessionIndices)
    {
      var list = new List<ParameterLogEntry>();
      if (sessionIndices == null)
        return list;
      foreach (int ix in sessionIndices)
        list.AddRange(LoadSessionEntries(ix));
      return list;
    }

    internal static bool IsHeaderRow(string line)
    {
      line = line ?? string.Empty;
      return line.IndexOf("ActivationZone", StringComparison.Ordinal) >= 0
             && line.IndexOf("Time", StringComparison.Ordinal) >= 0
             && line.IndexOf("Pulse", StringComparison.Ordinal) >= 0
             && line.IndexOf(';') >= 0;
    }

    private static List<ParameterLogEntry> ParseRows(List<Dictionary<string, string>> rows)
    {
      var list = new List<ParameterLogEntry>();
      foreach (var row in rows)
      {
        if (!CsvLogFileSessionReader.TryParseTimestamp(row["Time"], out DateTime ts))
          continue;
        list.Add(new ParameterLogEntry
        {
          Timestamp = ts,
          Pulse = CsvLogFileSessionReader.ParseInt(row.TryGetValue("Pulse", out string p) ? p : "0"),
          ParamId = CsvLogFileSessionReader.ParseInt(row.TryGetValue("ParamId", out string pid) ? pid : "0"),
          ParamName = row.TryGetValue("ParamName", out string pn) ? pn : "",
          Weight = CsvLogFileSessionReader.ParseInt(row.TryGetValue("Weight", out string w) ? w : "0"),
          NormaWell = CsvLogFileSessionReader.ParseInt(row.TryGetValue("NormaWell", out string nw) ? nw : "0"),
          Speed = CsvLogFileSessionReader.ParseInt(row.TryGetValue("Speed", out string sp) ? sp : "0"),
          Value = CsvLogFileSessionReader.ParseFloat(row.TryGetValue("Value", out string v) ? v : "0"),
          UrgencyFunction = CsvLogFileSessionReader.ParseFloat(row.TryGetValue("UrgencyFunction", out string u) ? u : "0"),
          ParameterState = row.TryGetValue("ParameterState", out string ps) ? ps : "",
          ActivationZone = row.TryGetValue("ActivationZone", out string az) ? az : ""
        });
      }
      return list;
    }
  }
}
