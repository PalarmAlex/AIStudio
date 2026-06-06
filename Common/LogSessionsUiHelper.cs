using System;
using System.Collections.Generic;
using System.Linq;

namespace AIStudio.Common
{
  /// <summary>Подписи кнопки выбора сессий и диалог.</summary>
  public static class LogSessionsUiHelper
  {
    public static string BuildButtonLabel(HashSet<string> selectedSessionKeys)
    {
      bool hasCurrent = selectedSessionKeys.Contains(LogFileSessionInfo.CurrentSessionKey);
      int fileCount = selectedSessionKeys.Count(k => k != LogFileSessionInfo.CurrentSessionKey);
      if (hasCurrent && fileCount == 0)
        return "СЕССИИ: текущая";
      if (!hasCurrent && fileCount == 1)
        return "СЕССИИ: 1 из файла";
      if (!hasCurrent && fileCount > 1)
        return "СЕССИИ: " + fileCount + " из файла";
      if (hasCurrent && fileCount > 0)
        return "СЕССИИ: текущая + " + fileCount;
      return "СЕССИИ: не выбрано";
    }

    public static bool UsesOnlyCurrentSession(HashSet<string> selectedSessionKeys) =>
        selectedSessionKeys.Count == 1
        && selectedSessionKeys.Contains(LogFileSessionInfo.CurrentSessionKey);
  }
}
