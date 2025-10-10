using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ISIDA.Common;
using ISIDA.Actions;
using ISIDA.Gomeostas;

namespace AIStudio.Dialogs
{
  public partial class ActionInfluencesEditor : Window
  {
    public Dictionary<int, int> SelectedInfluences { get; private set; }

    public ActionInfluencesEditor(string title,
                                List<GomeostasSystem.ParameterData> parameters,
                                Dictionary<int, int> currentInfluences)
    {
      InitializeComponent();
      Title = title;

      // Инициализация с защитой от null
      SelectedInfluences = new Dictionary<int, int>(currentInfluences ?? new Dictionary<int, int>());

      // Безопасное создание коллекции элементов
      var items = parameters?
          .Where(p => p != null)
          .GroupBy(p => p.Id)  // Защита от дубликатов ID
          .Select(g => g.First())
          .Select(p => new ParameterInfluence
          {
            Id = p.Id,
            Name = p.Name ?? "Неизвестный параметр",
            Effect = SelectedInfluences.TryGetValue(p.Id, out var effect) ? effect : 0
          })
          .ToList() ?? new List<ParameterInfluence>();

      ParametersGrid.ItemsSource = items;
    }

    private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      if (!(sender is TextBox textBox))
      {
        e.Handled = true;
        return;
      }

      try
      {
        string currentText = textBox.Text ?? string.Empty;
        int selectionStart = Math.Min(textBox.SelectionStart, currentText.Length);
        int selectionLength = Math.Min(textBox.SelectionLength, currentText.Length - selectionStart);

        string newText = currentText.Remove(selectionStart, selectionLength) + e.Text;

        e.Handled = !IsValidInput(newText);
      }
      catch
      {
        e.Handled = true;
      }
    }

    private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
      try
      {
        if (e.DataObject?.GetDataPresent(typeof(string)) != true)
        {
          e.CancelCommand();
          return;
        }

        string text = e.DataObject.GetData(typeof(string)) as string;
        if (!IsValidInput(text))
        {
          e.CancelCommand();
        }
      }
      catch
      {
        e.CancelCommand();
      }
    }

    private bool IsValidInput(string input)
    {
      if (string.IsNullOrWhiteSpace(input))
        return true;

      // Разрешаем минус только в начале и только один
      if (input.Count(c => c == '-') > 1 || (input.Contains("-") && input.IndexOf('-') > 0))
        return false;

      // Удаляем все минусы для проверки цифр
      string digitsOnly = input.Replace("-", "");
      if (digitsOnly.Length == 0) return true; // Только минус

      return int.TryParse(input, out int result) && result >= -10 && result <= 10;
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        ParametersGrid.CommitEdit();

        if (!(ParametersGrid.ItemsSource is IEnumerable<ParameterInfluence> items))
        {
          DialogResult = false;
          Close();
          return;
        }

        var itemList = items.ToList();
        var validationResults = itemList
            .Select(item =>
            {
              return SettingsValidator.ValidateInfluencesParametr(item.Effect);
            })
            .ToList();

        var invalidResults = validationResults
            .Where(result => !result.isValid)
            .ToList();

        if (invalidResults.Any())
        {
          string errorMessage = string.Join("\n", invalidResults.Select(r => r.errorMessage).Take(5));
          MessageBox.Show(
              $"Обнаружены ошибки валидации:\n{errorMessage}" +
              (invalidResults.Count > 5 ? $"\n\n... и еще {invalidResults.Count - 5} ошибок" : ""),
              "Ошибка валидации",
              MessageBoxButton.OK,
              MessageBoxImage.Warning);
          return;
        }

        SelectedInfluences = itemList
            .Where(item => item != null && item.Effect != 0)
            .ToDictionary(item => item.Id, item => item.Effect);

        DialogResult = true;
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при сохранении: {ex.Message}",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        DialogResult = false;
      }
      finally
      {
        Close();
      }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
      Close();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Escape)
      {
        Close(); // Закрываем окно при нажатии Esc
        e.Handled = true;
      }
    }
  }

  public class ParameterInfluence
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public int Effect { get; set; }
  }
}