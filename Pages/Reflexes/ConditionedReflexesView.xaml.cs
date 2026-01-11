using AIStudio.Common;
using AIStudio.Dialogs;
using AIStudio.ViewModels;
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
        if (!IsFormDeletion)
        {
          e.Handled = true;
          return;
        }

        var grid = (DataGrid)sender;

        if (grid.IsEditing())
          return;

        if (grid.SelectedItems.Count > 0 && DataContext is ConditionedReflexesViewModel viewModel)
        {
          var actions = grid.SelectedItems
            .Cast<object>()
            .Where(item => item is ConditionedReflexesViewModel.ConditionedReflexWithSourceActions)
            .Cast<ConditionedReflexesViewModel.ConditionedReflexWithSourceActions>()
            .ToList();

          var result = MessageBox.Show(
              $"Вы действительно хотите удалить {actions.Count} условных рефлексов?",
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

    private bool IsFormDeletion
    {
      get
      {
        if (DataContext is ConditionedReflexesViewModel viewModel && !viewModel.IsDeletionEnabled)
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

  }
}