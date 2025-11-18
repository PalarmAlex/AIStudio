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

namespace AIStudio.ViewModels
{
  public class AntagonistMatrixViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    private readonly GomeostasSystem _gomeostas;
    private ObservableCollection<MatrixCell> _matrixCells;
    private List<int> _unpairedStyleIds;

    public ObservableCollection<MatrixCell> MatrixCells
    {
      get => _matrixCells;
      set
      {
        _matrixCells = value;
        OnPropertyChanged(nameof(MatrixCells));
      }
    }

    public List<int> UnpairedStyleIds => _unpairedStyleIds;

    public ICommand BackCommand { get; }
    public ICommand RefreshCommand { get; }

    public AntagonistMatrixViewModel(GomeostasSystem gomeostas, List<GomeostasSystem.BehaviorStyle> currentStyles)
    {
      _gomeostas = gomeostas ?? throw new ArgumentNullException(nameof(gomeostas));
      BackCommand = new RelayCommand(_ => NavigateBack(currentStyles));

      MatrixCells = new ObservableCollection<MatrixCell>();
      _unpairedStyleIds = new List<int>();

      LoadMatrixFromStyles(currentStyles);
    }

    internal void LoadMatrixFromStyles(List<GomeostasSystem.BehaviorStyle> styles)
    {
      try
      {
        var cells = new ObservableCollection<MatrixCell>();

        if (!styles.Any())
        {
          MessageBox.Show("Стили поведения не загружены", "Информация",
              MessageBoxButton.OK, MessageBoxImage.Information);
          return;
        }

        var styleList = styles.OrderBy(s => s.Id).ToList();
        int size = styleList.Count;

        // Находим стили без парных антагонистов
        FindUnpairedStyles(styleList);

        // Создаем матрицу (size + 1) x (size + 1) для заголовков
        for (int row = 0; row <= size; row++)
        {
          for (int col = 0; col <= size; col++)
          {
            var cell = new MatrixCell();

            if (row == 0 && col == 0)
            {
              cell.Content = "";
              cell.IsHeader = true;
              cell.ToolTip = "Матрица антагонистов (несохраненные данные)";
            }
            else if (row == 0)
            {
              var style = styleList[col - 1];
              cell.Content = style.Id.ToString();
              cell.IsHeader = true;
              cell.StyleName = style.Name;
              cell.ToolTip = GenerateTooltip(style, styleList);
              cell.IsUnpaired = _unpairedStyleIds.Contains(style.Id);
            }
            else if (col == 0)
            {
              var style = styleList[row - 1];
              cell.Content = style.Id.ToString();
              cell.IsHeader = true;
              cell.StyleName = style.Name;
              cell.ToolTip = GenerateTooltip(style, styleList);
              cell.IsUnpaired = _unpairedStyleIds.Contains(style.Id);
            }
            else
            {
              var rowStyle = styleList[row - 1];
              var colStyle = styleList[col - 1];

              bool isAntagonist = rowStyle.AntagonistStyles.Contains(colStyle.Id) &&
                               colStyle.AntagonistStyles.Contains(rowStyle.Id);

              cell.Content = isAntagonist ? "╳" : "";
              cell.IsAntagonist = isAntagonist;

              if (isAntagonist)
              {
                cell.ToolTip = $"✓ {rowStyle.Name} ↔ {colStyle.Name}\nВзаимные антагонисты";
              }
              else if (rowStyle.AntagonistStyles.Contains(colStyle.Id) &&
                      !colStyle.AntagonistStyles.Contains(rowStyle.Id))
              {
                cell.ToolTip = $"⚠ {rowStyle.Name} → {colStyle.Name}\nОдносторонняя связь";
              }
              else if (!rowStyle.AntagonistStyles.Contains(colStyle.Id) &&
                       colStyle.AntagonistStyles.Contains(rowStyle.Id))
              {
                cell.ToolTip = $"⚠ {colStyle.Name} → {rowStyle.Name}\nОдносторонняя связь";
              }
              else
              {
                cell.ToolTip = $"{rowStyle.Name} - {colStyle.Name}\nНет связи";
              }
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
        MessageBox.Show($"Ошибка загрузки матрицы: {ex.Message}", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void FindUnpairedStyles(List<GomeostasSystem.BehaviorStyle> styles)
    {
      _unpairedStyleIds.Clear();

      var styleDict = styles.ToDictionary(s => s.Id, s => s);

      foreach (var style in styles)
      {
        List<int> unpairedAntagonists = new List<int>();
        List<int> nonExistentAntagonists = new List<int>();

        foreach (var antagonistId in style.AntagonistStyles)
        {
          if (styleDict.ContainsKey(antagonistId))
          {
            var antagonist = styleDict[antagonistId];

            if (!antagonist.AntagonistStyles.Contains(style.Id))
            {
              unpairedAntagonists.Add(antagonistId);
            }
          }
          else
          {
            nonExistentAntagonists.Add(antagonistId);
          }
        }

        // Если есть проблемы с антагонистами
        if (unpairedAntagonists.Any() || nonExistentAntagonists.Any())
        {
          if (!_unpairedStyleIds.Contains(style.Id))
          {
            _unpairedStyleIds.Add(style.Id);
          }
        }
      }
    }

    private string GenerateTooltip(GomeostasSystem.BehaviorStyle style, List<GomeostasSystem.BehaviorStyle> allStyles)
    {
      var styleDict = allStyles.ToDictionary(s => s.Id, s => s);
      var unpairedAntagonists = new List<int>();
      var nonExistentAntagonists = new List<int>();
      var validAntagonists = new List<int>();

      // Анализируем антагонистов
      foreach (var antagonistId in style.AntagonistStyles)
      {
        if (styleDict.ContainsKey(antagonistId))
        {
          var antagonist = styleDict[antagonistId];

          if (antagonist.AntagonistStyles.Contains(style.Id))
          {
            validAntagonists.Add(antagonistId);
          }
          else
          {
            unpairedAntagonists.Add(antagonistId);
          }
        }
        else
        {
          nonExistentAntagonists.Add(antagonistId);
        }
      }

      // Формируем подсказку
      var tooltip = new System.Text.StringBuilder();
      tooltip.AppendLine($"{style.Name} (ID:{style.Id})");
      tooltip.AppendLine($"{style.Description}");
      tooltip.AppendLine();

      if (!unpairedAntagonists.Any() && !nonExistentAntagonists.Any())
      {
        tooltip.AppendLine("✓ Все антагонисты симметричны");

        if (validAntagonists.Any())
        {
          var antagonistNames = string.Join(", ", validAntagonists.Select(id =>
              $"{styleDict[id].Name} (ID:{id})"));
          tooltip.AppendLine($"Антагонисты: {antagonistNames}");
        }
        else
        {
          tooltip.AppendLine("Нет антагонистов");
        }
      }
      else
      {
        tooltip.AppendLine("⚠ Нарушена симметрия антагонистов:");

        if (unpairedAntagonists.Any())
        {
          var unpairedNames = string.Join(", ", unpairedAntagonists.Select(id =>
              $"{styleDict[id].Name} (ID:{id})"));
          tooltip.AppendLine($"• Без обратной связи: {unpairedNames}");
        }

        if (nonExistentAntagonists.Any())
        {
          tooltip.AppendLine($"• Несуществующие ID: {string.Join(", ", nonExistentAntagonists)}");
        }

        if (validAntagonists.Any())
        {
          var validNames = string.Join(", ", validAntagonists.Select(id =>
              $"{styleDict[id].Name} (ID:{id})"));
          tooltip.AppendLine($"✓ Корректные: {validNames}");
        }
      }

      return tooltip.ToString();
    }

    private void NavigateBack(List<GomeostasSystem.BehaviorStyle> currentStyles)
    {
      var mainWindow = Application.Current.MainWindow as MainWindow;
      if (mainWindow?.DataContext is MainViewModel mainViewModel)
      {
        var behaviorStylesView = new BehaviorStylesView();

        // Передаем текущие данные обратно в BehaviorStylesViewModel
        var viewModel = new BehaviorStylesViewModel(_gomeostas, currentStyles);
        behaviorStylesView.DataContext = viewModel;
        mainViewModel.CurrentContent = behaviorStylesView;
      }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class MatrixCell
    {
      public int RowId { get; set; }
      public int ColId { get; set; }
      public string Content { get; set; } = string.Empty;
      public string StyleName { get; set; } = string.Empty;
      public bool IsHeader { get; set; }
      public bool IsAntagonist { get; set; }
      public bool IsUnpaired { get; set; }
      public string ToolTip { get; set; } = string.Empty;

      public Brush BackgroundColor
      {
        get
        {
          if (IsHeader && IsUnpaired) return new SolidColorBrush(Color.FromRgb(128, 0, 0)); // Красный для заголовков без пар
          if (IsHeader) return new SolidColorBrush(Color.FromRgb(16, 16, 16)); // Темно-серый для обычных заголовков
          if (IsAntagonist) return new SolidColorBrush(Color.FromRgb(0, 80, 0)); // Зеленый для антагонистов
          return new SolidColorBrush(Color.FromRgb(8, 8, 8)); // Черный для остальных
        }
      }

      public Brush TextColor
      {
        get
        {
          if (IsHeader && IsUnpaired) return new SolidColorBrush(Color.FromRgb(255, 128, 128)); // Светло-красный для текста без пар
          if (IsHeader) return new SolidColorBrush(Color.FromRgb(255, 255, 0)); // желтый для номеров стилей
          if (IsAntagonist) return new SolidColorBrush(Color.FromRgb(0, 255, 0)); // Зеленый для антагонистов
          return new SolidColorBrush(Color.FromRgb(64, 64, 64)); // Серый для остальных
        }
      }

      public Brush BorderColor => new SolidColorBrush(Color.FromRgb(32, 32, 32));
    }
  }
}