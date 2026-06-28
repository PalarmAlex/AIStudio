using System.Windows;
using System.Windows.Controls;

namespace AIStudio.Common.SymbiontEnv
{
  /// <summary>Общие обработчики UI выбора +/− для метрик среды.</summary>
  public static class EnvironmentProbeSelectionUi
  {
    public static void ResetRowClick(object sender, RoutedEventArgs e)
    {
      if (!(sender is Button button))
        return;
      if (!(button.DataContext is EnvironmentProbeActionItem item))
        return;
      item.IsPressure = false;
      item.IsRelease = false;
    }
  }
}
