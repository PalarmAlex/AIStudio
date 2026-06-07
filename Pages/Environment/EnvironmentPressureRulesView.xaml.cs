using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AIStudio.Common.SymbiontEnv;
using AIStudio.Dialogs;
using AIStudio.ViewModels.SymbiontEnv;
using ISIDA.Actions;
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

    private void InfluencesCell_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ClickCount != 2 || Vm == null || !Vm.IsEditingEnabled)
        return;
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

    private void AntagonistsCell_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ClickCount != 2 || Vm == null || !Vm.IsEditingEnabled)
        return;
      if (!(sender is FrameworkElement element) || !(element.DataContext is EnvironmentPressureRuleRow row))
        return;
      var availableRules = Vm.Rules
          .Where(r => r.RuleId != row.RuleId)
          .Select(r => new InfluenceActionSystem.GomeostasisInfluenceAction
          {
            Id = r.RuleId,
            Name = r.Name
          });
      var editor = new AntagonistInfluenceEditor(
          $"Antagonists: {row.Name} (RuleId {row.RuleId})",
          availableRules,
          row.Antagonists ?? new List<int>());
      editor.Owner = Window.GetWindow(this);
      if (editor.ShowDialog() == true)
      {
        row.Antagonists = editor.SelectedInfluenceIds.ToList();
        RulesGrid.CommitEdit(DataGridEditingUnit.Row, true);
        RulesGrid.Items.Refresh();
      }
      e.Handled = true;
    }
  }
}
