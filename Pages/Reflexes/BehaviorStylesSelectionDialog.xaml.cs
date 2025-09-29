using AIStudio.Common;
using ISIDA.Gomeostas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using static ISIDA.Gomeostas.GomeostasSystem;

namespace AIStudio.Dialogs
{
  public partial class BehaviorStylesSelectionDialog : Window
  {
    public List<int> SelectedBehaviorStyles { get; private set; }
    private List<BehaviorStyleItem> _behaviorStyles;
    private readonly GomeostasSystem _gomeostasSystem;
    private bool _combinationsLoaded = false;
    private int _totalPossibleCombinations = 0;
    private int _actualCombinations = 0;
    private List<int> _initiallySelected;

    public BehaviorStylesSelectionDialog(List<int> initiallySelected, GomeostasSystem gomeostasSystem)
    {
      InitializeComponent();
      _gomeostasSystem = gomeostasSystem;
      _initiallySelected = initiallySelected;
      Loaded += OnLoaded;
      LoadBehaviorStyles(initiallySelected);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
      PreviewKeyDown += (s, args) =>
      {
        if (args.Key == Key.Escape)
        {
          DialogResult = false;
          Close();
        }
      };
    }

    private void LoadBehaviorStyles(List<int> initiallySelected)
    {
      _behaviorStyles = new List<BehaviorStyleItem>();

      try
      {
        if (_gomeostasSystem == null) return;

        // Пытаемся загрузить существующие комбинации
        var combinations = _gomeostasSystem.LoadStyleCombinations();

        if (combinations.Any())
        {
          LoadCombinationsIntoList(combinations, initiallySelected);
          _combinationsLoaded = true;
          _actualCombinations = combinations.Count;

          // Рассчитываем общее количество возможных комбинаций
          var allStyles = _gomeostasSystem.GetAllBehaviorStyles();
          _totalPossibleCombinations = CalculateTotalCombinations(allStyles.Count);

          UpdateStatusText();
        }
        else
        {
          _behaviorStyles.Clear();
          _combinationsLoaded = false;
          UpdateStatusText();
        }

        // Устанавливаем источник данных для ComboBox
        BehaviorStylesComboBox.ItemsSource = _behaviorStyles;

        // Выбираем изначально выбранный элемент
        if (initiallySelected != null && initiallySelected.Any())
        {
          var selectedItem = _behaviorStyles.FirstOrDefault(item =>
              item.StyleIds.SequenceEqual(initiallySelected.OrderBy(id => id)));

          if (selectedItem != null)
            BehaviorStylesComboBox.SelectedValue = selectedItem.StyleIds;
        }

        UpdateGenerateButtonState();
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка загрузки стилей поведения: {ex.Message}",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private int CalculateTotalCombinations(int styleCount)
    {
      // C(n,1) + C(n,2) + C(n,3)
      if (styleCount < 1) return 0;
      return styleCount + (styleCount * (styleCount - 1)) / 2 + (styleCount * (styleCount - 1) * (styleCount - 2)) / 6;
    }

    private void LoadCombinationsIntoList(List<List<BehaviorStyle>> combinations, List<int> initiallySelected)
    {
      _behaviorStyles.Clear();

      // Добавляем пустой элемент для возможности сброса выбора
      _behaviorStyles.Add(new BehaviorStyleItem
      {
        Id = 0,
        Name = "[Не выбрано]",
        Description = "Сбросить выбор комбинации стилей",
        StyleIds = new List<int>(),
        IsCombination = false,
        IsSelected = initiallySelected == null || !initiallySelected.Any()
      });

      foreach (var combination in combinations.OrderBy(c => c.Count))
      {
        var styleIds = combination.Select(s => s.Id).OrderBy(id => id).ToList();
        var styleNames = combination.Select(s => s.Name).ToList();

        _behaviorStyles.Add(new BehaviorStyleItem
        {
          Id = GetCombinationHashCode(styleIds),
          Name = $"Комбинация [{combination.Count}]: {string.Join(" + ", styleNames)}",
          Description = $"ID стилей: {string.Join(", ", styleIds)}",
          StyleIds = styleIds,
          IsCombination = true,
          IsSelected = initiallySelected != null && initiallySelected.SequenceEqual(styleIds)
        });
      }
    }

    private int GetCombinationHashCode(List<int> styleIds)
    {
      // Создаем уникальную строку и берем ее хэш-код
      string combinedString = string.Join(",", styleIds.OrderBy(id => id));
      return combinedString.GetHashCode();
    }

    private void UpdateStatusText()
    {
      if (_combinationsLoaded)
      {
        StatusText.Text = $"Всего реально возможных комбинаций: {_totalPossibleCombinations}. Сформировано после контрастирования: {_actualCombinations}";
      }
      else
      {
        StatusText.Text = "Список комбинаций стилей реагирования не сформирован. Нажмите кнопку 'Сформировать'.";
      }
    }

    private void GenerateCombinationsButton_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        var message = _combinationsLoaded
            ? "Обновить существующий список комбинаций стилей?"
            : "Сформировать комбинации стилей?";

        var result = MessageBox.Show(message, "Подтверждение",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        // Генерируем комбинации
        var combinations = _gomeostasSystem.GenerateStyleCombinations(3, true, true);

        // Загружаем в список
        LoadCombinationsIntoList(combinations, _initiallySelected);
        _combinationsLoaded = true;
        _actualCombinations = combinations.Count;

        // Рассчитываем общее количество возможных комбинаций
        var allStyles = _gomeostasSystem.GetAllBehaviorStyles();
        _totalPossibleCombinations = CalculateTotalCombinations(allStyles.Count);

        // Обновляем источник данных ComboBox
        BehaviorStylesComboBox.ItemsSource = null;
        BehaviorStylesComboBox.ItemsSource = _behaviorStyles;

        // Восстанавливаем выбор
        if (_initiallySelected != null && _initiallySelected.Any())
        {
          var selectedItem = _behaviorStyles.FirstOrDefault(item =>
              item.StyleIds.SequenceEqual(_initiallySelected.OrderBy(id => id)));
          BehaviorStylesComboBox.SelectedItem = selectedItem;
        }

        UpdateGenerateButtonState();
        UpdateStatusText();

        MessageBox.Show($"Сгенерировано {combinations.Count} валидных комбинаций стилей",
            "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка генерации комбинаций: {ex.Message}",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void UpdateGenerateButtonState()
    {
      GenerateButton.Content = _combinationsLoaded ? "Обновить" : "Сформировать";
      GenerateButton.ToolTip = _combinationsLoaded
          ? "Обновить список комбинаций стилей"
          : "Сформировать комбинации стилей";
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
      // Получаем выбранный элемент из ComboBox
      var selectedItem = BehaviorStylesComboBox.SelectedItem as BehaviorStyleItem;

      if (selectedItem != null)
        SelectedBehaviorStyles = selectedItem.StyleIds;
      else
      {
        // Также можно попробовать получить через SelectedValue
        var selectedValue = BehaviorStylesComboBox.SelectedValue as List<int>;
        if (selectedValue != null)
          SelectedBehaviorStyles = selectedValue;
        else
          SelectedBehaviorStyles = new List<int>();
      }

      DialogResult = true;
      Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
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

  public class BehaviorStyleItem : AntagonistItem
  {
    public List<int> StyleIds { get; set; } = new List<int>();
    public bool IsCombination { get; set; }
  }
}