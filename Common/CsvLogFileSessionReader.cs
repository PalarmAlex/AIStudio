using ISIDA.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace AIStudio.Common
{
  /// <summary>Чтение сессий из CSV с повторяющимися строками заголовка.</summary>
  public static class CsvLogFileSessionReader
  {
    private static readonly string[] TimeFormats =
    {
      "yyyy-MM-dd HH:mm:ss",
      "dd.MM.yyyy HH:mm:ss"
    };

    public static IReadOnlyList<LogFileSessionInfo> ListSessions(
        string csvFileName,
        Func<string, bool> isHeaderRow,
        string timeColumnName)
    {
      var path = LogFilePaths.ResolveLogFile(csvFileName);
      if (!File.Exists(path))
        return Array.Empty<LogFileSessionInfo>();

      try
      {
        var blocks = ReadBlocks(path, isHeaderRow, timeColumnName);
        var list = new List<LogFileSessionInfo>();
        for (int i = 0; i < blocks.Count; i++)
        {
          if (blocks[i].RowCount == 0)
            continue;
          list.Add(new LogFileSessionInfo
          {
            SessionKey = i.ToString(CultureInfo.InvariantCulture),
            SessionIndex = i,
            StartedLocal = blocks[i].StartedLocal,
            EndedLocal = blocks[i].EndedLocal,
            EntryCount = blocks[i].RowCount
          });
        }

        return list.OrderByDescending(s => s.StartedLocal).ToList();
      }
      catch (Exception ex)
      {
        Logger.Error(csvFileName + " сессии: " + ex.Message);
        return Array.Empty<LogFileSessionInfo>();
      }
    }

    public static List<Dictionary<string, string>> ReadSessionRows(
        string csvFileName,
        int sessionIndex,
        Func<string, bool> isHeaderRow)
    {
      var path = LogFilePaths.ResolveLogFile(csvFileName);
      if (!File.Exists(path))
        return new List<Dictionary<string, string>>();

      var blocks = ReadBlocks(path, isHeaderRow, "Time");
      if (sessionIndex < 0 || sessionIndex >= blocks.Count)
        return new List<Dictionary<string, string>>();

      return blocks[sessionIndex].Rows;
    }

    public static List<Dictionary<string, string>> ReadSessionRows(
        string csvFileName,
        int sessionIndex,
        Func<string, bool> isHeaderRow,
        string timeColumnName)
    {
      var path = LogFilePaths.ResolveLogFile(csvFileName);
      if (!File.Exists(path))
        return new List<Dictionary<string, string>>();

      var blocks = ReadBlocks(path, isHeaderRow, timeColumnName);
      if (sessionIndex < 0 || sessionIndex >= blocks.Count)
        return new List<Dictionary<string, string>>();

      return blocks[sessionIndex].Rows;
    }

    private sealed class Block
    {
      public List<Dictionary<string, string>> Rows { get; } = new List<Dictionary<string, string>>();
      public int RowCount { get; set; }
      public DateTime StartedLocal { get; set; }
      public DateTime EndedLocal { get; set; }
    }

    private static List<Block> ReadBlocks(string path, Func<string, bool> isHeaderRow, string timeColumnName)
    {
      var blocks = new List<Block>();
      Block current = null;
      Dictionary<string, int> columns = null;

      foreach (var line in ReadLinesShared(path))
      {
        if (string.IsNullOrWhiteSpace(line))
          continue;

        if (isHeaderRow(line))
        {
          current = new Block();
          blocks.Add(current);
          columns = ParseHeaderColumns(line);
          continue;
        }

        if (current == null || columns == null)
          continue;

        var row = ParseDataRow(line, columns, timeColumnName);
        if (row == null)
          continue;

        current.Rows.Add(row);
        current.RowCount++;

        if (TryParseTimestamp(row, timeColumnName, out DateTime ts))
        {
          if (current.RowCount == 1)
            current.StartedLocal = ts;
          current.EndedLocal = ts;
        }
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

    private static Dictionary<string, string> ParseDataRow(string line, Dictionary<string, int> columns, string timeColumnName)
    {
      var parts = line.Split(';');
      if (parts.Length < 3)
        return null;

      string Get(string name)
      {
        if (!columns.TryGetValue(name, out int ix) || ix >= parts.Length)
          return string.Empty;
        return parts[ix]?.Trim() ?? string.Empty;
      }

      if (!TryParseTimestamp(Get(timeColumnName), out _))
        return null;

      var row = new Dictionary<string, string>(StringComparer.Ordinal);
      foreach (var kv in columns)
        row[kv.Key] = Get(kv.Key);
      return row;
    }

    private static bool TryParseTimestamp(Dictionary<string, string> row, string timeColumnName, out DateTime timestamp)
    {
      timestamp = default;
      return row != null && row.TryGetValue(timeColumnName, out string raw) && TryParseTimestamp(raw, out timestamp);
    }

    public static bool TryParseTimestamp(string raw, out DateTime timestamp)
    {
      timestamp = default;
      if (string.IsNullOrWhiteSpace(raw))
        return false;

      if (DateTime.TryParseExact(raw, TimeFormats, CultureInfo.InvariantCulture,
              DateTimeStyles.AssumeLocal, out timestamp))
        return true;

      return DateTime.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out timestamp);
    }

    public static int ParseInt(string raw, int defaultValue = 0)
    {
      if (string.IsNullOrWhiteSpace(raw))
        return defaultValue;
      return int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)
          ? v
          : defaultValue;
    }

    public static float ParseFloat(string raw)
    {
      if (string.IsNullOrWhiteSpace(raw))
        return 0f;
      raw = raw.Trim().Replace(',', '.');
      return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : 0f;
    }

    /// <summary>Удаляет блоки сессий по индексам в CSV и при наличии — в одноимённом .jsonl.</summary>
    public static bool TryDeleteSessionsByBlockIndex(
        string csvFileName,
        Func<string, bool> isHeaderRow,
        IEnumerable<int> blockIndicesToDelete,
        out string errorMessage)
    {
      errorMessage = null;
      var toDelete = new HashSet<int>(blockIndicesToDelete ?? Enumerable.Empty<int>());
      if (toDelete.Count == 0)
        return true;

      var path = LogFilePaths.ResolveLogFile(csvFileName);
      if (!File.Exists(path))
      {
        errorMessage = "Файл логов не найден.";
        return false;
      }

      try
      {
        var blocks = ReadRawBlocks(path, isHeaderRow);
        var remaining = blocks.Where((_, i) => !toDelete.Contains(i)).ToList();
        WriteRawBlocks(path, remaining);

        var jsonlPath = Path.ChangeExtension(path, ".jsonl");
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
        return false;
      }
    }

    private sealed class RawBlock
    {
      public string HeaderLine { get; set; }
      public List<string> DataLines { get; } = new List<string>();
    }

    private sealed class RawJsonlBlock
    {
      public List<string> Lines { get; } = new List<string>();
    }

    private static List<RawBlock> ReadRawBlocks(string path, Func<string, bool> isHeaderRow)
    {
      var blocks = new List<RawBlock>();
      RawBlock current = null;

      foreach (var line in ReadLinesShared(path))
      {
        if (string.IsNullOrWhiteSpace(line))
          continue;

        if (isHeaderRow(line))
        {
          current = new RawBlock { HeaderLine = line };
          blocks.Add(current);
          continue;
        }

        current?.DataLines.Add(line);
      }

      return blocks;
    }

    private static void WriteRawBlocks(string path, IReadOnlyList<RawBlock> blocks)
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

        if (!TryParseJsonlPulse(line, out int pulse))
          continue;

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

    private static bool TryParseJsonlPulse(string line, out int pulse)
    {
      pulse = 0;
      try
      {
        var token = JObject.Parse(line)["Pulse"];
        if (token == null)
          return false;

        return int.TryParse(token.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out pulse);
      }
      catch
      {
        return false;
      }
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
