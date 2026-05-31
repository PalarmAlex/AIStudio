using AIStudio.ViewModels;
using ISIDA.Common;
using System;
using System.Collections.Generic;

namespace AIStudio.Common
{
  /// <summary>Удаление сохранённых сессий логов из файлов на диске.</summary>
  public static class LogFileSessionDeletion
  {
    public static bool TryDeleteSessions(
        ResearchLogger researchLogger,
        LogSessionPickerKind kind,
        IEnumerable<int> blockIndices,
        out string errorMessage)
    {
      errorMessage = null;
      if (researchLogger == null)
      {
        errorMessage = "Логгер недоступен.";
        return false;
      }

      if (researchLogger.IsDisposed)
      {
        errorMessage = "Логгер уже освобождён.";
        return false;
      }

      string innerError = null;
      try
      {
        researchLogger.RunLogFileMaintenance(MapScope(kind), () =>
        {
          if (!TryDeleteSessionsWithoutMaintenance(kind, blockIndices, out innerError))
            throw new InvalidOperationException(innerError ?? "Не удалось удалить сессии.");
        });
        return true;
      }
      catch (Exception ex)
      {
        errorMessage = ex.Message;
        return false;
      }
    }

    private static ResearchLogger.LogFileMaintenanceScope MapScope(LogSessionPickerKind kind)
    {
      switch (kind)
      {
        case LogSessionPickerKind.Agent:
          return ResearchLogger.LogFileMaintenanceScope.Agent;
        case LogSessionPickerKind.Style:
          return ResearchLogger.LogFileMaintenanceScope.Styles;
        case LogSessionPickerKind.Parameter:
          return ResearchLogger.LogFileMaintenanceScope.Parameters;
        default:
          throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
      }
    }

    private static bool TryDeleteSessionsWithoutMaintenance(
        LogSessionPickerKind kind,
        IEnumerable<int> blockIndices,
        out string errorMessage)
    {
      switch (kind)
      {
        case LogSessionPickerKind.Agent:
          return AgentLogFileSessions.TryDeleteFileSessions(blockIndices, out errorMessage);
        case LogSessionPickerKind.Style:
          return CsvLogFileSessionReader.TryDeleteSessionsByBlockIndex(
              "AgentLogs_Styles.csv",
              StyleLogFileSessions.IsHeaderRow,
              blockIndices,
              out errorMessage);
        case LogSessionPickerKind.Parameter:
          return CsvLogFileSessionReader.TryDeleteSessionsByBlockIndex(
              "AgentLogs_Parameters.csv",
              ParameterLogFileSessions.IsHeaderRow,
              blockIndices,
              out errorMessage);
        default:
          errorMessage = "Неизвестный тип лога.";
          return false;
      }
    }
  }
}
