using AIStudio.Common;
using AIStudio.Dialogs;
using AIStudio.ViewModels;
using ISIDA.Psychic.Automatism;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static AIStudio.ViewModels.AutomatizmsViewModel;

namespace AIStudio.Pages.Automatizm
{
  public partial class AutomatizmsView : UserControl
  {
    private readonly AutomatizmChainsSystem _automatizmChainsSystem;
    private AutomatizmsViewModel.AutomatizmDisplayItem _contextMenuAutomatizm;

    public AutomatizmsView()
    {
      InitializeComponent();

      // Инициализация системы цепочек автоматизмов
      _automatizmChainsSystem = AutomatizmChainsSystem.Instance;
    }

    private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Delete)
      {
        if (!IsFormDeletion)
        {
          e.Handled = true;
          return;
        }

        var grid = (DataGrid)sender;

        if (grid.IsEditing())
          return;

        if (grid.SelectedItems.Count > 0 && DataContext is AutomatizmsViewModel viewModel)
        {
          var automatizms = grid.SelectedItems
              .Cast<object>()
              .Where(item => item is AutomatizmsViewModel.AutomatizmDisplayItem)
              .Cast<AutomatizmsViewModel.AutomatizmDisplayItem>()
              .ToList();

          var result = MessageBox.Show(
              $"Вы действительно хотите удалить {automatizms.Count} автоматизмов?",
              "Подтверждение удаления",
              MessageBoxButton.YesNo,
              MessageBoxImage.Question);

          if (result == MessageBoxResult.Yes)
          {
            foreach (var automatizm in automatizms)
            {
              viewModel.RemoveSelectedAutomatizm(automatizm);
            }
          }

          e.Handled = true;
        }
      }
    }

    private bool IsFormDeletion
    {
      get
      {
        if (DataContext is AutomatizmsViewModel viewModel && !viewModel.IsDeletionEnabled)
        {
          MessageBox.Show(
              viewModel.PulseWarningMessage,
              "Удаление недоступно",
              MessageBoxButton.OK,
              MessageBoxImage.Warning);
          return false;
        }
        return true;
      }
    }

    #region Контекстное меню для цепочек автоматизмов

    private void ChainCell_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (sender is DataGridCell cell && cell.DataContext is AutomatizmsViewModel.AutomatizmDisplayItem automatizm)
      {
        _contextMenuAutomatizm = automatizm;
      }
    }

    private void AddAutomatizmChainMenuItem_Click(object sender, RoutedEventArgs e)
    {
      if (!IsFormEnabled)
        return;

      var menuItem = sender as MenuItem;
      var contextMenu = menuItem?.Parent as ContextMenu;
      var cell = contextMenu?.PlacementTarget as DataGridCell;

      AutomatizmsViewModel.AutomatizmDisplayItem automatizm = null;

      if (cell != null && cell.DataContext is AutomatizmsViewModel.AutomatizmDisplayItem cellAutomatizm)
        automatizm = cellAutomatizm;
      else if (_contextMenuAutomatizm != null)
        automatizm = _contextMenuAutomatizm;

      if (automatizm != null)
        OpenNewAutomatizmChainEditor(automatizm);
    }

    private void AttachAutomatizmChainMenuItem_Click(object sender, RoutedEventArgs e)
    {
      if (!IsFormEnabled)
        return;

      var menuItem = sender as MenuItem;
      var contextMenu = menuItem?.Parent as ContextMenu;
      var cell = contextMenu?.PlacementTarget as DataGridCell;

      AutomatizmsViewModel.AutomatizmDisplayItem automatizm = null;

      if (cell != null && cell.DataContext is AutomatizmsViewModel.AutomatizmDisplayItem cellAutomatizm)
        automatizm = cellAutomatizm;
      else if (_contextMenuAutomatizm != null)
        automatizm = _contextMenuAutomatizm;

      if (automatizm != null)
        OpenAutomatizmChainBindingDialog(automatizm);
    }

    private void EditAutomatizmChainMenuItem_Click(object sender, RoutedEventArgs e)
    {
      if (AutomatizmsGrid.SelectedItem is AutomatizmDisplayItem selectedAutomatizm)
      {
        try
        {
          var viewModel = DataContext as AutomatizmsViewModel;
          var dialog = new AutomatizmChainEditorDialog(
              selectedAutomatizm.BranchID,
              selectedAutomatizm.ChainID,
              AutomatizmChainsSystem.Instance,
              ActionsImagesSystem.Instance,
              viewModel);

          if (dialog.ShowDialog() == true)
          {
            selectedAutomatizm.ChainID = dialog.ChainId;

            AutomatizmsGrid.CommitEdit(DataGridEditingUnit.Row, true);
            AutomatizmsGrid.Items.Refresh();

            viewModel?.UpdateChainBindingForAutomatizm(selectedAutomatizm);
          }
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Ошибка открытия редактора цепочек: {ex.Message}",
              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }
    }

    private void DetachAutomatizmChainMenuItem_Click(object sender, RoutedEventArgs e)
    {
      if (!IsFormEnabled)
        return;

      var menuItem = sender as MenuItem;
      var contextMenu = menuItem?.Parent as ContextMenu;
      var cell = contextMenu?.PlacementTarget as DataGridCell;

      AutomatizmsViewModel.AutomatizmDisplayItem automatizm = null;

      if (cell != null && cell.DataContext is AutomatizmsViewModel.AutomatizmDisplayItem cellAutomatizm)
        automatizm = cellAutomatizm;
      else if (_contextMenuAutomatizm != null)
        automatizm = _contextMenuAutomatizm;

      if (automatizm != null)
        DetachChainFromAutomatizm(automatizm);
    }

    #endregion

    #region Вспомогательные методы для работы с цепочками автоматизмов

    private void OpenNewAutomatizmChainEditor(AutomatizmsViewModel.AutomatizmDisplayItem automatizm)
    {
      if (automatizm.BranchID <= 0)
      {
        MessageBox.Show(
            "Для создания цепочки необходимо корректный ID узла дерева.",
            "Узел дерева не найден",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
      }

      if (!AutomatizmChainsSystem.IsInitialized)
      {
        MessageBox.Show("Система цепочек автоматизмов не инициализирована", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      var dialog = new AutomatizmChainEditorDialog(
          automatizm.BranchID,
          0,
          AutomatizmChainsSystem.Instance,
          ActionsImagesSystem.Instance
      )
      {
        Owner = Window.GetWindow(this),
        Title = "Создание новой цепочки автоматизмов"
      };

      if (dialog.ShowDialog() == true && dialog.ChainId > 0)
      {
        automatizm.ChainID = dialog.ChainId;

        if (DataContext is AutomatizmsViewModel viewModel)
          viewModel.UpdateChainBindingForAutomatizm(automatizm);

        AutomatizmsGrid.CommitEdit(DataGridEditingUnit.Row, true);
        AutomatizmsGrid.Items.Refresh();

        MessageBox.Show(
            $"Новая цепочка автоматизмов {dialog.ChainId} создана и привязана к узлу {automatizm.BranchID}",
            "Цепочка создана",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
      }
    }

    private void OpenAutomatizmChainBindingDialog(AutomatizmsViewModel.AutomatizmDisplayItem automatizm)
    {
      if (automatizm.BranchID <= 0)
      {
        MessageBox.Show(
            "Для привязки цепочки необходим корректный ID узла дерева.",
            "Узел дерева не найден",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
      }

      var allChains = _automatizmChainsSystem.GetAllAutomatizmChains();
      if (!allChains.Any())
      {
        var result = MessageBox.Show(
            "Нет существующих цепочек автоматизмов для привязки.\n" +
            "Хотите создать новую цепочку?",
            "Цепочки не найдены",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
          OpenNewAutomatizmChainEditor(automatizm);
        }
        return;
      }

      var bindingDialog = new AutomatizmChainBindingDialog(
          automatizm.BranchID,
          automatizm.ChainID,
          AutomatizmChainsSystem.Instance,
          AutomatizmTreeSystem.Instance)
      {
        Owner = Window.GetWindow(this),
        Title = $"Управление цепочками для узла {automatizm.BranchID}"
      };

      if (bindingDialog.ShowDialog() == true)
      {
        if (bindingDialog.SelectedChainId > 0)
        {
          automatizm.ChainID = bindingDialog.SelectedChainId;

          if (DataContext is AutomatizmsViewModel viewModel)
          {
            viewModel.UpdateChainBindingForAutomatizm(automatizm);
            AutomatizmsGrid.CommitEdit(DataGridEditingUnit.Row, true);
            AutomatizmsGrid.Items.Refresh();
          }
          MessageBox.Show(
              $"Цепочка автоматизмов {bindingDialog.SelectedChainId} успешно привязана к узлу {automatizm.BranchID}",
              "Цепочка привязана",
              MessageBoxButton.OK,
              MessageBoxImage.Information);
        }
      }
      else if (bindingDialog.ChainDeleted)
      {
        if (DataContext is AutomatizmsViewModel viewModel)
        {
          AutomatizmsGrid.CommitEdit(DataGridEditingUnit.Row, true);
          AutomatizmsGrid.Items.Refresh();
        }
      }
    }

    private void OpenChainEditorForAutomatizm(DataGridCell cell = null, AutomatizmsViewModel.AutomatizmDisplayItem automatizm = null)
    {
      if (!IsFormEnabled)
        return;

      if (automatizm == null && cell != null && cell.DataContext is AutomatizmsViewModel.AutomatizmDisplayItem cellAutomatizm)
      {
        automatizm = cellAutomatizm;
      }

      if (automatizm == null)
        return;

      if (automatizm.BranchID <= 0)
      {
        MessageBox.Show(
            "Для работы с цепочками необходим корректный ID узла дерева.",
            "Узел дерева не найден",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
      }

      if (!AutomatizmChainsSystem.IsInitialized)
      {
        MessageBox.Show("Система цепочек автоматизмов не инициализирована", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      if (automatizm.ChainID > 0)
      {
        var dialog = new AutomatizmChainEditorDialog(
            automatizm.BranchID,
            automatizm.ChainID,
            AutomatizmChainsSystem.Instance,
            ActionsImagesSystem.Instance
        );

        if (dialog.ShowDialog() == true)
        {
          automatizm.ChainID = dialog.ChainId;

          if (DataContext is AutomatizmsViewModel viewModel)
            viewModel.UpdateChainBindingForAutomatizm(automatizm);

          AutomatizmsGrid.CommitEdit(DataGridEditingUnit.Row, true);
          AutomatizmsGrid.Items.Refresh();
        }
      }
      else
      {
        ShowAutomatizmChainChoiceDialogSimple(automatizm);
      }
    }

    private void DetachChainFromAutomatizm(AutomatizmsViewModel.AutomatizmDisplayItem automatizm)
    {
      if (automatizm.ChainID <= 0)
      {
        MessageBox.Show(
            "У этого узла дерева нет привязанной цепочки.",
            "Цепочка не привязана",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        return;
      }

      var result = MessageBox.Show(
          $"Вы действительно хотите отвязать цепочку {automatizm.ChainID} от узла {automatizm.BranchID}?",
          "Подтверждение отвязки цепочки",
          MessageBoxButton.YesNo,
          MessageBoxImage.Question);

      if (result == MessageBoxResult.Yes)
      {
        int oldChainId = automatizm.ChainID;
        automatizm.ChainID = 0;

        if (DataContext is AutomatizmsViewModel viewModel)
          viewModel.UpdateChainBindingForAutomatizm(automatizm);

        AutomatizmsGrid.CommitEdit(DataGridEditingUnit.Row, true);
        AutomatizmsGrid.Items.Refresh();

        MessageBox.Show(
            $"Цепочка {oldChainId} успешно отвязана от узла {automatizm.BranchID}",
            "Цепочка отвязана",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
      }
    }

    private void ShowAutomatizmChainChoiceDialogSimple(AutomatizmsViewModel.AutomatizmDisplayItem automatizm)
    {
      var allChains = _automatizmChainsSystem.GetAllAutomatizmChains();
      bool hasChains = allChains.Any();

      if (!hasChains)
      {
        var result = MessageBox.Show(
            "В системе нет ни одной цепочки автоматизмов.\n" +
            "Хотите создать новую цепочку?",
            "Цепочки не найдены",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
          OpenNewAutomatizmChainEditor(automatizm);
        return;
      }

      var firstResult = MessageBox.Show(
          "Хотите создать новую цепочку или выбрать из существующих?",
          "Выбор действия",
          MessageBoxButton.YesNoCancel,
          MessageBoxImage.Question);

      if (firstResult == MessageBoxResult.Yes)
        OpenNewAutomatizmChainEditor(automatizm);
      else if (firstResult == MessageBoxResult.No)
        OpenAutomatizmChainBindingDialog(automatizm);
    }

    #endregion

    private void ChainCell_DoubleClick(object sender, RoutedEventArgs e)
    {
      if (sender is DataGridCell cell)
      {
        if (cell.DataContext is AutomatizmsViewModel.AutomatizmDisplayItem automatizm)
        {
          if (automatizm.ChainID > 0)
            OpenChainEditorForAutomatizm(cell, automatizm);
          else
            ShowAutomatizmChainChoiceDialogSimple(automatizm);
        }
      }
    }

    private bool IsFormEnabled
    {
      get
      {
        if (DataContext is AutomatizmsViewModel viewModel && !viewModel.IsDeletionEnabled)
        {
          MessageBox.Show(
              viewModel.PulseWarningMessage,
              "Управление недоступно",
              MessageBoxButton.OK,
              MessageBoxImage.Warning);
          return false;
        }
        return true;
      }
    }
  }
}