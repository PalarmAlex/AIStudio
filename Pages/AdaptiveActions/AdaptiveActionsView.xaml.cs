using AIStudio.Common;
using AIStudio.Dialogs;
using AIStudio.ViewModels;
using ISIDA.Actions;
using ISIDA.Gomeostas;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace AIStudio.Pages
{
  public partial class AdaptiveActionsView : UserControl
  {
    public AdaptiveActionsView()
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
      e.NewItem = new AdaptiveActionsSystem.AdaptiveAction
      {
        Name = "Новое действие",
        Description = string.Empty,
        AntagonistActions = new List<int>(),
      };
    }

    private void ActionsGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Delete)
      {
        var grid = (DataGrid)sender;

        if (grid.IsEditing())
          return;

        if (grid.SelectedItems.Count > 0 && DataContext is AdaptiveActionsViewModel viewModel)
        {
          var actions = grid.SelectedItems
          .Cast<object>()
          .Where(item => item is AdaptiveActionsSystem.AdaptiveAction)
          .Cast<AdaptiveActionsSystem.AdaptiveAction>()
          .ToList();

          var result = MessageBox.Show(
              $"Вы действительно хотите удалить {actions.Count} действий?",
              "Подтверждение удаления",
              MessageBoxButton.YesNo,
              MessageBoxImage.Question);

          if (result == MessageBoxResult.Yes)
          {
            foreach (var action in actions)
            {
              viewModel.RemoveSelectedAction(action);
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

    private void AntagonistCell_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ClickCount == 2 && DataContext is AdaptiveActionsViewModel vm)
      {
        if (!IsFormEnabled)
        {
          e.Handled = true;
          return;
        }

        if (sender is FrameworkElement element &&
            element.DataContext is AdaptiveActionsSystem.AdaptiveAction action)
        {
          var editor = new AntagonistActionsEditor(
              $"Антагонисты действия: {action.Name} (ID: {action.Id})",
              vm.AdaptiveActions.Where(a => a.Id != action.Id),
              action.AntagonistActions ?? new List<int>());

          if (editor.ShowDialog() == true)
          {
            action.AntagonistActions = editor.SelectedActionIds.ToList();
            ActionsGrid.CommitEdit(DataGridEditingUnit.Row, true);
            ActionsGrid.Items.Refresh();
          }
        }
        e.Handled = true;
      }
    }

    private void DescriptionCell_DoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (sender is DataGridCell cell && cell.DataContext is AdaptiveActionsSystem.AdaptiveAction action)
      {
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
          ActionsGrid.CommitEdit(DataGridEditingUnit.Row, true);
          ActionsGrid.Items.Refresh();
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

      if (e.Column.Header.ToString() == "Энергичность")
      {
        if (!int.TryParse(input, out int value) || value < 1 || value > 10)
          msgText = "Введите число от 1 до 10";
      }
      else if (e.Column.Header.ToString() == "К. усталости")
      {
        if (!TryParseFloat(input, out float value) || value < 0.0f || value > 0.8f)
          msgText = "Введите число от 0.0 до 0.8";
      }
      else if (e.Column.Header.ToString() == "К. восстановления")
      {
        if (!TryParseFloat(input, out float value) || value < 0.01f || value > 1.0f)
          msgText = "Введите число от 0.01 до 1.0";
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

    #region Вспомогательные методы

    /// <summary>
    /// Парсит строку в float с поддержкой и точки, и запятой как разделителя
    /// </summary>
    private bool TryParseFloat(string input, out float result)
    {
      result = 0f;

      if (string.IsNullOrWhiteSpace(input))
        return false;

      // Заменяем запятую на точку для унификации
      string normalizedInput = input.Replace(',', '.');

      return float.TryParse(normalizedInput, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }

    private bool IsFormEnabled
    {
      get
      {
        if (DataContext is AdaptiveActionsViewModel viewModel && !viewModel.IsEditingEnabled)
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

    #endregion
  }
}