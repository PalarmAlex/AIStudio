using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Gomeostas;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static ISIDA.Gomeostas.GomeostasSystem;

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
    private int _currentAgentStage;
    public bool IsStageZero => _currentAgentStage == 0;

    public ObservableCollection<InfluenceActionSystem.GomeostasisInfluenceAction> InfluenceActions { get; } = new ObservableCollection<InfluenceActionSystem.GomeostasisInfluenceAction>();

    public string CurrentAgentTitle => $"Воздействия Оператора на Агента: {_currentAgentName ?? "Не определен"}";

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
    public bool IsReadOnlyMode => !IsEditingEnabled;
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
      OnPropertyChanged(nameof(CurrentAgentTitle));
      OnPropertyChanged(nameof(IsReadOnlyMode));
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

          var existingInfluenceAction = _influenceActionSystem.GetAllInfluenceActions().ToList();
          bool influenceActionExistsInSystem = InfluenceActions.Any(a => a.Id == action.Id);

          if (influenceActionExistsInSystem)
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
              else
                RefreshAllCollections(); // чтобы обновились записи в таблице, после их чистки при удалении
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
      bool needRevalidation = false;

      if (!_influenceActionSystem.ValidateAllInfluenceActions(InfluenceActions, out string errorMsg))
      {
        if (errorMsg.Contains("AsymmetricInfluences"))
        {
          var asymmetricInfluences = _influenceActionSystem.FindAsymmetricInfluences(InfluenceActions);
          if (asymmetricInfluences.Any())
          {
            var asymmetricList = string.Join(", ", asymmetricInfluences.Select(s => $"{s.Name} (ID:{s.Id})"));

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
                int fixesCount = _influenceActionSystem.FixInfluenceAntagonistSymmetry(InfluenceActions);
                MessageBox.Show($"Исправлено {fixesCount} асимметричных связей",
                    "Автокоррекция завершена",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                ApplyLocalInfluencesToSystem();
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
          MessageBox.Show($"Ошибка валидации гомеостатических воздействий:\n{errorMsg}",
              "Ошибка сохранения",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
          return false;
        }
      }

      if (needRevalidation)
      {
        if (!_influenceActionSystem.ValidateAllInfluenceActions(InfluenceActions, out errorMsg))
        {
          MessageBox.Show($"Ошибка валидации после исправления асимметрии:\n{errorMsg}",
              "Ошибка сохранения",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
          return false;
        }
      }

      if (!needRevalidation)
      {
        ApplyLocalInfluencesToSystem();
      }

      return true;
    }

    /// <summary>
    /// Применяет изменения из локальной коллекции в систему гомеостатических воздействий
    /// </summary>
    private void ApplyLocalInfluencesToSystem()
    {
      var currentActions = _influenceActionSystem.GetAllInfluenceActions().ToDictionary(a => a.Id);
      var actionsToRemove = currentActions.Keys.Except(InfluenceActions.Select(a => a.Id)).ToList();

      foreach (var actionId in actionsToRemove)
      {
        _influenceActionSystem.RemoveAction(actionId);
      }

      foreach (var action in InfluenceActions)
      {
        if (currentActions.ContainsKey(action.Id))
        {
          var existingAction = currentActions[action.Id];
          existingAction.Name = action.Name;
          existingAction.Description = action.Description;
          existingAction.Influences = new Dictionary<int, int>(action.Influences);
          existingAction.AntagonistInfluences = new List<int>(action.AntagonistInfluences);
        }
        else
        {
          var (newId, warnings) = _influenceActionSystem.AddInfluenceAction(
              action.Name,
              action.Description,
              new Dictionary<int, int>(action.Influences),
              new List<int>(action.AntagonistInfluences)
          );
          action.Id = newId;
        }
      }
    }

    public class DescriptionWithLink
    {
      public string Text { get; set; }
      public string LinkText { get; set; } = "Подробнее...";
      public string Url { get; set; } = "https://scorcher.ru/isida/iadaptive_agents_guide.php#ref_10";
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
          Text = "Редактор воздействий на систему гомеостаза агента, имитирующих его физическое взаимодействие с внешней средой."
        };
      }
    }
  }
}
