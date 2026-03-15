using AIStudio.Common;
using AIStudio.ViewModels;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AIStudio.Pages.Understanding
{
  public partial class ThemeTypesView : UserControl
  {
    public ThemeTypesView()
    {
      InitializeComponent();
    }

    private void DataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
    {
      var vm = DataContext as ThemeTypesViewModel;
      if (vm == null) return;

      int nextId = vm.GetNextId();
      e.NewItem = new ThemeTypeItem { Id = nextId, Description = "" };
    }

    private void ThemesGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key != Key.Delete) return;

      var vm = DataContext as ThemeTypesViewModel;
      if (vm == null || !vm.IsEditingEnabled)
      {
        e.Handled = true;
        return;
      }

      var grid = (DataGrid)sender;
      if (grid.IsEditing()) return;

      if (grid.SelectedItems.Count > 0)
      {
        var toRemove = grid.SelectedItems.OfType<ThemeTypeItem>().ToList();
        if (toRemove.Count > 0)
        {
          var result = MessageBox.Show(
              $"Удалить выбранные записи ({toRemove.Count}) из таблицы?",
              "Подтверждение удаления",
              MessageBoxButton.YesNo,
              MessageBoxImage.Question);

          if (result == MessageBoxResult.Yes)
            vm.RemoveRecordsAndSave(toRemove);
          e.Handled = true;
        }
      }
    }

    private void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
    {
      if (e.EditAction == DataGridEditAction.Commit && e.Row != null && !e.Row.IsEditing)
      {
        var grid = (DataGrid)sender;
        grid.CommitEdit(DataGridEditingUnit.Row, true);
      }
    }

    private void SaveButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      ThemesGrid.CommitEdit(DataGridEditingUnit.Row, true);
      ThemesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
      if (sender is Control ctrl)
        ctrl.Focus();
    }
  }
}
