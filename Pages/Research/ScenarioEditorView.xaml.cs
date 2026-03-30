using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AIStudio.Dialogs;
using AIStudio.ViewModels.Research;
using ISIDA.Scenarios;

namespace AIStudio.Pages.Research
{
  public partial class ScenarioEditorView : UserControl
  {
    public ScenarioEditorView()
    {
      InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
      if (!(DataContext is ScenarioEditorViewModel vm))
        return;
      if (!vm.TryCancelWithPrompt())
        return;
      if (vm.CloseAction != null)
        vm.CloseAction();
      else
        Window.GetWindow(this)?.Close();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) => CloseButton_Click(sender, e);

    private void LinesGrid_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key != Key.Delete)
        return;
      if (!(sender is DataGrid grid) || !(DataContext is ScenarioEditorViewModel vm))
        return;
      if (Keyboard.FocusedElement is TextBox)
        return;
      if (grid.SelectedItems.Count == 0)
        return;
      var rows = grid.SelectedItems.Cast<ScenarioLineRow>().ToList();
      vm.DeleteSelectedLines(rows);
      e.Handled = true;
    }

    private void LinesGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (!(sender is DataGrid) || !(DataContext is ScenarioEditorViewModel vm))
        return;

      var dep = e.OriginalSource as DependencyObject;
      while (dep != null && !(dep is DataGridCell))
        dep = VisualTreeHelper.GetParent(dep);
      if (!(dep is DataGridCell cell))
        return;
      if (!(cell.Column is DataGridTextColumn col))
        return;
      if (col.Header?.ToString() != "Воздействия")
        return;
      if (!(cell.DataContext is ScenarioLineRow row))
        return;

      var owner = Window.GetWindow(this);
      var dlg = new ScenarioInfluenceActionsEditor(
          "Воздействия по шагу",
          vm.InfluenceActions.GetAllInfluenceActions(),
          row.ActionIds)
      { Owner = owner };

      if (dlg.ShowDialog() != true)
        return;

      var newIds = dlg.SelectedActionIds != null
          ? new List<int>(dlg.SelectedActionIds)
          : new List<int>();
      bool same = row.ActionIds != null && row.ActionIds.Count == newIds.Count
          && !row.ActionIds.Except(newIds).Any();
      if (same)
        return;
      row.ActionIds = newIds;
      row.RefreshActionNames(vm.InfluenceActions);
      vm.MarkDirty();
    }

    private void LinesGrid_OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
      if (e.EditAction != DataGridEditAction.Commit)
        return;
      if (DataContext is ScenarioEditorViewModel vm)
      {
        vm.MarkDirty();
        vm.SyncExpectationRowsWithLinesFromEditor();
      }
    }

    private void ExpectGrid_OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
      if (e.EditAction != DataGridEditAction.Commit)
        return;
      if (DataContext is ScenarioEditorViewModel vm)
        vm.MarkDirty();
    }

    private void EditDescriptionButton_Click(object sender, RoutedEventArgs e)
    {
      if (!(DataContext is ScenarioEditorViewModel vm))
        return;

      var dialog = new TextInputDialog
      {
        Owner = Window.GetWindow(this),
        Title = "Редактирование описания сценария",
        Text = vm.Description ?? "",
        Multiline = true,
        Width = 700,
        Height = 500
      };

      if (dialog.ShowDialog() == true)
      {
        var text = dialog.Text ?? "";
        vm.Description = text.Replace("\\n", "\n");
      }
    }
  }
}
