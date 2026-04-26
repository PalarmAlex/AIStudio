using System.Windows;
using System.Windows.Controls;
using AIStudio.ViewModels;

namespace AIStudio.Pages
{
  /// <summary>
  /// Обёртка вокруг таблицы живых логов: к ней якорятся привязки подсказок ячеек.
  /// FindAncestor(DataGrid) и FindAncestor(UserControl) для DataGridCell ненадёжны при виртуализации и разборке дерева.
  /// </summary>
  public sealed class LiveLogsToolTipScope : Border
  {
    public static readonly DependencyProperty LogsViewModelProperty = DependencyProperty.Register(
      nameof(LogsViewModel),
      typeof(LiveLogsViewModel),
      typeof(LiveLogsToolTipScope),
      new PropertyMetadata(null));

    public LiveLogsViewModel LogsViewModel
    {
      get => (LiveLogsViewModel)GetValue(LogsViewModelProperty);
      set => SetValue(LogsViewModelProperty, value);
    }
  }
}
