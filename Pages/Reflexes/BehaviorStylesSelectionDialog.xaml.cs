using AIStudio.Common;
using ISIDA.Gomeostas;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using static ISIDA.Gomeostas.GomeostasSystem;

namespace AIStudio.Dialogs
{
  public partial class BehaviorStylesSelectionDialog : Window
  {
    public List<int> SelectedBehaviorStyles { get; private set; }
    private ObservableCollection<BehaviorStyleItem> _behaviorStyles;
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
      try
      {
        if (_gomeostasSystem == null) return;

        var combinations = _gomeostasSystem.LoadStyleCombinations();
        _combinationsLoaded = combinations.Any();
        _actualCombinations = combinations.Count;

        if (_combinationsLoaded)
        {
          var allStyles = _gomeostasSystem.GetAllBehaviorStyles();
          _totalPossibleCombinations = CalculateTotalCombinations(allStyles.Count);
        }

        ApplyCombinationsToComboBox(combinations, initiallySelected);
        UpdateStatusText();
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

    private void ApplyCombinationsToComboBox(List<List<BehaviorStyle>> combinations, List<int> initiallySelected)
    {
      _behaviorStyles = BuildBehaviorStyleItems(combinations, initiallySelected);

      BehaviorStylesComboBox.SelectedItem = null;
      BehaviorStylesComboBox.SelectedValue = null;
      BehaviorStylesComboBox.ItemsSource = null;
      BehaviorStylesComboBox.ItemsSource = _behaviorStyles;

      if (initiallySelected != null && initiallySelected.Any())
      {
        var orderedSelection = initiallySelected.OrderBy(id => id).ToList();
        var selectedItem = _behaviorStyles.FirstOrDefault(item =>
            item.StyleIds.SequenceEqual(orderedSelection));
        if (selectedItem != null)
          BehaviorStylesComboBox.SelectedItem = selectedItem;
      }
    }

    private static ObservableCollection<BehaviorStyleItem> BuildBehaviorStyleItems(
        List<List<BehaviorStyle>> combinations, List<int> initiallySelected)
    {
      var items = new ObservableCollection<BehaviorStyleItem>
      {
        new BehaviorStyleItem
        {
          Id = 0,
          Name = "[Не выбрано]",
          Description = "Сбросить выбор комбинации стилей",
          StyleIds = new List<int>(),
          IsCombination = false,
          IsSelected = initiallySelected == null || !initiallySelected.Any()
        }
      };

      foreach (var combination in combinations.OrderBy(c => c.Count))
      {
        var styleIds = combination.Select(s => s.Id).OrderBy(id => id).ToList();
        var styleNames = combination.Select(s => s.Name).ToList();
        items.Add(new BehaviorStyleItem
        {
          Id = GetCombinationHashCode(styleIds),
          Name = $"Комбинация [{combination.Count}]: {string.Join(" + ", styleNames)}",
          Description = $"ID стилей: {string.Join(", ", styleIds)}",
          StyleIds = styleIds,
          IsCombination = true,
          IsSelected = initiallySelected != null && initiallySelected.SequenceEqual(styleIds)
        });
      }

      return items;
    }

    private static int GetCombinationHashCode(List<int> styleIds)
    {
      string combinedString = string.Join(",", styleIds.OrderBy(id => id));
      return combinedString.GetHashCode();
    }

    private void UpdateStatusText()
    {
      if (_combinationsLoaded)
      {
        StatusText.Text = $"Всего теоретически возможных комбинаций (1...3): {_totalPossibleCombinations}. Сформировано по данным параметров гомеостаза: {_actualCombinations}";
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

        var combinations = _gomeostasSystem.GenerateStyleCombinations(true);
        combinations = _gomeostasSystem.LoadStyleCombinations();

        _combinationsLoaded = combinations.Any();
        _actualCombinations = combinations.Count;

        var allStyles = _gomeostasSystem.GetAllBehaviorStyles();
        _totalPossibleCombinations = CalculateTotalCombinations(allStyles.Count);

        ApplyCombinationsToComboBox(combinations, _initiallySelected);
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
      var selectedItem = BehaviorStylesComboBox.SelectedItem as BehaviorStyleItem;
      if (selectedItem != null)
        SelectedBehaviorStyles = selectedItem.StyleIds;
      else
      {
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
        Close();
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
