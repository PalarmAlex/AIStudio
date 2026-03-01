using AIStudio.Common;
using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Reflexes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace AIStudio.ViewModels
{
  public class ReflexChainsViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly ReflexChainsSystem _chainsSystem;
    private readonly GeneticReflexesSystem _reflexesSystem;
    private readonly AdaptiveActionsSystem _actionsSystem;

    private readonly ObservableCollection<ChainDisplayItem> _allChains = new ObservableCollection<ChainDisplayItem>();
    private readonly ObservableCollection<ChainDisplayItem> _displayChains = new ObservableCollection<ChainDisplayItem>();
    private readonly Dictionary<int, string> _actionNameById = new Dictionary<int, string>();

    public List<KeyValuePair<int?, string>> PageSizeOptions { get; } = new List<KeyValuePair<int?, string>>
    {
      new KeyValuePair<int?, string>(100, "100"),
      new KeyValuePair<int?, string>(500, "500"),
      new KeyValuePair<int?, string>(1000, "1000"),
      new KeyValuePair<int?, string>(5000, "5000"),
      new KeyValuePair<int?, string>(10000, "10000"),
      new KeyValuePair<int?, string>(null, "Все")
    };

    private int? _selectedPageSize = 100;
    public int? SelectedPageSize
    {
      get => _selectedPageSize;
      set
      {
        _selectedPageSize = value;
        OnPropertyChanged(nameof(SelectedPageSize));
        RefreshDisplay();
      }
    }

    public string DisplayCountText
    {
      get
      {
        int filtered = _allChains.Count(FilterChains);
        int shown = Math.Min(filtered, SelectedPageSize ?? int.MaxValue);
        return filtered == shown ? $"Показано: {shown}" : $"Показано: {shown} из {filtered}";
      }
    }

    private string _filterReflexId;
    private string _filterChainId;
    private int _selectedActionFilterId;
    private string _filterChainName;

    public ICollectionView ChainsView { get; }
    public ICommand ClearFiltersCommand { get; }
    public ICommand ApplyFiltersCommand { get; }
    public List<KeyValuePair<int, string>> ActionFilterOptions { get; } = new List<KeyValuePair<int, string>>();

    public string FilterReflexId
    {
      get => _filterReflexId;
      set
      {
        _filterReflexId = value;
        OnPropertyChanged(nameof(FilterReflexId));
      }
    }

    public string FilterChainId
    {
      get => _filterChainId;
      set
      {
        _filterChainId = value;
        OnPropertyChanged(nameof(FilterChainId));
      }
    }

    public int SelectedActionFilterId
    {
      get => _selectedActionFilterId;
      set
      {
        _selectedActionFilterId = value;
        OnPropertyChanged(nameof(SelectedActionFilterId));
      }
    }

    public string FilterChainName
    {
      get => _filterChainName;
      set
      {
        _filterChainName = value;
        OnPropertyChanged(nameof(FilterChainName));
      }
    }

    public ReflexChainsViewModel(
      ReflexChainsSystem chainsSystem,
      GeneticReflexesSystem reflexesSystem,
      AdaptiveActionsSystem actionsSystem)
    {
      _chainsSystem = chainsSystem ?? throw new ArgumentNullException(nameof(chainsSystem));
      _reflexesSystem = reflexesSystem ?? throw new ArgumentNullException(nameof(reflexesSystem));
      _actionsSystem = actionsSystem ?? throw new ArgumentNullException(nameof(actionsSystem));

      ChainsView = CollectionViewSource.GetDefaultView(_displayChains);

      ClearFiltersCommand = new RelayCommand(ClearFilters);
      ApplyFiltersCommand = new RelayCommand(_ => ApplyFilters());

      LoadActionNames();
      InitializeActionFilterOptions();
      LoadChainsData();
    }

    private void LoadActionNames()
    {
      _actionNameById.Clear();

      foreach (var action in _actionsSystem.GetAllAdaptiveActions())
      {
        _actionNameById[action.Id] = action.Name;
      }
    }

    private void InitializeActionFilterOptions()
    {
      ActionFilterOptions.Clear();
      ActionFilterOptions.Add(new KeyValuePair<int, string>(0, "Все действия"));

      foreach (var action in _actionsSystem.GetAllAdaptiveActions().OrderBy(a => a.Name))
      {
        ActionFilterOptions.Add(new KeyValuePair<int, string>(action.Id, action.Name));
      }

      SelectedActionFilterId = 0;
    }

    private void LoadChainsData()
    {
      try
      {
        _allChains.Clear();

        var allChains = _chainsSystem.GetAllReflexChains();
        foreach (var chain in allChains.Values.OrderBy(c => c.ID))
        {
          var linkedReflexIds = _reflexesSystem.GetReflexesForChain(chain.ID)
            .OrderBy(id => id)
            .ToList();

          var reflexIdsText = linkedReflexIds.Any()
            ? string.Join(", ", linkedReflexIds)
            : string.Empty;

          var reflexIdsTooltip = linkedReflexIds.Any()
            ? $"Привязана к рефлексам: {reflexIdsText}"
            : "Цепочка пока не привязана ни к одному рефлексу";

          var links = (chain.Links ?? new List<ReflexChainsSystem.ChainLink>())
            .OrderBy(l => l.ID)
            .ToList();

          if (!links.Any())
          {
            _allChains.Add(new ChainDisplayItem
            {
              ChainId = chain.ID,
              ChainName = chain.Name ?? string.Empty,
              ChainDescription = chain.Description ?? string.Empty,
              ReflexIds = linkedReflexIds,
              ReflexIdsText = reflexIdsText,
              ReflexIdsTooltip = reflexIdsTooltip
            });
            continue;
          }

          foreach (var link in links)
          {
            var actionText = GetActionText(link.ActionId);
            _allChains.Add(new ChainDisplayItem
            {
              ChainId = chain.ID,
              ChainName = chain.Name ?? string.Empty,
              ChainDescription = chain.Description ?? string.Empty,
              ReflexIds = linkedReflexIds,
              ReflexIdsText = reflexIdsText,
              ReflexIdsTooltip = reflexIdsTooltip,
              LinkId = link.ID,
              ActionId = link.ActionId,
              ActionText = actionText,
              ActionTooltip = $"Действие ID {link.ActionId}: {actionText}",
              LinkDescription = link.Description ?? string.Empty,
              SuccessNextLink = link.SuccessNextLink,
              FailureNextLink = link.FailureNextLink
            });
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Error(ex.Message);
      }

      RefreshDisplay();
    }

    private void RefreshDisplay()
    {
      var filtered = _allChains.Where(FilterChains).ToList();
      int take = SelectedPageSize ?? int.MaxValue;
      _displayChains.Clear();
      foreach (var item in filtered.Take(take))
        _displayChains.Add(item);
      OnPropertyChanged(nameof(DisplayCountText));
    }

    private string GetActionText(int actionId)
    {
      if (actionId <= 0)
        return string.Empty;

      if (_actionNameById.TryGetValue(actionId, out var actionName))
        return actionName;

      return $"Действие #{actionId}";
    }

    private bool FilterChains(object item)
    {
      if (!(item is ChainDisplayItem chainItem))
        return false;

      if (!string.IsNullOrWhiteSpace(FilterReflexId))
      {
        var reflexIdsStr = chainItem.ReflexIdsText ?? string.Empty;
        if (reflexIdsStr.IndexOf(FilterReflexId.Trim(), StringComparison.OrdinalIgnoreCase) < 0)
          return false;
      }

      if (!string.IsNullOrWhiteSpace(FilterChainId))
      {
        var chainIdStr = chainItem.ChainId.ToString();
        if (chainIdStr.IndexOf(FilterChainId.Trim(), StringComparison.OrdinalIgnoreCase) < 0)
          return false;
      }

      if (SelectedActionFilterId > 0)
      {
        if (chainItem.ActionId != SelectedActionFilterId)
          return false;
      }

      if (!string.IsNullOrWhiteSpace(FilterChainName))
      {
        var chainName = chainItem.ChainName ?? string.Empty;
        if (chainName.IndexOf(FilterChainName.Trim(), StringComparison.OrdinalIgnoreCase) < 0)
          return false;
      }

      return true;
    }

    private void ApplyFilters()
    {
      RefreshDisplay();
    }

    private void ClearFilters(object parameter = null)
    {
      FilterReflexId = string.Empty;
      FilterChainId = string.Empty;
      SelectedActionFilterId = 0;
      FilterChainName = string.Empty;
    }

    public class ChainDisplayItem
    {
      public int ChainId { get; set; }
      public string ChainName { get; set; }
      public string ChainDescription { get; set; }
      public List<int> ReflexIds { get; set; } = new List<int>();
      public string ReflexIdsText { get; set; }
      public string ReflexIdsTooltip { get; set; }
      public int LinkId { get; set; }
      public int ActionId { get; set; }
      public string ActionText { get; set; }
      public string ActionTooltip { get; set; }
      public string LinkDescription { get; set; }
      public int SuccessNextLink { get; set; }
      public int FailureNextLink { get; set; }
    }
  }
}
