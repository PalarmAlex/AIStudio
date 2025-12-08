using AIStudio.Common;
using ISIDA.Actions;
using ISIDA.Reflexes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace AIStudio.ViewModels
{
  public class ReflexChainBindingViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly ReflexChainsSystem _chainsSystem;
    private readonly GeneticReflexesSystem _reflexesSystem;
    private readonly int _reflexId;
    private readonly int _currentChainId;

    #region Properties

    private string _reflexInfo;
    public string ReflexInfo
    {
      get => _reflexInfo;
      set
      {
        if (_reflexInfo != value)
        {
          _reflexInfo = value;
          OnPropertyChanged(nameof(ReflexInfo));
        }
      }
    }

    private string _chainsInfo;
    public string ChainsInfo
    {
      get => _chainsInfo;
      set
      {
        if (_chainsInfo != value)
        {
          _chainsInfo = value;
          OnPropertyChanged(nameof(ChainsInfo));
        }
      }
    }

    private string _selectionInfo;
    public string SelectionInfo
    {
      get => _selectionInfo;
      set
      {
        if (_selectionInfo != value)
        {
          _selectionInfo = value;
          OnPropertyChanged(nameof(SelectionInfo));
        }
      }
    }

    private string _currentBindingInfo;
    public string CurrentBindingInfo
    {
      get => _currentBindingInfo;
      set
      {
        if (_currentBindingInfo != value)
        {
          _currentBindingInfo = value;
          OnPropertyChanged(nameof(CurrentBindingInfo));
        }
      }
    }

    private bool _hasCurrentBinding;
    public bool HasCurrentBinding
    {
      get => _hasCurrentBinding;
      set
      {
        if (_hasCurrentBinding != value)
        {
          _hasCurrentBinding = value;
          OnPropertyChanged(nameof(HasCurrentBinding));
        }
      }
    }

    private int _chainsCount;
    public int ChainsCount
    {
      get => _chainsCount;
      set
      {
        if (_chainsCount != value)
        {
          _chainsCount = value;
          OnPropertyChanged(nameof(ChainsCount));
        }
      }
    }

    private int _linksCount;
    public int LinksCount
    {
      get => _linksCount;
      set
      {
        if (_linksCount != value)
        {
          _linksCount = value;
          OnPropertyChanged(nameof(LinksCount));
        }
      }
    }

    private string _selectedChainInfo;
    public string SelectedChainInfo
    {
      get => _selectedChainInfo;
      set
      {
        if (_selectedChainInfo != value)
        {
          _selectedChainInfo = value;
          OnPropertyChanged(nameof(SelectedChainInfo));
        }
      }
    }

    // Фильтры
    private string _chainNameFilter;
    public string ChainNameFilter
    {
      get => _chainNameFilter;
      set
      {
        if (_chainNameFilter != value)
        {
          _chainNameFilter = value;
          OnPropertyChanged(nameof(ChainNameFilter));
          FilterChains();
        }
      }
    }

    private int _selectedBindingFilter;
    public int SelectedBindingFilter
    {
      get => _selectedBindingFilter;
      set
      {
        if (_selectedBindingFilter != value)
        {
          _selectedBindingFilter = value;
          OnPropertyChanged(nameof(SelectedBindingFilter));
          FilterChains();
        }
      }
    }

    // Данные
    private List<ReflexChainsSystem.ReflexChain> _allChains;
    public List<ReflexChainsSystem.ReflexChain> AllChains
    {
      get => _allChains;
      set
      {
        if (_allChains != value)
        {
          _allChains = value;
          OnPropertyChanged(nameof(AllChains));
        }
      }
    }

    private ObservableCollection<ReflexChainsSystem.ReflexChain> _filteredChains;
    public ObservableCollection<ReflexChainsSystem.ReflexChain> FilteredChains
    {
      get => _filteredChains;
      set
      {
        if (_filteredChains != value)
        {
          _filteredChains = value;
          OnPropertyChanged(nameof(FilteredChains));
        }
      }
    }

    private ReflexChainsSystem.ReflexChain _selectedChain;
    public ReflexChainsSystem.ReflexChain SelectedChain
    {
      get => _selectedChain;
      set
      {
        if (_selectedChain != value)
        {
          _selectedChain = value;
          OnPropertyChanged(nameof(SelectedChain));
          UpdateSelectedChainLinks();
          UpdateSelectionInfo();
          OnPropertyChanged(nameof(IsChainSelected));
        }
      }
    }

    private ObservableCollection<ReflexChainsSystem.ChainLink> _selectedChainLinks;
    public ObservableCollection<ReflexChainsSystem.ChainLink> SelectedChainLinks
    {
      get => _selectedChainLinks;
      set
      {
        if (_selectedChainLinks != value)
        {
          _selectedChainLinks = value;
          OnPropertyChanged(nameof(SelectedChainLinks));
        }
      }
    }

    public bool IsChainSelected => SelectedChain != null;

    // Опции фильтров
    public List<KeyValuePair<int, string>> BindingFilterOptions { get; private set; }

    #endregion

    #region Commands

    private ICommand _clearFiltersCommand;
    public ICommand ClearFiltersCommand => _clearFiltersCommand ?? (_clearFiltersCommand = new RelayCommand(ClearFilters));

    #endregion

    public ReflexChainBindingViewModel(int reflexId, int currentChainId,
        ReflexChainsSystem chainsSystem, GeneticReflexesSystem reflexesSystem)
    {
      _reflexId = reflexId;
      _currentChainId = currentChainId;
      _chainsSystem = chainsSystem;
      _reflexesSystem = reflexesSystem;

      InitializeProperties();
      InitializeFilterOptions();
    }

    private void InitializeProperties()
    {
      ReflexInfo = $"Рефлекс ID: {_reflexId}";
      ChainsInfo = "Загрузка...";
      SelectionInfo = "Выберите цепочку для просмотра звеньев";
      CurrentBindingInfo = _currentChainId > 0 ? $"Текущая привязка: цепочка {_currentChainId}" : "Текущая привязка: отсутствует";
      HasCurrentBinding = _currentChainId > 0;

      AllChains = new List<ReflexChainsSystem.ReflexChain>();
      FilteredChains = new ObservableCollection<ReflexChainsSystem.ReflexChain>();
      SelectedChainLinks = new ObservableCollection<ReflexChainsSystem.ChainLink>();
    }

    private void InitializeFilterOptions()
    {
      BindingFilterOptions = new List<KeyValuePair<int, string>>
      {
          new KeyValuePair<int, string>(0, "Все цепочки"),
          new KeyValuePair<int, string>(1, "Свободные"),
          new KeyValuePair<int, string>(2, "Привязанные"),
          new KeyValuePair<int, string>(3, "К текущему рефлексу")
      };
      SelectedBindingFilter = 0;
    }

    public Dictionary<int, string> ChainReflexesInfo { get; set; }
    public void LoadData()
    {
      try
      {
        var allChainsDict = _chainsSystem.GetAllReflexChains();
        AllChains = allChainsDict.Values
            .OrderBy(c => c.ID)
            .ToList();

        ChainsCount = AllChains.Count;
        ChainsInfo = $"Загружено цепочек: {ChainsCount}";

        ChainReflexesInfo = new Dictionary<int, string>();
        foreach (var chain in AllChains)
        {
          var reflexes = _reflexesSystem.GetReflexesForChain(chain.ID);
          ChainReflexesInfo[chain.ID] = reflexes.Any() ? string.Join(", ", reflexes) : "Нет";
        }

        FilterChains();
        UpdateCurrentBindingInfo();
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка загрузки цепочек: {ex.Message}",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        AllChains = new List<ReflexChainsSystem.ReflexChain>();
        FilteredChains = new ObservableCollection<ReflexChainsSystem.ReflexChain>();
      }
    }

    private void FilterChains()
    {
      if (AllChains == null) return;

      var filtered = AllChains.AsEnumerable();

      // Фильтр по названию
      if (!string.IsNullOrWhiteSpace(ChainNameFilter))
      {
        filtered = filtered.Where(c =>
            (c.Name != null && c.Name.IndexOf(ChainNameFilter, StringComparison.OrdinalIgnoreCase) >= 0) ||
            (c.Description != null && c.Description.IndexOf(ChainNameFilter, StringComparison.OrdinalIgnoreCase) >= 0));
      }

      // Фильтр по привязке
      if (SelectedBindingFilter > 0)
      {
        filtered = filtered.Where(c =>
        {
          var reflexes = _reflexesSystem.GetReflexesForChain(c.ID);
          bool hasBinding = reflexes.Any();

          if (SelectedBindingFilter == 1) return !hasBinding; // Свободные
          if (SelectedBindingFilter == 2) return hasBinding;  // Привязанные
          if (SelectedBindingFilter == 3) return reflexes.Contains(_reflexId); // К текущему рефлексу
          return true;
        });
      }

      FilteredChains = new ObservableCollection<ReflexChainsSystem.ReflexChain>(filtered);

      if (SelectedChain != null && !FilteredChains.Contains(SelectedChain))
      {
        SelectedChain = null;
      }
    }

    private void UpdateSelectedChainLinks()
    {
      if (SelectedChain == null)
      {
        SelectedChainLinks.Clear();
        LinksCount = 0;
        SelectedChainInfo = "Цепочка не выбрана";
        return;
      }

      var links = _chainsSystem.GetChainLinks(SelectedChain.ID);
      SelectedChainLinks = new ObservableCollection<ReflexChainsSystem.ChainLink>(links);
      LinksCount = links.Count;
      SelectedChainInfo = $"Цепочка: {SelectedChain.Name} (ID: {SelectedChain.ID})";
    }

    public void UpdateSelectionInfo()
    {
      if (SelectedChain == null)
      {
        SelectionInfo = "Выберите цепочку для просмотра звеньев";
        return;
      }

      var reflexes = _reflexesSystem.GetReflexesForChain(SelectedChain.ID);
      string bindingInfo = reflexes.Any()
          ? $"Привязана к рефлексам: {string.Join(", ", reflexes)}"
          : "Не привязана к рефлексам";

      // Убрали приоритет из вывода информации
      SelectionInfo = $"Выбрана цепочка: {SelectedChain.Name} (ID: {SelectedChain.ID}) - {bindingInfo}";
    }

    private void UpdateCurrentBindingInfo()
    {
      if (_currentChainId > 0)
      {
        var chain = AllChains.FirstOrDefault(c => c.ID == _currentChainId);
        if (chain != null)
        {
          CurrentBindingInfo = $"Текущая привязка: {chain.Name} (ID: {chain.ID})";
          HasCurrentBinding = true;
          return;
        }
      }

      CurrentBindingInfo = "Текущая привязка: отсутствует";
      HasCurrentBinding = false;
    }

    public string GetReflexesForChain(int chainId)
    {
      try
      {
        var reflexes = _reflexesSystem.GetReflexesForChain(chainId);
        return reflexes.Any() ? string.Join(", ", reflexes) : "";
      }
      catch
      {
        return "";
      }
    }

    private void ClearFilters(object parameter = null)
    {
      ChainNameFilter = string.Empty;
      SelectedBindingFilter = 0;
      FilterChains();
    }

    public string GetChainBindingText(int chainId)
    {
      try
      {
        var reflexes = _reflexesSystem.GetReflexesForChain(chainId);
        return reflexes.Any() ? string.Join(", ", reflexes) : "Нет";
      }
      catch
      {
        return "Ошибка";
      }
    }
  }
}