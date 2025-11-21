using AIStudio.Common;
using AIStudio.Pages;
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
    private List<GomeostasSystem.ParameterData> _parameters;
    private List<GomeostasSystem.BehaviorStyle> _styles;

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
        OnPropertyChanged(nameof(SelectedStyleFilter));
        ApplyStyleFilter();
      }
    }

    public ICommand BackCommand { get; }
    public ICommand RefreshCommand { get; }

    public ParametersStylesMatrixViewModel(GomeostasSystem gomeostas, List<GomeostasSystem.ParameterData> currentParameters = null)
    {
      _gomeostas = gomeostas ?? throw new ArgumentNullException(nameof(gomeostas));
      BackCommand = new RelayCommand(_ => NavigateBack(currentParameters));

      MatrixCells = new ObservableCollection<ParameterStyleCell>();
      StyleFilterItems = new ObservableCollection<StyleFilterItem>();
      _parameters = new List<GomeostasSystem.ParameterData>();
      _styles = new List<GomeostasSystem.BehaviorStyle>();

      LoadMatrixFromParameters(currentParameters);
    }

    public AgentStateInfo GetAgentState()
    {
      return _gomeostas.GetAgentState();
    }

    internal void LoadMatrixFromParameters(List<GomeostasSystem.ParameterData> parameters)
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

        // Заполняем фильтр стилей
        InitializeStyleFilter();

        // Создаем матрицу
        CreateMatrix();
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка загрузки матрицы: {ex.Message}", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void InitializeStyleFilter()
    {
      var filterItems = new ObservableCollection<StyleFilterItem>
            {
                new StyleFilterItem { Id = 0, Name = "Все", Description = "Показать все стили" }
            };

      foreach (var style in _styles)
      {
        filterItems.Add(new StyleFilterItem
        {
          Id = style.Id,
          Name = style.Name,
          Description = style.Description
        });
      }

      Application.Current.Dispatcher.Invoke(() =>
      {
        StyleFilterItems = filterItems;
        SelectedStyleFilter = filterItems.First(); // "Все" по умолчанию
      });
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
              cell.ToolTip = $"Зона параметра: {zoneHeaders[col - 1]}";
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
        Debug.WriteLine($"Ошибка создания матрицы: {ex.Message}");
      }
    }

    public GomeostasSystem.ParameterData GetParameterById(int parameterId)
    {
      return _parameters?.FirstOrDefault(p => p.Id == parameterId);
    }

    public List<GomeostasSystem.ParameterData> GetAllParameters()
    {
      return _parameters;
    }

    public List<GomeostasSystem.BehaviorStyle> GetAllStyles()
    {
      return _styles;
    }

    private List<int> GetStyleIdsForParameterZone(GomeostasSystem.ParameterData param, int zoneId)
    {
      if (param.StyleActivations != null && param.StyleActivations.ContainsKey(zoneId))
      {
        return param.StyleActivations[zoneId];
      }
      return new List<int>();
    }

    private string GenerateParameterTooltip(GomeostasSystem.ParameterData param)
    {
      return $"{param.Name} (ID:{param.Id})\n{param.Description}\n" +
             $"Текущее значение: {param.Value:F1}\n" +
             $"Норма: {param.NormaWell}\n" +
             $"Вес: {param.Weight}\n" +
             $"Скорость: {param.Speed}";
    }

    private string GenerateCellTooltip(GomeostasSystem.ParameterData param, int zoneId, List<int> styleIds)
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

    private void ApplyStyleFilter()
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

        // Для фильтра "Все" сбрасываем подсветку
        if (selectedStyleId == 0)
        {
          cell.IsHighlighted = false;
        }
        else
        {
          // Подсвечиваем ячейки, содержащие выбранный стиль
          cell.IsHighlighted = cell.StyleIds.Contains(selectedStyleId) ||
                              cell.StyleIds.Contains(-selectedStyleId);
        }
      }

      // Обновляем отображение
      var tempCells = new ObservableCollection<ParameterStyleCell>(MatrixCells);
      MatrixCells = null;
      MatrixCells = tempCells;
    }

    private void NavigateBack(List<GomeostasSystem.ParameterData> currentParameters)
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
          viewModel.SystemParameters = new ObservableCollection<GomeostasSystem.ParameterData>(currentParameters);
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
      public int ParameterId { get; set; }
      public int ZoneId { get; set; }
      public string ToolTip { get; set; } = string.Empty;

      public Brush BackgroundColor
      {
        get
        {
          if (IsHighlighted) return new SolidColorBrush(Color.FromRgb(128, 128, 0)); // Желтый для подсветки
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

      public override string ToString()
      {
        return Id == 0 ? "Все" : $"{Name} (ID:{Id})";
      }
    }
  }
}