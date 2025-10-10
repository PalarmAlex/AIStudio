﻿using AIStudio.Common;
using AIStudio.Dialogs;
using AIStudio.ViewModels;
using ISIDA.Gomeostas;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace AIStudio.Pages
{
  public partial class SystemParametersView : UserControl
  {
    public SystemParametersView()
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
        disposable.Dispose();
    }

    private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Delete)
      {
        var grid = (DataGrid)sender;

        if (grid.IsEditing())
          return;

        if (grid.SelectedItems.Count > 0)
        {
          var viewModel = (SystemParametersViewModel)DataContext;
          var parameters = grid.SelectedItems
              .Cast<object>()
              .Where(item => item is GomeostasSystem.ParameterData)
              .Cast<GomeostasSystem.ParameterData>()
              .ToList();

          var result = MessageBox.Show(
              $"Вы действительно хотите удалить {parameters.Count} параметров?",
              "Подтверждение удаления",
              MessageBoxButton.YesNo,
              MessageBoxImage.Question);

          if (result == MessageBoxResult.Yes)
          {
            viewModel.RemoveParameters(parameters);
          }

          e.Handled = true; // Блокируем дальнейшую обработку Delete
        }
      }
    }

    private void ParametersGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
    {
      if (e.EditAction == DataGridEditAction.Commit)
      {
        var grid = (DataGrid)sender;
        grid.CommitEdit(DataGridEditingUnit.Row, true);
      }
    }

    private void DataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
    {
      e.NewItem = new GomeostasSystem.ParameterData
      {
        Name = "Новый параметр",
        Description = string.Empty,
        Value = 50,
        Weight = 50,
        NormaWell = 50,
        Speed = 0,
        StyleActivations = new Dictionary<int, List<int>>()
      };
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

      if (e.Column.Header.ToString() == "Значение")
      {
        if (!double.TryParse(input, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double value)
           || value < 0 || value > 100)
          msgText = "Введите число от 0 до 100. Для дробных чисел разделитель точка";
      }
      else if (e.Column.Header.ToString() == "Вес" || e.Column.Header.ToString() == "Норма")
      {
        if (!int.TryParse(input, out int value)
            || value < 1 || value > 99)
          msgText = "Введите целое число от 1 до 99";
      }
      else if (e.Column.Header.ToString() == "% затухания")
      {
        if (!int.TryParse(input, out int value)
            || value < -10 || value > 10)
          msgText = "Введите целое число от -10 до +10";
      }
      else if(e.Column.Header.ToString() == "Влияние (Плохо)" || e.Column.Header.ToString() == "Влияние (Хорошо)")
      {
        if (!Regex.IsMatch(input, @"^(\s*\d+\s*:\s*-?\d+(\.\d+)?\s*(,\s*\d+\s*:\s*-?\d+(\.\d+)?\s*)*)$"))
          msgText = "Неправильный формат данных для вставки.\nПример: '1:0.5, 2:-1, 3:1'";
        else
        {
          string txt = input?.ToString() ?? string.Empty;

          try
          {
            var pairs = txt.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var pair in pairs)
            {
              var parts = pair.Trim().Split(':');
              if (parts.Length != 2)
              {
                msgText = "Неправильный формат данных для вставки.\nПример: '1:0.5, 2:-1, 3:1'";
                break;
              }

              if (int.TryParse(parts[0], out int paramId))
              {
                List<int> validParamId = new List<int> { 1, 2, 3, 4 };
                if (!validParamId.Contains(paramId))
                {
                  msgText = $"Указан ID параметра гомеостаза, который не существует: {paramId}";
                  break;
                }
              }
              else
              {
                msgText = $"Введенное значение не является целым числом: {parts[0]}";
                break;
              }
             
              if (float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float influence))
              {
                if(influence < -10 || influence > 10)
                {
                  msgText = $"Введенное значение должно быть в диапазоне от -10 до 10: {parts[1]}";
                  break;
                }

              }
              else
              {
                msgText = $"Введенное значение не является числом: {parts[1]}";
                break;
              }
            }
          }
          catch
          {
            
          }
        }
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

    private void SpeedColumn_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      var textBox = sender as TextBox;
      if (textBox == null) return;

      // Получаем текущий текст и предполагаемый новый текст
      string currentText = textBox.Text;
      string newText = currentText.Substring(0, textBox.SelectionStart) +
                      e.Text +
                      currentText.Substring(textBox.SelectionStart + textBox.SelectionLength);

      // Проверяем, является ли ввод допустимым целым числом в диапазоне -10..10
      if (!IsValidSpeedValue(newText))
      {
        e.Handled = true;
        return;
      }

      // Дополнительная проверка для предотвращения множественных минусов
      if (e.Text == "-" && (currentText.Contains("-") || textBox.SelectionStart != 0))
      {
        e.Handled = true;
      }
    }

    private bool IsValidSpeedValue(string text)
    {
      // Пустая строка разрешена (пользователь может стирать текст)
      if (string.IsNullOrEmpty(text))
        return true;

      // Проверяем, является ли текст целым числом
      if (int.TryParse(text, out int result))
      {
        // Проверяем диапазон -10 до 10
        return result >= -10 && result <= 10;
      }

      // Проверяем частичный ввод (например, "-", "1", "-1")
      if (text == "-")
        return true;

      // Проверяем, содержит ли текст только цифры и один минус в начале
      if (text.StartsWith("-") && text.Length > 1)
      {
        string digits = text.Substring(1);
        return digits.All(char.IsDigit) && int.TryParse(text, out _);
      }

      return text.All(char.IsDigit);
    }

    private void SpeedColumn_Pasting(object sender, DataObjectPastingEventArgs e)
    {
      if (e.DataObject.GetDataPresent(typeof(string)))
      {
        string pasteText = (string)e.DataObject.GetData(typeof(string));
        if (!IsValidSpeedValue(pasteText))
        {
          e.CancelCommand();
        }
      }
      else
      {
        e.CancelCommand();
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

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (e.ChangedButton != MouseButton.Left) return;

      var dataGrid = (DataGrid)sender;
      var selectedItem = dataGrid.SelectedItem as GomeostasSystem.ParameterData;

      if (selectedItem != null && dataGrid.CurrentColumn?.Header?.ToString() == "Активации стилей")
      {
        var viewModel = (SystemParametersViewModel)DataContext;
        var allStyles = viewModel.GetAllBehaviorStyles();

        string title = $"Редактирование активаций стилей для параметра: {selectedItem.Name}";
        var editor = new StyleActivationsEditor(
            title,
            selectedItem.StyleActivations,
            allStyles);

        if (editor.ShowDialog() == true)
        {
          selectedItem.StyleActivations = editor.ResultActivations;
          dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
          // Отложенное обновление
          Dispatcher.BeginInvoke(new Action(() =>
          {
            dataGrid.Items.Refresh();
          }), DispatcherPriority.Background);
        }

        e.Handled = true;
      }
    }

    private void InfluenceCell_DoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (e.ChangedButton != MouseButton.Left) return;

      var cell = sender as DataGridCell;
      if (cell == null) return;

      var dataGrid = FindVisualParent<DataGrid>(cell);
      if (dataGrid == null) return;

      var parameter = dataGrid.SelectedItem as GomeostasSystem.ParameterData;
      if (parameter == null) return;

      bool isBadInfluence = cell.Column.Header.ToString().Contains("Плохо");
      var currentInfluences = isBadInfluence
          ? parameter.BadStateInfluence
          : parameter.WellStateInfluence;

      var viewModel = (SystemParametersViewModel)DataContext;
      var editor = new InfluenceEditor(
          $"{parameter.Name}: {cell.Column.Header.ToString()}",
          sourceParameterId: parameter.Id,
          viewModel.GetAllParameters(),
          currentInfluences.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

      if (editor.ShowDialog() == true)
      {
        var newInfluences = editor.ResultInfluences;
        if (isBadInfluence)
          parameter.BadStateInfluence = newInfluences;
        else
          parameter.WellStateInfluence = newInfluences;

        dataGrid.CommitEdit(DataGridEditingUnit.Row, true);
        // Отложенное обновление
        Dispatcher.BeginInvoke(new Action(() =>
        {
          dataGrid.Items.Refresh();
        }), DispatcherPriority.Background);
      }
    }

    private void DescriptionCell_DoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (sender is DataGridCell cell && cell.DataContext is GomeostasSystem.ParameterData parameter)
      {
        var dialog = new TextInputDialog
        {
          Owner = Window.GetWindow(this),
          Title = "Редактирование описания",
          Text = parameter.Description,
          Multiline = true
        };

        dialog.SetText(parameter.Description, true);

        if (dialog.ShowDialog() == true)
        {
          parameter.Description = dialog.Text;
        }
      }
    }

    // Вспомогательный метод для поиска родительского DataGrid
    private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
      while (child != null && !(child is T))
      {
        child = VisualTreeHelper.GetParent(child);
      }
      return child as T;
    }

  }
}
