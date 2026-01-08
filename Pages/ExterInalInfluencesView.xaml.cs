using AIStudio.Common;
using AIStudio.Dialogs;
using AIStudio.ViewModels;
using ISIDA.Actions;
using ISIDA.Gomeostas;
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
using System.Windows.Threading;

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
      Loaded += OnLoaded;
      Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
      if (DataContext is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
      if (DataContext is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }

    private void DataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
    {
      int nextId = GetNextId();

      e.NewItem = new InfluenceActionSystem.GomeostasisInfluenceAction
      {
        Id = nextId,
        Name = $"Новое действие {nextId}",
        Description = string.Empty,
        Influences = new Dictionary<int, int>()
      };
    }

    private int GetNextId()
    {
      var viewModel = DataContext as ExterInalInfluencesViewModel;
      if (viewModel == null) return 1;

      int maxId = 0;
      if (viewModel.InfluenceActions != null && viewModel.InfluenceActions.Any())
      {
        maxId = viewModel.InfluenceActions.Max(a => a.Id);
      }

      var grid = ExternInfluencesGrid;
      if (grid?.ItemsSource != null)
      {
        var items = grid.ItemsSource.Cast<InfluenceActionSystem.GomeostasisInfluenceAction>();
        if (items.Any())
        {
          int gridMaxId = items.Max(a => a.Id);
          maxId = Math.Max(maxId, gridMaxId);
        }
      }

      return maxId + 1;
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

        if (grid.SelectedItems.Count > 0 && DataContext is ExterInalInfluencesViewModel viewModel)
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
            {
              viewModel.RemoveSelectedInfluence(action);
            }
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

    private void InfluencesCell_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ClickCount == 2 && DataContext is ExterInalInfluencesViewModel vm)
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
              vm.GetAllParameters(),
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
      if (e.ClickCount == 2 && DataContext is ExterInalInfluencesViewModel vm)
      {
        if (!IsFormEnabled)
        {
          e.Handled = true;
          return;
        }

        if (sender is FrameworkElement element &&
            element.DataContext is InfluenceActionSystem.GomeostasisInfluenceAction action)
        {
          var availableActions = vm.InfluenceActions
            .Where(a => a.Id != action.Id)
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
        if (DataContext is ExterInalInfluencesViewModel viewModel && !viewModel.IsEditingEnabled)
        {
          MessageBox.Show(
              viewModel.PulseWarningMessage,
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
