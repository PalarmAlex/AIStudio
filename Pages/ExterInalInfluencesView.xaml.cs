using AIStudio.Common;
using AIStudio.Dialogs;
using AIStudio.ViewModels;
using ISIDA.Actions;
using ISIDA.Gomeostas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AIStudio.Pages
{
  /// <summary>
  /// Логика взаимодействия для ExterInal_Influences.xaml
  /// </summary>
  public partial class ExterInalInfluencesView : UserControl
  {
    public ExterInalInfluencesView()
    {
      InitializeComponent();
      DataContextChanged += OnDataContextChanged;
      Loaded += OnLoaded;
      Unloaded += OnUnloaded;
    }

    private ExterInalInfluencesViewModel Vm => DataContext as ExterInalInfluencesViewModel;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      UpdateColumnVisibility();
    }

    private void UpdateColumnVisibility()
    {
      if (AntagonistColumn == null || ProbeKeyColumn == null)
        return;
      bool showAntagonists = Vm == null || Vm.ShowAntagonistColumn;
      AntagonistColumn.Visibility = showAntagonists ? Visibility.Visible : Visibility.Collapsed;
      bool showProbeKey = Vm != null && Vm.ShowProbeKeyColumn;
      ProbeKeyColumn.Visibility = showProbeKey ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
      UpdateColumnVisibility();
      if (DataContext is IDisposable disposable)
        disposable.Dispose();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
      if (DataContext is IDisposable disposable)
        disposable.Dispose();
    }

    private void DataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
    {
      if (Vm == null)
        return;
      e.NewItem = Vm.CreateNewRow();
    }

    private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Delete)
      {
        if (!IsFormEnabled)
        {
          e.Handled = true;
          return;
        }
        var grid = (DataGrid)sender;
        if (grid.IsEditing())
          return;
        if (grid.SelectedItems.Count > 0 && Vm != null)
        {
          var actions = grid.SelectedItems
            .Cast<object>()
            .Where(item => item is InfluenceActionSystem.GomeostasisInfluenceAction)
            .Cast<InfluenceActionSystem.GomeostasisInfluenceAction>()
            .ToList();
          var result = MessageBox.Show(
              $"Вы действительно хотите удалить {actions.Count} гомеостатических воздействий?",
              "Подтверждение удаления",
              MessageBoxButton.YesNo,
              MessageBoxImage.Question);
          if (result == MessageBoxResult.Yes)
          {
            foreach (var action in actions)
              Vm.RemoveSelectedInfluence(action);
          }
          e.Handled = true;
        }
      }
    }

    private void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
    {
      if (e.EditAction == DataGridEditAction.Commit && !e.Row.IsEditing)
      {
        var grid = (DataGrid)sender;
        grid.CommitEdit(DataGridEditingUnit.Row, true);
      }
    }

    private void ProbeKeyCell_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ClickCount != 2 || Vm == null)
        return;
      if (!IsFormEnabled)
      {
        e.Handled = true;
        return;
      }
      if (!(sender is FrameworkElement element) ||
          !(element.DataContext is InfluenceActionSystem.GomeostasisInfluenceAction action))
        return;

      var editor = new MetricProbeKeySelectionDialog(action.ProbeKey, Vm.ProbeKeyOptions)
      {
        Owner = Window.GetWindow(this),
        Title = $"ProbeKey: {action.Name} (ID {action.Id})"
      };
      if (editor.ShowDialog() == true)
      {
        action.ProbeKey = editor.SelectedProbeKey ?? string.Empty;
        if (Vm.IsEnvironmentAction(action))
          action.AntagonistInfluences = new List<int>();
        ExternInfluencesGrid.CommitEdit(DataGridEditingUnit.Row, true);
        ExternInfluencesGrid.Items.Refresh();
      }
      e.Handled = true;
    }

    private void InfluencesCell_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ClickCount == 2 && Vm != null)
      {
        if (!IsFormEnabled)
        {
          e.Handled = true;
          return;
        }
        if (sender is FrameworkElement element &&
            element.DataContext is InfluenceActionSystem.GomeostasisInfluenceAction action)
        {
          var editor = new ActionInfluencesEditor(
              $"Влияния гомеостатического воздействия: {action.Name} (ID: {action.Id})",
              Vm.GetAllParameters(),
              action.Influences);
          if (editor.ShowDialog() == true)
          {
            action.Influences = editor.SelectedInfluences.ToDictionary(
                kvp => kvp.Key,
                kvp => GomeostasSystem.ClampInt(kvp.Value, -10, 10));
            ExternInfluencesGrid.CommitEdit(DataGridEditingUnit.Row, true);
            ExternInfluencesGrid.Items.Refresh();
          }
        }
        e.Handled = true;
      }
    }

    private void AntagonistCell_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ClickCount == 2 && Vm != null)
      {
        if (!IsFormEnabled)
        {
          e.Handled = true;
          return;
        }
        if (sender is FrameworkElement element &&
            element.DataContext is InfluenceActionSystem.GomeostasisInfluenceAction action)
        {
          if (Vm.IsEnvironmentAction(action))
          {
            MessageBox.Show(
                "Для EA с ProbeKey (метрика среды) антагонисты запрещены.",
                "Редактирование недоступно",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            e.Handled = true;
            return;
          }

          var availableActions = Vm.InfluenceActions
            .Where(a => a.Id != action.Id && !Vm.IsEnvironmentAction(a))
            .Select(a => new InfluenceActionSystem.GomeostasisInfluenceAction
            {
              Id = a.Id,
              Name = a.Name,
            });
          var editor = new AntagonistInfluenceEditor(
              $"Антагонисты действия: {action.Name} (ID: {action.Id})",
              availableActions,
              action.AntagonistInfluences ?? new List<int>());
          if (editor.ShowDialog() == true)
          {
            action.AntagonistInfluences = editor.SelectedInfluenceIds.ToList();
            ExternInfluencesGrid.CommitEdit(DataGridEditingUnit.Row, true);
            ExternInfluencesGrid.Items.Refresh();
          }
        }
        e.Handled = true;
      }
    }

    private void DescriptionCell_DoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (sender is DataGridCell cell && cell.DataContext is InfluenceActionSystem.GomeostasisInfluenceAction action)
      {
        ExternInfluencesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        ExternInfluencesGrid.CommitEdit(DataGridEditingUnit.Row, true);
        var dialog = new TextInputDialog
        {
          Owner = Window.GetWindow(this),
          Title = "Редактирование описания",
          Text = action.Description,
          Multiline = true
        };
        if (dialog.ShowDialog() == true)
        {
          action.Description = dialog.Text;
          ExternInfluencesGrid.CommitEdit(DataGridEditingUnit.Row, true);
          ExternInfluencesGrid.Items.Refresh();
        }
      }
    }

    private bool IsFormEnabled
    {
      get
      {
        if (Vm != null && !Vm.IsEditingEnabled)
        {
          MessageBox.Show(
              Vm.PulseWarningMessage,
              "Редактирование недоступно",
              MessageBoxButton.OK,
              MessageBoxImage.Warning);
          return false;
        }
        return true;
      }
    }
  }
}
