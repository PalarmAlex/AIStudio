using AIStudio.Common;
using AIStudio.Common.SymbiontEnv;
using ISIDA.Gomeostas;
using ISIDA.Reflexes;
using ISIDA.SymbiontEnv.Contract;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>Read-only обзор связей триггеров, рефлексов и рецептов.</summary>
  public sealed class EnvironmentBehaviorOverviewViewModel : INotifyPropertyChanged
  {
    private readonly GomeostasSystem _gomeostas;
    private readonly GeneticReflexesSystem _geneticReflexes;
    private string _filterText = string.Empty;
    private bool _showTriggers = true;
    private bool _showRecipes = true;
    private bool _showGapsOnly;
    private int _currentAgentStage;
    private string _currentAgentName;

    public EnvironmentBehaviorOverviewViewModel(GomeostasSystem gomeostas, GeneticReflexesSystem geneticReflexes)
    {
      _geneticReflexes = geneticReflexes;
      _gomeostas = gomeostas;
      Chains = new ObservableCollection<EnvironmentBehaviorChainRow>();
      RefreshCommand = new RelayCommand(_ => Reload());
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

    public string CurrentAgentTitle =>
        SymbiontPageTitleFormatter.Format("Обзор поведения", _currentAgentName, _currentAgentStage);

    public int GapCount { get; private set; }

    public ICommand RefreshCommand { get; }

    /// <summary>
    /// Свойство для описания с ссылкой
    /// </summary>
    public DescriptionWithLink CurrentAgentDescription { get; } = new DescriptionWithLink();

    public event PropertyChangedEventHandler PropertyChanged;

    public void Reload() => RefreshChains();

    private void RefreshChains()
    {
      var agentInfo = _gomeostas.GetAgentState();
      _currentAgentStage = _gomeostas?.GetAgentState()?.EvolutionStage ?? 0;
      _currentAgentName = agentInfo.Name;
      OnPropertyChanged(nameof(CurrentAgentTitle));

      var errors = new List<string>();
      IList<EnvironmentTriggerData> triggers = EnvironmentCatalogStorage.LoadTriggers(errors);
      IList<EnvironmentRecipeData> recipes = EnvironmentCatalogStorage.LoadRecipes(errors);
      IList<EnvironmentBehaviorChainRow> all = EnvironmentLinksService.BuildChains(triggers, recipes, _geneticReflexes);
      _allChains = all.ToList();
      GapCount = _allChains.Count(c => c.HasGap);
      OnPropertyChanged(nameof(GapCount));
      ApplyFilter();
    }

    private List<EnvironmentBehaviorChainRow> _allChains = new List<EnvironmentBehaviorChainRow>();

    private void ApplyFilter()
    {
      Chains.Clear();
      IEnumerable<EnvironmentBehaviorChainRow> q = _allChains;
      if (!_showTriggers && !_showRecipes)
      {
        return;
      }
      if (_showTriggers || _showRecipes)
      {
        q = q.Where(c =>
        {
          bool hasTrigger = !string.IsNullOrWhiteSpace(c.TriggerId);
          bool hasRecipe = !string.IsNullOrWhiteSpace(c.RecipeId);
          return (_showTriggers && hasTrigger) || (_showRecipes && hasRecipe);
        });
      }
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

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Класс описания с ссылкой
    /// </summary>
    public class DescriptionWithLink
    {
      public string Text { get; set; } = "Обзор связей триггеров, рефлексов и рецептов поведения симбионта.";
      public string LinkText { get; set; } = "Подробнее...";
      public string Url { get; set; } = "https://scorcher.ru/isida/iadaptive_agents_guide.php#ref_9";
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

  }
}