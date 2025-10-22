using AIStudio.Common;
using ISIDA.Common;
using ISIDA.Gomeostas;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static ISIDA.Gomeostas.GomeostasSystem;

namespace AIStudio.ViewModels
{
  public class AgentViewModel : INotifyPropertyChanged
  {
    private readonly GomeostasSystem _gomeostas;
    private ICommand _changeStageCommand;
    private ICommand _updateCommand;
    public ICommand UpdateCommand => _updateCommand ?? (_updateCommand = new RelayCommand(_ => UpdateAgentProperties(), _ => !IsAgentDead));
    public ICommand ChangeStageCommand => _changeStageCommand ?? (_changeStageCommand = new RelayCommand(ChangeStage, _ => !IsAgentDead));
    private bool _disposed = false;
    private int _previousStage;
    private bool _isAgentDead;

    public ObservableCollection<AgentProperty> AgentProperties { get; }
    public ObservableCollection<int> AvailableStages { get; } = new ObservableCollection<int> { 0, 1, 2, 3, 4, 5 };

    private string _agentName;
    public string AgentName
    {
      get => _agentName;
      set
      {
        _agentName = value;
        OnPropertyChanged(nameof(AgentName));
      }
    }

    public string AgentBaseSost => IsAgentDead
        ? "АГЕНТ МЕРТВ"
        : $"Жизненные параметры агента. Состояние: {HomeostasisStatus}";

    private Brush _headerBackground;
    public Brush HeaderBackground
    {
      get => _headerBackground;
      set
      {
        _headerBackground = value;
        OnPropertyChanged(nameof(HeaderBackground));
      }
    }

    private Brush _textForeground;
    public Brush TextForeground
    {
      get => _textForeground;
      set
      {
        _textForeground = value;
        OnPropertyChanged(nameof(TextForeground));
      }
    }

    public bool IsAgentDead
    {
      get => _isAgentDead;
      set
      {
        if (_isAgentDead != value)
        {
          _isAgentDead = value;
          OnPropertyChanged(nameof(IsAgentDead));
          OnPropertyChanged(nameof(IsStageComboEnabled));
          OnPropertyChanged(nameof(IsStageSelectionEnabled));
          OnPropertyChanged(nameof(IsEditingEnabled));
          OnPropertyChanged(nameof(PulseWarningMessage));
          OnPropertyChanged(nameof(AgentBaseSost));
          OnPropertyChanged(nameof(IsAnyControlEnabled));

          // Обновляем команды
          (_updateCommand as RelayCommand)?.RaiseCanExecuteChanged();
          (_changeStageCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
      }
    }

    public bool IsAnyControlEnabled => !IsAgentDead && !GlobalTimer.IsPulsationRunning;
    public bool IsStageComboEnabled => IsAnyControlEnabled;
    public bool IsStageSelectionEnabled => IsAnyControlEnabled;
    public bool IsEditingEnabled => IsAnyControlEnabled;

    private Brush _warningMessageColor;
    public Brush WarningMessageColor
    {
      get => _warningMessageColor;
      set
      {
        _warningMessageColor = value;
        OnPropertyChanged(nameof(WarningMessageColor));
      }
    }

    private string _pulseWarningMessage;
    public string PulseWarningMessage
    {
      get => _pulseWarningMessage;
      set
      {
        _pulseWarningMessage = value;
        OnPropertyChanged(nameof(PulseWarningMessage));
      }
    }

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

          if (AgentProperties.Count > 0 && !IsAgentDead)
          {
            AgentName = $"{AgentProperties[0].Value}. Стадия развития: {value}";
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
        OnPropertyChanged(nameof(AgentAdaptiveActionsViewModel));
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
          OnPropertyChanged(nameof(HomeostasisStatus));
          OnPropertyChanged(nameof(HomeostasisStatusColor));
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

    public string HomeostasisStatus => IsAgentDead
        ? "СМЕРТЬ"
        : AppConfig.GetBaseStateDisplay((int)(CurrentHomeostasisState?.OverallState ?? HomeostasisOverallState.Normal));

    public Brush HomeostasisStatusColor => IsAgentDead
        ? Brushes.DarkRed
        : AppConfig.GetBaseStateColor((int)(CurrentHomeostasisState?.OverallState ?? HomeostasisOverallState.Normal));

    public AgentViewModel(GomeostasSystem gomeostas)
    {
      _gomeostas = gomeostas;
      _previousStage = _gomeostas.GetAgentState().EvolutionStage;
      AgentProperties = new ObservableCollection<AgentProperty>();
      ParametersViewModel = new AgentParametersViewModel(_gomeostas);
      BehaviorStylesViewModel = new AgentBehaviorStylesViewModel(_gomeostas);
      AgentAdaptiveActionsViewModel = new AgentAdaptiveActionsViewModel();
      AgentPultViewModel = new AgentPultViewModel();

      // Инициализация цветов
      TextForeground = Brushes.Black;
      WarningMessageColor = Brushes.Transparent;

      GlobalTimer.PulsationStateChanged += OnPulsationStateChanged;
      GlobalTimer.OnPulseStateChanged += isActive =>
      {
        if (isActive)
          OnPulseStateChanged(isActive);
      };

      LoadAgentData();
      UpdateEditableProperties();
      UpdateWarningMessage();
    }

    private void UpdateAgentProperties()
    {
      if (IsAgentDead)
      {
        MessageBox.Show("Невозможно изменить свойства мертвого агента",
            "Агент мертв",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        return;
      }

      try
      {
        _gomeostas.SetAgentName(AgentProperties[0].Value);
        _gomeostas.SetAgentDescription(AgentProperties[1].Value);
        _gomeostas.SetEvolutionStage(SelectedStage);

        var (success, error) = _gomeostas.SaveAgentProperties();

        if (success)
        {
          AgentName = $"{AgentProperties[0].Value}. Стадия развития: {SelectedStage}";

          var result = MessageBox.Show("Изменения успешно сохранены.\n" +
              "Сохранить так же значения параметров?",
              "Подтверждение сохранения значений параметров",
              MessageBoxButton.YesNo,
              MessageBoxImage.Question);

          if (result == MessageBoxResult.Yes)
          {
            (success, error) = _gomeostas.SaveAgentParameters(false);
            if (success)
            {
              MessageBox.Show("Значения параметров успешно сохранены",
                  "Сохранение",
                  MessageBoxButton.OK,
                  MessageBoxImage.Information);
            }
            else
            {
              MessageBox.Show($"Не удалось сохранить значения параметров:\n{error}",
                  "Ошибка сохранения",
                  MessageBoxButton.OK,
                  MessageBoxImage.Error);
            }
          }
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

    private void UpdateWarningMessage()
    {
      if (IsAgentDead)
      {
        PulseWarningMessage = "АГЕНТ МЕРТВ - все операции заблокированы";
        WarningMessageColor = Brushes.DarkRed;
      }
      else if (GlobalTimer.IsPulsationRunning)
      {
        PulseWarningMessage = "Редактирование свойств недоступно во время пульсации";
        WarningMessageColor = Brushes.Gray;
      }
      else
      {
        PulseWarningMessage = string.Empty;
        WarningMessageColor = Brushes.Transparent;
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
        UpdateWarningMessage();
        OnPropertyChanged(nameof(IsEditingEnabled));
        OnPropertyChanged(nameof(IsStageComboEnabled));
        OnPropertyChanged(nameof(IsStageSelectionEnabled));
        OnPropertyChanged(nameof(IsAnyControlEnabled));
        UpdateEditableProperties();
      });
    }

    private void LoadAgentData()
    {
      try
      {
        var agentInfo = _gomeostas.GetAgentState();
        if (agentInfo == null) return;

        IsAgentDead = agentInfo.IsDead;

        if (IsAgentDead)
        {
          // Устанавливаем визуальные индикации смерти
          HeaderBackground = Brushes.Black;
          TextForeground = Brushes.DarkRed;
          AgentName = $"{agentInfo.Name} - МЕРТВ";

          // Блокируем все свойства
          foreach (var prop in AgentProperties)
          {
            prop.IsEditable = false;
          }
        }
        else
        {
          _previousStage = SelectedStage;
          SelectedStage = agentInfo.EvolutionStage;
          AgentName = $"{agentInfo.Name}. Стадия развития: {SelectedStage}";

          // Нормальные цвета в зависимости от состояния
          HeaderBackground = agentInfo.IsSleeping ? Brushes.DodgerBlue : Brushes.ForestGreen;
          TextForeground = Brushes.Black;
        }

        // Обновляем свойства агента
        if (AgentProperties.Count == 0)
        {
          AgentProperties.Add(new AgentProperty("Имя", agentInfo.Name, !IsAgentDead));
          AgentProperties.Add(new AgentProperty("Описание", agentInfo.Description, !IsAgentDead));
          AgentProperties.Add(new AgentProperty("Стадия", agentInfo.EvolutionStage.ToString(), !IsAgentDead));
        }
        else
        {
          AgentProperties[0].Value = agentInfo.Name;
          AgentProperties[1].Value = agentInfo.Description;
          AgentProperties[2].Value = agentInfo.EvolutionStage.ToString();

          // Обновляем редактируемость только если агент жив
          if (!IsAgentDead)
          {
            foreach (var prop in AgentProperties)
            {
              prop.IsEditable = !GlobalTimer.IsPulsationRunning;
            }
          }
        }

        var activeActions = _gomeostas.GetActiveAdaptiveActionsList();
        AgentAdaptiveActionsViewModel.UpdateActions(activeActions);

        // Обновляем состояние гомеостаза
        CurrentHomeostasisState = new AgentHomeostasisState
        {
          OverallState = agentInfo.OverallState,
        };

        UpdateWarningMessage();

        OnPropertyChanged(nameof(HeaderBackground));
        OnPropertyChanged(nameof(TextForeground));
        OnPropertyChanged(nameof(AgentName));
        OnPropertyChanged(nameof(AgentBaseSost));
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
      bool isEditable = !IsAgentDead && !GlobalTimer.IsPulsationRunning;

      foreach (var prop in AgentProperties)
      {
        prop.IsEditable = isEditable;
      }

      UpdateWarningMessage();
      OnPropertyChanged(nameof(IsStageSelectionEnabled));
      OnPropertyChanged(nameof(IsEditingEnabled));
      OnPropertyChanged(nameof(IsAnyControlEnabled));
    }

    private void ChangeStage(object stageObj)
    {
      if (IsAgentDead)
      {
        MessageBox.Show("Невозможно изменить стадию мертвого агента",
            "Агент мертв",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        return;
      }

      if (!IsStageSelectionEnabled || GlobalTimer.IsPulsationRunning)
        return;

      if (stageObj is int newStage)
      {
        int currentStage = SelectedStage;
        if (newStage == currentStage) return;

        try
        {
          var result = _gomeostas.SetEvolutionStageWithValidation(newStage);

          if (result.Success)
          {
            var dialogResult = MessageBox.Show("Вы собираетесь перейти на следующую стадию?", "Подтверждение смены стадии",
              MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if(dialogResult == MessageBoxResult.Yes)
            {
              SelectedStage = newStage;
              LoadAgentData();
              _previousStage = currentStage;

              var (success, error) = _gomeostas.SaveAgentProperties();
              if (!success)
                MessageBox.Show(error, "Ошибка сохранения стадии",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
              throw new InvalidOperationException("Переход на следующую стадию отменен");
            }
          }
          else
          {
            if (result.Message.Contains("Продолжить?"))
            {
              var dialogResult = MessageBox.Show(result.Message, "Подтверждение возврата",
                  MessageBoxButton.YesNo, MessageBoxImage.Warning);

              if (dialogResult == MessageBoxResult.Yes)
              {
                var forceResult = _gomeostas.SetEvolutionStageWithValidation(newStage, true);

                if (forceResult.Success)
                {
                  SelectedStage = newStage;
                  LoadAgentData();
                  _previousStage = currentStage;

                  MessageBox.Show($"Успешно возвращены на стадию {newStage}. Данные последующих стадий очищены.",
                      "Возврат выполнен", MessageBoxButton.OK, MessageBoxImage.Information);

                  var (success, error) = _gomeostas.SaveAgentProperties();
                  if (!success)
                    MessageBox.Show(error, "Ошибка сохранения стадии",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
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