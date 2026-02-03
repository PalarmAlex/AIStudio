using AIStudio.Common;
using ISIDA.Psychic.Automatism;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace AIStudio.ViewModels
{
  public class AutomatizmChainBindingViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly AutomatizmChainsSystem _chainsSystem;
    private readonly AutomatizmTreeSystem _treeSystem;
    private readonly int _treeNodeId;
    private readonly int _currentChainId;

    #region Properties

    private string _treeNodeInfo;
    public string TreeNodeInfo
    {
      get => _treeNodeInfo;
      set
      {
        if (_treeNodeInfo != value)
        {
          _treeNodeInfo = value;
          OnPropertyChanged(nameof(TreeNodeInfo));
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

    private int _selectedTreeNodeFilter;
    public int SelectedTreeNodeFilter
    {
      get => _selectedTreeNodeFilter;
      set
      {
        if (_selectedTreeNodeFilter != value)
        {
          _selectedTreeNodeFilter = value;
          OnPropertyChanged(nameof(SelectedTreeNodeFilter));
          FilterChains();
        }
      }
    }

    private string _chainIdFilter;
    public string ChainIdFilter
    {
      get => _chainIdFilter;
      set
      {
        if (_chainIdFilter != value)
        {
          _chainIdFilter = value;
          OnPropertyChanged(nameof(ChainIdFilter));
          FilterChains();
        }
      }
    }

    // Данные
    private List<AutomatizmChainsSystem.AutomatizmChain> _allChains;
    public List<AutomatizmChainsSystem.AutomatizmChain> AllChains
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

    private ObservableCollection<AutomatizmChainsSystem.AutomatizmChain> _filteredChains;
    public ObservableCollection<AutomatizmChainsSystem.AutomatizmChain> FilteredChains
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

    private AutomatizmChainsSystem.AutomatizmChain _selectedChain;
    public AutomatizmChainsSystem.AutomatizmChain SelectedChain
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

    private ObservableCollection<AutomatizmChainsSystem.ChainLink> _selectedChainLinks;
    public ObservableCollection<AutomatizmChainsSystem.ChainLink> SelectedChainLinks
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

    private AutomatizmChainsSystem.ChainLink _selectedLink;
    public AutomatizmChainsSystem.ChainLink SelectedLink
    {
      get => _selectedLink;
      set
      {
        if (_selectedLink != value)
        {
          _selectedLink = value;
          OnPropertyChanged(nameof(SelectedLink));
        }
      }
    }

    public bool IsChainSelected => SelectedChain != null;

    // Опции фильтров
    public List<KeyValuePair<int, string>> TreeNodeFilterOptions { get; private set; }

    #endregion

    #region Commands

    private ICommand _clearFiltersCommand;
    public ICommand ClearFiltersCommand => _clearFiltersCommand ?? (_clearFiltersCommand = new RelayCommand(ClearFilters));

    #endregion

    public AutomatizmChainBindingViewModel(int treeNodeId, int currentChainId,
        AutomatizmChainsSystem chainsSystem, AutomatizmTreeSystem treeSystem)
    {
      _treeNodeId = treeNodeId;
      _currentChainId = currentChainId;
      _chainsSystem = chainsSystem;
      _treeSystem = treeSystem;

      InitializeProperties();
      InitializeFilterOptions();
      LoadTreeNodeInfo();
    }

    private void LoadTreeNodeInfo()
    {
      try
      {
        var node = _treeSystem.GetNodeById(_treeNodeId);
        if (node != null)
        {
          TreeNodeInfo = $"Узел дерева ID: {_treeNodeId} | Базовое состояние: {GetBaseStateText(node.BaseID)}";
        }
        else
        {
          TreeNodeInfo = $"Узел дерева ID: {_treeNodeId} (не найден)";
        }
      }
      catch
      {
        TreeNodeInfo = $"Узел дерева ID: {_treeNodeId}";
      }
    }

    private string GetBaseStateText(int baseId)
    {
      switch (baseId)
      {
        case -1: return "Плохо";
        case 0: return "Норма";
        case 1: return "Хорошо";
        default: return $"Неизвестно ({baseId})";
      }
    }

    private void InitializeProperties()
    {
      ChainsInfo = "Загрузка...";
      SelectionInfo = "Выберите цепочку для просмотра звеньев";
      CurrentBindingInfo = _currentChainId > 0 ? $"Текущая привязка: цепочка {_currentChainId}" : "Текущая привязка: отсутствует";
      HasCurrentBinding = _currentChainId > 0;

      AllChains = new List<AutomatizmChainsSystem.AutomatizmChain>();
      FilteredChains = new ObservableCollection<AutomatizmChainsSystem.AutomatizmChain>();
      SelectedChainLinks = new ObservableCollection<AutomatizmChainsSystem.ChainLink>();

      ChainIdFilter = string.Empty;
      ChainNameFilter = string.Empty;
    }

    private void InitializeFilterOptions()
    {
      TreeNodeFilterOptions = new List<KeyValuePair<int, string>>
      {
          new KeyValuePair<int, string>(0, "Все цепочки"),
          new KeyValuePair<int, string>(1, "Свободные (нет узла)"),
          new KeyValuePair<int, string>(2, "Привязанные к узлам"),
          new KeyValuePair<int, string>(3, "К текущему узлу")
      };
      SelectedTreeNodeFilter = 0;
    }

    public void LoadData()
    {
      try
      {
        var allChainsDict = _chainsSystem.GetAllAutomatizmChains();
        AllChains = allChainsDict.Values
            .OrderBy(c => c.ID)
            .ToList();

        ChainsCount = AllChains.Count;
        ChainsInfo = $"Загружено цепочек: {ChainsCount}";

        FilterChains();
        UpdateCurrentBindingInfo();
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка загрузки цепочек: {ex.Message}",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        AllChains = new List<AutomatizmChainsSystem.AutomatizmChain>();
        FilteredChains = new ObservableCollection<AutomatizmChainsSystem.AutomatizmChain>();
      }
    }

    private void FilterChains()
    {
      if (AllChains == null) return;

      var filtered = AllChains.AsEnumerable();

      // Фильтр по ID (контекстный поиск)
      if (!string.IsNullOrWhiteSpace(ChainIdFilter))
      {
        filtered = filtered.Where(c =>
            c.ID.ToString().IndexOf(ChainIdFilter, StringComparison.OrdinalIgnoreCase) >= 0);
      }

      // Фильтр по названию
      if (!string.IsNullOrWhiteSpace(ChainNameFilter))
      {
        filtered = filtered.Where(c =>
            (c.Name != null && c.Name.IndexOf(ChainNameFilter, StringComparison.OrdinalIgnoreCase) >= 0) ||
            (c.Description != null && c.Description.IndexOf(ChainNameFilter, StringComparison.OrdinalIgnoreCase) >= 0));
      }

      // Фильтр по узлу дерева
      if (SelectedTreeNodeFilter > 0)
      {
        filtered = filtered.Where(c =>
        {
          if (SelectedTreeNodeFilter == 1) return c.TreeNodeId == 0; // Свободные
          if (SelectedTreeNodeFilter == 2) return c.TreeNodeId > 0;  // Привязанные к узлам
          if (SelectedTreeNodeFilter == 3) return c.TreeNodeId == _treeNodeId; // К текущему узлу
          return true;
        });
      }

      FilteredChains = new ObservableCollection<AutomatizmChainsSystem.AutomatizmChain>(filtered);

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
      SelectedChainLinks = new ObservableCollection<AutomatizmChainsSystem.ChainLink>(links);
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

      string bindingInfo = SelectedChain.TreeNodeId > 0
          ? $"Привязана к узлу дерева: {SelectedChain.TreeNodeId}"
          : "Не привязана к узлу дерева";

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

    private void ClearFilters(object parameter = null)
    {
      ChainIdFilter = string.Empty;
      ChainNameFilter = string.Empty;
      SelectedTreeNodeFilter = 0;
      FilterChains();
    }
  }
}