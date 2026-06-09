using AIStudio.Common.SymbiontEnv;
using ISIDA.Gomeostas;
using ISIDA.Reflexes;
using ISIDA.SymbiontEnv.Contract;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>Read-only обзор связей триггеров, рефлексов и рецептов.</summary>
  public sealed class EnvironmentBehaviorOverviewViewModel : IEnvironmentChildViewModel, INotifyPropertyChanged
  {
    private readonly GeneticReflexesSystem _geneticReflexes;
    private string _filterText = string.Empty;
    private bool _showTriggers = true;
    private bool _showRecipes = true;
    private bool _showGapsOnly;

    public EnvironmentBehaviorOverviewViewModel(GeneticReflexesSystem geneticReflexes)
    {
      _geneticReflexes = geneticReflexes;
      Chains = new ObservableCollection<EnvironmentBehaviorChainRow>();
      RefreshCommand = new RelayCommand(_ => Reload());
      OpenTriggerCommand = new RelayCommand(p => OpenTrigger(p as EnvironmentBehaviorChainRow));
      OpenRecipeCommand = new RelayCommand(p => OpenRecipe(p as EnvironmentBehaviorChainRow));
      Reload();
    }

    public ObservableCollection<EnvironmentBehaviorChainRow> Chains { get; }

    public string FilterText
    {
      get => _filterText;
      set { _filterText = value ?? string.Empty; OnPropertyChanged(); ApplyFilter(); }
    }

    public bool ShowTriggers
    {
      get => _showTriggers;
      set { _showTriggers = value; OnPropertyChanged(); ApplyFilter(); }
    }

    public bool ShowRecipes
    {
      get => _showRecipes;
      set { _showRecipes = value; OnPropertyChanged(); ApplyFilter(); }
    }

    public bool ShowGapsOnly
    {
      get => _showGapsOnly;
      set { _showGapsOnly = value; OnPropertyChanged(); ApplyFilter(); }
    }

    public int GapCount { get; private set; }

    public ICommand RefreshCommand { get; }
    public ICommand OpenTriggerCommand { get; }
    public ICommand OpenRecipeCommand { get; }

    public event Action<EnvironmentNavigationRequest> NavigateRequest;
    public event Action DirtyChanged;
    public event Action<int> ValidationIssueCountChanged;
    public event PropertyChangedEventHandler PropertyChanged;

    public bool Dirty => false;
    public int ValidationIssueCount => GapCount;
    public bool CanSave => false;

    public void Save() { }
    public void Reload() => RefreshChains();
    public void Dispose() { }

    private void RefreshChains()
    {
      var errors = new List<string>();
      IList<EnvironmentTriggerData> triggers = EnvironmentCatalogStorage.LoadTriggers(errors);
      IList<EnvironmentRecipeData> recipes = EnvironmentCatalogStorage.LoadRecipes(errors);
      IList<EnvironmentBehaviorChainRow> all = EnvironmentLinksService.BuildChains(triggers, recipes, _geneticReflexes);
      _allChains = all.ToList();
      GapCount = _allChains.Count(c => c.HasGap);
      OnPropertyChanged(nameof(GapCount));
      OnPropertyChanged(nameof(ValidationIssueCount));
      DirtyChanged?.Invoke();
      ValidationIssueCountChanged?.Invoke(GapCount);
      ApplyFilter();
    }

    private List<EnvironmentBehaviorChainRow> _allChains = new List<EnvironmentBehaviorChainRow>();

    private void ApplyFilter()
    {
      Chains.Clear();
      IEnumerable<EnvironmentBehaviorChainRow> q = _allChains;
      if (_showGapsOnly)
        q = q.Where(c => c.HasGap);
      if (!string.IsNullOrWhiteSpace(_filterText))
      {
        string f = _filterText.Trim();
        q = q.Where(c =>
            (c.TriggerId ?? string.Empty).IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0
            || (c.RecipeId ?? string.Empty).IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0
            || (c.TriggerTitle ?? string.Empty).IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0);
      }
      foreach (EnvironmentBehaviorChainRow row in q)
        Chains.Add(row);
    }

    private void OpenTrigger(EnvironmentBehaviorChainRow row)
    {
      if (row == null || string.IsNullOrWhiteSpace(row.TriggerId))
        return;
      NavigateRequest?.Invoke(new EnvironmentNavigationRequest
      {
        Tab = EnvironmentShellTab.Triggers,
        TriggerId = row.TriggerId
      });
    }

    private void OpenRecipe(EnvironmentBehaviorChainRow row)
    {
      if (row == null || string.IsNullOrWhiteSpace(row.RecipeId))
        return;
      NavigateRequest?.Invoke(new EnvironmentNavigationRequest
      {
        Tab = EnvironmentShellTab.Recipes,
        RecipeId = row.RecipeId
      });
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
