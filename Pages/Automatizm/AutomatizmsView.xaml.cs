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
    public AutomatizmsView()
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