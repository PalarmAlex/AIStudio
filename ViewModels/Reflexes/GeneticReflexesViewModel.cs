using isida.Reflexes;
using ISIDA.Actions;
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
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using static ISIDA.Gomeostas.GomeostasSystem;

namespace AIStudio.ViewModels
{
  public class GeneticReflexesViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly GeneticReflexesSystem _geneticReflexesSystem;
    private readonly AdaptiveActionsSystem _actionsSystem;
    private readonly InfluenceActionSystem _influenceActionSystem;
    private readonly GomeostasSystem _gomeostas;
    public GomeostasSystem Gomeostas => _gomeostas; // для формы
    private string _currentAgentName;
    private string _currentAgentDescription;
    private int _currentAgentStage;

    // Фильтры
    private int? _selectedLevel1Filter;
    private int? _selectedLevel2Filter;
    private int? _selectedLevel3Filter;
    private int? _selectedAdaptiveActionsFilter;

    public bool IsStageZero => _currentAgentStage == 0;

    public string CurrentAgentTitle => $"Безусловные рефлексы Агента: {_currentAgentName ?? "Не определен"}";
    public string CurrentAgentDescription => _currentAgentDescription ?? "Нет описания";

    private ObservableCollection<GeneticReflexesSystem.GeneticReflex> _allGeneticReflexes = new ObservableCollection<GeneticReflexesSystem.GeneticReflex>();
    private ICollectionView _geneticReflexesView;
    public ICollectionView GeneticReflexesView => _geneticReflexesView;

    public ICommand SaveCommand { get; }
    public ICommand RemoveCommand { get; }
    public ICommand ClearFiltersCommand { get; }
    public ICommand RemoveAllCommand { get; }

    public GeneticReflexesViewModel(
      GomeostasSystem gomeostasSystem,
      GeneticReflexesSystem geneticReflexesSystem,
      AdaptiveActionsSystem actionsSystem,
      InfluenceActionSystem influenceActionSystem)
    {
      _gomeostas = gomeostasSystem;
      _geneticReflexesSystem = geneticReflexesSystem ?? throw new ArgumentNullException(nameof(geneticReflexesSystem));
      _actionsSystem = actionsSystem ?? throw new ArgumentNullException(nameof(actionsSystem));
      _influenceActionSystem = influenceActionSystem ?? throw new ArgumentNullException(nameof(influenceActionSystem));

      _geneticReflexesView = CollectionViewSource.GetDefaultView(_allGeneticReflexes);
      _geneticReflexesView.Filter = FilterGeneticReflexes;

      SaveCommand = new RelayCommand(SaveData);
      RemoveCommand = new RelayCommand(RemoveSelectedReflexes);
      ClearFiltersCommand = new RelayCommand(ClearFilters);
      RemoveAllCommand = new RelayCommand(RemoveAllReflexes);

      GlobalTimer.PulsationStateChanged += OnPulsationStateChanged;
      LoadAgentData();
    }

    private bool FilterGeneticReflexes(object item)
    {
      if (!(item is GeneticReflexesSystem.GeneticReflex reflex))
        return false;

      return (!SelectedLevel1Filter.HasValue || reflex.Level1 == SelectedLevel1Filter.Value) &&
             (!SelectedLevel2Filter.HasValue || (reflex.Level2 != null && reflex.Level2.Contains(SelectedLevel2Filter.Value))) &&
             (!SelectedLevel3Filter.HasValue || (reflex.Level3 != null && reflex.Level3.Contains(SelectedLevel3Filter.Value))) &&
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
      _geneticReflexesView.Refresh();
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

      var level2Items = _gomeostas?.GetAllBehaviorStyles()?.Values?.ToList() ?? new List<GomeostasSystem.BehaviorStyle>();
      Level2FilterOptions.AddRange(level2Items.Select(x => new KeyValuePair<int?, string>(x.Id, x.Name)));

      Level3FilterOptions = new List<KeyValuePair<int?, string>> { new KeyValuePair<int?, string>(null, "Все воздействия") };

      var level3Items = _influenceActionSystem?.GetAllInfluenceActions()?.ToList() ?? new List<InfluenceActionSystem.GomeostasisInfluenceAction>();
      Level3FilterOptions.AddRange(level3Items.Select(x => new KeyValuePair<int?, string>(x.Id, x.Name)));

      AdaptiveActionsFilterOptions = new List<KeyValuePair<int?, string>> { new KeyValuePair<int?, string>(null, "Все действия") };

      var adaptiveItems = _actionsSystem?.GetAllAdaptiveActions()?.ToList() ?? new List<AdaptiveActionsSystem.AdaptiveAction>();
      AdaptiveActionsFilterOptions.AddRange(adaptiveItems.Select(x => new KeyValuePair<int?, string>(x.Id, x.Name)));

      OnPropertyChanged(nameof(Level2FilterOptions));
      OnPropertyChanged(nameof(Level3FilterOptions));
      OnPropertyChanged(nameof(AdaptiveActionsFilterOptions));


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
        MessageBox.Show($"Ошибка загрузки действий: {ex.Message}", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void RefreshAllCollections()
    {
      var agentInfo = _gomeostas.GetAgentState();
      _currentAgentStage = agentInfo?.EvolutionStage ?? 0;
      _currentAgentDescription = agentInfo.Description;
      _currentAgentName = agentInfo.Name;

      _allGeneticReflexes.Clear();
      //GeneticReflexes.Clear();

      foreach (var reflex in _geneticReflexesSystem.GetAllGeneticReflexes().OrderBy(a => a.Id))
      {
        var reflexCopy = new GeneticReflexesSystem.GeneticReflex
        {
          Id = reflex.Id,
          Name = reflex.Name,
          Description = reflex.Description,
          Level1 = reflex.Level1,
          Level2 = new List<int>(reflex.Level2),
          Level3 = new List<int>(reflex.Level3),
          AdaptiveActions = new List<int>(reflex.AdaptiveActions)
        };

        _allGeneticReflexes.Add(reflexCopy);
        //GeneticReflexes.Add(reflexCopy);
      }

      // Загружаем опции фильтров
      LoadFilterOptions();

      OnPropertyChanged(nameof(IsStageZero));
      OnPropertyChanged(nameof(IsEditingEnabled));
      OnPropertyChanged(nameof(PulseWarningMessage));
      OnPropertyChanged(nameof(WarningMessageColor));
      OnPropertyChanged(nameof(CurrentAgentDescription));
      OnPropertyChanged(nameof(CurrentAgentTitle));
    }

    private void SaveData(object parameter)
    {
      try
      {
        if (!UpdateGeneticReflexesSystemFromTable())
        {
          MessageBox.Show("Не удалось обновить данные рефлексов",
              "Ошибка",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
          return;
        }

        try
        {
          var (success, error) = _geneticReflexesSystem.SaveGeneticReflexes(true); // Включить валидацию

          if (success)
          {
            // Только при успешном сохранении обновляем коллекции
            RefreshAllCollections();
            MessageBox.Show("Безусловные рефлексы успешно сохранены",
                "Сохранение завершено",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
          }
          else
          {
            MessageBox.Show($"Не удалось сохранить безусловные рефлексы:\n{error}",
                "Ошибка сохранения",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            // Восстанавливаем исходные данные при ошибке
            RefreshAllCollections();
          }
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Не удалось сохранить безусловные рефлексы:\n{ex.Message}",
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

    private bool UpdateGeneticReflexesSystemFromTable()
    {
      try
      {
        // Валидация всех рефлексов
        foreach (var reflex in _allGeneticReflexes)
        {
          var validationResult = _geneticReflexesSystem.ValidateGeneticReflex(reflex);
          if (!validationResult.IsValid)
          {
            MessageBox.Show($"Ошибка валидации рефлекса '{reflex.Name}':\n{validationResult.ErrorMessage}",
                "Ошибка сохранения",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
          }
        }

        var currentReflexes = _geneticReflexesSystem.GetAllGeneticReflexes().ToDictionary(a => a.Id);

        // Удаление рефлексов
        var reflexesToRemove = currentReflexes.Keys.Except(_allGeneticReflexes.Select(a => a.Id)).ToList();
        foreach (var reflexId in reflexesToRemove)
        {
          if (!_geneticReflexesSystem.RemoveGeneticReflex(reflexId))
          {
            MessageBox.Show($"Не удалось удалить рефлекс с ID {reflexId}",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
          }
        }

        // Обновление и добавление рефлексов
        foreach (var reflex in _allGeneticReflexes)
        {
          if (currentReflexes.ContainsKey(reflex.Id))
          {
            // Обновление существующего
            var warnings = _geneticReflexesSystem.UpdateGeneticReflex(reflex);
            if (warnings != null && warnings.Length > 0)
            {
              MessageBox.Show($"Предупреждения при обновлении рефлекса '{reflex.Name}':\n{string.Join("\n", warnings)}",
                  "Предупреждения",
                  MessageBoxButton.OK,
                  MessageBoxImage.Warning);
            }
          }
          else
          {
            // Добавление нового
            var (newId, warnings) = _geneticReflexesSystem.AddGeneticReflex(
                reflex.Name,
                reflex.Description,
                reflex.Level1,
                new List<int>(reflex.Level2),
                new List<int>(reflex.Level3),
                new List<int>(reflex.AdaptiveActions)
            );

            if (warnings != null && warnings.Length > 0)
            {
              MessageBox.Show($"Предупреждения при добавлении рефлекса '{reflex.Name}':\n{string.Join("\n", warnings)}",
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
        MessageBox.Show($"Ошибка при обновлении системы рефлексов:\n{ex.Message}",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        return false;
      }
    }

    public void RemoveSelectedReflexes(object parameter)
    {
      if (parameter is GeneticReflexesSystem.GeneticReflex reflex)
      {
        try
        {
          if (_allGeneticReflexes.Contains(reflex))
            _allGeneticReflexes.Remove(reflex);

          //if (GeneticReflexes.Contains(reflex))
          //  GeneticReflexes.Remove(reflex);

          if (reflex.Id > 0)
          {
            if (_geneticReflexesSystem.RemoveGeneticReflex(reflex.Id))
            {
              _geneticReflexesSystem.SaveGeneticReflexes();
            }
            else
              MessageBox.Show("Не удалось удалить безусловные рефлексы", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
          }
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Ошибка удаления безусловных рефлексов: {ex.Message}", "Ошибка",
              MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }
    }

    public void RemoveAllReflexes(object parameter)
    {
      var result = MessageBox.Show(
          $"Вы действительно хотите удалить ВСЕ безусловные рефлексы агента? Это действие нельзя будет отменить.",
          "Подтверждение удаления",
          MessageBoxButton.YesNo,
          MessageBoxImage.Warning);

      if (result == MessageBoxResult.Yes)
      {
        try
        {
          // Сохраняем дефолтный стиль
          var defaultReflex = _allGeneticReflexes.FirstOrDefault(style => style.Id == AppConfig.DefaultGeneticReflexId);

          // Удаляем все рефлексы из системы (кроме дефолтного)
          foreach (var reflex in _allGeneticReflexes.Where(reflex => reflex.Id != AppConfig.DefaultGeneticReflexId))
          {
            _geneticReflexesSystem.RemoveGeneticReflex(reflex.Id);
          }

          // Очищаем и перезаполняем коллекцию только дефолтным стилем
          _allGeneticReflexes.Clear();
          if (defaultReflex != null)
            _allGeneticReflexes.Add(defaultReflex);

          var (success, error) = _geneticReflexesSystem.SaveGeneticReflexes(false); // все удалено - не надо валидаций 
          if (success)
          {
            MessageBox.Show("Все безусловные рефлексы агента, кроме заданного по умолчанию, успешно удалены",
                "Удаление завершено",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
          }
          else
          {
            MessageBox.Show($"Не удалось удалить безусловные рефлексы агента:\n{error}",
                "Ошибка сохранения после удаления",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
          }
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Ошибка удаления безусловных рефлексов агента: {ex.Message}",
              "Ошибка",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
        }
      }
    }
  }
}