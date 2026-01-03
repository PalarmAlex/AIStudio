using AIStudio.Common;
using AIStudio.Converters;
using AIStudio.Dialogs;
using AIStudio.ViewModels;
using ISIDA.Actions;
using ISIDA.Gomeostas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace AIStudio.Pages
{
  public partial class BehaviorStylesView : UserControl
  {
    public BehaviorStylesView()
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

      e.NewItem = new GomeostasSystem.BehaviorStyle
      {
        Id = nextId,
        Name = $"Новый стиль {nextId}",
        Weight = 50,
        AntagonistStyles = new List<int>()
      };
    }

    private int GetNextId()
    {
      var viewModel = DataContext as BehaviorStylesViewModel;
      if (viewModel == null) return 1;

      int maxId = 0;
      if (viewModel.BehaviorStyles != null && viewModel.BehaviorStyles.Any())
      {
        maxId = viewModel.BehaviorStyles.Max(a => a.Id);
      }

      var grid = BehaviorStylesGrid;
      if (grid?.ItemsSource != null)
      {
        var items = grid.ItemsSource.Cast<GomeostasSystem.BehaviorStyle>();
        if (items.Any())
        {
          int gridMaxId = items.Max(a => a.Id);
          maxId = Math.Max(maxId, gridMaxId);
        }
      }

      return maxId + 1;
    }

    private void BehaviorStylesGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Delete)
      {
        var grid = (DataGrid)sender;

        if (grid.IsEditing())
          return;

        if (grid.SelectedItems.Count > 0 && DataContext is BehaviorStylesViewModel viewModel)
        {
          var styles = grid.SelectedItems
            .Cast<object>()
            .Where(item => item is GomeostasSystem.BehaviorStyle)
            .Cast<GomeostasSystem.BehaviorStyle>()
            .ToList();

          // Подтверждение удаления
          var result = MessageBox.Show(
              $"Вы действительно хотите удалить {styles.Count} стилей?",
              "Подтверждение удаления",
              MessageBoxButton.YesNo,
              MessageBoxImage.Question);

          if (result == MessageBoxResult.Yes)
          {
            foreach (var style in styles)
            {
              viewModel.RemoveSelectedStyle(style);
            }
          }

          e.Handled = true;
        }
      }
    }

    private void DataGrid_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      var grid = (DataGrid)sender;
      var column = grid.CurrentColumn;

      // Для колонки "Вес" разрешаем только цифры
      if (column?.Header?.ToString() == "Вес")
      {
        if (!char.IsDigit(e.Text, 0))
        {
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

    private void AntagonistCell_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ClickCount == 2 && DataContext is BehaviorStylesViewModel vm)
      {
        if (!IsFormEnabled)
        {
          e.Handled = true;
          return;
        }

        if (sender is FrameworkElement element &&
            element.DataContext is GomeostasSystem.BehaviorStyle behaviorStyle)
        {
          var editor = new AntagonistStylesEditor(
              "Выбор антагонистических стилей для: " + behaviorStyle.Name + " (ID: " + behaviorStyle.Id + ")",
              vm.BehaviorStyles.Where(s => s.Id != behaviorStyle.Id),
              behaviorStyle.AntagonistStyles ?? new List<int>());

          if (editor.ShowDialog() == true)
          {
            behaviorStyle.AntagonistStyles = editor.SelectedStyleIds.ToList();
            BehaviorStylesGrid.CommitEdit(DataGridEditingUnit.Row, true);
            BehaviorStylesGrid.Items.Refresh();
          }
        }
        e.Handled = true;
      }
    }

    private void DescriptionCell_DoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (sender is DataGridCell cell && cell.DataContext is GomeostasSystem.BehaviorStyle parameter)
      {
        var dialog = new TextInputDialog
        {
          Owner = Window.GetWindow(this),
          Title = "Редактирование описания",
          Text = parameter.Description,
          Multiline = true
        };

        if (dialog.ShowDialog() == true)
        {
          parameter.Description = dialog.Text;
          BehaviorStylesGrid.CommitEdit(DataGridEditingUnit.Row, true);
          BehaviorStylesGrid.Items.Refresh();
        }
      }
    }

    private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
      if (e.EditAction != DataGridEditAction.Commit || !(e.EditingElement is TextBox textBox))
        return;

      if (e.Column.Header == null)
        return;

      string input = textBox.Text.Trim();
      if (input == "") return;

      string msgText = "";

      if (e.Column.Header.ToString() == "Вес")
      {
        if (!int.TryParse(input, out int value) || value < 0 || value > 100)
          msgText = "Введите число от 0 до 100";
      }
      if (msgText != "")
      {
        MessageBox.Show(msgText, "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);

        e.Cancel = true;

        var binding = textBox.GetBindingExpression(TextBox.TextProperty);
        binding?.UpdateTarget();
        return;
      }
    }

    private void NumericColumn_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      // Разрешаем: цифры, запятые, двоеточия, минусы и точки
      if (!char.IsDigit(e.Text, 0) &&
          e.Text != "," &&
          e.Text != ":" &&
          e.Text != "-" &&
          e.Text != ".")
      {
        e.Handled = true;
      }
    }

    private bool IsFormEnabled
    {
      get
      {
        if (DataContext is BehaviorStylesViewModel viewModel && !viewModel.IsEditingEnabled)
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