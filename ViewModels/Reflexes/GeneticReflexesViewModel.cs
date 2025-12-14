using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Gomeostas;
using ISIDA.Reflexes;
using ISIDA.Sensors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using static ISIDA.Gomeostas.GomeostasSystem;
using static ISIDA.Reflexes.ReflexChainsSystem;

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
    private readonly ReflexTreeSystem _reflexTreeSystem;
    private readonly ReflexChainsSystem _reflexChainsSystem;

    private readonly GomeostasSystem _gomeostas;
    public GomeostasSystem Gomeostas => _gomeostas;
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
    public ICommand GenerateReflexesCommand { get; }
    public ICommand UpdateAllCommand { get; }

    public GeneticReflexesViewModel(
      GomeostasSystem gomeostasSystem,
      GeneticReflexesSystem geneticReflexesSystem,
      AdaptiveActionsSystem actionsSystem,
      InfluenceActionSystem influenceActionSystem,
      ReflexTreeSystem reflexTreeSystem,
      ReflexChainsSystem reflexChainsSystem)
    {
      _gomeostas = gomeostasSystem;
      _geneticReflexesSystem = geneticReflexesSystem ?? throw new ArgumentNullException(nameof(geneticReflexesSystem));
      _actionsSystem = actionsSystem ?? throw new ArgumentNullException(nameof(actionsSystem));
      _influenceActionSystem = influenceActionSystem ?? throw new ArgumentNullException(nameof(influenceActionSystem));
      _reflexTreeSystem = reflexTreeSystem ?? throw new ArgumentNullException(nameof(reflexTreeSystem));
      _reflexChainsSystem = reflexChainsSystem ?? throw new ArgumentNullException(nameof(reflexChainsSystem));

      _geneticReflexesView = CollectionViewSource.GetDefaultView(_allGeneticReflexes);
      _geneticReflexesView.Filter = FilterGeneticReflexes;

      SaveCommand = new RelayCommand(SaveData);
      RemoveCommand = new RelayCommand(RemoveSelectedReflexes);
      ClearFiltersCommand = new RelayCommand(ClearFilters);
      RemoveAllCommand = new RelayCommand(RemoveAllReflexes);
      GenerateReflexesCommand = new RelayCommand(GenerateReflexes);
      UpdateAllCommand = new RelayCommand(UpdateAllReflexes);

      GlobalTimer.PulsationStateChanged += OnPulsationStateChanged;
      LoadAgentData();
    }

    /// <summary>
    /// Обновляет все рефлексы и синхронизирует с деревом рефлексов
    /// </summary>
    private void UpdateAllReflexes(object parameter)
    {
      if (!IsEditingEnabled)
      {
        MessageBox.Show("Обновление рефлексов доступно только в стадии 0 при выключенной пульсации",
            "Невозможно выполнить",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
      }

      var result = MessageBox.Show(
          "Вы действительно хотите обновить все рефлексы?\n\n" +
          "Это действие:\n" +
          "• Обновит дерево рефлексов\n" +
          "• Обновит образы триггеров и стилей\n" +
          "• Синхронизирует данные после ручного редактирования файла\n" +
          "• Сохранит текущие привязки цепочек\n\n" +
          "Используйте эту функцию после ручного редактирования файла рефлексов.",
          "Подтверждение обновления рефлексов",
          MessageBoxButton.YesNo,
          MessageBoxImage.Question);

      if (result == MessageBoxResult.Yes)
      {
        Mouse.OverrideCursor = Cursors.Wait;

        try
        {
          var (success, updatedCount, errorMessage) = _geneticReflexesSystem.UpdateAllGeneticReflex();

          Mouse.OverrideCursor = null;

          if (success)
          {
            MessageBox.Show($"Успешно обновлено {updatedCount} рефлексов.",
                "Обновление завершено",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
          }
          else
          {
            MessageBox.Show($"Не удалось обновить рефлексы:\n{errorMessage}",
                "Ошибка обновления",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
          }
        }
        catch (Exception ex)
        {
          Mouse.OverrideCursor = null;

          MessageBox.Show($"Ошибка при обновлении рефлексов:\n{ex.Message}",
              "Ошибка",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
        }
      }
    }

    /// <summary>
    /// Обновляет привязку цепочки к узлу дерева рефлексов
    /// </summary>
    public void UpdateChainBindingForReflex(GeneticReflexesSystem.GeneticReflex reflex)
    {
      try
      {
        if (reflex.Id <= 0)
        {
          MessageBox.Show("Нельзя привязать цепочку к несохраненному рефлексу",
              "Ошибка",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
          return;
        }

        if (!IsEditingEnabled)
        {
          MessageBox.Show("Обновление привязки цепочки доступно только при выключенной пульсации",
              "Невозможно выполнить",
              MessageBoxButton.OK,
              MessageBoxImage.Warning);
          return;
        }

        // Находим ID образа стилей поведения
        int styleImageId = 0;
        if (reflex.Level2 != null && reflex.Level2.Any())
        {
          // Используем PerceptionImagesSystem для получения ID образа
          // Если система не инициализирована, используем хэш
          if (PerceptionImagesSystem.IsInitialized)
          {
            try
            {
              // Получаем образ стилей поведения
              styleImageId = PerceptionImagesSystem.Instance.AddBehaviorStyleImage(reflex.Level2);
            }
            catch (Exception ex)
            {
              Debug.WriteLine($"Ошибка получения образа стилей: {ex.Message}");
              styleImageId = GetHashForList(reflex.Level2);
            }
          }
          else
          {
            styleImageId = GetHashForList(reflex.Level2);
          }
        }

        // Находим ID образа пусковых стимулов
        int actionImageId = 0;
        if (reflex.Level3 != null && reflex.Level3.Any())
        {
          if (PerceptionImagesSystem.IsInitialized)
          {
            try
            {
              // фразу не передаем - рефлексы не учитывают вербальное воздействие
              actionImageId = PerceptionImagesSystem.Instance.AddPerceptionImage(reflex.Level3, new List<int>());
            }
            catch (Exception ex)
            {
              Debug.WriteLine($"Ошибка получения образа воздействий: {ex.Message}");
              actionImageId = GetHashForList(reflex.Level3);
            }
          }
          else
          {
            actionImageId = GetHashForList(reflex.Level3);
          }
        }

        int[] conditionArr = new int[] { reflex.Level1, styleImageId, actionImageId };

        var (nodeId, node) = _reflexTreeSystem.FindReflexTreeNodeFromCondition(
            reflex.Level1, styleImageId, actionImageId);

        if (node != null)
        {
          if (reflex.ReflexChainID > 0)
          {
            bool attached = _reflexTreeSystem.AttachChainToNode(nodeId, reflex.ReflexChainID);
            if (attached)
            {
              Debug.WriteLine($"Цепочка {reflex.ReflexChainID} привязана к узлу {nodeId} для рефлекса {reflex.Id}");
            }
            else
            {
              MessageBox.Show($"Не удалось привязать цепочку {reflex.ReflexChainID} к узлу дерева",
                  "Ошибка",
                  MessageBoxButton.OK,
                  MessageBoxImage.Warning);
            }
          }
          else
          {
            bool detached = _reflexTreeSystem.DetachChainFromNode(nodeId);
            if (detached)
              Debug.WriteLine($"Цепочка отвязана от узла {nodeId} для рефлекса {reflex.Id}");
          }

          var (saveSuccess, error) = _reflexTreeSystem.SaveReflexTree();
          if (!saveSuccess)
            MessageBox.Show($"Ошибка сохранения дерева рефлексов: {error}",
                "Ошибка сохранения",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

          var (reflexSuccess, reflexError) = _geneticReflexesSystem.SaveGeneticReflexes();
          if (!reflexSuccess)
          {
            MessageBox.Show($"Не удалось сохранить безусловные рефлексы:\n{reflexError}",
                "Ошибка сохранения",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
          }
        }
        else
        {
          MessageBox.Show($"Не найден узел дерева рефлексов для условий: [{reflex.Level1}, {styleImageId}, {actionImageId}]",
              "Ошибка",
              MessageBoxButton.OK,
              MessageBoxImage.Warning);
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка обновления привязки цепочки: {ex.Message}",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
      }
    }

    /// <summary>
    /// Получает хэш для списка ID (для случая, когда PerceptionImagesSystem не доступен)
    /// </summary>
    private int GetHashForList(List<int> ids)
    {
      if (ids == null || !ids.Any()) return 0;

      // Сортируем для обеспечения уникальности независимо от порядка
      var sorted = ids.OrderBy(x => x).ToList();
      return string.Join(",", sorted).GetHashCode();
    }

    /// <summary>
    /// Обновляет все привязки цепочек для всех рефлексов
    /// </summary>
    public void UpdateAllChainBindings()
    {
      try
      {
        if (!IsEditingEnabled)
        {
          MessageBox.Show("Обновление привязок доступно только при выключенной пульсации",
              "Невозможно выполнить",
              MessageBoxButton.OK,
              MessageBoxImage.Warning);
          return;
        }

        int updatedCount = 0;
        int errorCount = 0;

        foreach (var reflex in _allGeneticReflexes)
        {
          try
          {
            UpdateChainBindingForReflex(reflex);
            updatedCount++;
          }
          catch (Exception ex)
          {
            errorCount++;
            Debug.WriteLine($"Ошибка обновления привязки для рефлекса {reflex.Id}: {ex.Message}");
          }
        }

        if (errorCount > 0)
        {
          MessageBox.Show($"Обновлено {updatedCount} привязок, ошибок: {errorCount}",
              "Результат обновления",
              MessageBoxButton.OK,
              MessageBoxImage.Information);
        }
        else
        {
          Debug.WriteLine($"Успешно обновлено {updatedCount} привязок цепочек");
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка массового обновления привязок: {ex.Message}",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
      }
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
        OnPropertyChanged(nameof(IsReadOnlyMode));
      });
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
    public List<KeyValuePair<int?, string>> WordFilterOptions { get; private set; } = new List<KeyValuePair<int?, string>>();

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

      foreach (var reflex in _geneticReflexesSystem.GetAllGeneticReflexes().OrderBy(a => a.Id))
      {
        var reflexCopy = new GeneticReflexesSystem.GeneticReflex
        {
          Id = reflex.Id,
          Level1 = reflex.Level1,
          Level2 = new List<int>(reflex.Level2),
          Level3 = new List<int>(reflex.Level3),
          AdaptiveActions = new List<int>(reflex.AdaptiveActions),
          ReflexChainID = reflex.ReflexChainID
        };

        _allGeneticReflexes.Add(reflexCopy);
      }

      LoadFilterOptions();

      OnPropertyChanged(nameof(IsStageZero));
      OnPropertyChanged(nameof(IsEditingEnabled));
      OnPropertyChanged(nameof(PulseWarningMessage));
      OnPropertyChanged(nameof(WarningMessageColor));
      OnPropertyChanged(nameof(CurrentAgentDescription));
      OnPropertyChanged(nameof(CurrentAgentTitle));
      OnPropertyChanged(nameof(IsReadOnlyMode));
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
          var (success, error) = _geneticReflexesSystem.SaveGeneticReflexes(false);

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
        var currentReflexes = _geneticReflexesSystem.GetAllGeneticReflexes().ToDictionary(a => a.Id);

        foreach (var reflex in _allGeneticReflexes)
        {
          var validationResult = _geneticReflexesSystem.ValidateGeneticReflex(reflex);
          if (!validationResult.IsValid)
          {
            MessageBox.Show($"Ошибка валидации рефлекса '{reflex.Id}':\n{validationResult.ErrorMessage}",
                "Ошибка сохранения",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
          }
        }

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

        foreach (var reflex in _allGeneticReflexes)
        {
          if (currentReflexes.ContainsKey(reflex.Id) && reflex.Id > 0)
          {
            var originalReflex = currentReflexes[reflex.Id];
            originalReflex.Level1 = reflex.Level1;
            originalReflex.Level2 = new List<int>(reflex.Level2);
            originalReflex.Level3 = new List<int>(reflex.Level3);
            originalReflex.AdaptiveActions = new List<int>(reflex.AdaptiveActions);
            originalReflex.ReflexChainID = reflex.ReflexChainID;

            var warnings = _geneticReflexesSystem.UpdateGeneticReflex(originalReflex);
            if (warnings != null && warnings.Length > 0)
            {
              MessageBox.Show($"Предупреждения при обновлении рефлекса '{reflex.Id}':\n{string.Join("\n", warnings)}",
                  "Предупреждения",
                  MessageBoxButton.OK,
                  MessageBoxImage.Warning);
            }
          }
          else
          {
            var (newId, warnings) = _geneticReflexesSystem.AddGeneticReflex(
                reflex.Level1,
                new List<int>(reflex.Level2),
                new List<int>(reflex.Level3),
                new List<int>(reflex.AdaptiveActions)
            );

            if (warnings != null && warnings.Length > 0)
            {
              MessageBox.Show($"Предупреждения при добавлении рефлекса '{newId}':\n{string.Join("\n", warnings)}",
                  "Предупреждения",
                  MessageBoxButton.OK,
                  MessageBoxImage.Warning);
            }

            reflex.Id = newId;

            if (reflex.ReflexChainID > 0)
            {
              try
              {
                bool attached = _geneticReflexesSystem.AttachChainToReflex(newId, reflex.ReflexChainID);
                if (!attached)
                {
                  MessageBox.Show($"Не удалось привязать цепочку {reflex.ReflexChainID} к новому рефлексу {newId}",
                      "Предупреждение",
                      MessageBoxButton.OK,
                      MessageBoxImage.Warning);
                }
              }
              catch (Exception ex)
              {
                MessageBox.Show($"Ошибка привязки цепочки к рефлексу: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
              }
            }
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

          if (reflex.Id > 0)
          {
            if (_geneticReflexesSystem.RemoveGeneticReflex(reflex.Id))
              _geneticReflexesSystem.SaveGeneticReflexes();
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
          var removeAll = _geneticReflexesSystem.RemoveAllGeneticReflex();

          if (removeAll)
          {
            _allGeneticReflexes.Clear();

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

    /// <summary>
    /// Генерация рефлексов по всем состояниям и комбинациям стилей
    /// </summary>
    private void GenerateReflexes(object parameter)
    {
      if (!IsEditingEnabled)
      {
        MessageBox.Show("Генерация рефлексов доступна только в стадии 0 при выключенной пульсации",
            "Невозможно выполнить",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
      }

      var result = MessageBox.Show(
          "Вы действительно хотите сгенерировать безусловные рефлексы для всех состояний и комбинаций стилей?\n\n" +
          "Это действие:\n" +
          "• Создаст рефлексы для состояний: Плохо, Норма, Хорошо\n" +
          "• Использует все существующие комбинации стилей поведения\n" +
          "• Применит действие по умолчанию для всех рефлексов\n" +
          "• Автоматически заполнит дерево рефлексов и образы восприятия\n" +
          "• Существующие рефлексы не будут дублироваться\n\n" +
          "Существующие рефлексы будут сохранены, новые добавятся к ним.",
          "Подтверждение генерации рефлексов",
          MessageBoxButton.YesNo,
          MessageBoxImage.Question);

      if (result == MessageBoxResult.Yes)
      {
        try
        {
          var (success, createdCount, errorMessage) = _geneticReflexesSystem.CreateGeneticReflexesForAllStatesAndStyles(_gomeostas);

          if (success)
          {
            RefreshAllCollections();

            MessageBox.Show(errorMessage,
                "Генерация завершена",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
          }
          else
          {
            MessageBox.Show($"Не удалось сгенерировать рефлексы:\n{errorMessage}",
                "Ошибка генерации",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
          }
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Ошибка при генерации рефлексов:\n{ex.Message}",
              "Ошибка",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
        }
      }
    }

  }
}