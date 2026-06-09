using AIStudio.Common;
using AIStudio.Common.Adapters;
using AIStudio.Common.SymbiontEnv;
using ISIDA.Common;
using ISIDA.Gomeostas;
using ISIDA.Reflexes;
using ISIDA.SymbiontEnv.Contract;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>Оболочка редакторов среды с tab-bar и единым статусом.</summary>
  public sealed class EnvironmentShellViewModel : INotifyPropertyChanged, IDisposable
  {
    private readonly GomeostasSystem _gomeostas;
    private readonly GeneticReflexesSystem _geneticReflexes;
    private EnvironmentShellTab _selectedTab;
    private object _activeChild;
    private EnvironmentRecipeEditorViewModel _recipeEditor;
    private string _currentAgentName = string.Empty;
    private int _currentAgentStage;
    private string _adapterDisplayName = string.Empty;
    private bool _hasUnsavedChanges;
    private int _validationIssueCount;

    public EnvironmentShellViewModel(
        GomeostasSystem gomeostas,
        GeneticReflexesSystem geneticReflexes,
        EnvironmentShellTab initialTab)
    {
      _gomeostas = gomeostas ?? throw new ArgumentNullException(nameof(gomeostas));
      _geneticReflexes = geneticReflexes;
      OverviewVm = new EnvironmentBehaviorOverviewViewModel(geneticReflexes);
      TriggersVm = new EnvironmentTriggersViewModel(gomeostas, geneticReflexes);
      RegistryVm = new EnvironmentRecipesRegistryViewModel(gomeostas, geneticReflexes, OpenRecipeEditor);
      PressureVm = new EnvironmentPressureRulesViewModel(gomeostas);

      OverviewVm.NavigateRequest += OnNavigateRequest;
      TriggersVm.NavigateRequest += OnNavigateRequest;

      WireChild(OverviewVm);
      WireChild(TriggersVm);
      WireChild(RegistryVm);
      WireChild(PressureVm);

      SaveCommand = new RelayCommand(_ => SaveActive(), _ => CanSaveActive());
      DiscardCommand = new RelayCommand(_ => DiscardActive(), _ => HasUnsavedChanges);
      NavigateTabCommand = new RelayCommand(p => NavigateTab((EnvironmentShellTab)p), _ => true);

      GlobalTimer.PulsationStateChanged += OnPulsationStateChanged;
      RefreshHeader();
      NavigateTab(initialTab);
    }

    public EnvironmentBehaviorOverviewViewModel OverviewVm { get; }
    public EnvironmentTriggersViewModel TriggersVm { get; }
    public EnvironmentRecipesRegistryViewModel RegistryVm { get; }
    public EnvironmentPressureRulesViewModel PressureVm { get; }
    public EnvironmentRecipeEditorViewModel RecipeEditorVm => _recipeEditor;

    public EnvironmentShellTab SelectedTab
    {
      get => _selectedTab;
      private set { _selectedTab = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsRecipesEditorActive)); }
    }

    public object ActiveChild
    {
      get => _activeChild;
      private set { _activeChild = value; OnPropertyChanged(); }
    }

    public bool IsRecipesEditorActive => _recipeEditor != null && SelectedTab == EnvironmentShellTab.Recipes;

    public string ShellTitle =>
        "Среда · " + (_adapterDisplayName ?? string.Empty) + " · " + (_currentAgentName ?? string.Empty)
        + " · стадия " + _currentAgentStage.ToString();

    public string PulseWarningMessage => GetActivePulseWarning();
    public Brush WarningMessageColor => GetActiveWarningColor();

    public bool IsEditingEnabled => SymbiontEnvironmentGate.IsEnvironmentEditingAllowed()
        && _currentAgentStage == 0
        && !GlobalTimer.IsPulsationRunning;

    public bool HasUnsavedChanges
    {
      get => _hasUnsavedChanges;
      private set { _hasUnsavedChanges = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusMessage)); }
    }

    public int ValidationIssueCount
    {
      get => _validationIssueCount;
      private set { _validationIssueCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusMessage)); }
    }

    public string StatusMessage =>
        (_hasUnsavedChanges ? "Изменено" : "Сохранено")
        + (_validationIssueCount > 0 ? " · " + _validationIssueCount + " ошибок валидации" : string.Empty);

    public ICommand SaveCommand { get; }
    public ICommand DiscardCommand { get; }
    public ICommand NavigateTabCommand { get; }

    public event PropertyChangedEventHandler PropertyChanged;

    public void Dispose()
    {
      GlobalTimer.PulsationStateChanged -= OnPulsationStateChanged;
      OverviewVm.Dispose();
      TriggersVm.Dispose();
      RegistryVm.Dispose();
      PressureVm.Dispose();
    }

    private void OpenRecipeEditor(EnvironmentRecipeEditorViewModel editorVm)
    {
      _recipeEditor = editorVm ?? throw new ArgumentNullException(nameof(editorVm));
      _recipeEditor.CloseAction = CloseRecipeEditor;
      _recipeEditor.RequestClose += _ => CloseRecipeEditor();
      _recipeEditor.NavigateRequest += OnNavigateRequest;
      ActiveChild = _recipeEditor;
      SelectedTab = EnvironmentShellTab.Recipes;
      OnPropertyChanged(nameof(RecipeEditorVm));
      OnPropertyChanged(nameof(IsRecipesEditorActive));
      UpdateAggregateState();
    }

    private void CloseRecipeEditor()
    {
      _recipeEditor = null;
      ActiveChild = RegistryVm;
      OnPropertyChanged(nameof(RecipeEditorVm));
      OnPropertyChanged(nameof(IsRecipesEditorActive));
      UpdateAggregateState();
    }

    private void NavigateTab(EnvironmentShellTab tab)
    {
      if (!TryLeaveCurrentTab())
        return;
      SelectedTab = tab;
      switch (tab)
      {
        case EnvironmentShellTab.Overview:
          ActiveChild = OverviewVm;
          break;
        case EnvironmentShellTab.Triggers:
          ActiveChild = TriggersVm;
          break;
        case EnvironmentShellTab.Recipes:
          ActiveChild = _recipeEditor ?? (object)RegistryVm;
          break;
        case EnvironmentShellTab.Pressure:
          ActiveChild = PressureVm;
          break;
      }
      RefreshHeader();
      UpdateAggregateState();
    }

    private void OnNavigateRequest(EnvironmentNavigationRequest request)
    {
      if (request == null)
        return;
      if (!TryLeaveCurrentTab())
        return;
      NavigateTab(request.Tab);
      if (request.Tab == EnvironmentShellTab.Triggers && !string.IsNullOrWhiteSpace(request.TriggerId))
      {
        EnvironmentTriggerRow trigger = TriggersVm.Triggers.FirstOrDefault(
            t => string.Equals(t?.Id, request.TriggerId, StringComparison.OrdinalIgnoreCase));
        if (trigger != null)
          TriggersVm.SelectedTrigger = trigger;
      }
      if (request.Tab == EnvironmentShellTab.Recipes && !string.IsNullOrWhiteSpace(request.RecipeId))
        RegistryVm.SelectAndOpen(request.RecipeId);
    }

    private bool TryLeaveCurrentTab()
    {
      if (!HasUnsavedChanges)
        return true;
      MessageBoxResult result = MessageBox.Show(
          "Есть несохранённые изменения. Сохранить перед переходом?",
          "Среда",
          MessageBoxButton.YesNoCancel,
          MessageBoxImage.Question);
      if (result == MessageBoxResult.Cancel)
        return false;
      if (result == MessageBoxResult.Yes)
        SaveActive();
      else
        DiscardActive();
      return true;
    }

    private void SaveActive()
    {
      if (_recipeEditor != null && SelectedTab == EnvironmentShellTab.Recipes)
      {
        _recipeEditor.SaveCommand.Execute(null);
        return;
      }
      IEnvironmentChildViewModel child = ActiveChild as IEnvironmentChildViewModel;
      child?.Save();
      UpdateAggregateState();
    }

    private void DiscardActive()
    {
      if (_recipeEditor != null && SelectedTab == EnvironmentShellTab.Recipes)
      {
        CloseRecipeEditor();
        return;
      }
      IEnvironmentChildViewModel child = ActiveChild as IEnvironmentChildViewModel;
      child?.Reload();
      UpdateAggregateState();
    }

    private bool CanSaveActive()
    {
      if (_recipeEditor != null && SelectedTab == EnvironmentShellTab.Recipes)
        return _recipeEditor.IsEditingEnabled;
      IEnvironmentChildViewModel child = ActiveChild as IEnvironmentChildViewModel;
      return child != null && child.CanSave;
    }

    private void WireChild(IEnvironmentChildViewModel child)
    {
      child.DirtyChanged += UpdateAggregateState;
      child.ValidationIssueCountChanged += _ => UpdateAggregateState();
    }

    private void UpdateAggregateState()
    {
      bool dirty = false;
      int issues = 0;
      if (_recipeEditor != null && SelectedTab == EnvironmentShellTab.Recipes)
      {
        dirty = _recipeEditor.Dirty;
      }
      else if (ActiveChild is IEnvironmentChildViewModel child)
      {
        dirty = child.Dirty;
        issues = child.ValidationIssueCount;
      }
      HasUnsavedChanges = dirty;
      ValidationIssueCount = issues;
      OnPropertyChanged(nameof(CanSaveActive));
    }

    private void RefreshHeader()
    {
      var agent = _gomeostas.GetAgentState();
      _currentAgentStage = agent?.EvolutionStage ?? 0;
      _currentAgentName = agent?.Name ?? string.Empty;
      _adapterDisplayName = string.Empty;
      if (SymbiontProjectAdapterSettings.TryGetValidatedCurrentAdapterId(out string adapterId))
      {
        AdapterManifest manifest = AdapterRegistry.TryGetById(adapterId);
        _adapterDisplayName = manifest?.DisplayName ?? adapterId;
      }
      OnPropertyChanged(nameof(ShellTitle));
      OnPropertyChanged(nameof(PulseWarningMessage));
      OnPropertyChanged(nameof(WarningMessageColor));
      OnPropertyChanged(nameof(IsEditingEnabled));
    }

    private string GetActivePulseWarning()
    {
      if (ActiveChild is EnvironmentTriggersViewModel triggers)
        return triggers.PulseWarningMessage;
      if (ActiveChild is EnvironmentRecipesRegistryViewModel registry)
        return registry.PulseWarningMessage;
      if (ActiveChild is EnvironmentRecipeEditorViewModel editor)
        return editor.PulseWarningMessage;
      if (ActiveChild is EnvironmentPressureRulesViewModel pressure)
        return pressure.PulseWarningMessage;
      return string.Empty;
    }

    private Brush GetActiveWarningColor()
    {
      if (ActiveChild is EnvironmentTriggersViewModel triggers)
        return triggers.WarningMessageColor;
      if (ActiveChild is EnvironmentRecipesRegistryViewModel registry)
        return registry.WarningMessageColor;
      if (ActiveChild is EnvironmentRecipeEditorViewModel editor)
        return editor.WarningMessageColor;
      if (ActiveChild is EnvironmentPressureRulesViewModel pressure)
        return pressure.WarningMessageColor;
      return Brushes.Gray;
    }

    private void OnPulsationStateChanged()
    {
      Application.Current?.Dispatcher.Invoke(() =>
      {
        RefreshHeader();
        UpdateAggregateState();
      });
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
