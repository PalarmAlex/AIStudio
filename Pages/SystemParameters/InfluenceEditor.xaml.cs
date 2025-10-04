using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AIStudio.Common;
using ISIDA.Gomeostas;

namespace AIStudio.Dialogs
{
  public partial class InfluenceEditor : Window
  {
    public class InfluenceItem
    {
      public int ParameterId { get; set; } = 0; // По умолчанию 0 (не выбран)
      public string Name { get; set; }
      public double InfluenceValue { get; set; }
    }

    public Dictionary<int, float> ResultInfluences { get; private set; }

    private readonly List<GomeostasSystem.ParameterData> _parameters;

    private readonly int _sourceParameterId;
    public InfluenceEditor(
      string title,
      int sourceParameterId, // Добавляем этот параметр
      IEnumerable<GomeostasSystem.ParameterData> parameters,
      Dictionary<int, float> currentInfluences)
    {
      InitializeComponent();
      Title = title;
      _sourceParameterId = sourceParameterId;
      _parameters = parameters.ToList();

      var comboItems = new List<object>();
      comboItems.AddRange(_parameters
          .Where(p => p.Id != _sourceParameterId) // Исключаем текущий параметр
          .OrderBy(p => p.Id)
          .Select(p => new { Id = p.Id, Name = $"{p.Name} (ID: {p.Id})" }));

      var comboBoxColumn = (DataGridComboBoxColumn)InfluencesGrid.Columns[0];
      comboBoxColumn.ItemsSource = comboItems;

      // Загружаем текущие влияния
      var items = new List<InfluenceItem>();
      foreach (var influence in currentInfluences)
      {
        var param = _parameters.FirstOrDefault(p => p.Id == influence.Key);
        if (param != null)
        {       
          items.Add(new InfluenceItem
          {
            ParameterId = param.Id,
            Name = param.Name,
            InfluenceValue = influence.Value
          });
        }
      }
      InfluencesGrid.ItemsSource = items;
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Escape)
      {
        Close(); // Закрываем окно при нажатии Esc
        e.Handled = true;
      }
    }

    private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      var textBox = sender as TextBox;
      string newText = textBox?.Text.Insert(textBox.CaretIndex, e.Text) ?? e.Text;

      // Автозамена запятой на точку
      if (e.Text == ",")
      {
        e.Handled = true;
        textBox.Text = textBox.Text.Insert(textBox.CaretIndex, ".");
        textBox.CaretIndex++;
        return;
      }

      // Проверяем, что ввод — число в диапазоне [-99, 99]
      bool isNumber = double.TryParse(
          newText,
          NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
          CultureInfo.InvariantCulture,
          out double number
      );

      e.Handled = !isNumber || number < -99 || number > 99;
    }

    private void NumericTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
      var textBox = sender as TextBox;
      if (textBox == null) return;

      // Приводим значение к корректному формату при потере фокуса
      if (double.TryParse(textBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
      {
        textBox.Text = value.ToString("0.##", CultureInfo.InvariantCulture);
      }
      else
      {
        textBox.Text = "0";
      }
    }

    protected override void OnActivated(EventArgs e)
    {
      base.OnActivated(e);

      // Подписываемся на событие вставки для всех TextBox в DataGrid
      EventManager.RegisterClassHandler(typeof(TextBox),
          DataObject.PastingEvent,
          new DataObjectPastingEventHandler(TextBox_Pasting));
    }

    private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
      if (e.DataObject.GetDataPresent(typeof(string)))
      {
        string text = (string)e.DataObject.GetData(typeof(string));
        text = text.Trim();

        if (sender is TextBox textBox)
        {
          // Для поля влияния проверяем вещественные числа от -1 до 1
          if (textBox.Name.Contains("Influence") ||
              (textBox.Parent is DataGridCell cell && cell.Column.Header?.ToString() == "Влияние"))
          {
            // Заменяем запятые на точки
            text = text.Replace(',', '.');

            if (!double.TryParse(text,
                               NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                               CultureInfo.InvariantCulture,
                               out double number)
                || number < -1
                || number > 1)
            {
              e.CancelCommand();
              return;
            }
          }

          // Если всё в порядке, вставляем обработанный текст
          int caretIndex = textBox.CaretIndex;
          textBox.Text = textBox.Text.Insert(caretIndex, text);
          textBox.CaretIndex = caretIndex + text.Length;
          e.CancelCommand();
        }
      }
      else
      {
        e.CancelCommand();
      }
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
      ResultInfluences = new Dictionary<int, float>();

      foreach (InfluenceItem item in InfluencesGrid.ItemsSource)
      {
        // Пропускаем строки, где не выбран параметр (ParameterId == 0)
        if (item.ParameterId == 0)
          continue;

        // Проверяем корректность значений перед сохранением
        if (item.InfluenceValue < -1 || item.InfluenceValue > 1)
        {
          MessageBox.Show($"Значение влияния должно быть между -1 и +1 (параметр {item.ParameterId})",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
          return;
        }

        ResultInfluences[item.ParameterId] = (float)item.InfluenceValue;
      }

      DialogResult = true;
      Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
      Close();
    }

    private void ParameterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      var comboBox = sender as ComboBox;
      if (comboBox == null || comboBox.SelectedValue == null)
        return;

      var newId = (int)comboBox.SelectedValue;
      if (newId == 0) return;

      var currentItem = comboBox.DataContext as InfluenceItem;
      if (currentItem == null) return;

      // Проверка дублирования
      bool isDuplicate = false;
      foreach (InfluenceItem item in InfluencesGrid.ItemsSource)
      {
        if (item != currentItem && item.ParameterId == newId)
        {
          isDuplicate = true;
          break;
        }
      }

      if (isDuplicate)
      {
        MessageBox.Show("Этот параметр уже выбран в другой строке",
                      "Ошибка",
                      MessageBoxButton.OK,
                      MessageBoxImage.Warning);

        comboBox.SelectedValue = 0;
      }
    }

    private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Delete && sender is DataGrid grid)
      {
        // Если редактируется ячейка - разрешаем стандартное поведение Delete
        if (grid.IsEditing())
          return;

        if (grid.SelectedItems.Count > 0)
        {
          // Подтверждение удаления
          var result = MessageBox.Show(
              "Вы действительно хотите удалить выбранные строки?",
              "Подтверждение удаления",
              MessageBoxButton.YesNo,
              MessageBoxImage.Question);

          if (result != MessageBoxResult.Yes)
          {
            e.Handled = true;
          }
        }
      }
    }

    private void InfluenceTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      var textBox = sender as TextBox;
      if (textBox == null) return;

      // Автозамена запятой на точку
      if (e.Text == ",")
      {
        e.Handled = true;
        textBox.Text = textBox.Text.Insert(textBox.CaretIndex, ".");
        textBox.CaretIndex++;
        return;
      }

      // Получаем предполагаемый новый текст
      string newText = textBox.Text.Insert(textBox.CaretIndex, e.Text);

      // Проверяем, что ввод — число в диапазоне [-10, 10]
      bool isNumber = double.TryParse(
          newText,
          NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
          CultureInfo.InvariantCulture,
          out double number
      );

      e.Handled = !isNumber || number < -10 || number > 10;
    }

    private void InfluenceTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
      var textBox = sender as TextBox;
      if (textBox == null) return;

      // Приводим значение к корректному формату при потере фокуса
      if (double.TryParse(textBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
      {
        // Обеспечиваем, что значение в диапазоне -10..10
        value = Math.Max(-10, Math.Min(10, value));
        textBox.Text = value.ToString("0.##", CultureInfo.InvariantCulture);
      }
      else
      {
        textBox.Text = "0";
      }
    }

    private void InfluenceTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
      if (e.DataObject.GetDataPresent(typeof(string)))
      {
        string pasteText = (string)e.DataObject.GetData(typeof(string));

        // Заменяем запятые на точки
        pasteText = pasteText.Replace(',', '.');

        // Проверяем, является ли вставленное значение числом в диапазоне [-10, 10]
        if (!double.TryParse(pasteText,
                           NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                           CultureInfo.InvariantCulture,
                           out double number)
            || number < -10
            || number > 10)
        {
          e.CancelCommand();
        }
      }
      else
      {
        e.CancelCommand();
      }
    }

  }
}
