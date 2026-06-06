using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AIStudio.Dialogs;
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

    private void InfluenceActionCell_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ClickCount != 2 || Vm == null || !Vm.IsEditingEnabled)
        return;
      if (!(sender is FrameworkElement element) || !(element.DataContext is EnvironmentTriggerRow row))
        return;
      var dialog = new InfluenceActionsSelectionDialog(
          row.InfluenceActionId > 0 ? new List<int> { row.InfluenceActionId } : new List<int>(),
          maxSelectionCount: 1);
      dialog.Owner = Window.GetWindow(this);
      if (dialog.ShowDialog() == true && dialog.SelectedInfluenceActions != null &&
          dialog.SelectedInfluenceActions.Count > 0)
      {
        row.InfluenceActionId = dialog.SelectedInfluenceActions[0];
        TriggersGrid.CommitEdit(DataGridEditingUnit.Row, true);
        TriggersGrid.Items.Refresh();
      }
      e.Handled = true;
    }

    private void DetectCell_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ClickCount != 2 || Vm == null || !Vm.IsEditingEnabled)
        return;
      if (!(sender is FrameworkElement element) || !(element.DataContext is EnvironmentTriggerRow row))
        return;
      var editor = new EnvironmentTriggerDetectEditorDialog(row.DetectRules);
      editor.Owner = Window.GetWindow(this);
      if (editor.ShowDialog() == true)
      {
        row.DetectRules = editor.ResultRules;
        Vm.RefreshDetectSummary(row);
        TriggersGrid.CommitEdit(DataGridEditingUnit.Row, true);
        TriggersGrid.Items.Refresh();
      }
      e.Handled = true;
    }
  }
}
