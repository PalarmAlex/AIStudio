using AIStudio.Dialogs;
using ISIDA.Reflexes;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AIStudio.Pages.Reflexes
{
  public partial class ConditionedReflexesView : UserControl
  {
    public ConditionedReflexesView()
    {
      InitializeComponent();
    }

    private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Delete)
      {
        var dataGrid = sender as DataGrid;
        if (dataGrid != null && dataGrid.SelectedItems.Count > 0)
        {
          var result = MessageBox.Show(
              $"Удалить выбранные условные рефлексы?",
              "Подтверждение удаления",
              MessageBoxButton.YesNo,
              MessageBoxImage.Question);

          if (result == MessageBoxResult.No)
          {
            e.Handled = true;
          }
        }
      }
    }

    private void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
    {
      // Можно добавить логику валидации при завершении редактирования строки
      if (e.EditAction == DataGridEditAction.Commit)
      {
        // Автоматическое сохранение или валидация
      }
    }

    private void DataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
    {
      // Инициализация нового условного рефлекса значениями по умолчанию
      var newReflex = new ConditionedReflexesSystem.ConditionedReflex
      {
        Level1 = 0, // Норма по умолчанию
        Rank = 0,   // Базовый ранг
        AssociationStrength = 0.3f, // Начальная крепость
        LastActivation = 0,
        BirthTime = 0,
        SourceGeneticReflexId = 0
      };

      e.NewItem = newReflex;
    }

    private void Level2Cell_DoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (sender is DataGridCell cell && cell.DataContext is ConditionedReflexesSystem.ConditionedReflex reflex)
      {
        // Открытие диалога для редактирования контекстов реагирования
        ShowLevel2EditDialog(reflex);
      }
    }

    private void Level3Cell_DoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (sender is DataGridCell cell && cell.DataContext is ConditionedReflexesSystem.ConditionedReflex reflex)
        ShowLevel3EditDialog(reflex);
    }

    private void AdaptiveActionsCell_DoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (sender is DataGridCell cell && cell.DataContext is ConditionedReflexesSystem.ConditionedReflex reflex)
      {
        // Открытие диалога для редактирования адаптивных действий
        ShowAdaptiveActionsEditDialog(reflex);
      }
    }

    private void ShowLevel2EditDialog(ConditionedReflexesSystem.ConditionedReflex reflex)
    {
      try
      {
        var vm = DataContext as ViewModels.ConditionedReflexesViewModel;
        if (vm == null) return;

        var dialog = new BehaviorStylesSelectionDialog(reflex.Level2, vm.GomeostasSystem);
        if (dialog.ShowDialog() == true && dialog.SelectedBehaviorStyles != null)
        {
          reflex.Level2 = new List<int>(dialog.SelectedBehaviorStyles);
          ConditionedReflexesGrid.CommitEdit(DataGridEditingUnit.Row, true);
          ConditionedReflexesGrid.Items.Refresh();
        }
      }
      catch (System.Exception ex)
      {
        MessageBox.Show($"Ошибка при редактировании контекстов реагирования: {ex.Message}",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void ShowLevel3EditDialog(ConditionedReflexesSystem.ConditionedReflex reflex)
    {
      try
      {
        var vm = DataContext as ViewModels.ConditionedReflexesViewModel;
        if (vm == null) return;

        var dialog = new PerceptionImageSelectionDialog(reflex.Level3, vm.PerceptionImagesSystem);
        if (dialog.ShowDialog() == true && dialog.SelectedPerceptionImageId > 0)
        {
          reflex.Level3 = dialog.SelectedPerceptionImageId;
          ConditionedReflexesGrid.CommitEdit(DataGridEditingUnit.Row, true);
          ConditionedReflexesGrid.Items.Refresh();
        }
      }
      catch (System.Exception ex)
      {
        MessageBox.Show($"Ошибка при выборе образа восприятия: {ex.Message}",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void ShowAdaptiveActionsEditDialog(ConditionedReflexesSystem.ConditionedReflex reflex)
    {
      try
      {
        var dialog = new AdaptiveActionsSelectionDialog(reflex.AdaptiveActions);
        if (dialog.ShowDialog() == true && dialog.SelectedAdaptiveActions != null)
        {
          reflex.AdaptiveActions = new List<int>(dialog.SelectedAdaptiveActions);
          ConditionedReflexesGrid.CommitEdit(DataGridEditingUnit.Row, true);
          ConditionedReflexesGrid.Items.Refresh();
        }
      }
      catch (System.Exception ex)
      {
        MessageBox.Show($"Ошибка при редактировании адаптивных действий: {ex.Message}",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }
  }
}