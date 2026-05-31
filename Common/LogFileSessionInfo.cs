using System;
using System.Globalization;

namespace AIStudio.Common
{
  /// <summary>Описание сессии в CSV-логе (блок между строками заголовка).</summary>
  public sealed class LogFileSessionInfo
  {
    public const string CurrentSessionKey = "__current__";

    public string SessionKey { get; set; }
    public int SessionIndex { get; set; }
    public DateTime StartedLocal { get; set; }
    public DateTime EndedLocal { get; set; }
    public int EntryCount { get; set; }

    public string BuildDisplayLabel()
    {
      if (StartedLocal.Date == EndedLocal.Date)
      {
        return StartedLocal.ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture)
               + "–" + EndedLocal.ToString("HH:mm", CultureInfo.CurrentCulture)
               + " (" + EntryCount + ")";
      }

      return StartedLocal.ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture)
             + " – " + EndedLocal.ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture)
             + " (" + EntryCount + ")";
    }
  }
}
