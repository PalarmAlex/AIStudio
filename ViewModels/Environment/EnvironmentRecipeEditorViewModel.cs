using AIStudio.Common;
using AIStudio.Common.Adapters;
using AIStudio.Common.SymbiontEnv;
using AIStudio.Dialogs;
using ISIDA.SymbiontEnv.Contract;
using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Gomeostas;
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
  /// <summary>
  /// Детальный редактор одного рецепта среды.
  /// </summary>
  public sealed class EnvironmentRecipeEditorViewModel : INotifyPropertyChanged
  {
    private readonly GomeostasSystem _gomeostas;
    private readonly Action<EnvironmentRecipeEditorModel, bool> _onSaveAll;
    private readonly bool _isNew;
    private readonly EnvironmentRecipeData _sourceRecipe;
    private readonly AdapterEnvironmentSchema _schema;
    private string _currentAgentName;
    private int _currentAgentStage;
    private string _adaptiveActionDisplay;
    private string _adaptiveActionDescription;
    private string _recommendedTriggerDisplay;
    private string _recommendedTriggerDescription;
    private EnvironmentRecipeStepRow _selectedStep;

    /// <summary>
    /// Создаёт модель редактора.
    /// </summary>
    public EnvironmentRecipeEditorViewModel(
        GomeostasSystem gomeostas,
        EnvironmentRecipeEditorModel model,
        bool isNew,
        Action<EnvironmentRecipeEditorModel, bool> onSaveAll,
        EnvironmentRecipeData sourceRecipe = null)
    {
      _gomeostas = gomeostas ?? throw new ArgumentNullException(nameof(gomeostas));
      Model = model ?? throw new ArgumentNullException(nameof(model));
      _isNew = isNew;
      _sourceRecipe = sourceRecipe;
      _onSaveAll = onSaveAll ?? throw new ArgumentNullException(nameof(onSaveAll));
      _schema = AdapterSchemaLoader.LoadForCurrentProject();
      RecipeIdOptions = new ObservableCollection<AdapterSchemaRecipeCatalogEntry>();
      SaveCommand = new RelayCommand(_ => Save(), _ => IsEditingEnabled);
      CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
      RefreshAgent();
      RefreshRecipeIdOptions();
      UpdateAdaptiveActionDisplay();
      UpdateRecommendedTriggerDisplay();
      EnvironmentRecipePreconditionSchemaHelper.Initialize(
          Model,
          _sourceRecipe,
          _schema,
          applyNewRecipeDefaults: isNew && sourceRecipe == null);
      EnvironmentRecipeStepSchemaHelper.InitializeAllSteps(Model.Steps, _schema);
      SelectedStep = Model.Steps.FirstOrDefault();
    }

    /// <summary>Модель полей.</summary>
    public EnvironmentRecipeEditorModel Model { get; }

    /// <summary>Допустимые ID рецепта из schema/recipe-catalog.json и существующих записей.</summary>
    public ObservableCollection<AdapterSchemaRecipeCatalogEntry> RecipeIdOptions { get; }

    /// <summary>Закрыть редактор (saved).</summary>
    public event Action<bool> RequestClose;

    /// <summary>Вернуться к реестру без сохранения.</summary>
    public Action CloseAction { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public string CurrentAgentTitle =>
        SymbiontPageTitleFormatter.Format("Редактор рецепта среды", _currentAgentName, _currentAgentStage);

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

    public IReadOnlyList<EnvironmentRecipeRiskTier> RiskTierChoices { get; } = new[]
    {
      EnvironmentRecipeRiskTier.A,
      EnvironmentRecipeRiskTier.B,
      EnvironmentRecipeRiskTier.C
    };

    public IReadOnlyList<AdapterSchemaStepType> StepTypeOptions =>
        EnvironmentRecipeStepSchemaHelper.GetStepTypes(_schema).ToList();

    public EnvironmentRecipeStepRow SelectedStep
    {
      get => _selectedStep;
      set
      {
        if (ReferenceEquals(_selectedStep, value))
          return;
        _selectedStep = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(HasSelectedStep));
      }
    }

    public bool HasSelectedStep => SelectedStep != null;

    public EnvironmentRecipeStepRow CreateNewStep() =>
        EnvironmentRecipeStepSchemaHelper.CreateDefaultStep(_schema);

    public string AdaptiveActionDisplay
    {
      get => _adaptiveActionDisplay;
      private set
      {
        if (_adaptiveActionDisplay == value)
          return;
        _adaptiveActionDisplay = value;
        OnPropertyChanged();
      }
    }

    public string AdaptiveActionDescription
    {
      get => _adaptiveActionDescription;
      private set
      {
        if (_adaptiveActionDescription == value)
          return;
        _adaptiveActionDescription = value;
        OnPropertyChanged();
      }
    }

    public string RecommendedTriggerDisplay
    {
      get => _recommendedTriggerDisplay;
      private set
      {
        if (_recommendedTriggerDisplay == value)
          return;
        _recommendedTriggerDisplay = value;
        OnPropertyChanged();
      }
    }

    public string RecommendedTriggerDescription
    {
      get => _recommendedTriggerDescription;
      private set
      {
        if (_recommendedTriggerDescription == value)
          return;
        _recommendedTriggerDescription = value;
        OnPropertyChanged();
      }
    }

    public string RecipeId
    {
      get => Model.Id;
      set
      {
        string normalized = value ?? string.Empty;
        if (string.Equals(Model.Id, normalized, StringComparison.Ordinal))
          return;
        Model.Id = normalized;
        OnPropertyChanged();
      }
    }

    /// <summary>
    /// Перестраивает поля параметров выбранного шага после смены типа.
    /// </summary>
    public void SyncSelectedStepSchema()
    {
      if (SelectedStep == null)
        return;
      var preserved = EnvironmentRecipeStepSchemaHelper.ToParametersDictionary(SelectedStep);
      EnvironmentRecipeStepSchemaHelper.ApplyStepType(
          SelectedStep,
          SelectedStep.StepType,
          _schema,
          preserved);
    }

    /// <summary>
    /// Вставляет плейсхолдер в поле template.
    /// </summary>
    public void InsertTemplatePlaceholder(Window owner, EnvironmentRecipeStepParameterField field)
    {
      if (!IsEditingEnabled || field == null)
        return;
      var dialog = new RecipeStepValueSelectionDialog(
          "Плейсхолдер шаблона",
          "Выберите плейсхолдер для вставки в template:",
          EnvironmentRecipeStepSchemaHelper.KnownTemplatePlaceholders,
          field.Value);
      if (owner != null)
        dialog.Owner = owner;
      if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.SelectedValue))
        return;
      field.Value = string.IsNullOrWhiteSpace(field.Value)
          ? dialog.SelectedValue
          : field.Value + dialog.SelectedValue;
      EnvironmentRecipeStepSchemaHelper.RefreshSummary(SelectedStep);
    }

    /// <summary>
    /// Выбирает имя свойства из справочника КБ.
    /// </summary>
    public void PickPropertyName(Window owner, EnvironmentRecipeStepParameterField field)
    {
      if (!IsEditingEnabled || field == null)
        return;
      var dialog = new RecipeStepValueSelectionDialog(
          "Имя свойства",
          "Выберите имя пользовательского свойства документа:",
          EnvironmentRecipeStepSchemaHelper.KnownPropertyNames,
          field.Value);
      if (owner != null)
        dialog.Owner = owner;
      if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.SelectedValue))
        return;
      field.Value = dialog.SelectedValue;
      EnvironmentRecipeStepSchemaHelper.RefreshSummary(SelectedStep);
    }

    /// <summary>
    /// Выбор ID рецепта из каталога адаптера.
    /// </summary>
    public void PickRecipeId(Window owner)
    {
      if (!IsEditingEnabled)
        return;

      var dialog = new RecipeIdSelectionDialog(Model.Id, RecipeIdOptions)
      {
        Owner = owner
      };
      if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.SelectedRecipeId))
        RecipeId = dialog.SelectedRecipeId;
    }

    /// <summary>
    /// Выбор одного рекомендуемого воздействия (триггер).
    /// </summary>
    public void PickRecommendedTriggers(Window owner)
    {
      if (!IsEditingEnabled)
        return;

      int currentId = Model.RecommendedTriggerInfluenceIds != null && Model.RecommendedTriggerInfluenceIds.Count > 0
          ? Model.RecommendedTriggerInfluenceIds[0]
          : 0;
      var dialog = new InfluenceActionRadioSelectionDialog(currentId);
      if (owner != null)
        dialog.Owner = owner;
      if (dialog.ShowDialog() == true && dialog.SelectedInfluenceActionId > 0)
      {
        Model.RecommendedTriggerInfluenceIds = new List<int> { dialog.SelectedInfluenceActionId };
        UpdateRecommendedTriggerDisplay();
      }
    }

    /// <summary>
    /// Выбор одного адаптивного действия.
    /// </summary>
    public void PickAdaptiveAction(Window owner)
    {
      if (!IsEditingEnabled || !AdaptiveActionsSystem.IsInitialized)
        return;

      var dialog = new AdaptiveActionRadioSelectionDialog(Model.AdaptiveActionId);
      if (owner != null)
        dialog.Owner = owner;
      if (dialog.ShowDialog() == true && dialog.SelectedAdaptiveActionId > 0)
      {
        Model.AdaptiveActionId = dialog.SelectedAdaptiveActionId;
        UpdateAdaptiveActionDisplay();
      }
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

      string currentId = (Model.Id ?? string.Empty).Trim();
      if (currentId.Length > 0 && knownIds.Add(currentId))
      {
        RecipeIdOptions.Add(new AdapterSchemaRecipeCatalogEntry
        {
          Id = currentId,
          Label = Model.DisplayName ?? currentId
        });
      }
    }

    private void UpdateAdaptiveActionDisplay()
    {
      if (Model.AdaptiveActionId <= 0)
      {
        AdaptiveActionDisplay = string.Empty;
        AdaptiveActionDescription = string.Empty;
        return;
      }

      AdaptiveActionDisplay = Model.AdaptiveActionId.ToString(CultureInfo.InvariantCulture);
      if (!AdaptiveActionsSystem.IsInitialized)
      {
        AdaptiveActionDescription = string.Empty;
        return;
      }

      var action = AdaptiveActionsSystem.Instance.GetAllAdaptiveActions()
          ?.FirstOrDefault(a => a.Id == Model.AdaptiveActionId);
      AdaptiveActionDescription = action != null
          ? (action.Name ?? string.Empty) + (string.IsNullOrWhiteSpace(action.Description)
              ? string.Empty
              : " — " + action.Description)
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
      if (!InfluenceActionSystem.IsInitialized)
      {
        RecommendedTriggerDescription = string.Empty;
        return;
      }

      var action = InfluenceActionSystem.Instance.GetAllInfluenceActions()
          ?.FirstOrDefault(a => a.Id == id);
      RecommendedTriggerDescription = action != null
          ? (action.Name ?? string.Empty) + (string.IsNullOrWhiteSpace(action.Description)
              ? string.Empty
              : " — " + action.Description)
          : string.Empty;
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
        RequestClose?.Invoke(true);
        CloseAction?.Invoke();
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message, "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
      }
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
