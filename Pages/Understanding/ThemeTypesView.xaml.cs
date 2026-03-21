using AIStudio.Dialogs;
using AIStudio.ViewModels;
using System.Globalization;
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

    private void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
    {
      if (e.EditAction == DataGridEditAction.Commit && e.Row != null && !e.Row.IsEditing)
      {
        var grid = (DataGrid)sender;
        grid.CommitEdit(DataGridEditingUnit.Row, true);
      }
    }

    private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
      if (e.EditAction != DataGridEditAction.Commit || !(e.EditingElement is TextBox textBox))
        return;
      if (e.Column is DataGridTextColumn col && col.Header?.ToString() == "Вес")
      {
        string input = textBox.Text.Trim();
        if (!int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) || value < 1 || value > 10)
        {
          MessageBox.Show("Введите целое число от 1 до 10.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
          e.Cancel = true;
          textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
        }
      }
    }

    private void ThemesGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Delete)
        e.Handled = true;
    }

    private void NumericColumn_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      if (!char.IsDigit(e.Text, 0))
        e.Handled = true;
    }

    private void InfoFunctionsCell_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ClickCount != 2)
        return;
      if (!IsFormEnabled)
      {
        e.Handled = true;
        return;
      }
      if (sender is FrameworkElement fe && fe.DataContext is ThemeTypeItem item)
      {
        var editor = new InfoFunctionsChecklistEditor(
            $"Инфо-функции темы: {item.Description} (ID {item.Id})",
            item.AllowedInfoFuncIds);

        if (editor.ShowDialog() == true)
        {
          item.SetAllowedInfoFuncIds(editor.SelectedIds);
          ThemesGrid.CommitEdit(DataGridEditingUnit.Row, true);
          ThemesGrid.Items.Refresh();
        }
      }
      e.Handled = true;
    }

    private void SaveButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      ThemesGrid.CommitEdit(DataGridEditingUnit.Row, true);
      ThemesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
      if (sender is Control ctrl)
        ctrl.Focus();
    }

    private bool IsFormEnabled
    {
      get
      {
        if (DataContext is ThemeTypesViewModel viewModel && !viewModel.IsEditingEnabled)
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
