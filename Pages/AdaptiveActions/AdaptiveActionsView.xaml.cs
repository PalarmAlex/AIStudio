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
        Influences = new Dictionary<int, int>(),
        AntagonistActions = new List<int>(),
        FatigueCoefficient = 0.2f, // Значение по умолчанию
        RecoveryCoefficient = 0.05f // Значение по умолчанию
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

    private void InfluencesCell_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ClickCount == 2 && DataContext is AdaptiveActionsViewModel vm)
      {
        if (sender is FrameworkElement element &&
            element.DataContext is AdaptiveActionsSystem.AdaptiveAction action)
        {
          var editor = new ActionInfluencesEditor(
              $"Влияния действия: {action.Name} (ID: {action.Id})",
              vm.GetAllParameters(),
              action.Influences);

          if (editor.ShowDialog() == true)
          {
            action.Influences = editor.SelectedInfluences.ToDictionary(
                kvp => kvp.Key,
                kvp => GomeostasSystem.ClampInt(kvp.Value, -10, 10));
            // Отложенное обновление
            Dispatcher.BeginInvoke(new Action(() =>
            {
              ActionsGrid.Items.Refresh();
            }), DispatcherPriority.Background);
          }
        }
        e.Handled = true;
      }
    }

    private void AntagonistCell_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ClickCount == 2 && DataContext is AdaptiveActionsViewModel vm)
      {
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
            // Отложенное обновление
            Dispatcher.BeginInvoke(new Action(() =>
            {
              ActionsGrid.Items.Refresh();
            }), DispatcherPriority.Background);
          }
        }
        e.Handled = true;
      }
    }

    private void CostsCell_MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ClickCount == 2 && DataContext is AdaptiveActionsViewModel vm)
      {
        if (sender is FrameworkElement element &&
            element.DataContext is AdaptiveActionsSystem.AdaptiveAction action)
        {
          var editor = new ActionInfluencesEditor(
              $"Затраты действия: {action.Name} (ID: {action.Id})",
              vm.GetAllParameters(),
              action.Costs); // Передаём Costs вместо Influences

          if (editor.ShowDialog() == true)
          {
            // Обновляем Costs, применяя Clamp
            action.Costs = editor.SelectedInfluences.ToDictionary(
                kvp => kvp.Key,
                kvp => GomeostasSystem.ClampInt(kvp.Value, -10, 10));

            // Отложенное обновление
            Dispatcher.BeginInvoke(new Action(() =>
            {
              ActionsGrid.Items.Refresh();
            }), DispatcherPriority.Background);
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
          // Отложенное обновление
          Dispatcher.BeginInvoke(new Action(() =>
          {
            ActionsGrid.Items.Refresh();
          }), DispatcherPriority.Background);
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
        if (!TryParseFloat(input, out float value) || value < 0.05f || value > 0.8f)
          msgText = "Введите число от 0.05 до 0.8";
      }
      else if (e.Column.Header.ToString() == "К. восстановления")
      {
        if (!TryParseFloat(input, out float value) || value < 0.01f || value > 0.2f)
          msgText = "Введите число от 0.01 до 0.2";
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

    private void FatigueCoefficient_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      var textBox = e.OriginalSource as TextBox;
      if (textBox == null) return;

      string newText = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength) + e.Text;

      // Разрешаем только цифры, точку и запятую
      if (!IsValidFloatInput(newText))
      {
        e.Handled = true;
        return;
      }

      // Автозамена запятой на точку
      if (e.Text == ",")
      {
        e.Handled = true;
        textBox.SelectedText = ".";
        textBox.CaretIndex = textBox.SelectionStart + 1;
      }
    }

    private void FatigueCoefficient_LostFocus(object sender, RoutedEventArgs e)
    {
      if (sender is TextBox textBox && textBox.DataContext is AdaptiveActionsSystem.AdaptiveAction action)
      {
        if (TryParseFloat(textBox.Text, out float value))
        {
          // Применяем ограничения диапазона
          value = Math.Max(0.05f, Math.Min(0.8f, value));
          action.FatigueCoefficient = value;

          // Обновляем отображение
          textBox.Text = value.ToString("F2", CultureInfo.InvariantCulture);
        }
        else
        {
          // Восстанавливаем предыдущее значение при ошибке
          textBox.Text = action.FatigueCoefficient.ToString("F2", CultureInfo.InvariantCulture);
        }
      }
    }

    private void RecoveryCoefficient_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      var textBox = e.OriginalSource as TextBox;
      if (textBox == null) return;

      string newText = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength) + e.Text;

      // Разрешаем только цифры, точку и запятую
      if (!IsValidFloatInput(newText))
      {
        e.Handled = true;
        return;
      }

      // Автозамена запятой на точку
      if (e.Text == ",")
      {
        e.Handled = true;
        textBox.SelectedText = ".";
        textBox.CaretIndex = textBox.SelectionStart + 1;
      }
    }

    private void RecoveryCoefficient_LostFocus(object sender, RoutedEventArgs e)
    {
      if (sender is TextBox textBox && textBox.DataContext is AdaptiveActionsSystem.AdaptiveAction action)
      {
        if (TryParseFloat(textBox.Text, out float value))
        {
          // Применяем ограничения диапазона
          value = Math.Max(0.01f, Math.Min(0.2f, value));
          action.RecoveryCoefficient = value;

          // Обновляем отображение
          textBox.Text = value.ToString("F2", CultureInfo.InvariantCulture);
        }
        else
        {
          // Восстанавливаем предыдущее значение при ошибке
          textBox.Text = action.RecoveryCoefficient.ToString("F2", CultureInfo.InvariantCulture);
        }
      }
    }

    #region Вспомогательные методы

    /// <summary>
    /// Проверяет валидность ввода для дробного числа
    /// </summary>
    private bool IsValidFloatInput(string input)
    {
      if (string.IsNullOrEmpty(input)) return true;

      // Разрешаем только цифры, точку, запятую и знак минус в начале
      var regex = new Regex(@"^-?[0-9]*[,.]?[0-9]*$");
      return regex.IsMatch(input);
    }

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

    #endregion
  }
}