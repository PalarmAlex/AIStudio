using AIStudio.Common;
using AIStudio.Pages;
using ISIDA.Gomeostas;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

    public AntagonistMatrixViewModel()
    {
      _gomeostas = GomeostasSystem.Instance;
      BackCommand = new RelayCommand(_ => NavigateBack());

      MatrixCells = new ObservableCollection<MatrixCell>();
      _unpairedStyleIds = new List<int>();
      LoadMatrix();
    }

    private void LoadMatrix()
    {
      try
      {
        var styles = _gomeostas.GetAllBehaviorStyles();
        var cells = new ObservableCollection<MatrixCell>();

        if (!styles.Any())
        {
          MessageBox.Show("Стили поведения не загружены", "Информация",
              MessageBoxButton.OK, MessageBoxImage.Information);
          return;
        }

        var styleList = styles.Values.OrderBy(s => s.Id).ToList();
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
              // Левый верхний угол - пустая ячейка
              cell.Content = "";
              cell.IsHeader = true;
              cell.ToolTip = "Матрица антагонистов";
            }
            else if (row == 0)
            {
              // Заголовки столбцов
              var style = styleList[col - 1];
              cell.Content = style.Id.ToString();
              cell.IsHeader = true;
              cell.StyleName = style.Name;
              cell.ToolTip = $"{style.Name}\n{style.Description}";
              cell.IsUnpaired = _unpairedStyleIds.Contains(style.Id);
            }
            else if (col == 0)
            {
              // Заголовки строк
              var style = styleList[row - 1];
              cell.Content = style.Id.ToString();
              cell.IsHeader = true;
              cell.StyleName = style.Name;
              cell.ToolTip = $"{style.Name}\n{style.Description}";
              cell.IsUnpaired = _unpairedStyleIds.Contains(style.Id);
            }
            else
            {
              // Ячейки матрицы
              var rowStyle = styleList[row - 1];
              var colStyle = styleList[col - 1];

              bool isAntagonist = rowStyle.AntagonistStyles.Contains(colStyle.Id) &&
                                 colStyle.AntagonistStyles.Contains(rowStyle.Id);

              cell.Content = isAntagonist ? "╳" : "";
              cell.IsAntagonist = isAntagonist;
              cell.ToolTip = isAntagonist ?
                  $"{rowStyle.Name} ↔ {colStyle.Name}\nАнтагонисты" :
                  $"{rowStyle.Name} - {colStyle.Name}\nНет связи";
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

      foreach (var style in styles)
      {
        bool hasPair = false;
        foreach (var antagonistId in style.AntagonistStyles)
        {
          var antagonist = styles.FirstOrDefault(s => s.Id == antagonistId);
          if (antagonist != null && antagonist.AntagonistStyles.Contains(style.Id))
          {
            hasPair = true;
            break;
          }
        }

        if (!hasPair && style.AntagonistStyles.Any())
        {
          _unpairedStyleIds.Add(style.Id);
        }
      }
    }

    private void NavigateBack()
    {
      var mainWindow = Application.Current.MainWindow as MainWindow;
      if (mainWindow?.DataContext is MainViewModel mainViewModel)
      {
        var behaviorStylesView = new BehaviorStylesView();
        var viewModel = new BehaviorStylesViewModel(_gomeostas);
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