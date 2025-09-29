﻿using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Gomeostas;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace AIStudio.ViewModels
{
  public class ExterInalInfluencesViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly GomeostasSystem _gomeostas;
    private readonly InfluenceActionSystem _influenceActionSystem;
    private string _currentAgentName;
    private string _currentAgentDescription;
    private int _currentAgentStage;
    public bool IsStageZero => _currentAgentStage == 0;

    public ObservableCollection<InfluenceActionSystem.GomeostasisInfluenceAction> InfluenceActions { get; } = new ObservableCollection<InfluenceActionSystem.GomeostasisInfluenceAction>();

    public string CurrentAgentTitle => $"Внешние воздействия на Агента: {_currentAgentName ?? "Не определен"}";
    public string CurrentAgentDescription => _currentAgentDescription ?? "Нет описания";

    public ICommand SaveCommand { get; }
    public ICommand RemoveActionCommand { get; }
    public ICommand RemoveAllCommand { get; }

    public ExterInalInfluencesViewModel(GomeostasSystem gomeostas, InfluenceActionSystem influence)
    {
      _gomeostas = gomeostas;
      _influenceActionSystem = influence ?? throw new ArgumentNullException(nameof(influence));

      SaveCommand = new RelayCommand(SaveData);
      RemoveActionCommand = new RelayCommand(RemoveSelectedInfluence);
      RemoveAllCommand = new RelayCommand(RemoveAllInfluences);
      GlobalTimer.PulsationStateChanged += OnPulsationStateChanged;
      LoadAgentData();
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

      InfluenceActions.Clear();

      foreach (var action in _influenceActionSystem.GetAllInfluenceActions().OrderBy(a => a.Id))
      {
        InfluenceActions.Add(new InfluenceActionSystem.GomeostasisInfluenceAction
        {
          Id = action.Id,
          Name = action.Name,
          Description = action.Description,
          Influences = new Dictionary<int, int>(action.Influences),
          AntagonistInfluences = new List<int>(action.AntagonistInfluences)
        });
      }

      OnPropertyChanged(nameof(IsStageZero));
      OnPropertyChanged(nameof(IsEditingEnabled));
      OnPropertyChanged(nameof(PulseWarningMessage));
      OnPropertyChanged(nameof(WarningMessageColor));
      OnPropertyChanged(nameof(CurrentAgentDescription));
      OnPropertyChanged(nameof(CurrentAgentTitle));
    }

    private void LoadAgentData()
    {
      try
      {
        RefreshAllCollections();
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка загрузки внешних воздействий: {ex.Message}", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void SaveData(object parameter)
    {
      try
      {
        if (!UpdateInfluenceActionsSystemFromTable())
          return;

        var (success, error) = _influenceActionSystem.SaveInfluenceActions(false);
        if (success)
        {
          RefreshAllCollections();
          MessageBox.Show("Гомеостатические воздействия успешно сохранены",
              "Сохранение завершено",
              MessageBoxButton.OK,
              MessageBoxImage.Information);
        }
        else
        {
          MessageBox.Show($"Не удалось сохранить гомеостатические воздействия:\n{error}",
              "Ошибка сохранения",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Не удалось сохранить гомеостатические воздействия:\n{ex.Message}",
            "Ошибка сохранения",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
      }
    }

    public void RemoveSelectedInfluence(object parameter)
    {
      if (parameter is InfluenceActionSystem.GomeostasisInfluenceAction action)
      {
        try
        {
          if (InfluenceActions.Contains(action))
            InfluenceActions.Remove(action);

          if (action.Id > 0)
          {
            if (_influenceActionSystem.RemoveAction(action.Id))
            {
              InfluenceActions.Remove(action);

              var (success, error) = _influenceActionSystem.SaveInfluenceActions();
              if (!success)
              {
                MessageBox.Show($"Не удалось удалить гомеостатические воздействия:\n{error}",
                    "Ошибка сохранения после удаления",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
              }
            }
            else
              MessageBox.Show("Не удалось удалить гомеостатического действия", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
          }
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Ошибка удаления гомеостатического действия: {ex.Message}", "Ошибка",
              MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }
    }

    public void RemoveAllInfluences(object parameter)
    {
      var result = MessageBox.Show(
          $"Вы действительно хотите удалить ВСЕ гомеостатические воздействия? Это действие нельзя будет отменить.",
          "Подтверждение удаления",
          MessageBoxButton.YesNo,
          MessageBoxImage.Warning);

      if (result == MessageBoxResult.Yes)
      {
        try
        {
          // Удаляем все действия из системы
          var allActions = _influenceActionSystem.GetAllInfluenceActions().ToList();

          foreach (var action in allActions)
          {
            _influenceActionSystem.RemoveAction(action.Id);
          }

          // Очищаем коллекцию представления
          InfluenceActions.Clear();

          var (success, error) = _influenceActionSystem.SaveInfluenceActions(false); // все удалено - не надо валидаций 
          if (success)
          {
            MessageBox.Show("Все гомеостатические воздействия успешно удалены",
                "Удаление завершено",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
          }
          else
          {
            MessageBox.Show($"Не удалось удалить гомеостатические воздействия:\n{error}",
                "Ошибка сохранения после удаления",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
          }
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Ошибка удаления гомеостатических воздействий: {ex.Message}",
              "Ошибка",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
        }
      }
    }

    private bool UpdateInfluenceActionsSystemFromTable()
    {
      // валидация - возможно будет
           
      // Получаем текущие действия из системы
      var currentActions = _influenceActionSystem.GetAllInfluenceActions().ToDictionary(a => a.Id);

      // Удаляем действия, которых нет в таблице
      var actionsToRemove = currentActions.Keys.Except(InfluenceActions.Select(a => a.Id)).ToList();
      foreach (var actionId in actionsToRemove)
      {
        _influenceActionSystem.RemoveAction(actionId);
      }

      // Добавляем/обновляем действия из таблицы
      foreach (var action in InfluenceActions)
      {
        if (currentActions.ContainsKey(action.Id))
        {
          // Обновляем существующее действие
          var existingAction = currentActions[action.Id];
          existingAction.Name = action.Name;
          existingAction.Description = action.Description;
          existingAction.Influences = new Dictionary<int, int>(action.Influences);
          existingAction.AntagonistInfluences = new List<int>(action.AntagonistInfluences);
        }
        else
        {
          // Добавляем новое действие
          var (newId, warnings) = _influenceActionSystem.AddInfluenceAction(
              action.Name,
              action.Description,
              new Dictionary<int, int>(action.Influences),
              new List<int>(action.AntagonistInfluences)
          );
          action.Id = newId;
        }
      }
      return true;
    }
  }
}
