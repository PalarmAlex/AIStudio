using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AIStudio.Common.SymbiontEnv;
using AIStudio.Dialogs;
using AIStudio.ViewModels.SymbiontEnv;
using ISIDA.Gomeostas;

namespace AIStudio.Pages.SymbiontEnv
{
  /// <summary>
  /// Таблица правил давления среды.
  /// </summary>
  public partial class EnvironmentPressureRulesView : UserControl
  {
    public EnvironmentPressureRulesView()
    {
      InitializeComponent();
    }

    private EnvironmentPressureRulesViewModel Vm => DataContext as EnvironmentPressureRulesViewModel;

    private void RulesGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
    {
      if (Vm == null)
        return;
      var row = Vm.CreateNewRow();
      e.NewItem = row;
      Vm.RegisterNewRow(row);
    }

    private void RulesGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key != Key.Delete || Vm == null)
        return;
      var rows = new List<EnvironmentPressureRuleRow>();
      foreach (object item in RulesGrid.SelectedItems)
      {
        if (item is EnvironmentPressureRuleRow row)
          rows.Add(row);
      }
      if (rows.Count == 0)
        return;
      if (Vm.TryRemoveRows(rows))
        e.Handled = true;
    }

    private void ProbeKeyCell_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ClickCount != 2 || Vm == null)
        return;
      if (!Vm.IsEditingEnabled)
      {
        MessageBox.Show(
            Vm.PulseWarningMessage,
            "Редактирование недоступно",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        e.Handled = true;
        return;
      }
      if (!(sender is FrameworkElement element) || !(element.DataContext is EnvironmentPressureRuleRow row))
        return;

      var editor = new MetricProbeKeySelectionDialog(row.ProbeKey, Vm.ProbeKeyOptions)
      {
        Owner = Window.GetWindow(this),
        Title = $"ProbeKey: {row.Name} (RuleId {row.RuleId})"
      };
      if (editor.ShowDialog() == true)
      {
        row.ProbeKey = editor.SelectedProbeKey ?? string.Empty;
        RulesGrid.CommitEdit(DataGridEditingUnit.Row, true);
        RulesGrid.Items.Refresh();
      }
      e.Handled = true;
    }

    private void InfluencesCell_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ClickCount != 2 || Vm == null)
        return;
      if (!Vm.IsEditingEnabled)
      {
        MessageBox.Show(
            Vm.PulseWarningMessage,
            "Редактирование недоступно",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        e.Handled = true;
        return;
      }
      if (!(sender is FrameworkElement element) || !(element.DataContext is EnvironmentPressureRuleRow row))
        return;
      var editor = new ActionInfluencesEditor(
          $"Influences: {row.Name} (RuleId {row.RuleId})",
          Vm.GetAllParameters(),
          row.Influences);
      editor.Owner = Window.GetWindow(this);
      if (editor.ShowDialog() == true)
      {
        row.Influences = editor.SelectedInfluences.ToDictionary(
            kvp => kvp.Key,
            kvp => GomeostasSystem.ClampInt(kvp.Value, -10, 10));
        RulesGrid.CommitEdit(DataGridEditingUnit.Row, true);
        RulesGrid.Items.Refresh();
      }
      e.Handled = true;
    }
  }
}
