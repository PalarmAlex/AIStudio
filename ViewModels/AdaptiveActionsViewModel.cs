using AIStudio.Common;
using ISIDA.Actions;
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
    private int _currentAgentStage;
    public bool IsStageZero => _currentAgentStage == 0;

    public bool IsReadOnlyMode => !IsEditingEnabled;
    public string CurrentAgentTitle => $"Адаптивные действия Агента: {_currentAgentName ?? "Не определен"}";
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

    public List<ParameterData> GetAllParameters()
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
        OnPropertyChanged(nameof(IsReadOnlyMode));
      });
    }

    private void RefreshAllCollections()
    {
      var agentInfo = _gomeostas.GetAgentState();
      _currentAgentStage = agentInfo?.EvolutionStage ?? 0;
      _currentAgentName = agentInfo.Name;

      AdaptiveActions.Clear();

      foreach (var action in _actionsSystem.GetAllAdaptiveActions().OrderBy(a => a.Id))
      {
        AdaptiveActions.Add(new AdaptiveActionsSystem.AdaptiveAction
        {
          Id = action.Id,
          Name = action.Name,
          Description = action.Description,
          Vigor = action.Vigor,
          AntagonistActions = new List<int>(action.AntagonistActions)
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
      bool needRevalidation = false;

      var (isValid, errors, validate_warnings) = _actionsSystem.ValidateAction(AdaptiveActions);
      if (!isValid)
      {
        if (errors.Contains("AsymmetricAction"))
        {
          var asymmetricActions = _actionsSystem.FindAsymmetricActions(AdaptiveActions);
          if (asymmetricActions.Any())
          {
            var asymmetricList = string.Join(", ", asymmetricActions.Select(s => $"{s.Name} (ID:{s.Id})"));

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
                int fixesCount = _actionsSystem.FixActionAntagonistSymmetry(AdaptiveActions);
                MessageBox.Show($"Исправлено {fixesCount} асимметричных связей",
                    "Автокоррекция завершена",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                ApplyLocalActionsToSystem();
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
          MessageBox.Show(errors,
              "Ошибки валидации",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
          return false;
        }
      }

      if (needRevalidation)
      {
        (isValid, errors, validate_warnings) = _actionsSystem.ValidateAction(AdaptiveActions);
        if (!isValid)
        {
          MessageBox.Show($"Ошибка валидации после исправления асимметрии:\n{errors}",
              "Ошибка сохранения",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
          return false;
        }
      }

      if (validate_warnings != "")
      {
        var resultMsg = MessageBox.Show(
            $"{validate_warnings}\n\nПродолжить сохранение?",
            "Предупреждения",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (resultMsg == MessageBoxResult.No)
          return false;
      }

      if (!needRevalidation)
        ApplyLocalActionsToSystem();

      return true;
    }

    /// <summary>
    /// Применяет изменения из локальной коллекции в систему адаптивных действий
    /// </summary>
    private void ApplyLocalActionsToSystem()
    {
      var currentActions = _actionsSystem.GetAllAdaptiveActions().ToDictionary(a => a.Id);
      var actionsToRemove = currentActions.Keys.Except(AdaptiveActions.Select(a => a.Id)).ToList();
      foreach (var actionId in actionsToRemove)
      {
        _actionsSystem.RemoveAction(actionId);
      }

      foreach (var action in AdaptiveActions)
      {
        if (currentActions.ContainsKey(action.Id))
        {
          var existingAction = currentActions[action.Id];
          existingAction.Name = action.Name;
          existingAction.Description = action.Description;
          existingAction.Vigor = action.Vigor;
          existingAction.AntagonistActions = new List<int>(action.AntagonistActions);
        }
        else
        {
          var (newId, warnings) = _actionsSystem.AddAction(
              action.Name,
              action.Description,
              new List<int>(action.AntagonistActions),
              false,
              action.Vigor
          );
          action.Id = newId;
        }
      }
    }

    public void RemoveSelectedAction(object parameter)
    {
      if (parameter is AdaptiveActionsSystem.AdaptiveAction action)
      {
        try
        {
          // Всегда удаляем из коллекции
          AdaptiveActions.Remove(action);

          var existingActions = _actionsSystem.GetAllAdaptiveActions().ToList();
          bool actionExistsInSystem = existingActions.Any(a => a.Id == action.Id);

          if (actionExistsInSystem)
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
              else
                RefreshAllCollections(); // чтобы обновились записи в таблице, после их чистки при удалении
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

    public class DescriptionWithLink
    {
      public string Text { get; set; }
      public string LinkText { get; set; } = "Подробнее...";
      public string Url { get; set; } = "https://scorcher.ru/isida/iadaptive_agents_guide.php#ref_6";
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
          Text = "Служит для создания адаптивных действий, которые могут активироваться в безусловных и условных рефлексах, а также в качестве базовой реакции на изменения состояний параметров гомеостаза. Представляет собой аналог врожденных базовых адаптивных действий живых организмов, передаваемых по наследству."
        };
      }
    }
  }
}
