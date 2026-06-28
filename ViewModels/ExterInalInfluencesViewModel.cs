using AIStudio.Common;
using AIStudio.Common.Adapters;
using AIStudio.Common.SymbiontEnv;
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
  public enum InfluenceActionsEditorScope
  {
    All,
    EnvironmentOnly
  }

  public class ExterInalInfluencesViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly GomeostasSystem _gomeostas;
    private readonly InfluenceActionSystem _influenceActionSystem;
    private readonly InfluenceActionsEditorScope _scope;
    private readonly HashSet<int> _legacyEnvironmentProxyIds = new HashSet<int>();
    private string _currentAgentName;
    private int _currentAgentStage;
    public bool IsEnvironmentScope => _scope == InfluenceActionsEditorScope.EnvironmentOnly;
    public bool HasAdapter => !IsEnvironmentScope || SymbiontEnvironmentGate.IsEnvironmentEditingAllowed();
    public bool IsStageZero => _currentAgentStage == 0;
    public ObservableCollection<InfluenceActionSystem.GomeostasisInfluenceAction> InfluenceActions { get; } = new ObservableCollection<InfluenceActionSystem.GomeostasisInfluenceAction>();
    public ObservableCollection<AdapterSchemaMetricProbe> ProbeKeyOptions { get; } = new ObservableCollection<AdapterSchemaMetricProbe>();
    public string CurrentAgentTitle => IsEnvironmentScope
        ? SymbiontPageTitleFormatter.Format("Метрики среды (InfluenceActions)", _currentAgentName, _currentAgentStage)
        : SymbiontPageTitleFormatter.Format("Стимулы (воздействия на агента)", _currentAgentName, _currentAgentStage);
    public ICommand SaveCommand { get; }
    public ICommand RemoveActionCommand { get; }
    public ICommand RemoveAllCommand { get; }
    public ExterInalInfluencesViewModel(
        GomeostasSystem gomeostas,
        InfluenceActionSystem influence,
        InfluenceActionsEditorScope scope = InfluenceActionsEditorScope.All)
    {
      _gomeostas = gomeostas;
      _influenceActionSystem = influence ?? throw new ArgumentNullException(nameof(influence));
      _scope = scope;
      SaveCommand = new RelayCommand(SaveData);
      RemoveActionCommand = new RelayCommand(RemoveSelectedInfluence);
      RemoveAllCommand = new RelayCommand(RemoveAllInfluences);
      GlobalTimer.PulsationStateChanged += OnPulsationStateChanged;
      LoadAgentData();
    }
    #region Блокировка страницы в зависимости от стажа
    public bool IsEditingEnabled =>
        IsStageZero && !GlobalTimer.IsPulsationRunning && HasAdapter;
    public bool IsReadOnlyMode => !IsEditingEnabled;
    public bool ShowAntagonistColumn => !IsEnvironmentScope;
    public bool ShowProbeKeyColumn => IsEnvironmentScope;
    public string PulseWarningMessage =>
        IsEnvironmentScope && !HasAdapter
            ? "Укажите тип среды в свойствах симбионта"
            : !IsStageZero
            ? "[КРИТИЧНО] Редактирование параметров доступно только в стадии 0"
            : GlobalTimer.IsPulsationRunning
                ? "Редактирование параметров доступно только при выключенной пульсации"
                : string.Empty;
    public Brush WarningMessageColor =>
        (IsEnvironmentScope && !HasAdapter) || !IsStageZero ? Brushes.Red :
        Brushes.Gray;
    #endregion
    public List<ParameterData> GetAllParameters()
    {
      return _gomeostas.GetAllParameters().ToList();
    }

    public bool IsEnvironmentAction(InfluenceActionSystem.GomeostasisInfluenceAction action) =>
        action != null && !string.IsNullOrWhiteSpace(action.ProbeKey);

    public int AllocateNextRowId()
    {
      int maxId = InfluenceActions.Count == 0 ? 0 : InfluenceActions.Max(a => a.Id);
      return IsEnvironmentScope
          ? InfluenceActionIdPolicy.AllocateNextEnvironmentId(maxId)
          : InfluenceActionIdPolicy.AllocateNextId(maxId);
    }

    public InfluenceActionSystem.GomeostasisInfluenceAction CreateNewRow()
    {
      int nextId = AllocateNextRowId();
      string defaultProbeKey = string.Empty;
      if (IsEnvironmentScope)
      {
        defaultProbeKey = ProbeKeyOptions
            .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p?.Key))?.Key ?? string.Empty;
      }

      return new InfluenceActionSystem.GomeostasisInfluenceAction
      {
        Id = nextId,
        Name = IsEnvironmentScope ? "Среда: новая метрика" : $"Новое действие {nextId}",
        Description = string.Empty,
        Influences = new Dictionary<int, int>(),
        AntagonistInfluences = new List<int>(),
        ProbeKey = defaultProbeKey
      };
    }

    private void RefreshProbeKeyOptions()
    {
      ProbeKeyOptions.Clear();
      if (!IsEnvironmentScope)
        ProbeKeyOptions.Add(AdapterSchemaMetricProbe.OperatorOnly);
      var knownKeys = new HashSet<string>(StringComparer.Ordinal);
      IReadOnlyList<AdapterSchemaMetricProbe> schemaProbes = AdapterSchemaLoader.LoadMetricProbesForCurrentProject();
      for (int i = 0; i < schemaProbes.Count; i++)
      {
        AdapterSchemaMetricProbe probe = schemaProbes[i];
        if (probe == null || string.IsNullOrWhiteSpace(probe.Key) || !knownKeys.Add(probe.Key))
          continue;
        ProbeKeyOptions.Add(probe);
      }

      foreach (var action in InfluenceActions)
      {
        string key = (action?.ProbeKey ?? string.Empty).Trim();
        if (key.Length == 0 || !knownKeys.Add(key))
          continue;
        ProbeKeyOptions.Add(new AdapterSchemaMetricProbe { Key = key, Label = key });
      }
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
      _currentAgentStage = _gomeostas?.GetAgentState()?.EvolutionStage ?? 0;
      _currentAgentName = agentInfo.Name;
      InfluenceActions.Clear();
      _legacyEnvironmentProxyIds.Clear();
      foreach (var action in _influenceActionSystem.GetAllInfluenceActions().OrderBy(a => a.Id))
      {
        if (IsEnvironmentScope && !action.IsEnvironmentProbeAction)
          continue;
        if (!IsEnvironmentScope && action.IsEnvironmentProbeAction)
          continue;

        if (InfluenceActionIdPolicy.IsDeprecatedEnvironmentProxyRange(action.Id))
          _legacyEnvironmentProxyIds.Add(action.Id);
        InfluenceActions.Add(new InfluenceActionSystem.GomeostasisInfluenceAction
        {
          Id = action.Id,
          Name = action.Name,
          Description = action.Description,
          Influences = new Dictionary<int, int>(action.Influences),
          AntagonistInfluences = new List<int>(action.AntagonistInfluences),
          ProbeKey = action.ProbeKey ?? string.Empty
        });
      }
      RefreshProbeKeyOptions();
      OnPropertyChanged(nameof(IsStageZero));
      OnPropertyChanged(nameof(IsEditingEnabled));
      OnPropertyChanged(nameof(PulseWarningMessage));
      OnPropertyChanged(nameof(WarningMessageColor));
      OnPropertyChanged(nameof(CurrentAgentTitle));
      OnPropertyChanged(nameof(IsReadOnlyMode));
      OnPropertyChanged(nameof(ShowAntagonistColumn));
      OnPropertyChanged(nameof(ShowProbeKeyColumn));
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
          if (IsEnvironmentScope)
          {
            foreach (var action in _influenceActionSystem.GetAllInfluenceActions()
                .Where(a => a.IsEnvironmentProbeAction)
                .ToList())
            {
              _influenceActionSystem.RemoveAction(action.Id);
            }
          }
          else
          {
            foreach (var action in _influenceActionSystem.GetAllInfluenceActions().ToList())
            {
              if (!action.IsEnvironmentProbeAction)
                _influenceActionSystem.RemoveAction(action.Id);
            }
          }

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
      if (!ValidateStimulusIdRangesBeforeSave())
        return false;

      if (IsEnvironmentScope && !ValidateEnvironmentRowsBeforeSave())
        return false;

      if (SymbiontEnvironmentGate.IsEnvironmentEditingAllowed())
      {
        var coverage = EnvironmentInfluenceValidation.ValidateProbeCoverage(InfluenceActions);
        if (!coverage.IsValid)
        {
          var answer = MessageBox.Show(
              coverage.ErrorMessage + Environment.NewLine + Environment.NewLine + "Сохранить всё равно?",
              "Покрытие ProbeKey",
              MessageBoxButton.YesNo,
              MessageBoxImage.Warning);
          if (answer != MessageBoxResult.Yes)
            return false;
        }
      }

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

      if (!IsEnvironmentScope)
      {
        var tableIds = new HashSet<int>(InfluenceActions.Select(a => a.Id));
        foreach (var kv in currentActions)
        {
          if (kv.Value.IsEnvironmentProbeAction)
            continue;
          if (!tableIds.Contains(kv.Key))
            _influenceActionSystem.RemoveAction(kv.Key);
        }
      }
      else
      {
        var envIdsInTable = new HashSet<int>(InfluenceActions.Select(a => a.Id));
        foreach (var kv in currentActions)
        {
          if (kv.Value.IsEnvironmentProbeAction && !envIdsInTable.Contains(kv.Key))
            _influenceActionSystem.RemoveAction(kv.Key);
        }
      }

      foreach (var action in InfluenceActions)
      {
        if (action.IsEnvironmentProbeAction)
          action.AntagonistInfluences = new List<int>();

        if (currentActions.ContainsKey(action.Id))
        {
          var existingAction = currentActions[action.Id];
          existingAction.Name = action.Name;
          existingAction.Description = action.Description;
          existingAction.Influences = new Dictionary<int, int>(action.Influences);
          existingAction.AntagonistInfluences = new List<int>(action.AntagonistInfluences);
          existingAction.ProbeKey = action.ProbeKey ?? string.Empty;
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
          var created = _influenceActionSystem.GetAllInfluenceActions().FirstOrDefault(a => a.Id == newId);
          if (created != null)
            created.ProbeKey = action.ProbeKey ?? string.Empty;
        }
      }
    }

    private bool ValidateEnvironmentRowsBeforeSave()
    {
      foreach (var action in InfluenceActions)
      {
        if (action == null)
          continue;
        if (string.IsNullOrWhiteSpace(action.ProbeKey))
        {
          MessageBox.Show(
              $"EA {action.Id} («{action.Name}»): ProbeKey обязателен для метрик среды.",
              "Ошибка сохранения",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
          return false;
        }

        var probeCheck = SettingsValidator.ValidateEnvironmentProbeKey(action.ProbeKey);
        if (!probeCheck.isValid)
        {
          MessageBox.Show(
              $"EA {action.Id}: {probeCheck.errorMessage}",
              "Ошибка сохранения",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
          return false;
        }
      }

      return true;
    }

    private bool ValidateStimulusIdRangesBeforeSave()
    {
      var errors = new List<string>();
      var warnings = new List<string>();
      foreach (var action in InfluenceActions)
      {
        if (action == null)
          continue;
        if (InfluenceActionIdPolicy.IsDeprecatedEnvironmentProxyRange(action.Id))
        {
          if (!_legacyEnvironmentProxyIds.Contains(action.Id))
          {
            errors.Add(
                $"ID {action.Id} («{action.Name}»): диапазон 101–1000 (EA-прокси среды) снят в contract 3.1. " +
                "Новые записи не создавайте — события среды через Command и pressure rules.");
          }
          else
          {
            warnings.Add(
                $"ID {action.Id} («{action.Name}»): устаревший EA-прокси (101–1000); только ручной вирт. тест на пульте AIStudio.");
          }
        }
        else if (action.Id < 1 || action.Id > InfluenceActionIdPolicy.OperatorMaxId)
        {
          warnings.Add(
              $"ID {action.Id} («{action.Name}»): операторский стимул рекомендуется в диапазоне 1–{InfluenceActionIdPolicy.OperatorMaxId}.");
        }
      }

      if (errors.Count > 0)
      {
        MessageBox.Show(
            string.Join(Environment.NewLine, errors),
            "Диапазоны ID стимулов",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        return false;
      }

      if (warnings.Count == 0)
        return true;

      string message = string.Join(Environment.NewLine, warnings) + Environment.NewLine + Environment.NewLine +
                       "Продолжить сохранение?";
      return MessageBox.Show(
          message,
          "Диапазоны ID стимулов",
          MessageBoxButton.YesNo,
          MessageBoxImage.Warning) == MessageBoxResult.Yes;
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
          Text = IsEnvironmentScope
              ? "Строки EA среды в InfluenceActions.dat: ключ метрики из schema/metric-probes.json и величина давления на виталы (колонка «Воздействие»). Антагонисты запрещены. Runtime: Velum фаза A на пульсе."
              : "Справочник InfluenceActions.dat: операторские стимулы без ключа метрики. Строки с ProbeKey редактируются в меню «Среда» → «Метрики среды»."
        };
      }
    }
  }
}
