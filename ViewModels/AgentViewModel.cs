using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using AIStudio.Common;
using ISIDA.Common;
using ISIDA.Gomeostas;
using static ISIDA.Gomeostas.GomeostasSystem;

namespace AIStudio.ViewModels
{
  public class AgentViewModel : INotifyPropertyChanged
  {
    private readonly GomeostasSystem _gomeostas;
    private ICommand _changeStageCommand;
    private ICommand _updateCommand;
    public ICommand UpdateCommand => _updateCommand ?? (_updateCommand = new RelayCommand(_ => UpdateAgentProperties()));
    public ICommand ChangeStageCommand => _changeStageCommand ?? (_changeStageCommand = new RelayCommand(ChangeStage));
    private bool _disposed = false;
    private int _previousStage;

    public ObservableCollection<AgentProperty> AgentProperties { get; }
    public ObservableCollection<int> AvailableStages { get; } = new ObservableCollection<int> { 0, 1, 2, 3, 4, 5 };
    public string AgentName { get; set; }
    public string AgentBaseSost => $"Жизненные параметры агента. Состояние: {HomeostasisStatus}";
    public Brush HeaderBackground { get; set; }
    public bool IsStageComboEnabled => !GlobalTimer.IsPulsationRunning;
    public bool IsStageSelectionEnabled => !GlobalTimer.IsPulsationRunning;
    public bool IsEditingEnabled => !GlobalTimer.IsPulsationRunning;
    public Brush WarningMessageColor => GlobalTimer.IsPulsationRunning ? Brushes.Gray : Brushes.Transparent;
    public string PulseWarningMessage => GlobalTimer.IsPulsationRunning
        ? "Редактирование свойств недоступно во время пульсации"
        : string.Empty;

    private int _selectedStage;
    public int SelectedStage
    {
      get => _selectedStage;
      set
      {
        if (_selectedStage != value)
        {
          _selectedStage = value;
          OnPropertyChanged(nameof(SelectedStage));
          OnPropertyChanged(nameof(IsEditingEnabled));
          OnPropertyChanged(nameof(PulseWarningMessage));
          OnPropertyChanged(nameof(WarningMessageColor));

          if (AgentProperties.Count > 0)
          {
            AgentName = $"{AgentProperties[0].Value}. Стадия развития: {value}";
            OnPropertyChanged(nameof(AgentName));
          }

          UpdateEditableProperties();
        }
      }
    }

    private AgentAdaptiveActionsViewModel _agentAdaptiveActionsViewModel;
    public AgentAdaptiveActionsViewModel AgentAdaptiveActionsViewModel
    {
      get => _agentAdaptiveActionsViewModel;
      set
      {
        _agentAdaptiveActionsViewModel = value;
        OnPropertyChanged(nameof(AdaptiveActionsViewModel));
      }
    }

    private AgentParametersViewModel _parametersViewModel;
    public AgentParametersViewModel ParametersViewModel
    {
      get => _parametersViewModel;
      set
      {
        _parametersViewModel = value;
        OnPropertyChanged(nameof(ParametersViewModel));
      }
    }

    private AgentHomeostasisState _currentHomeostasisState;
    public AgentHomeostasisState CurrentHomeostasisState
    {
      get => _currentHomeostasisState;
      set
      {
        if (_currentHomeostasisState != value)
        {
          _currentHomeostasisState = value;
          OnPropertyChanged(nameof(CurrentHomeostasisState));
          OnPropertyChanged(nameof(HomeostasisStatus)); // Для отображения в UI
          OnPropertyChanged(nameof(HomeostasisStatusColor)); // Для цветовой индикации
        }
      }
    }

    private AgentBehaviorStylesViewModel _behaviorStylesViewModel;
    public AgentBehaviorStylesViewModel BehaviorStylesViewModel
    {
      get => _behaviorStylesViewModel;
      set
      {
        _behaviorStylesViewModel = value;
        OnPropertyChanged(nameof(BehaviorStylesViewModel));
      }
    }

    private AgentPultViewModel _agentPultViewModel;
    public AgentPultViewModel AgentPultViewModel
    {
      get => _agentPultViewModel;
      set
      {
        _agentPultViewModel = value;
        OnPropertyChanged(nameof(AgentPultViewModel));
      }
    }
    public string HomeostasisStatus =>
      AppConfig.GetBaseStateDisplay((int)(CurrentHomeostasisState?.OverallState ?? HomeostasisOverallState.Normal));
    public Brush HomeostasisStatusColor =>
      AppConfig.GetBaseStateColor((int)(CurrentHomeostasisState?.OverallState ?? HomeostasisOverallState.Normal));

    public AgentViewModel(GomeostasSystem gomeostas)
    {
      _gomeostas = gomeostas;
      _previousStage = _gomeostas.GetAgentState().EvolutionStage;
      AgentProperties = new ObservableCollection<AgentProperty>();
      ParametersViewModel = new AgentParametersViewModel(_gomeostas);
      BehaviorStylesViewModel = new AgentBehaviorStylesViewModel(_gomeostas);
      AgentAdaptiveActionsViewModel = new AgentAdaptiveActionsViewModel();
      AgentPultViewModel = new AgentPultViewModel();

      GlobalTimer.PulsationStateChanged += OnPulsationStateChanged;
      GlobalTimer.OnPulseStateChanged += isActive =>
      {
        if (isActive) // Реагируем только на true (старт пульса)
          OnPulseStateChanged(isActive);
      };

      LoadAgentData();
      UpdateEditableProperties();
    }

    private void UpdateAgentProperties()
    {
      try
      {
        _gomeostas.SetAgentName(AgentProperties[0].Value);
        _gomeostas.SetAgentDescription(AgentProperties[1].Value);
        _gomeostas.SetEvolutionStage(SelectedStage);

        var (success, error) = _gomeostas.SaveAgentProperties();

        if (success)
        {
          AgentName = $"{AgentProperties[0].Value}. Стадия развития: {SelectedStage}";
          OnPropertyChanged(nameof(AgentName));

          MessageBox.Show("Изменения сохранены успешно",
              "Сохранение",
              MessageBoxButton.OK,
              MessageBoxImage.Information);
        }
        else
        {
          MessageBox.Show($"Не удалось сохранить свойства агента:\n{error}",
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

    private void OnPulseStateChanged(bool isPulseActive)
    {
      Application.Current.Dispatcher.Invoke(LoadAgentData);
    }

    private void OnPulsationStateChanged()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        OnPropertyChanged(nameof(IsEditingEnabled));
        OnPropertyChanged(nameof(IsStageComboEnabled));
        OnPropertyChanged(nameof(PulseWarningMessage));
        OnPropertyChanged(nameof(WarningMessageColor));
        OnPropertyChanged(nameof(IsStageSelectionEnabled));
        UpdateEditableProperties();
      });
    }

    private void LoadAgentData()
    {
      try
      {
        var agentInfo = _gomeostas.GetAgentState();
        if (agentInfo == null) return;

        _previousStage = SelectedStage;
        SelectedStage = agentInfo.EvolutionStage;
        AgentName = $"{agentInfo.Name}. Стадия развития: {SelectedStage}";
        HeaderBackground = agentInfo.IsDead ? Brushes.Black :
                agentInfo.IsSleeping ? Brushes.DodgerBlue : Brushes.ForestGreen;

        // Обновляем свойства агента
        if (AgentProperties.Count == 0)
        {
          AgentProperties.Add(new AgentProperty("Имя", agentInfo.Name, true));
          AgentProperties.Add(new AgentProperty("Описание", agentInfo.Description, true));
          AgentProperties.Add(new AgentProperty("Стадия", agentInfo.EvolutionStage.ToString(), true));
        }
        else
        {
          AgentProperties[0].Value = agentInfo.Name;
          AgentProperties[1].Value = agentInfo.Description;
          AgentProperties[2].Value = agentInfo.EvolutionStage.ToString();
        }

        var activeActions = _gomeostas.GetActiveAdaptiveActionsList();
        AgentAdaptiveActionsViewModel.UpdateActions(activeActions);

        // Обновляем состояние гомеостаза
        CurrentHomeostasisState = new AgentHomeostasisState
        {
          OverallState = agentInfo.OverallState,
        };

        OnPropertyChanged(nameof(HeaderBackground));
        OnPropertyChanged(nameof(AgentName));
        OnPropertyChanged(nameof(AgentBaseSost));
        OnPropertyChanged(nameof(IsStageComboEnabled));
        OnPropertyChanged(nameof(WarningMessageColor));
        OnPropertyChanged(nameof(HomeostasisStatus));
        OnPropertyChanged(nameof(HomeostasisStatusColor));

        UpdateEditableProperties();

        BehaviorStylesViewModel.LoadBehaviorStyles();
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Ошибка при загрузке данных агента: {ex.Message}");
      }
    }

    public void UpdateEditableProperties()
    {
      bool isPulsationRunning = GlobalTimer.IsPulsationRunning;

      // Блокируем все поля при пульсации
      foreach (var prop in AgentProperties)
      {
        prop.IsEditable = !isPulsationRunning;
      }

      OnPropertyChanged(nameof(IsStageSelectionEnabled));
      OnPropertyChanged(nameof(IsEditingEnabled));
      OnPropertyChanged(nameof(PulseWarningMessage));
      OnPropertyChanged(nameof(WarningMessageColor));
    }

    private void ChangeStage(object stageObj)
    {
      if (!IsStageSelectionEnabled || GlobalTimer.IsPulsationRunning)
        return;

      if (stageObj is int newStage)
      {
        int currentStage = SelectedStage;
        if (newStage == currentStage) return;

        try
        {
          // Первый вызов - без флага force
          var result = _gomeostas.SetEvolutionStageWithValidation(newStage);

          if (result.Success)
          {
            // Обычный переход вперед
            SelectedStage = newStage;
            LoadAgentData();
            _previousStage = currentStage;
          }
          else
          {
            // Проверяем, является ли это запросом подтверждения
            if (result.Message.Contains("Продолжить?"))
            {
              // Запрос подтверждения для обратного перехода
              var dialogResult = MessageBox.Show(result.Message, "Подтверждение возврата",
                  MessageBoxButton.YesNo, MessageBoxImage.Warning);

              if (dialogResult == MessageBoxResult.Yes)
              {
                // Рекурсивный вызов с флагом force
                var forceResult = _gomeostas.SetEvolutionStageWithValidation(newStage, true);

                if (forceResult.Success)
                {
                  SelectedStage = newStage;
                  LoadAgentData();
                  _previousStage = currentStage;

                  MessageBox.Show($"Успешно возвращены на стадию {newStage}. Данные последующих стадий очищены.",
                      "Возврат выполнен", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                  throw new InvalidOperationException(forceResult.Message);
                }
              }
              else
              {
                throw new InvalidOperationException("Возврат на предыдущую стадию отменен");
              }
            }
            else
            {
              // Обычная ошибка (прыжок вперед через стадию)
              MessageBox.Show(result.Message, "Ошибка перехода",
                  MessageBoxButton.OK, MessageBoxImage.Warning);

              throw new InvalidOperationException(result.Message);
            }
          }
        }
        catch (Exception ex) when (ex is InvalidOperationException)
        {
          throw;
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Ошибка при изменении стадии: {ex.Message}",
              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
          throw new InvalidOperationException("Stage change failed", ex);
        }
      }
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (_disposed) return;

      if (disposing)
      {
        GlobalTimer.PulsationStateChanged -= OnPulsationStateChanged;
        GlobalTimer.OnPulseStateChanged -= OnPulseStateChanged;
      }

      _disposed = true;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }

  public class AgentProperty : INotifyPropertyChanged
  {
    private string _value;
    private bool _isEditable;

    public string Name { get; }
    public bool IsEditable
    {
      get => _isEditable;
      set
      {
        if (_isEditable != value)
        {
          _isEditable = value;
          OnPropertyChanged(nameof(IsEditable));
        }
      }
    }

    public string Value
    {
      get => _value;
      set
      {
        if (_value != value)
        {
          _value = value;
          OnPropertyChanged(nameof(Value));
        }
      }
    }

    public AgentProperty(string name, string value, bool isEditable)
    {
      Name = name;
      Value = value;
      IsEditable = isEditable;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
}