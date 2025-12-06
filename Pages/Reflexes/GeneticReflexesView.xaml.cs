using AIStudio.Common;
using AIStudio.Dialogs;
using AIStudio.ViewModels;
using ISIDA.Actions;
using ISIDA.Gomeostas;
using ISIDA.Reflexes;
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

namespace AIStudio.Pages.Reflexes
{
  public partial class GeneticReflexesView : UserControl
  {
    private readonly ReflexChainsSystem _reflexChainsSystem;
    private readonly AdaptiveActionsSystem _actionsSystem;

    public GeneticReflexesView()
    {
      InitializeComponent();

      _reflexChainsSystem = ReflexChainsSystem.Instance;
      _actionsSystem = AdaptiveActionsSystem.Instance;

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

    private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Delete)
      {
        var grid = (DataGrid)sender;

        if (grid.IsEditing())
          return;

        if (grid.SelectedItems.Count > 0 && DataContext is GeneticReflexesViewModel viewModel)
        {
          var actions = grid.SelectedItems
            .Cast<object>()
            .Where(item => item is GeneticReflexesSystem.GeneticReflex)
            .Cast<GeneticReflexesSystem.GeneticReflex>()
            .ToList();

          var result = MessageBox.Show(
              $"Вы действительно хотите удалить {actions.Count} безусловных рефлексов?",
              "Подтверждение удаления",
              MessageBoxButton.YesNo,
              MessageBoxImage.Question);

          if (result == MessageBoxResult.Yes)
          {
            foreach (var action in actions)
            {
              viewModel.RemoveSelectedReflexes(action);
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

    private void DataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
    {
      e.NewItem = new GeneticReflexesSystem.GeneticReflex
      {
        Level1 = 0,
        Level2 = new List<int>(),
        Level3 = new List<int>(),
        AdaptiveActions = new List<int>()
      };
    }

    private void Level2Cell_DoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (!IsFormEnabled)
      {
        e.Handled = true;
        return;
      }    

      if (sender is DataGridCell cell && cell.DataContext is GeneticReflexesSystem.GeneticReflex reflex)
      {
        if (DataContext is GeneticReflexesViewModel viewModel)
        {
          var dialog = new BehaviorStylesSelectionDialog(reflex.Level2, viewModel.Gomeostas)
          {
            Owner = Window.GetWindow(this)
          };

          if (dialog.ShowDialog() == true)
          {
            reflex.Level2 = dialog.SelectedBehaviorStyles;
            GeneticReflexesGrid.CommitEdit(DataGridEditingUnit.Row, true);
            GeneticReflexesGrid.Items.Refresh();
          }
        }
      }
    }

    private void Level3Cell_DoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (!IsFormEnabled)
      {
        e.Handled = true;
        return;
      }

      if (sender is DataGridCell cell && cell.DataContext is GeneticReflexesSystem.GeneticReflex reflex)
      {
        var dialog = new InfluenceActionsSelectionDialog(reflex.Level3);
        if (dialog.ShowDialog() == true)
        {
          reflex.Level3 = dialog.SelectedInfluenceActions;
          GeneticReflexesGrid.CommitEdit(DataGridEditingUnit.Row, true);
          GeneticReflexesGrid.Items.Refresh();
        }
      }
    }

    private void AdaptiveActionsCell_DoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (!IsFormEnabled)
      {
        e.Handled = true;
        return;
      }

      if (sender is DataGridCell cell && cell.DataContext is GeneticReflexesSystem.GeneticReflex reflex)
      {
        var dialog = new AdaptiveActionsSelectionDialog(reflex.AdaptiveActions);
        if (dialog.ShowDialog() == true)
        {
          reflex.AdaptiveActions = dialog.SelectedAdaptiveActions;
          GeneticReflexesGrid.CommitEdit(DataGridEditingUnit.Row, true);
          GeneticReflexesGrid.Items.Refresh();
        }
      }
    }

    private void ChainCell_DoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (!IsFormEnabled)
      {
        e.Handled = true;
        return;
      }

      if (sender is DataGridCell cell && cell.DataContext is GeneticReflexesSystem.GeneticReflex reflex)
      {
        // Открываем редактор цепочек для этого рефлекса
        var dialog = new ReflexChainEditorDialog(
            reflex.Id,
            reflex.Level1,
            reflex.Level2,
            reflex.Level3, 
            reflex.AdaptiveActions,
            reflex.ReflexChainID,
            _reflexChainsSystem,
            _actionsSystem)
        {
          Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true)
        {
          reflex.ReflexChainID = dialog.ChainId;

          // Обновляем привязку в дереве рефлексов
          if (DataContext is GeneticReflexesViewModel viewModel)
          {
            viewModel.UpdateChainBindingForReflex(reflex);
          }

          GeneticReflexesGrid.CommitEdit(DataGridEditingUnit.Row, true);
          GeneticReflexesGrid.Items.Refresh();
        }
      }
    }

    private bool IsFormEnabled
    {
      get
      {
        if (DataContext is GeneticReflexesViewModel viewModel && !viewModel.IsEditingEnabled)
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
