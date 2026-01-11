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
    private GeneticReflexesSystem.GeneticReflex _contextMenuReflex;

    public GeneticReflexesView()
    {
      InitializeComponent();

      _reflexChainsSystem = ReflexChainsSystem.Instance;

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

        if (!IsFormEnabled)
        {
          e.Handled = true;
          return;
        }

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

    #region Контекстное меню для цепочки

    private void ChainCell_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (sender is DataGridCell cell && cell.DataContext is GeneticReflexesSystem.GeneticReflex reflex)
      {
        _contextMenuReflex = reflex;
      }
    }

    private void AddChainMenuItem_Click(object sender, RoutedEventArgs e)
    {
      if (!IsFormEnabled)
        return;

      var menuItem = sender as MenuItem;
      var contextMenu = menuItem?.Parent as ContextMenu;
      var cell = contextMenu?.PlacementTarget as DataGridCell;

      GeneticReflexesSystem.GeneticReflex reflex = null;

      if (cell != null && cell.DataContext is GeneticReflexesSystem.GeneticReflex cellReflex)
        reflex = cellReflex;
      else if (_contextMenuReflex != null)
        reflex = _contextMenuReflex;

      if (reflex != null)
        OpenNewChainEditor(reflex);
    }

    private void AttachChainMenuItem_Click(object sender, RoutedEventArgs e)
    {
      if (!IsFormEnabled)
        return;

      var menuItem = sender as MenuItem;
      var contextMenu = menuItem?.Parent as ContextMenu;
      var cell = contextMenu?.PlacementTarget as DataGridCell;

      GeneticReflexesSystem.GeneticReflex reflex = null;

      if (cell != null && cell.DataContext is GeneticReflexesSystem.GeneticReflex cellReflex)
        reflex = cellReflex;
      else if (_contextMenuReflex != null)
        reflex = _contextMenuReflex;

      if (reflex != null)
        OpenReflexChainBindingDialog(reflex);
    }

    private void EditChainMenuItem_Click(object sender, RoutedEventArgs e)
    {
      if (!IsFormEnabled)
        return;

      var menuItem = sender as MenuItem;
      var contextMenu = menuItem?.Parent as ContextMenu;
      var cell = contextMenu?.PlacementTarget as DataGridCell;

      if (cell != null && cell.DataContext is GeneticReflexesSystem.GeneticReflex reflex)
        OpenChainEditorForReflex(cell, reflex);
      else if (_contextMenuReflex != null)
        OpenChainEditorForReflex(null, _contextMenuReflex);
    }

    private void DetachChainMenuItem_Click(object sender, RoutedEventArgs e)
    {
      if (!IsFormEnabled)
        return;

      var menuItem = sender as MenuItem;
      var contextMenu = menuItem?.Parent as ContextMenu;
      var cell = contextMenu?.PlacementTarget as DataGridCell;

      GeneticReflexesSystem.GeneticReflex reflex = null;

      if (cell != null && cell.DataContext is GeneticReflexesSystem.GeneticReflex cellReflex)
        reflex = cellReflex;
      else if (_contextMenuReflex != null)
        reflex = _contextMenuReflex;

      if (reflex != null)
        DetachChainFromReflex(reflex);
    }

    #endregion

    #region Вспомогательные методы для работы с цепочками

    private void OpenNewChainEditor(GeneticReflexesSystem.GeneticReflex reflex)
    {
      if (reflex.Id <= 0)
      {
        MessageBox.Show(
            "Для создания цепочки необходимо сначала сохранить рефлекс.\n" +
            "Нажмите кнопку 'Сохранить'.",
            "Рефлекс не сохранен",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
      }

      if (!ReflexChainsSystem.IsInitialized)
      {
        MessageBox.Show("Система цепочек рефлексов не инициализирована", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      var dialog = new ReflexChainEditorDialog(
          reflex.Id,
          reflex.Level1,
          reflex.Level2 ?? new List<int>(),
          reflex.Level3 ?? new List<int>(),
          reflex.AdaptiveActions ?? new List<int>(),
          0,
          ReflexChainsSystem.Instance,
          AdaptiveActionsSystem.Instance
      )
      {
        Owner = Window.GetWindow(this),
        Title = "Создание новой цепочки"
      };

      if (dialog.ShowDialog() == true && dialog.ChainId > 0)
      {
        reflex.ReflexChainID = dialog.ChainId;

        if (DataContext is GeneticReflexesViewModel viewModel)
          viewModel.UpdateChainBindingForReflex(reflex);

        GeneticReflexesGrid.CommitEdit(DataGridEditingUnit.Row, true);
        GeneticReflexesGrid.Items.Refresh();

        MessageBox.Show(
            $"Новая цепочка {dialog.ChainId} создана и привязана к рефлексу {reflex.Id}",
            "Цепочка создана",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
      }
    }

    private void OpenReflexChainBindingDialog(GeneticReflexesSystem.GeneticReflex reflex)
    {
      if (reflex.Id <= 0)
      {
        MessageBox.Show(
            "Для привязки цепочки необходимо сначала сохранить рефлекс.\n" +
            "Нажмите кнопку 'Сохранить'.",
            "Рефлекс не сохранен",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
      }

      var allChains = _reflexChainsSystem.GetAllReflexChains();
      if (!allChains.Any())
      {
        var result = MessageBox.Show(
            "Нет существующих цепочек для привязки.\n" +
            "Хотите создать новую цепочку?",
            "Цепочки не найдены",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
          OpenNewChainEditor(reflex);
        }
        return;
      }

      var bindingDialog = new ReflexChainBindingDialog(
          reflex.Id,
          reflex.ReflexChainID,
          ReflexChainsSystem.Instance,
          GeneticReflexesSystem.Instance)
      {
        Owner = Window.GetWindow(this),
        Title = $"Управление цепочками для рефлекса {reflex.Id}"
      };

      if (bindingDialog.ShowDialog() == true)
      {
        if (bindingDialog.SelectedChainId > 0)
        {
          reflex.ReflexChainID = bindingDialog.SelectedChainId;

          if (DataContext is GeneticReflexesViewModel viewModel)
          {
            viewModel.UpdateChainBindingForReflex(reflex);
            GeneticReflexesGrid.CommitEdit(DataGridEditingUnit.Row, true);
            GeneticReflexesGrid.Items.Refresh();
          }
          MessageBox.Show(
              $"Цепочка {bindingDialog.SelectedChainId} успешно привязана к рефлексу {reflex.Id}",
              "Цепочка привязана",
              MessageBoxButton.OK,
              MessageBoxImage.Information);
        }
      }
      else if (bindingDialog.ChainDeleted)
      {
        if (DataContext is GeneticReflexesViewModel viewModel)
        {
          GeneticReflexesGrid.CommitEdit(DataGridEditingUnit.Row, true);
          GeneticReflexesGrid.Items.Refresh();
        }
      }
    }

    private void OpenChainEditorForReflex(DataGridCell cell = null, GeneticReflexesSystem.GeneticReflex reflex = null)
    {
      if (!IsFormEnabled)
        return;

      if (reflex == null && cell != null && cell.DataContext is GeneticReflexesSystem.GeneticReflex cellReflex)
      {
        reflex = cellReflex;
      }

      if (reflex == null)
        return;

      if (reflex.Id <= 0)
      {
        MessageBox.Show(
            "Для работы с цепочками необходимо сначала сохранить рефлекс.\n" +
            "Нажмите кнопку 'Сохранить'.",
            "Рефлекс не сохранен",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
      }

      if (!ReflexChainsSystem.IsInitialized)
      {
        MessageBox.Show("Система цепочек рефлексов не инициализирована", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      if (reflex.ReflexChainID > 0)
      {
        var dialog = new ReflexChainEditorDialog(
            reflex.Id,
            reflex.Level1,
            reflex.Level2 ?? new List<int>(),
            reflex.Level3 ?? new List<int>(),
            reflex.AdaptiveActions ?? new List<int>(),
            reflex.ReflexChainID,
            ReflexChainsSystem.Instance,
            AdaptiveActionsSystem.Instance
        );

        if (dialog.ShowDialog() == true)
        {
          reflex.ReflexChainID = dialog.ChainId;

          if (DataContext is GeneticReflexesViewModel viewModel)
            viewModel.UpdateChainBindingForReflex(reflex);

          GeneticReflexesGrid.CommitEdit(DataGridEditingUnit.Row, true);
          GeneticReflexesGrid.Items.Refresh();
        }
      }
      else
        ShowChainChoiceDialogSimple(reflex);
    }

    private void DetachChainFromReflex(GeneticReflexesSystem.GeneticReflex reflex)
    {
      if (reflex.ReflexChainID <= 0)
      {
        MessageBox.Show(
            "У этого рефлекса нет привязанной цепочки.",
            "Цепочка не привязана",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        return;
      }

      var result = MessageBox.Show(
          $"Вы действительно хотите отвязать цепочку {reflex.ReflexChainID} от рефлекса {reflex.Id}?",
          "Подтверждение отвязки цепочки",
          MessageBoxButton.YesNo,
          MessageBoxImage.Question);

      if (result == MessageBoxResult.Yes)
      {
        int oldChainId = reflex.ReflexChainID;
        reflex.ReflexChainID = 0;

        if (DataContext is GeneticReflexesViewModel viewModel)
          viewModel.UpdateChainBindingForReflex(reflex);

        GeneticReflexesGrid.CommitEdit(DataGridEditingUnit.Row, true);
        GeneticReflexesGrid.Items.Refresh();

        MessageBox.Show(
            $"Цепочка {oldChainId} успешно отвязана от рефлекса {reflex.Id}",
            "Цепочка отвязана",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
      }
    }

    private void ShowChainChoiceDialogSimple(GeneticReflexesSystem.GeneticReflex reflex)
    {
      var allChains = _reflexChainsSystem.GetAllReflexChains();
      bool hasChains = allChains.Any();

      if (!hasChains)
      {
        var result = MessageBox.Show(
            "В системе нет ни одной цепочки рефлексов.\n" +
            "Хотите создать новую цепочку?",
            "Цепочки не найдены",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
          OpenNewChainEditor(reflex);
        return;
      }

      var firstResult = MessageBox.Show(
          "Хотите создать новую цепочку или выбрать из существующих?",
          "Выбор действия",
          MessageBoxButton.YesNoCancel,
          MessageBoxImage.Question);

      if (firstResult == MessageBoxResult.Yes)
        OpenNewChainEditor(reflex);
      else if (firstResult == MessageBoxResult.No)
        OpenReflexChainBindingDialog(reflex);
    }

    #endregion

    private void ChainCell_DoubleClick(object sender, RoutedEventArgs e)
    {
      if (sender is DataGridCell cell)
      {
        if (cell.DataContext is GeneticReflexesSystem.GeneticReflex reflex)
        {
          if (reflex.ReflexChainID > 0)
            OpenChainEditorForReflex(cell, reflex);
          else
            ShowChainChoiceDialogSimple(reflex);
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