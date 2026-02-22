using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ISIDA.Gomeostas;
using ISIDA.Common;
using AIStudio.ViewModels;

namespace AIStudio.Dialogs
{
  public partial class AutomatizmLoadDialog : Window
  {
    public AutomatizmLoadDialogViewModel ViewModel { get; private set; }

    public AutomatizmLoadDialog(
        GomeostasSystem gomeostasSystem,
        string bootDataFolder)
    {
      InitializeComponent();

      // Создаем ViewModel
      ViewModel = new AutomatizmLoadDialogViewModel(gomeostasSystem, bootDataFolder);

      // Устанавливаем DataContext
      DataContext = ViewModel;

      // Устанавливаем CloseAction
      ViewModel.CloseAction = (result, baseState, styleIds) =>
      {
        DialogResult = result;
        SelectedBaseState = baseState;
        SelectedStyleIds = styleIds;
        Close();
      };
    }

    public int? SelectedBaseState { get; private set; }
    public List<int> SelectedStyleIds { get; private set; }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Escape)
      {
        DialogResult = false;
        Close();
      }
    }
  }

  public class AutomatizmLoadDialogViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private readonly GomeostasSystem _gomeostasSystem;
    private readonly string _bootDataFolder;

    public Action<bool, int?, List<int>> CloseAction { get; set; }

    // Команды
    public RelayCommand LoadStylesCommand { get; }
    public RelayCommand LoadCommand { get; }
    public RelayCommand CancelCommand { get; }

    public AutomatizmLoadDialogViewModel(GomeostasSystem gomeostasSystem, string bootDataFolder)
    {
      _gomeostasSystem = gomeostasSystem;
      _bootDataFolder = bootDataFolder;

      // Инициализация команд
      LoadStylesCommand = new RelayCommand(ExecuteGenerateStyles);
      CancelCommand = new RelayCommand(ExecuteCancel);
      LoadCommand = new RelayCommand(ExecuteLoad, CanExecuteLoad);

      // Базовые состояния
      BaseStates = new List<KeyValuePair<int, string>>
            {
                new KeyValuePair<int, string>(-1, "Плохо"),
                new KeyValuePair<int, string>(0, "Норма"),
                new KeyValuePair<int, string>(1, "Хорошо")
            };

      SelectedBaseState = 0;

      // Загружаем комбинации стилей
      LoadStyleCombinations();
    }

    private string _filePath;
    public string FilePath
    {
      get
      {
        if (string.IsNullOrEmpty(_filePath))
        {
          _filePath = Path.Combine(_bootDataFolder, "automatizm_generate_list_1.csv");
        }
        return _filePath;
      }
    }

    public List<KeyValuePair<int, string>> BaseStates { get; }

    private int? _selectedBaseState;
    public int? SelectedBaseState
    {
      get => _selectedBaseState;
      set
      {
        _selectedBaseState = value;
        OnPropertyChanged(nameof(SelectedBaseState));
        OnPropertyChanged(nameof(SelectedBaseStateDisplay));
        OnPropertyChanged(nameof(CanLoad));
        LoadCommand?.RaiseCanExecuteChanged();
      }
    }

    public string SelectedBaseStateDisplay
    {
      get
      {
        if (!SelectedBaseState.HasValue) return "Не выбрано";
        if (SelectedBaseState.Value == -1) return "Плохо";
        if (SelectedBaseState.Value == 0) return "Норма";
        if (SelectedBaseState.Value == 1) return "Хорошо";
        return SelectedBaseState.Value.ToString();
      }
    }

    private List<StyleCombinationItem> _styleCombinations = new List<StyleCombinationItem>();
    public List<StyleCombinationItem> StyleCombinations
    {
      get => _styleCombinations;
      set
      {
        _styleCombinations = value;
        OnPropertyChanged(nameof(StyleCombinations));
      }
    }

    private List<int> _selectedStyleIds = new List<int>();
    public List<int> SelectedStyleIds
    {
      get => _selectedStyleIds;
      set
      {
        _selectedStyleIds = value ?? new List<int>();
        OnPropertyChanged(nameof(SelectedStyleIds));
        OnPropertyChanged(nameof(SelectedStylesDisplay));
        OnPropertyChanged(nameof(CanLoad));
        LoadCommand?.RaiseCanExecuteChanged();
      }
    }

    public string SelectedStylesDisplay
    {
      get
      {
        if (SelectedStyleIds == null || SelectedStyleIds.Count == 0)
          return "Не выбрано";

        var selectedItem = StyleCombinations?.FirstOrDefault(x =>
            x.StyleIds != null && AreListsEqual(x.StyleIds, SelectedStyleIds));

        return selectedItem != null
            ? selectedItem.DisplayName
            : $"ID: {string.Join(", ", SelectedStyleIds)}";
      }
    }

    private string _stylesStatusText;
    public string StylesStatusText
    {
      get => _stylesStatusText;
      set
      {
        _stylesStatusText = value;
        OnPropertyChanged(nameof(StylesStatusText));
      }
    }

    public bool CanLoad => SelectedBaseState.HasValue && File.Exists(FilePath);

    private bool CanExecuteLoad(object parameter)
    {
      return CanLoad;
    }

    private bool AreListsEqual(List<int> list1, List<int> list2)
    {
      if (list1 == null && list2 == null) return true;
      if (list1 == null || list2 == null) return false;
      if (list1.Count != list2.Count) return false;

      var sorted1 = list1.OrderBy(x => x).ToList();
      var sorted2 = list2.OrderBy(x => x).ToList();

      for (int i = 0; i < sorted1.Count; i++)
      {
        if (sorted1[i] != sorted2[i]) return false;
      }
      return true;
    }

    private void LoadStyleCombinations()
    {
      try
      {
        List<List<GomeostasSystem.BehaviorStyle>> combinations;
        if (_gomeostasSystem != null)
        {
          combinations = _gomeostasSystem.LoadStyleCombinations();
        }
        else
        {
          combinations = new List<List<GomeostasSystem.BehaviorStyle>>();
        }

        var items = new List<StyleCombinationItem>
                {
                    new StyleCombinationItem
                    {
                        DisplayName = "[Не выбрано]",
                        StyleIds = new List<int>()
                    }
                };

        foreach (var combo in combinations.OrderBy(c => c.Count))
        {
          var styleIds = combo.Select(s => s.Id).OrderBy(id => id).ToList();
          var styleNames = combo.Select(s => s.Name).ToList();

          items.Add(new StyleCombinationItem
          {
            DisplayName = $"[{combo.Count}]: {string.Join(" + ", styleNames)}",
            StyleIds = styleIds
          });
        }

        StyleCombinations = items;
        StylesStatusText = $"Загружено комбинаций: {combinations.Count}";
      }
      catch (Exception ex)
      {
        StylesStatusText = $"Ошибка загрузки: {ex.Message}";
        StyleCombinations = new List<StyleCombinationItem>();
      }
    }

    private void ExecuteGenerateStyles(object parameter)
    {
      try
      {
        List<List<GomeostasSystem.BehaviorStyle>> combinations;
        if (_gomeostasSystem != null)
        {
          combinations = _gomeostasSystem.GenerateStyleCombinations(true);
        }
        else
        {
          combinations = new List<List<GomeostasSystem.BehaviorStyle>>();
        }

        var items = new List<StyleCombinationItem>
                {
                    new StyleCombinationItem
                    {
                        DisplayName = "[Не выбрано]",
                        StyleIds = new List<int>()
                    }
                };

        foreach (var combo in combinations.OrderBy(c => c.Count))
        {
          var styleIds = combo.Select(s => s.Id).OrderBy(id => id).ToList();
          var styleNames = combo.Select(s => s.Name).ToList();

          items.Add(new StyleCombinationItem
          {
            DisplayName = $"[{combo.Count}]: {string.Join(" + ", styleNames)}",
            StyleIds = styleIds
          });
        }

        StyleCombinations = items;
        StylesStatusText = $"Сгенерировано комбинаций: {combinations.Count}";

        // Если был выбран какой-то элемент, пытаемся сохранить выбор
        if (SelectedStyleIds != null && SelectedStyleIds.Count > 0)
        {
          var selectedItem = items.FirstOrDefault(x =>
              x.StyleIds != null && AreListsEqual(x.StyleIds, SelectedStyleIds));

          if (selectedItem == null)
          {
            SelectedStyleIds = new List<int>();
          }
        }

        MessageBox.Show($"Сгенерировано {combinations.Count} комбинаций стилей",
            "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка генерации: {ex.Message}", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void ExecuteLoad(object parameter)
    {
      CloseAction?.Invoke(true, SelectedBaseState, SelectedStyleIds ?? new List<int>());
    }

    private void ExecuteCancel(object parameter)
    {
      CloseAction?.Invoke(false, null, null);
    }
  }

  public class StyleCombinationItem
  {
    public string DisplayName { get; set; }
    public List<int> StyleIds { get; set; }
  }
}