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

namespace AIStudio.ViewModels
{
  public class BehaviorStylesViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly GomeostasSystem _gomeostas;
    private string _currentAgentName;
    private int _currentAgentStage;
    public bool IsStageZero => _currentAgentStage == 0;
    public bool IsReadOnlyMode => !IsEditingEnabled;

    private string _description;
    public string Description
    {
      get => _description;
      set
      {
        if (_description != value)
        {
          _description = value;
          OnPropertyChanged(nameof(Description));
        }
      }
    }

    public string CurrentAgentTitle => $"Стили реагирования Агента: {_currentAgentName ?? "Не определен"}";
    public ObservableCollection<GomeostasSystem.BehaviorStyle> BehaviorStyles { get; } = new ObservableCollection<GomeostasSystem.BehaviorStyle>();

    public ICommand SaveCommand { get; }
    public ICommand RemoveStyleCommand { get; }
    public ICommand RemoveAllCommand { get; }
    private ICommand _showMatrixCommand;
    public ICommand ShowMatrixCommand => _showMatrixCommand ?? (_showMatrixCommand = new RelayCommand(ShowAntagonistMatrix));

    public BehaviorStylesViewModel(GomeostasSystem gomeostas, List<GomeostasSystem.BehaviorStyle> currentStyles = null)
    {
      _gomeostas = gomeostas;
      SaveCommand = new RelayCommand(SaveData);
      RemoveStyleCommand = new RelayCommand(RemoveSelectedStyle);
      RemoveAllCommand = new RelayCommand(RemoveAllStyles);

      GlobalTimer.PulsationStateChanged += OnPulsationStateChanged;

      if (currentStyles != null)
        LoadAgentDataFromStyles(currentStyles);
      else
        LoadAgentData();
    }

    private void OnPulsationStateChanged()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        OnPropertyChanged(nameof(IsEditingEnabled));
        OnPropertyChanged(nameof(PulseWarningMessage));
        OnPropertyChanged(nameof(WarningMessageColor));
        OnPropertyChanged(nameof(IsReadOnlyMode));
      });
    }

    private void RefreshAllCollections()
    {
      var agentInfo = _gomeostas.GetAgentState();
      _currentAgentStage = agentInfo?.EvolutionStage ?? 0;
      _currentAgentName = agentInfo.Name;

      BehaviorStyles.Clear();

      foreach (var style in _gomeostas.GetAllBehaviorStyles().Values.OrderBy(s => s.Id))
      {
        BehaviorStyles.Add(new GomeostasSystem.BehaviorStyle
        {
          Id = style.Id,
          Name = style.Name,
          Description = style.Description,
          AntagonistStyles = style.AntagonistStyles
        });
      }

      OnPropertyChanged(nameof(IsStageZero));
      OnPropertyChanged(nameof(IsEditingEnabled));
      OnPropertyChanged(nameof(PulseWarningMessage));
      OnPropertyChanged(nameof(WarningMessageColor));
      OnPropertyChanged(nameof(CurrentAgentTitle));
      OnPropertyChanged(nameof(IsReadOnlyMode));
    }

    #region Блокировка страницы в зависимости от стажа

    public bool IsEditingEnabled => IsStageZero && !GlobalTimer.IsPulsationRunning;
    public string PulseWarningMessage =>
      !IsStageZero
        ? "[КРИТИЧНО] Редактирование параметров доступно только в стадии 0"
        : GlobalTimer.IsPulsationRunning
            ? "Редактирование параметров доступно только при выключенной пульсации"
            : string.Empty;
    public Brush WarningMessageColor =>
      !IsStageZero ? Brushes.Red :
      Brushes.Gray;

    #endregion

    private void ShowAntagonistMatrix(object parameter)
    {
      try
      {
        var matrixView = new AntagonistMatrixView();
        var currentStyles = BehaviorStyles.Select(bs => new GomeostasSystem.BehaviorStyle
        {
          Id = bs.Id,
          Name = bs.Name,
          Description = bs.Description,
          AntagonistStyles = bs.AntagonistStyles
        }).ToList();

        var matrixViewModel = new AntagonistMatrixViewModel(_gomeostas, currentStyles);
        matrixView.DataContext = matrixViewModel;

        var mainWindow = Application.Current.MainWindow as MainWindow;
        if (mainWindow?.DataContext is MainViewModel mainViewModel)
        {
          mainViewModel.CurrentContent = matrixView;
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка открытия матрицы антагонистов: {ex.Message}",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void LoadAgentDataFromStyles(List<GomeostasSystem.BehaviorStyle> styles)
    {
      try
      {
        var agentInfo = _gomeostas.GetAgentState();
        _currentAgentStage = agentInfo?.EvolutionStage ?? 0;
        _currentAgentName = agentInfo.Name;

        BehaviorStyles.Clear();

        // Используем переданные стили
        foreach (var style in styles.OrderBy(s => s.Id))
        {
          BehaviorStyles.Add(new GomeostasSystem.BehaviorStyle
          {
            Id = style.Id,
            Name = style.Name,
            Description = style.Description,
            AntagonistStyles = style.AntagonistStyles
          });
        }

        OnPropertyChanged(nameof(IsStageZero));
        OnPropertyChanged(nameof(IsEditingEnabled));
        OnPropertyChanged(nameof(PulseWarningMessage));
        OnPropertyChanged(nameof(WarningMessageColor));
        OnPropertyChanged(nameof(CurrentAgentTitle));
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка загрузки стилей: {ex.Message}", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void LoadAgentData()
    {
      try
      {
        RefreshAllCollections();
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка загрузки стилей: {ex.Message}", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void SaveData(object parameter)
    {
      try
      {
        if (!UpdateGomeostasStylesFromTable())
          return;

        var (success, error) = _gomeostas.SaveAgentBehaviorStyles();

        if (success)
        {
          MessageBox.Show("Стили поведения успешно сохранены",
              "Сохранение завершено",
              MessageBoxButton.OK,
              MessageBoxImage.Information);
          RefreshAllCollections();
          NotifyMatrixUpdate();
        }
        else
        {
          MessageBox.Show($"Не удалось сохранить стили поведения:\n{error}",
              "Ошибка сохранения",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Критическая ошибка при сохранении:\n{ex.Message}",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
      }
    }

    private bool UpdateGomeostasStylesFromTable()
    {
      bool needRevalidation = false;

      if (!_gomeostas.ValidateAgentBehaviorStyles(BehaviorStyles, out string errorMsg))
      {
        if (errorMsg.Contains("AsymmetricStyles"))
        {
          var asymmetricStyles = _gomeostas.FindAsymmetricStyles(BehaviorStyles);
          if (asymmetricStyles.Any())
          {
            var asymmetricList = string.Join(", ", asymmetricStyles.Select(s => $"{s.Name} (ID:{s.Id})"));

            var result = MessageBox.Show(
                $"Обнаружены асимметричные антагонистические связи:\n{asymmetricList}\n\n" +
                "Выберите действие:\n" +
                "• Да - автоматически исправить все связи\n" +
                "• Нет - сохранить без изменений\n" +
                "• Отмена - не сохранять",
                "Асимметричные антагонисты",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            switch (result)
            {
              case MessageBoxResult.Yes:
                int fixesCount = _gomeostas.FixAntagonistSymmetry(BehaviorStyles);
                MessageBox.Show($"Исправлено {fixesCount} асимметричных связей",
                    "Автокоррекция завершена",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                ApplyLocalStylesToGomeostas();
                RefreshAllCollections();

                needRevalidation = true;
                break;

              case MessageBoxResult.No:
                break;

              case MessageBoxResult.Cancel:
                return false;
            }
          }
        }
        else
        {
          MessageBox.Show($"Ошибка валидации стилей:\n{errorMsg}",
              "Ошибка сохранения",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
          return false;
        }
      }

      if (needRevalidation)
      {
        if (!_gomeostas.ValidateAgentBehaviorStyles(BehaviorStyles, out errorMsg))
        {
          MessageBox.Show($"Ошибка валидации после исправления асимметрии:\n{errorMsg}",
              "Ошибка сохранения",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
          return false;
        }
      }

      if (!needRevalidation)
        ApplyLocalStylesToGomeostas();

      return true;
    }

    /// <summary>
    /// Применяет изменения из локальной коллекции в систему гомеостаза
    /// </summary>
    private void ApplyLocalStylesToGomeostas()
    {
      var currentStyles = _gomeostas.GetAllBehaviorStyles();

      // Удаляем стили, которых нет в локальной коллекции
      var stylesToRemove = currentStyles.Keys.Except(BehaviorStyles.Select(s => s.Id)).ToList();
      foreach (var styleId in stylesToRemove)
      {
        _gomeostas.RemoveBehaviorStyle(styleId);
      }

      // Обновляем или добавляем стили
      foreach (var style in BehaviorStyles)
      {
        if (currentStyles.ContainsKey(style.Id))
        {
          // Обновляем существующий стиль
          var existingStyle = currentStyles[style.Id];
          existingStyle.Name = style.Name;
          existingStyle.Description = style.Description;
          existingStyle.AntagonistStyles = new List<int>(style.AntagonistStyles);
        }
        else
        {
          // Добавляем новый стиль
          var (newId, warnings) = _gomeostas.AddBehaviorStyle(
              style.Name,
              style.Description,
              new List<int>(style.AntagonistStyles));

          style.Id = newId;
        }
      }
    }

    public void RemoveSelectedStyle(object parameter)
    {
      if (parameter is GomeostasSystem.BehaviorStyle style)
      {
        try
        {
          if (!_gomeostas.ValidateAgentBehaviorStyles(new[] { style }, out string erroMsg, true))
          {
            MessageBox.Show($"Ошибка валидации стилей:\n{erroMsg}",
                "Ошибка сохранения",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
          }

          // Удаляем из локальной коллекции в любом случае (дефолтный валидация не даст удалить)
          if (BehaviorStyles.Contains(style)) 
            BehaviorStyles.Remove(style);

          // Удаляем из системы гомеостаза только если стиль уже сохранен (Id > 0)
          if (style.Id > 0)
          {
            if (!_gomeostas.RemoveBehaviorStyle(style.Id))
            {
              MessageBox.Show("Не удалось удалить стиль из системы", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
            }

            var (success, error) = _gomeostas.SaveAgentBehaviorStyles();
            if (!success)
            {
              MessageBox.Show($"Не удалось удалить стили поведения:\n{error}",
                  "Ошибка сохранения",
                  MessageBoxButton.OK,
                  MessageBoxImage.Error);
            }
            else
              RefreshAllCollections(); // чтобы обновились записи в таблице, после их чистки при удалении
          }
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Ошибка удаления стиля: {ex.Message}", "Ошибка",
              MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }
    }

    private void NotifyMatrixUpdate()
    {
      // Уведомляем матрицу об обновлении данных
      var mainWindow = Application.Current.MainWindow as MainWindow;
      if (mainWindow?.DataContext is MainViewModel mainViewModel && mainViewModel.CurrentContent is AntagonistMatrixView matrixView)
      {
        if (matrixView.DataContext is AntagonistMatrixViewModel matrixViewModel)
        {
          var currentStyles = BehaviorStyles.Select(bs => new GomeostasSystem.BehaviorStyle
          {
            Id = bs.Id,
            Name = bs.Name,
            Description = bs.Description,
            AntagonistStyles = bs.AntagonistStyles
          }).ToList();

          matrixViewModel.LoadMatrixFromStyles(currentStyles);
        }
      }
    }

    public void RemoveAllStyles(object parameter)
    {
      var result = MessageBox.Show(
          $"Вы действительно хотите удалить ВСЕ стили реагирования агента? Это действие нельзя будет отменить.",
          "Подтверждение удаления",
          MessageBoxButton.YesNo,
          MessageBoxImage.Warning);

      if (result == MessageBoxResult.Yes)
      {
        try
        {
          // Сохраняем дефолтный стиль
          var defaultStyle = BehaviorStyles.FirstOrDefault(style => style.Id == AppConfig.DefaultStileId);

          // Удаляем все стили из системы (кроме дефолтного)
          foreach (var style in BehaviorStyles.Where(style => style.Id != AppConfig.DefaultStileId))
          {
            _gomeostas.RemoveBehaviorStyle(style.Id);
          }

          // Очищаем и перезаполняем коллекцию только дефолтным стилем
          BehaviorStyles.Clear();
          if (defaultStyle != null)
            BehaviorStyles.Add(defaultStyle);

          var (success, error) = _gomeostas.SaveAgentBehaviorStyles(false); // все удалено - не надо валидаций 
          if (success)
          {
            MessageBox.Show("Все стили реагирования агента, кроме заданного по умолчанию, успешно удалены",
                "Удаление завершено",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
          }
          else
          {
            MessageBox.Show($"Не удалось удалить стили реагирования агента:\n{error}",
                "Ошибка сохранения после удаления",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
          }
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Ошибка удаления стилей реагирования агента: {ex.Message}",
              "Ошибка",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
        }
      }
    }

    public class DescriptionWithLink
    {
      public string Text { get; set; }
      public string LinkText { get; set; } = "Подробнее...";
      public string Url { get; set; } = "https://scorcher.ru/isida/iadaptive_agents_guide.php#ref_7";
      public ICommand OpenLinkCommand { get; }

      public DescriptionWithLink()
      {
        OpenLinkCommand = new RelayCommand(_ =>
        {
          try
          {
            Process.Start(new ProcessStartInfo(Url) { UseShellExecute = true });
          }
          catch { }
        });
      }
    }

    public DescriptionWithLink CurrentAgentDescription
    {
      get
      {
        return new DescriptionWithLink
        {
          Text = "Редактор стилей реагирования, которые служат контекстом для выполнения адаптивных действий."
        };
      }
    }

  }

}