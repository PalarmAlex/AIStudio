using AIStudio.Common;
using AIStudio.Common.Adapters;
using AIStudio.Common.SymbiontEnv;
using ISIDA.Common;
using ISIDA.Gomeostas;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static ISIDA.Gomeostas.GomeostasSystem;

namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>
  /// Редактор правил давления среды (<see cref="AppConfig.EnvironmentPressureRulesFilePath"/>).
  /// </summary>
  public sealed class EnvironmentPressureRulesViewModel : IDisposable
  {
    private readonly GomeostasSystem _gomeostas;
    private readonly List<EnvironmentPressureRuleRow> _allRows = new List<EnvironmentPressureRuleRow>();
    private string _currentAgentName;
    private int _currentAgentStage;

    public EnvironmentPressureRulesViewModel(GomeostasSystem gomeostas)
    {
      _gomeostas = gomeostas ?? throw new ArgumentNullException(nameof(gomeostas));
      Rules = new ObservableCollection<EnvironmentPressureRuleRow>();
      ProbeKeyOptions = new ObservableCollection<AdapterSchemaMetricProbe>();
      SaveCommand = new RelayCommand(_ => SaveToDisk(), _ => IsEditingEnabled);
      GlobalTimer.PulsationStateChanged += OnPulsationStateChanged;
      ReloadFromDisk();
    }

    public ObservableCollection<EnvironmentPressureRuleRow> Rules { get; }
    public ObservableCollection<AdapterSchemaMetricProbe> ProbeKeyOptions { get; }

    public string CurrentAgentTitle =>
        SymbiontPageTitleFormatter.Format("Давление среды на виталы", _currentAgentName, _currentAgentStage);

    public ICommand SaveCommand { get; }

    public bool IsStageZero => _currentAgentStage == 0;
    public bool HasAdapter => SymbiontEnvironmentGate.IsEnvironmentEditingAllowed();
    public bool IsEditingEnabled => HasAdapter && IsStageZero && !GlobalTimer.IsPulsationRunning;
    public bool IsReadOnlyMode => !IsEditingEnabled;

    public string PulseWarningMessage =>
        !HasAdapter
            ? "Укажите тип среды в свойствах симбионта"
            : !IsStageZero
                ? "[КРИТИЧНО] Редактирование доступно только в стадии 0"
                : GlobalTimer.IsPulsationRunning
                    ? "Редактирование доступно только при выключенной пульсации"
                    : string.Empty;

    public Brush WarningMessageColor => !HasAdapter || !IsStageZero ? Brushes.Red : Brushes.Gray;

    public List<ParameterData> GetAllParameters() => _gomeostas.GetAllParameters().ToList();

    public void ReloadFromDisk()
    {
      var agent = _gomeostas.GetAgentState();
      _currentAgentStage = agent?.EvolutionStage ?? 0;
      _currentAgentName = agent?.Name ?? string.Empty;
      _allRows.Clear();
      _allRows.AddRange(EnvironmentPressureRulesStorage.Load());
      RefreshProbeKeyOptions();
      RefreshRulesCollection();
    }

    public void RegisterNewRow(EnvironmentPressureRuleRow row)
    {
      if (row == null)
        return;
      if (!_allRows.Contains(row))
        _allRows.Add(row);
    }

    public EnvironmentPressureRuleRow CreateNewRow()
    {
      int nextId = _allRows.Count == 0 ? 1 : _allRows.Max(r => r.RuleId) + 1;
      return new EnvironmentPressureRuleRow
      {
        RuleId = nextId,
        Name = "Новое правило",
        Description = string.Empty,
        ProbeKey = ProbeKeyOptions.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p?.Key))?.Key ?? string.Empty,
        Influences = new Dictionary<int, int>(),
        Antagonists = new List<int>()
      };
    }

    public bool TryRemoveRows(IReadOnlyList<EnvironmentPressureRuleRow> rows)
    {
      if (rows == null || rows.Count == 0 || !IsEditingEnabled)
        return false;
      string msg = rows.Count == 1
          ? "Удалить правило \"" + rows[0].Name + "\"?"
          : "Удалить выбранные правила (" + rows.Count + ")?";
      if (MessageBox.Show(msg, "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        return false;
      foreach (EnvironmentPressureRuleRow row in rows)
      {
        _allRows.Remove(row);
        Rules.Remove(row);
      }
      return true;
    }

    private void SaveToDisk()
    {
      try
      {
        foreach (EnvironmentPressureRuleRow row in _allRows)
        {
          if (row == null)
            continue;
          var probeCheck = SettingsValidator.ValidateEnvironmentProbeKey(row.ProbeKey);
          if (!probeCheck.isValid)
            throw new InvalidOperationException($"RuleId {row.RuleId}: {probeCheck.errorMessage}");
          if (string.IsNullOrWhiteSpace(row.ProbeKey))
            throw new InvalidOperationException($"RuleId {row.RuleId}: ProbeKey не может быть пустым.");
        }

        EnvironmentPressureRulesStorage.Save(_allRows);
        MessageBox.Show("Правила давления среды сохранены.", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);
        ReloadFromDisk();
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message, "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void RefreshProbeKeyOptions()
    {
      ProbeKeyOptions.Clear();
      var knownKeys = new HashSet<string>(StringComparer.Ordinal);
      IReadOnlyList<AdapterSchemaMetricProbe> schemaProbes = AdapterSchemaLoader.LoadMetricProbesForCurrentProject();
      for (int i = 0; i < schemaProbes.Count; i++)
      {
        AdapterSchemaMetricProbe probe = schemaProbes[i];
        if (probe == null || string.IsNullOrWhiteSpace(probe.Key) || !knownKeys.Add(probe.Key))
          continue;
        ProbeKeyOptions.Add(probe);
      }
      foreach (EnvironmentPressureRuleRow row in _allRows)
      {
        string key = (row.ProbeKey ?? string.Empty).Trim();
        if (key.Length == 0 || !knownKeys.Add(key))
          continue;
        ProbeKeyOptions.Add(new AdapterSchemaMetricProbe { Key = key, Label = key });
      }
    }

    private void RefreshRulesCollection()
    {
      Rules.Clear();
      foreach (EnvironmentPressureRuleRow row in _allRows.OrderBy(r => r.RuleId))
        Rules.Add(row);
    }

    private void OnPulsationStateChanged()
    {
      Application.Current?.Dispatcher.Invoke(RefreshRulesCollection);
    }

    public void Dispose()
    {
      GlobalTimer.PulsationStateChanged -= OnPulsationStateChanged;
    }
  }
}
