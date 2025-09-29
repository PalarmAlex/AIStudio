using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ISIDA.Gomeostas;

namespace AIStudio.Dialogs
{
  public partial class StyleActivationsEditor : Window
  {
    public class ActivationItem
    {
      private readonly List<GomeostasSystem.BehaviorStyle> _availableStyles;

      public int Level { get; set; }
      public string LevelName { get; set; }
      public List<int> ActivateStyles { get; set; } = new List<int>();
      public List<int> DeactivateStyles { get; set; } = new List<int>();

      public ActivationItem(List<GomeostasSystem.BehaviorStyle> availableStyles)
      {
        _availableStyles = availableStyles;
      }

      public string ActivateStylesText => GetStylesText(ActivateStyles);
      public string DeactivateStylesText => GetStylesText(DeactivateStyles, true);

      private string GetStylesText(List<int> styleIds, bool withMinus = false)
      {
        var prefix = withMinus ? "-" : "";
        return string.Join(", ", styleIds.Select(id =>
        {
          var style = _availableStyles.FirstOrDefault(s => s.Id == id);
          return style != null ? $"{prefix}{style.Name} (ID: {id})" : $"{prefix}{id}";
        }));
      }
    }

    private static readonly Dictionary<int, string> LevelNames = new Dictionary<int, string>
        {
            {0, "0 - Выход из нормы"},
            {1, "1 - Возврат в норму"},
            {2, "2 - Норма"},
            {3, "3 - Слабое отклонение"},
            {4, "4 - Значительное отклонение"},
            {5, "5 - Сильное отклонение"},
            {6, "6 - Критическое отклонение"}
        };

    private readonly List<GomeostasSystem.BehaviorStyle> _availableStyles;
    private ObservableCollection<ActivationItem> _items;

    public Dictionary<int, List<int>> ResultActivations { get; private set; }

    public StyleActivationsEditor(
        string title,
        Dictionary<int, List<int>> activations,
        ReadOnlyDictionary<int, GomeostasSystem.BehaviorStyle> availableStyles)
    {
      InitializeComponent();
      Title = title;
      _availableStyles = availableStyles.Values.ToList();
      InitializeItems(activations ?? new Dictionary<int, List<int>>());
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Escape)
      {
        Close(); // Закрываем окно при нажатии Esc
        e.Handled = true;
      }
    }

    private void InitializeItems(Dictionary<int, List<int>> activations)
    {
      _items = new ObservableCollection<ActivationItem>();

      foreach (var level in LevelNames)
      {
        var item = new ActivationItem(_availableStyles)
        {
          Level = level.Key,
          LevelName = level.Value
        };

        if (activations.TryGetValue(level.Key, out var styleIds))
        {
          foreach (var styleId in styleIds)
          {
            if (styleId > 0)
              item.ActivateStyles.Add(styleId);
            else
              item.DeactivateStyles.Add(Math.Abs(styleId));
          }
        }

        _items.Add(item);
      }

      ActivationsGrid.ItemsSource = _items;
    }

    private void ActivateStylesCell_DoubleClick(object sender, RoutedEventArgs e)
    {
      if (ActivationsGrid.SelectedItem is ActivationItem item)
      {
        var editor = new StyleSelectionEditor(
            "Выбор активируемых стилей",
            _availableStyles,
            item.ActivateStyles);

        if (editor.ShowDialog() == true)
        {
          item.ActivateStyles = editor.SelectedStyleIds.ToList();
          ActivationsGrid.Items.Refresh();
        }
      }
    }

    private void DeactivateStylesCell_DoubleClick(object sender, RoutedEventArgs e)
    {
      if (ActivationsGrid.SelectedItem is ActivationItem item)
      {
        var editor = new StyleSelectionEditor(
            "Выбор деактивируемых стилей",
            _availableStyles,
            item.DeactivateStyles,
            true); // не проверять антагонистов

        if (editor.ShowDialog() == true)
        {
          item.DeactivateStyles = editor.SelectedStyleIds.ToList();
          ActivationsGrid.Items.Refresh();
        }
      }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
      var result = new Dictionary<int, List<int>>();
      var hasErrors = false;
      var errorMessages = new List<string>();

      foreach (var item in _items)
      {
        // Проверяем пересечение активируемых и деактивируемых стилей
        var conflictingStyles = item.ActivateStyles.Intersect(item.DeactivateStyles).ToList();
        if (conflictingStyles.Any())
        {
          var styleNames = string.Join(", ", conflictingStyles.Select(id =>
              _availableStyles.FirstOrDefault(s => s.Id == id)?.Name ?? id.ToString()));

          errorMessages.Add($"Уровень {item.LevelName}: стили {styleNames} одновременно активируются и деактивируются");
          hasErrors = true;
        }

        var styleIds = new List<int>();
        styleIds.AddRange(item.ActivateStyles);
        styleIds.AddRange(item.DeactivateStyles.Select(x => -x));

        if (styleIds.Any())
          result[item.Level] = styleIds;
      }

      if (hasErrors)
      {
        MessageBox.Show(
            $"Обнаружены конфликты стилей:\n\n{string.Join("\n", errorMessages)}",
            "Ошибка сохранения",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        return;
      }

      ResultActivations = result;
      DialogResult = true;
      Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
      Close();
    }
  }
}