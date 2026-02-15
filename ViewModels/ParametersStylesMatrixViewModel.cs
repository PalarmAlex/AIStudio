using AIStudio.Common;
using AIStudio.Pages;
using ISIDA.Common;
using ISIDA.Gomeostas;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static ISIDA.Gomeostas.GomeostasSystem;

namespace AIStudio.ViewModels
{
  public class ParametersStylesMatrixViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    private readonly GomeostasSystem _gomeostas;
    private ObservableCollection<ParameterStyleCell> _matrixCells;
    private ObservableCollection<StyleFilterItem> _styleFilterItems;
    private StyleFilterItem _selectedStyleFilter;
    private List<ParameterData> _parameters;
    private List<BehaviorStyle> _styles;
    private StyleFilterItem _lastSelectedFilter;
    private ObservableCollection<StyleGroupFilterItem> _styleGroupFilterItems;
    private StyleGroupFilterItem _selectedStyleGroupFilter;
    private StyleGroupFilterItem _lastSelectedGroupFilter;

    public ObservableCollection<ParameterStyleCell> MatrixCells
    {
      get => _matrixCells;
      set
      {
        _matrixCells = value;
        OnPropertyChanged(nameof(MatrixCells));
      }
    }

    public ObservableCollection<StyleFilterItem> StyleFilterItems
    {
      get => _styleFilterItems;
      set
      {
        _styleFilterItems = value;
        OnPropertyChanged(nameof(StyleFilterItems));
      }
    }

    public StyleFilterItem SelectedStyleFilter
    {
      get => _selectedStyleFilter;
      set
      {
        _selectedStyleFilter = value;

        // Сохраняем выбранный фильтр
        if (value != null)
        {
          _lastSelectedFilter = new StyleFilterItem
          {
            Id = value.Id,
            Name = value.Name,
            Description = value.Description,
            Count = value.Count
          };
        }

        OnPropertyChanged(nameof(SelectedStyleFilter));
        ApplyStyleFilter();
      }
    }

    public ObservableCollection<StyleGroupFilterItem> StyleGroupFilterItems
    {
      get => _styleGroupFilterItems;
      set
      {
        _styleGroupFilterItems = value;
        OnPropertyChanged(nameof(StyleGroupFilterItems));
      }
    }

    public StyleGroupFilterItem SelectedStyleGroupFilter
    {
      get => _selectedStyleGroupFilter;
      set
      {
        _selectedStyleGroupFilter = value;
        if (value != null)
        {
          // Сохраняем базовое название без количества групп
          string baseGroupKey = value.GroupKey.StartsWith("Всего дублеров")
              ? "Всего дублеров"
              : value.GroupKey.Split('(')[0].Trim();

          _lastSelectedGroupFilter = new StyleGroupFilterItem
          {
            GroupKey = baseGroupKey,
            StyleIds = new List<int>(value.StyleIds)
          };
        }

        OnPropertyChanged(nameof(SelectedStyleGroupFilter));
        ApplyStyleGroupFilter();
      }
    }

    public ICommand BackCommand { get; }
    public ICommand RefreshCommand { get; }

    public ParametersStylesMatrixViewModel(GomeostasSystem gomeostas, List<ParameterData> currentParameters = null)
    {
      _gomeostas = gomeostas ?? throw new ArgumentNullException(nameof(gomeostas));
      BackCommand = new RelayCommand(_ => NavigateBack(currentParameters));

      MatrixCells = new ObservableCollection<ParameterStyleCell>();
      StyleFilterItems = new ObservableCollection<StyleFilterItem>();
      _parameters = new List<ParameterData>();
      _styles = new List<BehaviorStyle>();

      LoadMatrixFromParameters(currentParameters);
    }

    public AgentStateInfo GetAgentState()
    {
      return _gomeostas.GetAgentState();
    }

    internal void LoadMatrixFromParameters(List<ParameterData> parameters)
    {
      try
      {
        if (parameters == null || !parameters.Any())
        {
          MessageBox.Show("Параметры гомеостаза не загружены", "Информация",
              MessageBoxButton.OK, MessageBoxImage.Information);
          return;
        }

        _parameters = parameters.OrderBy(p => p.Id).ToList();
        _styles = _gomeostas.GetAllBehaviorStyles().Values.OrderBy(s => s.Id).ToList();

        CreateMatrix();
        InitializeStyleFilter();
        InitializeStyleGroupFilter();
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка загрузки матрицы: {ex.Message}", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void InitializeStyleFilter()
    {
      // Для каждого стиля считаем количество назначений в матрице
      var styleCounts = new List<StyleFilterItem>();

      foreach (var style in _styles)
      {
        int styleCount = MatrixCells
            .Where(c => !c.IsHeader)
            .SelectMany(c => c.StyleIds)
            .Count(id => id == style.Id || id == -style.Id);

        styleCounts.Add(new StyleFilterItem
        {
          Id = style.Id,
          Name = style.Name,
          Description = style.Description,
          Count = styleCount
        });
      }

      // Сортируем по количеству назначений по возрастанию
      var sortedStyles = styleCounts
          .OrderBy(s => s.Count)
          .ThenBy(s => s.Name)
          .ToList();

      var filterItems = new ObservableCollection<StyleFilterItem>
    {
        new StyleFilterItem
        {
            Id = 0,
            Name = "Все",
            Description = "Показать все стили",
            Count = 0
        }
    };

      // Добавляем отсортированные стили
      foreach (var style in sortedStyles)
      {
        filterItems.Add(style);
      }

      InitializeStyleGroupFilter();

      Application.Current.Dispatcher.Invoke(() =>
      {
        StyleFilterItems = filterItems;

        // Восстанавливаем предыдущий фильтр или выбираем "Все" по умолчанию
        if (_lastSelectedFilter != null)
        {
          var restoredFilter = filterItems.FirstOrDefault(f => f.Id == _lastSelectedFilter.Id);
          SelectedStyleFilter = restoredFilter ?? filterItems.First();
        }
        else
          SelectedStyleFilter = filterItems.First(); // "Все" по умолчанию
      });
    }

    private void InitializeStyleGroupFilter()
    {
      var duplicateGroups = FindDuplicateStyleGroups();
      int groupCount = duplicateGroups.Count;

      var groupFilterItems = new ObservableCollection<StyleGroupFilterItem>
    {
        new StyleGroupFilterItem { GroupKey = $"Всего дублеров ({groupCount})", StyleIds = new List<int>() }
    };

      foreach (var group in duplicateGroups)
      {
        groupFilterItems.Add(new StyleGroupFilterItem
        {
          GroupKey = group.Key,
          StyleIds = group.Value
        });
      }

      Application.Current.Dispatcher.Invoke(() =>
      {
        StyleGroupFilterItems = groupFilterItems;

        if (_lastSelectedGroupFilter != null)
        {
          var restoredFilter = groupFilterItems.FirstOrDefault(f =>
              f.GroupKey.StartsWith(_lastSelectedGroupFilter.GroupKey.Split(' ')[0])); // Сравниваем только начало названия
          SelectedStyleGroupFilter = restoredFilter ?? groupFilterItems.First();
        }
        else
          SelectedStyleGroupFilter = groupFilterItems.First();
      });
    }

    private Dictionary<string, List<int>> FindDuplicateStyleGroups()
    {
      var styleGroups = new Dictionary<string, List<int>>();
      var groupCounts = new Dictionary<string, int>();

      // Проходим по всем не-заголовочным ячейкам
      foreach (var cell in MatrixCells.Where(c => !c.IsHeader && c.StyleIds.Any()))
      {
        // Создаем ключ группы - отсортированный список ID стилей
        var sortedIds = cell.StyleIds.OrderBy(id => id).ToList();
        var groupKey = string.Join(",", sortedIds);

        if (!groupCounts.ContainsKey(groupKey))
        {
          groupCounts[groupKey] = 0;
          styleGroups[groupKey] = sortedIds;
        }
        groupCounts[groupKey]++;
      }

      // Оставляем только группы, которые встречаются более 1 раза
      // и сортируем по количеству повторов по убыванию
      var duplicateGroups = styleGroups
          .Where(g => groupCounts[g.Key] > 1)
          .OrderByDescending(g => groupCounts[g.Key])
          .ToDictionary(
              g => $"Группа [{g.Key}] (встречается {groupCounts[g.Key]} раз)",
              g => g.Value);

      return duplicateGroups;
    }

    internal void ApplyStyleGroupFilter()
    {
      if (SelectedStyleGroupFilter == null || MatrixCells == null) return;

      string selectedGroupKey = SelectedStyleGroupFilter.GroupKey;
      var selectedStyleIds = SelectedStyleGroupFilter.StyleIds;

      foreach (var cell in MatrixCells)
      {
        if (cell.IsHeader)
        {
          cell.IsGroupHighlighted = false;
          continue;
        }

        if (selectedGroupKey.StartsWith("Всего дублеров"))
        {
          cell.IsGroupHighlighted = false;
        }
        else
        {
          // Сравниваем отсортированные списки стилей
          var cellSortedIds = cell.StyleIds.OrderBy(id => id).ToList();
          var filterSortedIds = selectedStyleIds.OrderBy(id => id).ToList();

          cell.IsGroupHighlighted = cellSortedIds.SequenceEqual(filterSortedIds);
        }
      }

      RefreshMatrixDisplay();
    }

    private void RefreshMatrixDisplay()
    {
      var tempCells = new ObservableCollection<ParameterStyleCell>(MatrixCells);
      MatrixCells = null;
      MatrixCells = tempCells;
    }

    private void CreateMatrix()
    {
      try
      {
        var cells = new ObservableCollection<ParameterStyleCell>();

        // Заголовки зон параметров
        var zoneHeaders = new[]
        {
          "Выход из нормы",
          "Возврат в норму",
          "Норма",
          "Слабое отклонение",
          "Значительное отклонение",
          "Сильное отклонение",
          "Критическое отклонение"
        };

        var zoneHeadersToolTip = new[]
        {
          "В зоне позитивных значений.\nИзменения в сторону ухудшения: временное состояние ПЛОХО",
          "В зоне позитивных значений.\nИзменения в сторону улучшения: временное состояние ХОРОШО",
          "В зоне позитивных значений.\nНезначительные изменения: Стабильное состояние НОРМА",
          "В зоне негативных значений.\nНебольшое отклонение от порога",
          "В зоне негативных значений.\nЗначительное отклонение от порога",
          "В зоне негативных значений.\nСильное отклонение от порога",
          "В зоне негативных значений.\nКритическое отклонение от порога"
        };

        int rows = _parameters.Count + 1; // +1 для заголовков
        int cols = zoneHeaders.Length + 1; // +1 для имен параметров

        // Создаем матрицу
        for (int row = 0; row < rows; row++)
        {
          for (int col = 0; col < cols; col++)
          {
            var cell = new ParameterStyleCell();

            if (row == 0 && col == 0)
            {
              // Левый верхний угол
              cell.Content = "";
              cell.IsHeader = true;
              cell.ToolTip = "Матрица связей параметров и стилей (несохраненные данные)";
            }
            else if (row == 0)
            {
              // Заголовки зон
              cell.Content = zoneHeaders[col - 1];
              cell.IsHeader = true;
              cell.IsZoneHeader = true;
              cell.ToolTip = $"{zoneHeadersToolTip[col - 1]}";
            }
            else if (col == 0)
            {
              // Имена параметров
              var param = _parameters[row - 1];
              cell.Content = param.Name;
              cell.IsHeader = true;
              cell.IsParameterHeader = true;
              cell.ParameterId = param.Id;
              cell.ToolTip = GenerateParameterTooltip(param);
            }
            else
            {
              // Ячейки со стилями
              var param = _parameters[row - 1];
              int zoneId = col - 1; // 0-6 соответствуют зонам параметров

              var styleIds = GetStyleIdsForParameterZone(param, zoneId);
              cell.Content = string.Join(", ", styleIds);
              cell.StyleIds = styleIds;
              cell.ParameterId = param.Id;
              cell.ZoneId = zoneId;
              cell.ToolTip = GenerateCellTooltip(param, zoneId, styleIds);
              cell.HasStyles = styleIds.Any();
            }

            cells.Add(cell);
          }
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
          MatrixCells = cells;
        });
      }
      catch (Exception ex)
      {
        Logger.Error(ex.Message);
      }
    }

    public ParameterData GetParameterById(int parameterId)
    {
      return _parameters?.FirstOrDefault(p => p.Id == parameterId);
    }

    public List<ParameterData> GetAllParameters()
    {
      return _parameters;
    }

    public List<BehaviorStyle> GetAllStyles()
    {
      return _styles;
    }

    private List<int> GetStyleIdsForParameterZone(ParameterData param, int zoneId)
    {
      if (param.StyleActivations != null && param.StyleActivations.ContainsKey(zoneId))
      {
        return param.StyleActivations[zoneId];
      }
      return new List<int>();
    }

    private string GenerateParameterTooltip(ParameterData param)
    {
      return $"{param.Name} (ID:{param.Id})\n{param.Description}\n" +
             $"Текущее значение: {param.Value:F1}\n" +
             $"Норма: {param.NormaWell}\n" +
             $"Вес: {param.Weight}\n" +
             $"Скорость: {param.Speed}";
    }

    private string GenerateCellTooltip(ParameterData param, int zoneId, List<int> styleIds)
    {
      var zoneNames = new[]
      {
        "Выход из нормы",
        "Возврат в норму",
        "Норма",
        "Слабое отклонение",
        "Значительное отклонение",
        "Сильное отклонение",
        "Критическое отклонение"
      };

      var tooltip = new System.Text.StringBuilder();
      tooltip.AppendLine($"Параметр: {param.Name}");
      tooltip.AppendLine($"Зона: {zoneNames[zoneId]}");
      tooltip.AppendLine();

      if (styleIds.Any())
      {
        tooltip.AppendLine("Активируемые стили:");
        foreach (var styleId in styleIds)
        {
          var style = _styles.FirstOrDefault(s => s.Id == Math.Abs(styleId));
          if (style != null)
          {
            tooltip.AppendLine($"• {style.Name} (ID:{Math.Abs(styleId)})");
          }
          else
          {
            tooltip.AppendLine($"• Неизвестный стиль (ID:{Math.Abs(styleId)})");
          }
        }
      }
      else
      {
        tooltip.AppendLine("Стили не назначены");
      }

      return tooltip.ToString();
    }

    internal void ApplyStyleFilter()
    {
      if (SelectedStyleFilter == null || MatrixCells == null) return;

      int selectedStyleId = SelectedStyleFilter.Id;

      foreach (var cell in MatrixCells)
      {
        if (cell.IsHeader)
        {
          cell.IsHighlighted = false;
          continue;
        }

        if (selectedStyleId == 0)
          cell.IsHighlighted = false;
        else
          // Подсвечиваем ячейки, содержащие выбранный стиль
          cell.IsHighlighted = cell.StyleIds.Contains(selectedStyleId) ||
                              cell.StyleIds.Contains(-selectedStyleId);
      }

      RefreshMatrixDisplay();
    }

    private void NavigateBack(List<ParameterData> currentParameters)
    {
      var mainWindow = Application.Current.MainWindow as MainWindow;
      if (mainWindow?.DataContext is MainViewModel mainViewModel)
      {
        var systemParametersView = new SystemParametersView();

        // Передаем текущие данные обратно в SystemParametersViewModel
        var viewModel = new SystemParametersViewModel(_gomeostas);
        if (currentParameters != null)
        {
          // Обновляем локальные данные
          viewModel.SystemParameters = new ObservableCollection<ParameterData>(currentParameters);
        }
        systemParametersView.DataContext = viewModel;
        mainViewModel.CurrentContent = systemParametersView;
      }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class ParameterStyleCell
    {
      public string Content { get; set; } = string.Empty;
      public List<int> StyleIds { get; set; } = new List<int>();
      public bool IsHeader { get; set; }
      public bool IsZoneHeader { get; set; }
      public bool IsParameterHeader { get; set; }
      public bool HasStyles { get; set; }
      public bool IsHighlighted { get; set; }
      public bool IsGroupHighlighted { get; set; }
      public int ParameterId { get; set; }
      public int ZoneId { get; set; }
      public string ToolTip { get; set; } = string.Empty;

      public Brush BackgroundColor
      {
        get
        {
          if (IsGroupHighlighted) return new SolidColorBrush(Color.FromRgb(0, 80, 160)); // Синий для групп
          if (IsHighlighted) return new SolidColorBrush(Color.FromRgb(128, 128, 0)); // Желтый для подсветки стилей
          if (IsHeader && IsZoneHeader) return new SolidColorBrush(Color.FromRgb(0, 32, 0)); // Темно-зеленый для заголовков зон
          if (IsHeader && IsParameterHeader) return new SolidColorBrush(Color.FromRgb(16, 16, 16)); // Темно-серый для заголовков параметров
          if (IsHeader) return new SolidColorBrush(Color.FromRgb(16, 16, 16)); // Темно-серый для других заголовков
          if (HasStyles) return new SolidColorBrush(Color.FromRgb(0, 80, 0)); // Зеленый для ячеек со стилями
          return new SolidColorBrush(Color.FromRgb(8, 8, 8)); // Черный для пустых ячеек
        }
      }

      public Brush TextColor
      {
        get
        {
          if (IsHeader) return new SolidColorBrush(Color.FromRgb(255, 255, 0)); // Желтый для заголовков
          if (HasStyles) return new SolidColorBrush(Color.FromRgb(0, 255, 0)); // Зеленый для стилей
          return new SolidColorBrush(Color.FromRgb(64, 64, 64)); // Серый для пустых
        }
      }

      public Brush BorderColor => new SolidColorBrush(Color.FromRgb(32, 32, 32));
    }

    public class StyleFilterItem
    {
      public int Id { get; set; }
      public string Name { get; set; } = string.Empty;
      public string Description { get; set; } = string.Empty;
      public int Count { get; set; } // Количество назначений в матрице

      public override string ToString()
      {
        if (Id == 0)
          return "Все";
        else
          return $"{Name} (ID:{Id}) - {Count} назначений";
      }
    }

    public class StyleGroupFilterItem
    {
      public string GroupKey { get; set; } = string.Empty;
      public List<int> StyleIds { get; set; } = new List<int>();

      public override string ToString()
      {
        return GroupKey;
      }
    }
  }
}