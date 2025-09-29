using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using AIStudio.Common;
using ISIDA.Common;
using ISIDA.Gomeostas;

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
    private string _currentAgentDescription;
    private int _currentAgentStage;
    public bool IsStageZero => _currentAgentStage == 0;

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
    public string CurrentAgentDescription => _currentAgentDescription ?? "Нет описания";
    public ObservableCollection<GomeostasSystem.BehaviorStyle> BehaviorStyles { get; } = new ObservableCollection<GomeostasSystem.BehaviorStyle>();

    public ICommand SaveCommand { get; }
    public ICommand RemoveStyleCommand { get; }
    public ICommand RemoveAllCommand { get; }

    public BehaviorStylesViewModel(GomeostasSystem gomeostas)
    {
      _gomeostas = gomeostas;
      SaveCommand = new RelayCommand(SaveData);
      RemoveStyleCommand = new RelayCommand(RemoveSelectedStyle);
      RemoveAllCommand = new RelayCommand(RemoveAllStyles);

      GlobalTimer.PulsationStateChanged += OnPulsationStateChanged;
      LoadAgentData();
    }

    private void OnPulsationStateChanged()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        OnPropertyChanged(nameof(IsEditingEnabled));
        OnPropertyChanged(nameof(PulseWarningMessage));
        OnPropertyChanged(nameof(WarningMessageColor));
      });
    }

    private void RefreshAllCollections()
    {
      var agentInfo = _gomeostas.GetAgentState();
      _currentAgentStage = agentInfo?.EvolutionStage ?? 0;
      _currentAgentDescription = agentInfo.Description;
      _currentAgentName = agentInfo.Name;

      BehaviorStyles.Clear();

      foreach (var style in _gomeostas.GetAllBehaviorStyles().Values.OrderBy(s => s.Id))
      {
        BehaviorStyles.Add(new GomeostasSystem.BehaviorStyle
        {
          Id = style.Id,
          Name = style.Name,
          Description = style.Description,
          Weight = style.Weight,
          AntagonistStyles = style.AntagonistStyles,
          StileActionInfluence = style.StileActionInfluence
        });
      }

      OnPropertyChanged(nameof(IsStageZero));
      OnPropertyChanged(nameof(IsEditingEnabled));
      OnPropertyChanged(nameof(PulseWarningMessage));
      OnPropertyChanged(nameof(WarningMessageColor));
      OnPropertyChanged(nameof(CurrentAgentDescription));
      OnPropertyChanged(nameof(CurrentAgentTitle));
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
      if (!_gomeostas.ValidateAgentBehaviorStyles(BehaviorStyles, out string erroMsg))
      {
        MessageBox.Show($"Ошибка валидации стилей:\n{erroMsg}",
            "Ошибка сохранения",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        return false;
      }
      
      // Получаем текущие стили из гомеостаза
      var currentStyles = _gomeostas.GetAllBehaviorStyles();

      // Удаляем стили, которых нет в таблице
      var stylesToRemove = currentStyles.Keys.Except(BehaviorStyles.Select(s => s.Id)).ToList();
      foreach (var styleId in stylesToRemove)
      {
        _gomeostas.RemoveBehaviorStyle(styleId);
      }

      // Добавляем/обновляем стили из таблицы
      foreach (var style in BehaviorStyles)
      {
        if (currentStyles.ContainsKey(style.Id))
        {
          // Обновляем существующий стиль
          var existingStyle = currentStyles[style.Id];
          existingStyle.Name = style.Name;
          existingStyle.Description = style.Description;
          existingStyle.Weight = style.Weight;
          existingStyle.AntagonistStyles = style.AntagonistStyles;
          existingStyle.StileActionInfluence = style.StileActionInfluence;
        }
        else
        {
          // Добавляем новый стиль
          var (newId, warnings) = _gomeostas.AddBehaviorStyle(
              style.Name,
              style.Description,
              style.Weight,
              style.AntagonistStyles,
              style.StileActionInfluence);

          style.Id = newId;
        }
      }
      return true;
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
          }
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Ошибка удаления стиля: {ex.Message}", "Ошибка",
              MessageBoxButton.OK, MessageBoxImage.Error);
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

  }

}