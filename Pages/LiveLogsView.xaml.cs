using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AIStudio.ViewModels;

namespace AIStudio.Pages
{
  /// <summary>
  /// Логика взаимодействия для LiveLogsView.xaml
  /// </summary>
  public partial class LiveLogsView : UserControl
  {
    public LiveLogsView()
    {
      InitializeComponent();
      this.Loaded += LiveLogsView_Loaded;
    }

    private void LiveLogsView_Loaded(object sender, RoutedEventArgs e)
    {
      // Находим DataGrid с логами
      var dataGrid = this.FindName("LogDataGrid") as DataGrid;
      if (dataGrid != null)
      {
        dataGrid.PreviewMouseMove += DataGrid_PreviewMouseMove;
      }
    }

    private void DataGrid_PreviewMouseMove(object sender, MouseEventArgs e)
    {
      var dataGrid = sender as DataGrid;
      if (dataGrid == null) return;

      var hitTestResult = VisualTreeHelper.HitTest(dataGrid, e.GetPosition(dataGrid));
      if (hitTestResult?.VisualHit == null) return;

      // Найти DataGridCell
      var cell = FindParent<DataGridCell>(hitTestResult.VisualHit);
      if (cell == null) return;

      // Получить DataGridColumn
      var column = cell.Column as DataGridTextColumn;
      if (column == null) return;

      var binding = column.Binding as Binding;
      if (binding == null) return;

      // Проверить, это ли колонка с цепочкой рефлекса или автоматизма
      if (binding.Path.Path == "DisplayReflexChainInfo" || binding.Path.Path == "DisplayAutomatizmChainInfo")
      {
        var dataContext = cell.DataContext as AIStudio.Common.MemoryLogManager.LogEntry;
        if (dataContext == null) return;

        var viewModel = this.DataContext as LiveLogsViewModel;
        if (viewModel == null) return;

        string tooltip = "";
        if (binding.Path.Path == "DisplayReflexChainInfo")
        {
          tooltip = viewModel.GetReflexChainTooltip(dataContext.DisplayReflexChainInfo);
        }
        else if (binding.Path.Path == "DisplayAutomatizmChainInfo")
        {
          tooltip = viewModel.GetAutomatizmChainTooltip(dataContext.DisplayAutomatizmChainInfo);
        }

        if (!string.IsNullOrEmpty(tooltip))
        {
          cell.ToolTip = tooltip;
        }
      }
    }

    private static T FindParent<T>(DependencyObject obj) where T : DependencyObject
    {
      while (obj != null)
      {
        if (obj is T parent)
          return parent;

        obj = VisualTreeHelper.GetParent(obj);
      }
      return null;
    }
  }
}
