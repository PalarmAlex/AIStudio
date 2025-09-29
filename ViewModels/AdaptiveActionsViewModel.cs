using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using AIStudio.Common;
using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Gomeostas;

namespace AIStudio.ViewModels
{
  public class AdaptiveActionsViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly AdaptiveActionsSystem _actionsSystem;
    private readonly GomeostasSystem _gomeostas;
    private string _currentAgentName;
    private string _currentAgentDescription;
    private int _currentAgentStage;
    public bool IsStageZero => _currentAgentStage == 0;

    public string CurrentAgentTitle => $"Адаптивные действия Агента: {_currentAgentName ?? "Не определен"}";
    public string CurrentAgentDescription => _currentAgentDescription ?? "Нет описания";
    public ObservableCollection<AdaptiveActionsSystem.AdaptiveAction> AdaptiveActions { get; } = new ObservableCollection<AdaptiveActionsSystem.AdaptiveAction>();

    public ICommand SaveCommand { get; }
    public ICommand RemoveActionCommand { get; }
    public ICommand RemoveAllCommand { get; }

    public AdaptiveActionsViewModel(GomeostasSystem gomeostas, AdaptiveActionsSystem actionsSystem)
    {
      _gomeostas = gomeostas;
      _actionsSystem = actionsSystem ?? throw new ArgumentNullException(nameof(actionsSystem));

      SaveCommand = new RelayCommand(SaveData);
      RemoveActionCommand = new RelayCommand(RemoveSelectedAction);
      RemoveAllCommand = new RelayCommand(RemoveAlldAction);
      GlobalTimer.PulsationStateChanged += OnPulsationStateChanged;
      LoadAgentData();
    }

    public List<GomeostasSystem.ParameterData> GetAllParameters()
    {
      return _gomeostas.GetAllParameters().ToList();
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

      AdaptiveActions.Clear();

      foreach (var action in _actionsSystem.GetAllAdaptiveActions().OrderBy(a => a.Id))
      {
        AdaptiveActions.Add(new AdaptiveActionsSystem.AdaptiveAction
        {
          Id = action.Id,
          Name = action.Name,
          Description = action.Description,
          IsElementary = action.IsElementary,
          Vigor = action.Vigor,
          Influences = new Dictionary<int, int>(action.Influences),
          Costs = new Dictionary<int, int>(action.Costs),
          AntagonistActions = new List<int>(action.AntagonistActions)
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
        MessageBox.Show($"Ошибка загрузки действий: {ex.Message}", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void SaveData(object parameter)
    {
      try
      {
        if (!UpdateActionsSystemFromTable())
          return;

        try
        {
          var (success, error) = _actionsSystem.SaveActions(false);
          if (success)
          {
            RefreshAllCollections();
            MessageBox.Show("Адаптивные действия успешно сохранены",
                "Сохранение завершено",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
          }
          else
          {
            MessageBox.Show($"Не удалось сохранить адаптивные действия:\n{error}",
                "Ошибка сохранения",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
          }
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Не удалось сохранить действия:\n{ex.Message}",
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

    private bool UpdateActionsSystemFromTable()
    {
      if (!_actionsSystem.ValidateAction(AdaptiveActions, out string erroMsg))
      {
        MessageBox.Show($"Ошибка валидации адаптивных действий:\n{erroMsg}",
            "Ошибка сохранения",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        return false;
      }

      // Получаем текущие действия из системы
      var currentActions = _actionsSystem.GetAllAdaptiveActions().ToDictionary(a => a.Id);

      // Удаляем действия, которых нет в таблице
      var actionsToRemove = currentActions.Keys.Except(AdaptiveActions.Select(a => a.Id)).ToList();
      foreach (var actionId in actionsToRemove)
      {
        _actionsSystem.RemoveAction(actionId);
      }

      // Добавляем/обновляем действия из таблицы
      foreach (var action in AdaptiveActions)
      {
        if (currentActions.ContainsKey(action.Id))
        {
          // Обновляем существующее действие
          var existingAction = currentActions[action.Id];
          existingAction.Name = action.Name;
          existingAction.Description = action.Description;
          existingAction.IsElementary = action.IsElementary;
          existingAction.Vigor = action.Vigor;
          existingAction.Influences = new Dictionary<int, int>(action.Influences);
          existingAction.Costs = new Dictionary<int, int>(action.Costs);
          existingAction.AntagonistActions = new List<int>(action.AntagonistActions);
        }
        else
        {
          // Добавляем новое действие
          var (newId, warnings) = _actionsSystem.AddAction(
              action.Name,
              action.Description,
              new Dictionary<int, int>(action.Influences),
              new Dictionary<int, int>(action.Costs),
              new List<int>(action.AntagonistActions),
              false,
              action.Vigor,
              action.IsElementary
          );
          action.Id = newId;
        }
      }
      return true;
    }

    public void RemoveSelectedAction(object parameter)
    {
      if (parameter is AdaptiveActionsSystem.AdaptiveAction action)
      {
        try
        {
          if(action.Id > 0)
          {
            if (_actionsSystem.RemoveAction(action.Id))
            {
              AdaptiveActions.Remove(action);

              var (success, error) = _actionsSystem.SaveActions();
              if (!success)
              {
                MessageBox.Show($"Не удалось удалить действия:\n{error}",
                    "Ошибка сохранения после удаления",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
              }
            }
            else
              MessageBox.Show("Не удалось удалить действие", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
          }
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Ошибка удаления действия: {ex.Message}", "Ошибка",
              MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }
    }

    public void RemoveAlldAction(object parameter)
    {
      var result = MessageBox.Show(
          $"Вы действительно хотите удалить ВСЕ адаптивные действия? Это действие нельзя будет отменить.",
          "Подтверждение удаления",
          MessageBoxButton.YesNo,
          MessageBoxImage.Warning);

      if (result == MessageBoxResult.Yes)
      {
        try
        {
          var defaultAction = AdaptiveActions.FirstOrDefault(action => action.Id == AppConfig.DefaultAdaptiveActionId);
          var allActions = _actionsSystem.GetAllAdaptiveActions().ToList();

          foreach (var action in allActions.Where(action => action.Id != AppConfig.DefaultAdaptiveActionId))
          {
            _actionsSystem.RemoveAction(action.Id);
          }

          AdaptiveActions.Clear();
          if (defaultAction != null)
            AdaptiveActions.Add(defaultAction);

          var (success, error) = _actionsSystem.SaveActions(false); // все удалено - не надо валидаций 
          if (success)
          {
            MessageBox.Show("Все адаптивные действия,кроме заданного по умолчанию, успешно удалены",
                "Удаление завершено",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
          }
          else
          {
            MessageBox.Show($"Не удалось удалить адаптивные действия:\n{error}",
                "Ошибка сохранения после удаления",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
          }
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Ошибка удаления адаптивных действий: {ex.Message}",
              "Ошибка",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
        }
      }
    }
  }
}
