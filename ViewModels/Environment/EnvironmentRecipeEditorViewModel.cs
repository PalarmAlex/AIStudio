using AIStudio.Common;
using AIStudio.Common.Adapters;
using AIStudio.Common.SymbiontEnv;
using AIStudio.Dialogs;
using ISIDA.SymbiontEnv.Contract;
using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Gomeostas;
using ISIDA.Reflexes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>Детальный редактор одного рецепта среды (вкладки).</summary>
  public sealed class EnvironmentRecipeEditorViewModel : INotifyPropertyChanged
  {
    private readonly GomeostasSystem _gomeostas;
    private readonly GeneticReflexesSystem _geneticReflexes;
    private readonly Action<EnvironmentRecipeEditorModel, bool> _onSaveAll;
    private readonly bool _isNew;
    private readonly AdapterEnvironmentSchema _schema;
    private string _currentAgentName;
    private int _currentAgentStage;
    private string _adaptiveActionDisplay;
    private string _adaptiveActionDescription;
    private string _recommendedTriggerDisplay;
    private string _recommendedTriggerDescription;
    private string _recipeIdDescription;
    private RecipeEditorTab _selectedTab = RecipeEditorTab.General;
    private EnvironmentRecipeStepRow _selectedStep;
    private bool _dirty;

    public EnvironmentRecipeEditorViewModel(
        GomeostasSystem gomeostas,
        GeneticReflexesSystem geneticReflexes,
        EnvironmentRecipeEditorModel model,
        bool isNew,
        Action<EnvironmentRecipeEditorModel, bool> onSaveAll)
    {
      _gomeostas = gomeostas ?? throw new ArgumentNullException(nameof(gomeostas));
      _geneticReflexes = geneticReflexes;
      Model = model ?? throw new ArgumentNullException(nameof(model));
      _isNew = isNew;
      _onSaveAll = onSaveAll ?? throw new ArgumentNullException(nameof(onSaveAll));
      _schema = AdapterSchemaLoader.LoadForCurrentProject();
      RecipeIdOptions = new ObservableCollection<AdapterSchemaRecipeCatalogEntry>();
      Links = new ObservableCollection<EnvironmentLinkItem>();
      StepHandlerSchema = new SchemaActionEditorViewModel(_schema);
      StepHandlerSchema.ValuesCommitted += OnStepSchemaCommitted;
      StepHandlerSchema.SelectedCatalogChanged += OnStepSchemaCatalogChanged;

      SaveCommand = new RelayCommand(_ => Save(), _ => IsEditingEnabled);
      CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
      AddInvokeStepCommand = new RelayCommand(_ => AddInvokeStep(), _ => IsEditingEnabled);
      AddCommentStepCommand = new RelayCommand(_ => AddCommentStep(), _ => IsEditingEnabled);
      DeleteSelectedStepsCommand = new RelayCommand(_ => DeleteSelectedSteps(), _ => IsEditingEnabled && SelectedStep != null);
      MoveStepUpCommand = new RelayCommand(_ => MoveStep(-1), _ => IsEditingEnabled && SelectedStep != null);
      MoveStepDownCommand = new RelayCommand(_ => MoveStep(1), _ => IsEditingEnabled && SelectedStep != null);
      NavigateToTriggerCommand = new RelayCommand(_ => NavigateToTrigger(), _ => Model.RecommendedTriggerInfluenceIds?.Count > 0);
      NavigateToReflexesCommand = new RelayCommand(_ => NavigateToReflexes());

      RefreshAgent();
      RefreshRecipeIdOptions();
      UpdateRecipeIdDescription();
      UpdateAdaptiveActionDisplay();
      UpdateRecommendedTriggerDisplay();
      EnvironmentRecipeStepSchemaHelper.InitializeAllSteps(Model.Steps, _schema);
      SelectedStep = Model.Steps.FirstOrDefault();
      RefreshLinks();
    }

    public EnvironmentRecipeEditorModel Model { get; }
    public ObservableCollection<AdapterSchemaRecipeCatalogEntry> RecipeIdOptions { get; }
    public ObservableCollection<EnvironmentLinkItem> Links { get; }
    public SchemaActionEditorViewModel StepHandlerSchema { get; }

    public event Action<bool> RequestClose;
    public event Action<EnvironmentNavigationRequest> NavigateRequest;
    public Action CloseAction { get; set; }
    public event PropertyChangedEventHandler PropertyChanged;

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AddInvokeStepCommand { get; }
    public ICommand AddCommentStepCommand { get; }
    public ICommand DeleteSelectedStepsCommand { get; }
    public ICommand MoveStepUpCommand { get; }
    public ICommand MoveStepDownCommand { get; }
    public ICommand NavigateToTriggerCommand { get; }
    public ICommand NavigateToReflexesCommand { get; }

    public RecipeEditorTab SelectedTab
    {
      get => _selectedTab;
      set
      {
        if (_selectedTab == value)
          return;
        _selectedTab = value;
        OnPropertyChanged();
        if (value == RecipeEditorTab.Links)
          RefreshLinks();
      }
    }

    public EnvironmentRecipeStepRow SelectedStep
    {
      get => _selectedStep;
      set
      {
        if (_selectedStep == value)
          return;
        _selectedStep = value;
        OnPropertyChanged();
        LoadSelectedStepSchema();
      }
    }

    public bool Dirty => _dirty;

    public string EditorTitle => "РЕЦЕПТ: " + (Model.Id ?? string.Empty);

    public bool IsStageZero => _currentAgentStage == 0;
    public bool HasAdapter => SymbiontEnvironmentGate.IsEnvironmentEditingAllowed();
    public bool IsEditingEnabled => HasAdapter && IsStageZero && !GlobalTimer.IsPulsationRunning;

    public string PulseWarningMessage =>
        !HasAdapter
            ? "Укажите тип среды в свойствах симбионта"
            : !IsStageZero
                ? "[КРИТИЧНО] Редактирование доступно только в стадии 0"
                : GlobalTimer.IsPulsationRunning
                    ? "Редактирование доступно только при выключенной пульсации"
                    : string.Empty;

    public Brush WarningMessageColor => !HasAdapter || !IsStageZero ? Brushes.Red : Brushes.Gray;

    public string RecipeId
    {
      get => Model.Id;
      set
      {
        string normalized = value ?? string.Empty;
        if (string.Equals(Model.Id, normalized, StringComparison.Ordinal))
          return;
        Model.Id = normalized;
        MarkDirty();
        OnPropertyChanged();
        UpdateRecipeIdDescription();
        OnPropertyChanged(nameof(EditorTitle));
      }
    }

    public string RecipeIdDescription
    {
      get => _recipeIdDescription;
      private set { _recipeIdDescription = value; OnPropertyChanged(); }
    }

    public string AdaptiveActionDisplay
    {
      get => _adaptiveActionDisplay;
      private set { _adaptiveActionDisplay = value; OnPropertyChanged(); }
    }

    public string AdaptiveActionDescription
    {
      get => _adaptiveActionDescription;
      private set { _adaptiveActionDescription = value; OnPropertyChanged(); }
    }

    public string RecommendedTriggerDisplay
    {
      get => _recommendedTriggerDisplay;
      private set { _recommendedTriggerDisplay = value; OnPropertyChanged(); }
    }

    public string RecommendedTriggerDescription
    {
      get => _recommendedTriggerDescription;
      private set { _recommendedTriggerDescription = value; OnPropertyChanged(); }
    }

    public void PickRecipeId(Window owner)
    {
      if (!IsEditingEnabled)
        return;
      var dialog = new RecipeIdSelectionDialog(Model.Id, RecipeIdOptions) { Owner = owner };
      if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.SelectedRecipeId))
        RecipeId = dialog.SelectedRecipeId;
    }

    public void PickRecommendedTriggers(Window owner)
    {
      if (!IsEditingEnabled)
        return;
      int currentId = Model.RecommendedTriggerInfluenceIds != null && Model.RecommendedTriggerInfluenceIds.Count > 0
          ? Model.RecommendedTriggerInfluenceIds[0]
          : 0;
      var dialog = new InfluenceActionRadioSelectionDialog(currentId) { Owner = owner };
      if (dialog.ShowDialog() == true && dialog.SelectedInfluenceActionId > 0)
      {
        Model.RecommendedTriggerInfluenceIds = new List<int> { dialog.SelectedInfluenceActionId };
        UpdateRecommendedTriggerDisplay();
        MarkDirty();
        RefreshLinks();
      }
    }

    public void PickAdaptiveAction(Window owner)
    {
      if (!IsEditingEnabled || !AdaptiveActionsSystem.IsInitialized)
        return;
      var dialog = new AdaptiveActionRadioSelectionDialog(Model.AdaptiveActionId) { Owner = owner };
      if (dialog.ShowDialog() == true && dialog.SelectedAdaptiveActionId > 0)
      {
        Model.AdaptiveActionId = dialog.SelectedAdaptiveActionId;
        UpdateAdaptiveActionDisplay();
        MarkDirty();
        RefreshLinks();
      }
    }

    public void OnModelFieldChanged()
    {
      MarkDirty();
    }

    private void AddInvokeStep()
    {
      EnvironmentRecipeStepRow row = EnvironmentRecipeStepSchemaHelper.CreateDefaultInvokeStep(_schema);
      Model.Steps.Add(row);
      SelectedStep = row;
      MarkDirty();
    }

    private void AddCommentStep()
    {
      EnvironmentRecipeStepRow row = EnvironmentRecipeStepSchemaHelper.CreateDefaultCommentStep();
      Model.Steps.Add(row);
      SelectedStep = row;
      MarkDirty();
    }

    private void DeleteSelectedSteps()
    {
      if (SelectedStep == null)
        return;
      int index = Model.Steps.IndexOf(SelectedStep);
      Model.Steps.Remove(SelectedStep);
      SelectedStep = index >= 0 && index < Model.Steps.Count ? Model.Steps[index] : Model.Steps.LastOrDefault();
      MarkDirty();
    }

    private void MoveStep(int delta)
    {
      if (SelectedStep == null)
        return;
      int index = Model.Steps.IndexOf(SelectedStep);
      int newIndex = index + delta;
      if (index < 0 || newIndex < 0 || newIndex >= Model.Steps.Count)
        return;
      Model.Steps.RemoveAt(index);
      Model.Steps.Insert(newIndex, SelectedStep);
      MarkDirty();
    }

    private void NavigateToTrigger()
    {
      int eaId = Model.RecommendedTriggerInfluenceIds != null && Model.RecommendedTriggerInfluenceIds.Count > 0
          ? Model.RecommendedTriggerInfluenceIds[0]
          : 0;
      NavigateRequest?.Invoke(new EnvironmentNavigationRequest
      {
        Tab = EnvironmentShellTab.Triggers,
        InfluenceActionId = eaId
      });
    }

    private void NavigateToReflexes()
    {
      NavigateRequest?.Invoke(new EnvironmentNavigationRequest { Tab = EnvironmentShellTab.Overview });
    }

    private void OnStepSchemaCommitted(Dictionary<string, string> values)
    {
      if (SelectedStep == null || SelectedStep.IsComment)
        return;
      SelectedStep.HandlerId = StepHandlerSchema.SelectedCatalogId;
      EnvironmentRecipeStepSchemaHelper.ApplyArgsFromDictionary(SelectedStep, values);
      EnvironmentRecipeStepSchemaHelper.RefreshSummary(SelectedStep, _schema);
      MarkDirty();
    }

    private void OnStepSchemaCatalogChanged()
    {
      if (SelectedStep == null || SelectedStep.IsComment)
        return;
      SelectedStep.HandlerId = StepHandlerSchema.SelectedCatalogId;
      EnvironmentRecipeStepSchemaHelper.RefreshSummary(SelectedStep, _schema);
      MarkDirty();
    }

    private void LoadSelectedStepSchema()
    {
      StepHandlerSchema.IsEditingEnabled = IsEditingEnabled && SelectedStep != null && SelectedStep.IsInvoke;
      if (SelectedStep == null || SelectedStep.IsComment)
        return;
      StepHandlerSchema.LoadFromHandler(SelectedStep.HandlerId, SelectedStep.Args);
    }

    private void RefreshLinks()
    {
      Links.Clear();
      var errors = new List<string>();
      IList<EnvironmentTriggerData> triggers = EnvironmentCatalogStorage.LoadTriggers(errors);
      foreach (EnvironmentLinkItem item in EnvironmentLinksService.BuildRecipeLinks(Model, triggers, _geneticReflexes, _schema))
        Links.Add(item);
    }

    private void Save()
    {
      if (string.IsNullOrWhiteSpace(Model.Id))
      {
        MessageBox.Show("Укажите ID рецепта.", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }
      if (Model.AdaptiveActionId <= 0)
      {
        MessageBox.Show("Укажите адаптивное действие.", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }
      string stepsError = EnvironmentRecipeStepSchemaHelper.ValidateSteps(Model.Steps, _schema);
      if (!string.IsNullOrWhiteSpace(stepsError))
      {
        MessageBox.Show(stepsError, "Сохранение", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }
      try
      {
        _onSaveAll(Model, _isNew);
        _dirty = false;
        RequestClose?.Invoke(true);
        CloseAction?.Invoke();
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message, "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void MarkDirty()
    {
      _dirty = true;
      OnPropertyChanged(nameof(Dirty));
    }

    private void RefreshRecipeIdOptions()
    {
      RecipeIdOptions.Clear();
      var knownIds = new HashSet<string>(StringComparer.Ordinal);
      foreach (AdapterSchemaRecipeCatalogEntry entry in AdapterSchemaLoader.LoadRecipeCatalogForCurrentProject())
      {
        if (entry == null || string.IsNullOrWhiteSpace(entry.Id) || !knownIds.Add(entry.Id))
          continue;
        RecipeIdOptions.Add(entry);
      }
      var errors = new List<string>();
      foreach (EnvironmentRecipeData recipe in EnvironmentCatalogStorage.LoadRecipes(errors))
      {
        string id = (recipe?.Id ?? string.Empty).Trim();
        if (id.Length == 0 || !knownIds.Add(id))
          continue;
        RecipeIdOptions.Add(new AdapterSchemaRecipeCatalogEntry
        {
          Id = id,
          Label = recipe.DisplayName ?? id,
          Description = recipe.Description ?? string.Empty
        });
      }
    }

    private void UpdateRecipeIdDescription()
    {
      string id = (Model.Id ?? string.Empty).Trim();
      if (id.Length == 0) { RecipeIdDescription = string.Empty; return; }
      AdapterSchemaRecipeCatalogEntry entry = RecipeIdOptions.FirstOrDefault(e => string.Equals(e?.Id, id, StringComparison.Ordinal));
      RecipeIdDescription = entry == null
          ? string.Empty
          : string.IsNullOrWhiteSpace(entry.Description) ? entry.Label : entry.Label + " — " + entry.Description;
    }

    private void UpdateAdaptiveActionDisplay()
    {
      if (Model.AdaptiveActionId <= 0) { AdaptiveActionDisplay = string.Empty; AdaptiveActionDescription = string.Empty; return; }
      AdaptiveActionDisplay = Model.AdaptiveActionId.ToString(CultureInfo.InvariantCulture);
      if (!AdaptiveActionsSystem.IsInitialized) { AdaptiveActionDescription = string.Empty; return; }
      var action = AdaptiveActionsSystem.Instance.GetAllAdaptiveActions()?.FirstOrDefault(a => a.Id == Model.AdaptiveActionId);
      AdaptiveActionDescription = action != null
          ? (action.Name ?? string.Empty) + (string.IsNullOrWhiteSpace(action.Description) ? string.Empty : " — " + action.Description)
          : string.Empty;
    }

    private void UpdateRecommendedTriggerDisplay()
    {
      if (Model.RecommendedTriggerInfluenceIds == null || Model.RecommendedTriggerInfluenceIds.Count == 0)
      {
        RecommendedTriggerDisplay = string.Empty;
        RecommendedTriggerDescription = string.Empty;
        return;
      }
      int id = Model.RecommendedTriggerInfluenceIds[0];
      RecommendedTriggerDisplay = id.ToString(CultureInfo.InvariantCulture);
      if (!InfluenceActionSystem.IsInitialized) { RecommendedTriggerDescription = string.Empty; return; }
      var action = InfluenceActionSystem.Instance.GetAllInfluenceActions()?.FirstOrDefault(a => a.Id == id);
      RecommendedTriggerDescription = action != null
          ? (action.Name ?? string.Empty) + (string.IsNullOrWhiteSpace(action.Description) ? string.Empty : " — " + action.Description)
          : string.Empty;
    }

    private void RefreshAgent()
    {
      var agent = _gomeostas.GetAgentState();
      _currentAgentStage = agent?.EvolutionStage ?? 0;
      _currentAgentName = agent?.Name ?? string.Empty;
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
