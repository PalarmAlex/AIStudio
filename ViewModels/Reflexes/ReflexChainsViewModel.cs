//using AIStudio.Common;
//using ISIDA.Actions;
//using ISIDA.Reflexes;
//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.ComponentModel;
//using System.Diagnostics;
//using System.Linq;
//using System.Windows;
//using System.Windows.Data;
//using System.Windows.Input;
//using static ISIDA.Reflexes.ReflexChainsSystem;

//namespace AIStudio.ViewModels
//{
//  public class ReflexChainsViewModel : INotifyPropertyChanged
//  {
//    public event PropertyChangedEventHandler PropertyChanged;
//    protected virtual void OnPropertyChanged(string propertyName)
//    {
//      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//    }

//    private readonly ReflexChainsSystem _reflexChainsSystem;
//    private readonly GeneticReflexesSystem _geneticReflexesSystem;
//    private readonly AdaptiveActionsSystem _actionsSystem;

//    private ObservableCollection<ReflexChain> _allReflexChains = new ObservableCollection<ReflexChain>();
//    private ICollectionView _reflexChainsView;
//    public ICollectionView ReflexChainsView => _reflexChainsView;

//    private ReflexChain _selectedChain;
//    public ReflexChain SelectedChain
//    {
//      get => _selectedChain;
//      set
//      {
//        _selectedChain = value;
//        OnPropertyChanged(nameof(SelectedChain));
//        OnPropertyChanged(nameof(IsChainSelected));
//        OnPropertyChanged(nameof(ChainValidationResult));
//        LoadChainLinks();
//      }
//    }

//    private ObservableCollection<ChainLink> _currentChainLinks = new ObservableCollection<ChainLink>();
//    public ObservableCollection<ChainLink> CurrentChainLinks => _currentChainLinks;

//    private ChainLink _selectedLink;
//    public ChainLink SelectedLink
//    {
//      get => _selectedLink;
//      set
//      {
//        _selectedLink = value;
//        OnPropertyChanged(nameof(SelectedLink));
//        OnPropertyChanged(nameof(IsLinkSelected));
//      }
//    }

//    public bool IsChainSelected => SelectedChain != null;
//    public bool IsLinkSelected => SelectedLink != null;

//    private int _localLinkIdCounter = 1;
//    private bool _hasReflexes = false;

//    // Фильтры
//    private string _nameFilter;
//    public string NameFilter
//    {
//      get => _nameFilter;
//      set
//      {
//        _nameFilter = value;
//        OnPropertyChanged(nameof(NameFilter));
//        ApplyFilters();
//      }
//    }

//    private int? _selectedPriorityFilter;
//    public int? SelectedPriorityFilter
//    {
//      get => _selectedPriorityFilter;
//      set
//      {
//        _selectedPriorityFilter = value;
//        OnPropertyChanged(nameof(SelectedPriorityFilter));
//        ApplyFilters();
//      }
//    }

//    // Команды
//    public ICommand SaveCommand { get; }
//    public ICommand RemoveChainCommand { get; }
//    public ICommand AddChainCommand { get; }
//    public ICommand AddLinkCommand { get; }
//    public ICommand RemoveLinkCommand { get; }
//    public ICommand ValidateChainCommand { get; }
//    public ICommand ClearFiltersCommand { get; }
//    public ICommand UpdateLinkCommand { get; }

//    public string ChainValidationResult
//    {
//      get
//      {
//        if (!IsChainSelected) return "Не выбрана цепочка";

//        var (isValid, issues) = _reflexChainsSystem.ValidateChain(SelectedChain.ID);
//        if (isValid)
//          return "✓ Цепочка валидна";
//        else
//          return $"✗ Ошибки: {string.Join("; ", issues.Take(3))}...";
//      }
//    }

//    public bool CanCreateChains => _hasReflexes;

//    public List<KeyValuePair<int?, string>> PriorityFilterOptions { get; } = new List<KeyValuePair<int?, string>>
//        {
//            new KeyValuePair<int?, string>(null, "Все приоритеты"),
//            new KeyValuePair<int?, string>(1, "Низкий (1)"),
//            new KeyValuePair<int?, string>(5, "Средний (5)"),
//            new KeyValuePair<int?, string>(10, "Высокий (10)")
//        };

//    public List<KeyValuePair<int, string>> ReflexOptions { get; private set; } = new List<KeyValuePair<int, string>>();
//    public List<KeyValuePair<int, string>> LinkOptions { get; private set; } = new List<KeyValuePair<int, string>>();

//    public ReflexChainsViewModel(
//        ReflexChainsSystem reflexChainsSystem,
//        GeneticReflexesSystem geneticReflexesSystem,
//        AdaptiveActionsSystem actionsSystem)
//    {
//      _reflexChainsSystem = reflexChainsSystem ?? throw new ArgumentNullException(nameof(reflexChainsSystem));
//      _geneticReflexesSystem = geneticReflexesSystem ?? throw new ArgumentNullException(nameof(geneticReflexesSystem));
//      _actionsSystem = actionsSystem ?? throw new ArgumentNullException(nameof(actionsSystem));

//      // Проверяем наличие рефлексов
//      CheckReflexesAvailability();

//      _reflexChainsView = CollectionViewSource.GetDefaultView(_allReflexChains);
//      _reflexChainsView.Filter = FilterReflexChains;

//      SaveCommand = new RelayCommand(SaveData, _ => CanCreateChains);
//      RemoveChainCommand = new RelayCommand(RemoveSelectedChain, _ => IsChainSelected);
//      AddChainCommand = new RelayCommand(AddNewChain, _ => CanCreateChains);
//      AddLinkCommand = new RelayCommand(AddNewLink, _ => IsChainSelected && CanCreateChains);
//      RemoveLinkCommand = new RelayCommand(RemoveSelectedLink, _ => IsLinkSelected);
//      ValidateChainCommand = new RelayCommand(ValidateSelectedChain, _ => IsChainSelected);
//      ClearFiltersCommand = new RelayCommand(ClearFilters);
//      UpdateLinkCommand = new RelayCommand(UpdateSelectedLink, _ => IsLinkSelected);

//      if (_hasReflexes)
//      {
//        LoadAllCollections();
//        LoadFilterOptions();
//      }
//    }

//    private void CheckReflexesAvailability()
//    {
//      var allReflexes = _geneticReflexesSystem.GetAllGeneticReflexes();
//      _hasReflexes = allReflexes.Any();

//      if (!_hasReflexes)
//      {
//        MessageBox.Show("Нельзя работать с цепочками рефлексов: нет доступных рефлексов.\nСначала создайте безусловные рефлексы на соответствующей странице.",
//            "Нет рефлексов",
//            MessageBoxButton.OK,
//            MessageBoxImage.Warning);
//      }

//      OnPropertyChanged(nameof(CanCreateChains));
//    }

//    private bool FilterReflexChains(object item)
//    {
//      if (!(item is ReflexChain chain))
//        return false;

//      return (string.IsNullOrEmpty(NameFilter) ||
//             chain.Name.IndexOf(NameFilter, StringComparison.OrdinalIgnoreCase) >= 0) &&
//             (!SelectedPriorityFilter.HasValue || chain.Priority == SelectedPriorityFilter.Value);
//    }

//    private void ApplyFilters()
//    {
//      _reflexChainsView.Refresh();
//    }

//    private void ClearFilters(object parameter = null)
//    {
//      NameFilter = string.Empty;
//      SelectedPriorityFilter = null;
//    }

//    private void LoadAllCollections()
//    {
//      _allReflexChains.Clear();
//      _currentChainLinks.Clear();
//      _localLinkIdCounter = 1;

//      var allChains = _reflexChainsSystem.GetAllReflexChains();
//      foreach (var chain in allChains.Values.OrderBy(c => c.ID))
//      {
//        _allReflexChains.Add(chain);
//      }

//      LoadFilterOptions();
//      OnPropertyChanged(nameof(ReflexChainsView));
//    }

//    private void LoadFilterOptions()
//    {
//      // Загружаем список рефлексов
//      ReflexOptions.Clear();
//      var allReflexes = _geneticReflexesSystem.GetAllGeneticReflexes();
//      foreach (var reflex in allReflexes.OrderBy(r => r.Id))
//      {
//        string reflexName = $"Рефлекс {reflex.Id} (L1:{reflex.Level1})";
//        ReflexOptions.Add(new KeyValuePair<int, string>(reflex.Id, reflexName));
//      }

//      // Загружаем список звеньев для выбранной цепочки
//      UpdateLinkOptions();

//      OnPropertyChanged(nameof(ReflexOptions));
//      OnPropertyChanged(nameof(LinkOptions));
//    }

//    private void UpdateLinkOptions()
//    {
//      LinkOptions.Clear();
//      LinkOptions.Add(new KeyValuePair<int, string>(0, "Нет следующего"));

//      if (SelectedChain != null)
//      {
//        foreach (var link in SelectedChain.Links.OrderBy(l => l.ID))
//        {
//          if (link.ID == SelectedLink?.ID) continue;
//          LinkOptions.Add(new KeyValuePair<int, string>(link.ID, $"Звено {link.ID}"));
//        }
//      }

//      OnPropertyChanged(nameof(LinkOptions));
//    }

//    private void LoadChainLinks()
//    {
//      _currentChainLinks.Clear();
//      SelectedLink = null;

//      if (SelectedChain != null)
//      {
//        foreach (var link in SelectedChain.Links.OrderBy(l => l.ID))
//        {
//          _currentChainLinks.Add(link);
//        }

//        // Обновляем счетчик на основе максимального ID
//        _localLinkIdCounter = _currentChainLinks.Any() ?
//            _currentChainLinks.Max(l => l.ID) + 1 : 1;
//      }

//      UpdateLinkOptions();
//      OnPropertyChanged(nameof(CurrentChainLinks));
//      OnPropertyChanged(nameof(ChainValidationResult));
//    }

//    private void SaveData(object parameter)
//    {
//      try
//      {
//        // Сначала обновляем все изменения в текущей цепочке
//        if (SelectedChain != null)
//        {
//          UpdateAllLinksInSelectedChain();
//        }

//        var (success, error) = _reflexChainsSystem.SaveReflexChains();
//        if (success)
//        {
//          LoadAllCollections();
//          MessageBox.Show("Цепочки рефлексов успешно сохранены",
//              "Сохранение завершено",
//              MessageBoxButton.OK,
//              MessageBoxImage.Information);
//        }
//        else
//        {
//          MessageBox.Show($"Не удалось сохранить цепочки рефлексов:\n{error}",
//              "Ошибка сохранения",
//              MessageBoxButton.OK,
//              MessageBoxImage.Error);
//        }
//      }
//      catch (Exception ex)
//      {
//        MessageBox.Show($"Ошибка при сохранении:\n{ex.Message}",
//            "Ошибка",
//            MessageBoxButton.OK,
//            MessageBoxImage.Error);
//      }
//    }

//    private void UpdateAllLinksInSelectedChain()
//    {
//      if (SelectedChain == null) return;

//      foreach (var link in _currentChainLinks)
//      {
//        try
//        {
//          _reflexChainsSystem.UpdateChainLink(
//              SelectedChain.ID,
//              link.ID,
//              link.ReflexID,
//              link.SuccessNextLink,
//              link.FailureNextLink,
//              link.IsTerminal,
//              link.Description);
//        }
//        catch (Exception ex)
//        {
//          Debug.WriteLine($"Ошибка обновления звена {link.ID}: {ex.Message}");
//        }
//      }
//    }

//    private void UpdateSelectedLink(object parameter)
//    {
//      if (!IsLinkSelected) return;

//      try
//      {
//        var (success, warnings) = _reflexChainsSystem.UpdateChainLink(
//            SelectedChain.ID,
//            SelectedLink.ID,
//            SelectedLink.ReflexID,
//            SelectedLink.SuccessNextLink,
//            SelectedLink.FailureNextLink,
//            SelectedLink.IsTerminal,
//            SelectedLink.Description);

//        var filteredWarnings = warnings.Where(w => !w.Contains("Рефлекс с ID 0 не существует"));
//        if (filteredWarnings.Any())
//        {
//          MessageBox.Show($"Предупреждения при обновлении звена:\n{string.Join("\n", filteredWarnings)}",
//              "Предупреждения",
//              MessageBoxButton.OK,
//              MessageBoxImage.Warning);
//        }

//        if (success)
//        {
//          MessageBox.Show("Звено успешно обновлено",
//              "Успех",
//              MessageBoxButton.OK,
//              MessageBoxImage.Information);
//          LoadChainLinks(); // Перезагружаем для синхронизации
//        }
//      }
//      catch (Exception ex)
//      {
//        MessageBox.Show($"Ошибка при обновлении звена:\n{ex.Message}",
//            "Ошибка",
//            MessageBoxButton.OK,
//            MessageBoxImage.Error);
//      }
//    }

//    private void AddNewChain(object parameter)
//    {
//      try
//      {
//        if (!_hasReflexes)
//        {
//          MessageBox.Show("Нельзя создать цепочку рефлексов: нет доступных рефлексов.",
//              "Нет рефлексов",
//              MessageBoxButton.OK,
//              MessageBoxImage.Warning);
//          return;
//        }

//        // Берем первый рефлекс из списка
//        int defaultReflexId = ReflexOptions.First().Key;

//        // Создаем новую цепочку с одним стартовым звеном
//        var startLink = new ChainLink
//        {
//          ID = 1,
//          ReflexID = defaultReflexId,
//          SuccessNextLink = 0,
//          FailureNextLink = 0,
//          IsTerminal = false,
//          Description = "Стартовое звено"
//        };

//        var links = new List<ChainLink> { startLink };

//        var (chainId, warnings) = _reflexChainsSystem.AddReflexChain(
//            "Новая цепочка",
//            "Описание цепочки",
//            5,
//            links);

//        // Фильтруем предупреждения
//        var filteredWarnings = warnings.Where(w => !w.Contains("Рефлекс с ID") || w.Contains(defaultReflexId.ToString()));
//        if (filteredWarnings.Any())
//        {
//          MessageBox.Show($"Предупреждения при создании цепочки:\n{string.Join("\n", filteredWarnings)}",
//              "Предупреждения",
//              MessageBoxButton.OK,
//              MessageBoxImage.Warning);
//        }

//        // Сохраняем сразу
//        _reflexChainsSystem.SaveReflexChains();
//        LoadAllCollections();
//        SelectedChain = _allReflexChains.FirstOrDefault(c => c.ID == chainId);
//      }
//      catch (Exception ex)
//      {
//        MessageBox.Show($"Ошибка при создании цепочки:\n{ex.Message}",
//            "Ошибка",
//            MessageBoxButton.OK,
//            MessageBoxImage.Error);
//      }
//    }

//    private void RemoveSelectedChain(object parameter)
//    {
//      if (SelectedChain == null) return;

//      var result = MessageBox.Show(
//          $"Вы действительно хотите удалить цепочку '{SelectedChain.Name}' (ID: {SelectedChain.ID})?",
//          "Подтверждение удаления",
//          MessageBoxButton.YesNo,
//          MessageBoxImage.Question);

//      if (result == MessageBoxResult.Yes)
//      {
//        try
//        {
//          if (_reflexChainsSystem.RemoveReflexChain(SelectedChain.ID))
//          {
//            // Сохраняем изменения
//            var (saveSuccess, error) = _reflexChainsSystem.SaveReflexChains();

//            if (saveSuccess)
//            {
//              LoadAllCollections();
//              SelectedChain = null;
//              MessageBox.Show("Цепочка успешно удалена",
//                  "Удаление завершено",
//                  MessageBoxButton.OK,
//                  MessageBoxImage.Information);
//            }
//            else
//            {
//              MessageBox.Show($"Цепочка удалена из памяти, но не сохранена:\n{error}",
//                  "Ошибка сохранения",
//                  MessageBoxButton.OK,
//                  MessageBoxImage.Error);
//              LoadAllCollections();
//            }
//          }
//        }
//        catch (Exception ex)
//        {
//          MessageBox.Show($"Ошибка удаления цепочки:\n{ex.Message}",
//              "Ошибка",
//              MessageBoxButton.OK,
//              MessageBoxImage.Error);
//        }
//      }
//    }

//    private void AddNewLink(object parameter)
//    {
//      if (!IsChainSelected || !_hasReflexes) return;

//      try
//      {
//        int nextLinkId = _localLinkIdCounter++;

//        // Берем первый рефлекс из списка
//        int defaultReflexId = ReflexOptions.First().Key;

//        var (linkId, warnings) = _reflexChainsSystem.AddChainLink(
//            SelectedChain.ID,
//            defaultReflexId,
//            0,
//            0,
//            false,
//            $"Звено {nextLinkId}");

//        // Фильтруем предупреждения
//        var filteredWarnings = warnings.Where(w => !w.Contains("Рефлекс с ID 0 не существует"));
//        if (filteredWarnings.Any())
//        {
//          MessageBox.Show($"Предупреждения при добавлении звена:\n{string.Join("\n", filteredWarnings)}",
//              "Предупреждения",
//              MessageBoxButton.OK,
//              MessageBoxImage.Warning);
//        }

//        LoadChainLinks();
//        SelectedLink = _currentChainLinks.FirstOrDefault(l => l.ID == linkId);
//      }
//      catch (Exception ex)
//      {
//        _localLinkIdCounter--; // Откатываем счетчик при ошибке
//        MessageBox.Show($"Ошибка при добавлении звена:\n{ex.Message}",
//            "Ошибка",
//            MessageBoxButton.OK,
//            MessageBoxImage.Error);
//      }
//    }

//    private void RemoveSelectedLink(object parameter)
//    {
//      if (!IsLinkSelected) return;

//      var result = MessageBox.Show(
//          $"Вы действительно хотите удалить звено {SelectedLink.ID}?",
//          "Подтверждение удаления",
//          MessageBoxButton.YesNo,
//          MessageBoxImage.Question);

//      if (result == MessageBoxResult.Yes)
//      {
//        try
//        {
//          if (_reflexChainsSystem.RemoveChainLink(SelectedChain.ID, SelectedLink.ID, true))
//          {
//            LoadChainLinks();
//          }
//        }
//        catch (Exception ex)
//        {
//          MessageBox.Show($"Ошибка удаления звена:\n{ex.Message}",
//              "Ошибка",
//              MessageBoxButton.OK,
//              MessageBoxImage.Error);
//        }
//      }
//    }

//    private void ValidateSelectedChain(object parameter)
//    {
//      if (!IsChainSelected) return;

//      var (isValid, issues) = _reflexChainsSystem.ValidateChain(SelectedChain.ID);

//      if (isValid)
//      {
//        MessageBox.Show("Цепочка валидна. Все проверки пройдены успешно.",
//            "Валидация пройдена",
//            MessageBoxButton.OK,
//            MessageBoxImage.Information);
//      }
//      else
//      {
//        string message = $"Обнаружены ошибки в цепочке:\n\n{string.Join("\n", issues)}";
//        MessageBox.Show(message,
//            "Ошибки валидации",
//            MessageBoxButton.OK,
//            MessageBoxImage.Warning);
//      }
//    }
//  }
//}