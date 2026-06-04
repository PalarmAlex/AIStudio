using AIStudio.Common;
using AIStudio.Common.SymbiontEnv;
using ISIDA.SymbiontEnv.Contract;
using AIStudio.Dialogs;
using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Gomeostas;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>
  /// Детальный редактор одного рецепта среды.
  /// </summary>
  public sealed class EnvironmentRecipeEditorViewModel
  {
    private readonly GomeostasSystem _gomeostas;
    private readonly Action<EnvironmentRecipeEditorModel, bool> _onSaveAll;
    private readonly bool _isNew;
    private string _currentAgentName;
    private int _currentAgentStage;

    /// <summary>
    /// Создаёт модель редактора.
    /// </summary>
    public EnvironmentRecipeEditorViewModel(
        GomeostasSystem gomeostas,
        EnvironmentRecipeEditorModel model,
        bool isNew,
        Action<EnvironmentRecipeEditorModel, bool> onSaveAll)
    {
      _gomeostas = gomeostas ?? throw new ArgumentNullException(nameof(gomeostas));
      Model = model ?? throw new ArgumentNullException(nameof(model));
      _isNew = isNew;
      _onSaveAll = onSaveAll ?? throw new ArgumentNullException(nameof(onSaveAll));

      SaveCommand = new RelayCommand(_ => Save(), _ => IsEditingEnabled);
      CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));

      RefreshAgent();
      UpdateRecommendedTriggersDisplay();
    }

    /// <summary>Модель полей.</summary>
    public EnvironmentRecipeEditorModel Model { get; }

    /// <summary>Закрыть редактор (saved).</summary>
    public event Action<bool> RequestClose;

    /// <summary>Вернуться к реестру без сохранения.</summary>
    public Action CloseAction { get; set; }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public string CurrentAgentTitle =>
        SymbiontPageTitleFormatter.Format("Редактор рецепта среды", _currentAgentName, _currentAgentStage);

    public bool IsStageZero => _currentAgentStage == 0;
    public bool HasAdapter => SymbiontEnvironmentGate.IsEnvironmentEditingAllowed();
    public bool IsEditingEnabled => HasAdapter && IsStageZero && !GlobalTimer.IsPulsationRunning;
    public string PulseWarningMessage =>
        !HasAdapter
            ? "Укажите AdapterId в проекте (новый проект с выбором адаптера)"
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

    public string[] StepTypeChoices { get; } =
    {
      "set_property",
      "run_sw_command",
      "rebuild",
      "log"
    };

    /// <summary>
    /// Выбор рекомендуемых воздействий (мультивыбор).
    /// </summary>
    public void PickRecommendedTriggers(Window owner)
    {
      if (!IsEditingEnabled)
        return;

      var dialog = new InfluenceActionsSelectionDialog(
          new List<int>(Model.RecommendedTriggerInfluenceIds ?? new List<int>()));
      if (owner != null)
        dialog.Owner = owner;

      if (dialog.ShowDialog() == true)
      {
        Model.RecommendedTriggerInfluenceIds = dialog.SelectedInfluenceActions ?? new List<int>();
        UpdateRecommendedTriggersDisplay();
      }
    }

    /// <summary>
    /// Выбор адаптивного действия (одно).
    /// </summary>
    public void PickAdaptiveAction(Window owner)
    {
      if (!IsEditingEnabled || !AdaptiveActionsSystem.IsInitialized)
        return;

      var current = Model.AdaptiveActionId > 0
          ? new List<int> { Model.AdaptiveActionId }
          : new List<int>();

      var dialog = new AdaptiveActionsSelectionDialog(current);
      if (owner != null)
        dialog.Owner = owner;

      if (dialog.ShowDialog() == true && dialog.SelectedAdaptiveActions != null &&
          dialog.SelectedAdaptiveActions.Count > 0)
      {
        Model.AdaptiveActionId = dialog.SelectedAdaptiveActions[0];
      }
    }

    private void UpdateRecommendedTriggersDisplay()
    {
      if (Model.RecommendedTriggerInfluenceIds == null || Model.RecommendedTriggerInfluenceIds.Count == 0)
      {
        Model.RecommendedTriggersDisplay = string.Empty;
        return;
      }

      if (!InfluenceActionSystem.IsInitialized)
      {
        Model.RecommendedTriggersDisplay = string.Join(", ", Model.RecommendedTriggerInfluenceIds);
        return;
      }

      var names = new List<string>();
      foreach (int id in Model.RecommendedTriggerInfluenceIds)
      {
        var allActions = InfluenceActionSystem.Instance.GetAllInfluenceActions();
        var action = allActions?.FirstOrDefault(a => a.Id == id);
        names.Add(action != null ? id + ": " + action.Name : id.ToString(CultureInfo.InvariantCulture));
      }

      Model.RecommendedTriggersDisplay = string.Join("; ", names);
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
  }
}
