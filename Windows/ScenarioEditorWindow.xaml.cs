using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AIStudio.Common;
using AIStudio.Dialogs;
using AIStudio.ViewModels.Research;
using ISIDA.Scenarios;

namespace AIStudio.Windows
{
  public partial class ScenarioEditorWindow : Window
  {
    public ScenarioEditorWindow()
    {
      InitializeComponent();
      Closing += OnClosingWindow;
    }

    private void OnClosingWindow(object sender, CancelEventArgs e)
    {
      if (DataContext is ScenarioEditorViewModel vm && vm.HasUnsavedChanges)
      {
        if (!vm.TryCancelWithPrompt())
        {
          e.Cancel = true;
          return;
        }
      }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
      Close();
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

      var dlg = new ScenarioInfluenceActionsEditor(
          "Воздействия по шагу",
          vm.InfluenceActions.GetAllInfluenceActions(),
          row.ActionIds)
      { Owner = this };

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
        vm.RecomputePulseSchedule();
    }

    private void HomeostasisSlider_OnCommitted(object sender, RoutedEventArgs e)
    {
      ScheduleHomeostasisPreviewRefresh();
    }

    private void HomeostasisSlider_OnPreviewKeyUp(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.PageUp || e.Key == Key.PageDown || e.Key == Key.Home || e.Key == Key.End)
        ScheduleHomeostasisPreviewRefresh();
    }

    /// <summary>Окно держит ViewModel; пересчёт переносим на Input после привязки значения ползунка.</summary>
    private void ScheduleHomeostasisPreviewRefresh()
    {
      if (!(DataContext is ScenarioEditorViewModel))
        return;
      Dispatcher.BeginInvoke(new Action(() =>
      {
        if (DataContext is ScenarioEditorViewModel vmRun)
          vmRun.OnHomeostasisSliderCommitted();
      }), DispatcherPriority.Input);
    }
  }
}
