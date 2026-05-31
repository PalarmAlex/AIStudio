using AIStudio.ViewModels;
using ISIDA.Common;
using System;
using System.Collections.Generic;
using System.Windows;

namespace AIStudio.Common
{
  /// <summary>Проверки доступности окна выбора сессий логов.</summary>
  public static class LogSessionPickerGate
  {
    public const string PulsationBlockedMessage =
        "Выбор и удаление сессий логов доступны только при выключенной пульсации.";

    public static bool EnsurePulsationStopped(Window owner)
    {
      if (!GlobalTimer.IsPulsationRunning)
        return true;

      MessageBox.Show(
          owner,
          PulsationBlockedMessage,
          "Сессии логов",
          MessageBoxButton.OK,
          MessageBoxImage.Information);
      return false;
    }
  }
}
