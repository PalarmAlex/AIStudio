using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AIStudio.ViewModels.SymbiontEnv;

namespace AIStudio.Pages.SymbiontEnv
{
  /// <summary>
  /// Таблица триггеров среды.
  /// </summary>
  public partial class EnvironmentTriggersView : UserControl
  {
    /// <summary>
    /// Создаёт представление.
    /// </summary>
    public EnvironmentTriggersView()
    {
      InitializeComponent();
    }

    private EnvironmentTriggersViewModel Vm => DataContext as EnvironmentTriggersViewModel;

    private void TriggersGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
    {
      if (Vm == null)
        return;
      var row = Vm.CreateNewRow();
      e.NewItem = row;
      Vm.RegisterNewRow(row);
    }

    private void TriggersGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key != Key.Delete || Vm == null)
        return;
      var rows = new List<EnvironmentTriggerRow>();
      foreach (object item in TriggersGrid.SelectedItems)
      {
        if (item is EnvironmentTriggerRow row)
          rows.Add(row);
      }
      if (rows.Count == 0)
        return;
      if (Vm.TryRemoveRows(rows))
        e.Handled = true;
    }

    private void IdCell_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ClickCount != 2 || Vm == null)
        return;
      if (!TryEnsureEditingEnabled())
      {
        e.Handled = true;
        return;
      }
      if (!(sender is FrameworkElement element) || !(element.DataContext is EnvironmentTriggerRow row))
        return;

      if (Vm.TryPickTriggerId(Window.GetWindow(this), row))
      {
        TriggersGrid.CommitEdit(DataGridEditingUnit.Row, true);
        TriggersGrid.Items.Refresh();
      }
      e.Handled = true;
    }

    private void InfluenceActionCell_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ClickCount != 2 || Vm == null)
        return;
      if (!TryEnsureEditingEnabled())
      {
        e.Handled = true;
        return;
      }
      if (!(sender is FrameworkElement element) || !(element.DataContext is EnvironmentTriggerRow row))
        return;

      if (Vm.TryPickInfluenceAction(Window.GetWindow(this), row))
      {
        TriggersGrid.CommitEdit(DataGridEditingUnit.Row, true);
        TriggersGrid.Items.Refresh();
      }
      e.Handled = true;
    }

    private void FilterCell_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ClickCount != 2 || Vm == null)
        return;
      if (!TryEnsureEditingEnabled())
      {
        e.Handled = true;
        return;
      }
      if (!(sender is FrameworkElement element) || !(element.DataContext is EnvironmentTriggerRow row))
        return;

      if (Vm.TryEditFilterFields(Window.GetWindow(this), row))
      {
        TriggersGrid.CommitEdit(DataGridEditingUnit.Row, true);
        TriggersGrid.Items.Refresh();
      }
      e.Handled = true;
    }

    private void DetectCell_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ClickCount != 2 || Vm == null)
        return;
      if (!TryEnsureEditingEnabled())
      {
        e.Handled = true;
        return;
      }
      if (!(sender is FrameworkElement element) || !(element.DataContext is EnvironmentTriggerRow row))
        return;

      if (Vm.TryEditDetectRules(Window.GetWindow(this), row))
      {
        TriggersGrid.CommitEdit(DataGridEditingUnit.Row, true);
        TriggersGrid.Items.Refresh();
      }
      e.Handled = true;
    }

    private bool TryEnsureEditingEnabled()
    {
      if (Vm == null || Vm.IsEditingEnabled)
        return true;

      MessageBox.Show(
          Vm.PulseWarningMessage,
          "Редактирование недоступно",
          MessageBoxButton.OK,
          MessageBoxImage.Warning);
      return false;
    }
  }
}
