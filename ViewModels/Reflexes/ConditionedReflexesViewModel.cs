using AIStudio.Views;
using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Gomeostas;
using ISIDA.Reflexes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using static ISIDA.Gomeostas.GomeostasSystem;

namespace AIStudio.ViewModels
{
  public class ConditionedReflexesViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly ConditionedReflexesSystem _conditionedReflexesSystem;
    private readonly AdaptiveActionsSystem _actionsSystem;
    private readonly GomeostasSystem _gomeostas;
    private readonly PerceptionImagesSystem _perceptionImagesSystem;
    private string _currentAgentName;
    private int _currentAgentStage;

    // Фильтры
    private int? _selectedLevel1Filter;
    private int? _selectedLevel2Filter;
    private int? _selectedLevel3Filter;
    private int? _selectedAdaptiveActionsFilter;

    public GomeostasSystem GomeostasSystem => _gomeostas;
    public PerceptionImagesSystem PerceptionImagesSystem => _perceptionImagesSystem;
    public bool IsStageOneOrHigher => _currentAgentStage >= 1;
    public string CurrentAgentTitle => $"Условные рефлексы Агента: {_currentAgentName ?? "Не определен"}";

    private ObservableCollection<ConditionedReflexesSystem.ConditionedReflex> _allConditionedReflexes = new ObservableCollection<ConditionedReflexesSystem.ConditionedReflex>();
    private ICollectionView _conditionedReflexesView;
    public ICollectionView ConditionedReflexesView => _conditionedReflexesView;

    public ICommand SaveCommand { get; }
    public ICommand RemoveCommand { get; }
    public ICommand ClearFiltersCommand { get; }
    public ICommand RemoveAllCommand { get; }
    public ICommand OpenSettingsCommand { get; }

    public ConditionedReflexesViewModel(
        GomeostasSystem gomeostasSystem,
        ConditionedReflexesSystem conditionedReflexesSystem,
        AdaptiveActionsSystem actionsSystem,
        PerceptionImagesSystem perceptionImagesSystem)
    {
      _gomeostas = gomeostasSystem ?? throw new ArgumentNullException(nameof(gomeostasSystem));
      _conditionedReflexesSystem = conditionedReflexesSystem ?? throw new ArgumentNullException(nameof(conditionedReflexesSystem));
      _actionsSystem = actionsSystem ?? throw new ArgumentNullException(nameof(actionsSystem));
      _perceptionImagesSystem = perceptionImagesSystem ?? throw new ArgumentNullException(nameof(perceptionImagesSystem));

      _conditionedReflexesView = CollectionViewSource.GetDefaultView(_allConditionedReflexes);
      _conditionedReflexesView.Filter = FilterConditionedReflexes;

      SaveCommand = new RelayCommand(SaveData);
      RemoveCommand = new RelayCommand(RemoveSelectedReflexes);
      ClearFiltersCommand = new RelayCommand(ClearFilters);
      RemoveAllCommand = new RelayCommand(RemoveAllReflexes);
      OpenSettingsCommand = new RelayCommand(OpenSettings);

      GlobalTimer.PulsationStateChanged += OnPulsationStateChanged;
      LoadAgentData();
    }

    private bool FilterConditionedReflexes(object item)
    {
      if (!(item is ConditionedReflexesSystem.ConditionedReflex reflex))
        return false;

      return (!SelectedLevel1Filter.HasValue || reflex.Level1 == SelectedLevel1Filter.Value) &&
             (!SelectedLevel2Filter.HasValue || (reflex.Level2 != null && reflex.Level2.Contains(SelectedLevel2Filter.Value))) &&
             (!SelectedLevel3Filter.HasValue || reflex.Level3 == SelectedLevel3Filter.Value) &&
             (!SelectedAdaptiveActionsFilter.HasValue || (reflex.AdaptiveActions != null && reflex.AdaptiveActions.Contains(SelectedAdaptiveActionsFilter.Value)));
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

    #region Блокировка страницы в зависимости от стажа

    public bool IsEditingEnabled => IsStageOneOrHigher && !GlobalTimer.IsPulsationRunning;
    public string PulseWarningMessage =>
        !IsStageOneOrHigher
            ? "[КРИТИЧНО] Редактирование параметров доступно только начиная со стадии 1"
            : GlobalTimer.IsPulsationRunning
                ? "Редактирование параметров доступно только при выключенной пульсации"
                : string.Empty;
    public Brush WarningMessageColor =>
        !IsStageOneOrHigher ? Brushes.Red : Brushes.Gray;

    #endregion

    #region Фильтры

    public List<KeyValuePair<int?, string>> Level1FilterOptions { get; } = new List<KeyValuePair<int?, string>>
        {
            new KeyValuePair<int?, string>(null, "Все состояния"),
            new KeyValuePair<int?, string>(-1, "Плохо"),
            new KeyValuePair<int?, string>(0, "Норма"),
            new KeyValuePair<int?, string>(1, "Хорошо")
        };

    public List<KeyValuePair<int?, string>> Level2FilterOptions { get; private set; } = new List<KeyValuePair<int?, string>>();
    public List<KeyValuePair<int?, string>> Level3FilterOptions { get; private set; } = new List<KeyValuePair<int?, string>>();
    public List<KeyValuePair<int?, string>> AdaptiveActionsFilterOptions { get; private set; } = new List<KeyValuePair<int?, string>>();

    public int? SelectedLevel1Filter
    {
      get => _selectedLevel1Filter;
      set
      {
        _selectedLevel1Filter = value;
        OnPropertyChanged(nameof(SelectedLevel1Filter));
        ApplyFilters();
      }
    }

    public int? SelectedLevel2Filter
    {
      get => _selectedLevel2Filter;
      set
      {
        _selectedLevel2Filter = value;
        OnPropertyChanged(nameof(SelectedLevel2Filter));
        ApplyFilters();
      }
    }

    public int? SelectedLevel3Filter
    {
      get => _selectedLevel3Filter;
      set
      {
        _selectedLevel3Filter = value;
        OnPropertyChanged(nameof(SelectedLevel3Filter));
        ApplyFilters();
      }
    }

    public int? SelectedAdaptiveActionsFilter
    {
      get => _selectedAdaptiveActionsFilter;
      set
      {
        _selectedAdaptiveActionsFilter = value;
        OnPropertyChanged(nameof(SelectedAdaptiveActionsFilter));
        ApplyFilters();
      }
    }

    private void ApplyFilters()
    {
      _conditionedReflexesView.Refresh();
    }

    private void ClearFilters(object parameter = null)
    {
      SelectedLevel1Filter = null;
      SelectedLevel2Filter = null;
      SelectedLevel3Filter = null;
      SelectedAdaptiveActionsFilter = null;
    }

    private void LoadFilterOptions()
    {
      Level2FilterOptions = new List<KeyValuePair<int?, string>> { new KeyValuePair<int?, string>(null, "Все контексты") };
      var level2Items = _gomeostas?.GetAllBehaviorStyles()?.Values?.ToList() ?? new List<BehaviorStyle>();
      Level2FilterOptions.AddRange(level2Items.Select(x => new KeyValuePair<int?, string>(x.Id, x.Name)));

      Level3FilterOptions = new List<KeyValuePair<int?, string>> { new KeyValuePair<int?, string>(null, "Все образы") };
      var level3Items = _perceptionImagesSystem?.GetAllPerceptionImagesList() ?? new List<PerceptionImagesSystem.PerceptionImage>();

      Level3FilterOptions.AddRange(level3Items.Select(x =>
          new KeyValuePair<int?, string>(x.Id, CreatePerceptionImageDescription(x))));

      AdaptiveActionsFilterOptions = new List<KeyValuePair<int?, string>> { new KeyValuePair<int?, string>(null, "Все действия") };
      var adaptiveItems = _actionsSystem?.GetAllAdaptiveActions()?.ToList() ?? new List<AdaptiveActionsSystem.AdaptiveAction>();
      AdaptiveActionsFilterOptions.AddRange(adaptiveItems.Select(x => new KeyValuePair<int?, string>(x.Id, x.Name)));

      OnPropertyChanged(nameof(Level2FilterOptions));
      OnPropertyChanged(nameof(Level3FilterOptions));
      OnPropertyChanged(nameof(AdaptiveActionsFilterOptions));
    }

    private string CreatePerceptionImageDescription(PerceptionImagesSystem.PerceptionImage image)
    {
      var description = $"Образ {image.Id}";

      if (image.InfluenceActionsList != null && image.InfluenceActionsList.Any())
      {
        description += $", возд.: {image.InfluenceActionsList.Count}";
      }

      if (image.PhraseIdList != null && image.PhraseIdList.Any())
      {
        description += $", фраз: {image.PhraseIdList.Count}";
      }

      return description;
    }

    #endregion

    public List<KeyValuePair<int, string>> Level1Options { get; } = new List<KeyValuePair<int, string>>
        {
            new KeyValuePair<int, string>(-1, "Плохо"),
            new KeyValuePair<int, string>(0, "Норма"),
            new KeyValuePair<int, string>(1, "Хорошо")
        };

    private void LoadAgentData()
    {
      try
      {
        RefreshAllCollections();
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка загрузки условных рефлексов: {ex.Message}", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void RefreshAllCollections()
    {
      var agentInfo = _gomeostas.GetAgentState();
      _currentAgentStage = agentInfo?.EvolutionStage ?? 0;
      _currentAgentName = agentInfo.Name;

      _allConditionedReflexes.Clear();

      foreach (var reflex in _conditionedReflexesSystem.GetAllConditionedReflexes().OrderBy(a => a.Id))
      {
        var reflexCopy = new ConditionedReflexesSystem.ConditionedReflex
        {
          Id = reflex.Id,
          Level1 = reflex.Level1,
          Level2 = new List<int>(reflex.Level2),
          Level3 = reflex.Level3,
          AdaptiveActions = new List<int>(reflex.AdaptiveActions),
          AssociationStrength = reflex.AssociationStrength,
          LastActivation = reflex.LastActivation,
          BirthTime = reflex.BirthTime,
          SourceGeneticReflexId = reflex.SourceGeneticReflexId
        };

        _allConditionedReflexes.Add(reflexCopy);
      }

      // Загружаем опции фильтров
      LoadFilterOptions();

      OnPropertyChanged(nameof(IsStageOneOrHigher));
      OnPropertyChanged(nameof(IsEditingEnabled));
      OnPropertyChanged(nameof(PulseWarningMessage));
      OnPropertyChanged(nameof(WarningMessageColor));
      OnPropertyChanged(nameof(CurrentAgentTitle));
    }

    private void SaveData(object parameter)
    {
      try
      {
        if (!UpdateConditionedReflexesSystemFromTable())
        {
          MessageBox.Show("Не удалось обновить данные условных рефлексов",
              "Ошибка",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
          return;
        }

        try
        {
          var (success, error) = _conditionedReflexesSystem.SaveConditionedReflexes();

          if (success)
          {
            // Только при успешном сохранении обновляем коллекции
            RefreshAllCollections();
            MessageBox.Show("Условные рефлексы успешно сохранены",
                "Сохранение завершено",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
          }
          else
          {
            MessageBox.Show($"Не удалось сохранить условные рефлексы:\n{error}",
                "Ошибка сохранения",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            // Восстанавливаем исходные данные при ошибке
            RefreshAllCollections();
          }
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Не удалось сохранить условные рефлексы:\n{ex.Message}",
              "Ошибка сохранения",
              MessageBoxButton.OK,
              MessageBoxImage.Error);

          // Восстанавливаем исходные данные при исключении
          RefreshAllCollections();
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

    private bool UpdateConditionedReflexesSystemFromTable()
    {
      try
      {
        var currentReflexes = _conditionedReflexesSystem.GetAllConditionedReflexes().ToDictionary(a => a.Id);
        // Удаление рефлексов
        var reflexesToRemove = currentReflexes.Keys.Except(_allConditionedReflexes.Select(a => a.Id)).ToList();
        foreach (var reflexId in reflexesToRemove)
        {
          var success = _conditionedReflexesSystem.RemoveConditionedReflex(reflexId);
          if (!success)
          {
            MessageBox.Show($"Не удалось удалить условный рефлекс ID: {reflexId}",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
          }
        }

        // Обновление и добавление рефлексов
        foreach (var reflex in _allConditionedReflexes)
        {
          if (currentReflexes.ContainsKey(reflex.Id) && reflex.Id > 0)
          {
            // Обновление существующего рефлекса
            var existingReflex = currentReflexes[reflex.Id];
            existingReflex.Level1 = reflex.Level1;
            existingReflex.Level2 = new List<int>(reflex.Level2);
            existingReflex.Level3 = reflex.Level3;
            existingReflex.AdaptiveActions = new List<int>(reflex.AdaptiveActions);
            // Не обновляем системные поля: AssociationStrength, LastActivation, BirthTime
          }
          else if (reflex.Id <= 0)
          {
            // Добавление нового рефлекса
            var (newId, warnings) = _conditionedReflexesSystem.AddConditionedReflex(
                reflex.Level1,
                new List<int>(reflex.Level2),
                reflex.Level3,
                new List<int>(reflex.AdaptiveActions),
                reflex.SourceGeneticReflexId
            );

            if (warnings != null && warnings.Length > 0)
            {
              MessageBox.Show($"Предупреждения при добавлении рефлекса '{reflex.Id}':\n{string.Join("\n", warnings)}",
                  "Предупреждения",
                  MessageBoxButton.OK,
                  MessageBoxImage.Warning);
            }

            reflex.Id = newId;
          }
        }
        return true;
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при обновлении системы условных рефлексов:\n{ex.Message}",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        return false;
      }
    }

    public void RemoveSelectedReflexes(object parameter)
    {
      if (parameter is ConditionedReflexesSystem.ConditionedReflex reflex)
      {
        try
        {
          if (_allConditionedReflexes.Contains(reflex))
            _allConditionedReflexes.Remove(reflex);

          if (reflex.Id > 0)
          {
            bool removed = _conditionedReflexesSystem.RemoveConditionedReflex(reflex.Id);
            if (removed)
            {
              var (success, error) = _conditionedReflexesSystem.SaveConditionedReflexes();
              if (!success)
              {
                MessageBox.Show($"Не удалось сохранить удаление: {error}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
              }
            }
            else
            {
              MessageBox.Show("Не удалось удалить рефлекс из системы", "Ошибка",
                  MessageBoxButton.OK, MessageBoxImage.Error);
            }
          }
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
              MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }
    }

    public void RemoveAllReflexes(object parameter)
    {
      var result = MessageBox.Show(
          $"Вы действительно хотите удалить ВСЕ условные рефлексы агента? Это действие нельзя будет отменить.",
          "Подтверждение удаления",
          MessageBoxButton.YesNo,
          MessageBoxImage.Warning);

      if (result == MessageBoxResult.Yes)
      {
        try
        {
          var success = _conditionedReflexesSystem.RemoveAllConditionedReflexes();

          if (success)
          {
            _allConditionedReflexes.Clear();

            var (saveSuccess, error) = _conditionedReflexesSystem.SaveConditionedReflexes();
            if (saveSuccess)
            {
              MessageBox.Show("Все условные рефлексы агента успешно удалены",
                  "Удаление завершено",
                  MessageBoxButton.OK,
                  MessageBoxImage.Information);
            }
            else
            {
              MessageBox.Show($"Не удалось сохранить изменения после удаления:\n{error}",
                  "Ошибка сохранения",
                  MessageBoxButton.OK,
                  MessageBoxImage.Error);
            }
          }
          else
          {
            MessageBox.Show("Не удалось удалить все условные рефлексы",
                "Ошибка удаления",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
          }
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Ошибка удаления условных рефлексов агента: {ex.Message}",
              "Ошибка",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
        }
      }
    }

    public void OpenSettings(object parameter)
    {
      try
      {
        if (!IsEditingEnabled)
        {
          MessageBox.Show("Редактирование настроек доступно только при выключенной пульсации",
              "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
          return;
        }
        var settingsViewModel = new ConditionedReflexSettingsViewModel(_conditionedReflexesSystem);
        var settingsWindow = new ConditionedReflexSettingsView(settingsViewModel)
        {
          Owner = Application.Current.MainWindow
        };

        var result = settingsWindow.ShowDialog();

        if (result == true)
        {
          // Обновляем данные на клиенте, если настройки были сохранены
          MessageBox.Show("Настройки успешно применены и сохранены!",
              "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка открытия настроек:\n{ex.Message}",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    public class DescriptionWithLink
    {
      public string Text { get; set; }
      public string LinkText { get; set; } = "Подробнее...";
      public string Url { get; set; } = "https://scorcher.ru/isida/iadaptive_agents_guide.php#ref_16";
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
          Text = "Редактор условных рефлексов."
        };
      }
    }
  }
}