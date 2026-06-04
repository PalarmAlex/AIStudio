using AIStudio.Common;
using AIStudio.Common.SymbiontEnv;
using ISIDA.SymbiontEnv.Contract;
using ISIDA.Common;
using ISIDA.Gomeostas;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>
  /// Редактор триггеров среды (<see cref="EnvironmentPaths.TriggersFilePath"/>).
  /// </summary>
  public sealed class EnvironmentTriggersViewModel : IDisposable
  {
    private readonly GomeostasSystem _gomeostas;
    private readonly List<EnvironmentTriggerRow> _allRows = new List<EnvironmentTriggerRow>();
    private string _currentAgentName;
    private int _currentAgentStage;
    private string _filterId = string.Empty;
    private string _filterTitle = string.Empty;

    /// <summary>
    /// Создаёт модель таблицы триггеров.
    /// </summary>
    public EnvironmentTriggersViewModel(GomeostasSystem gomeostas)
    {
      _gomeostas = gomeostas ?? throw new ArgumentNullException(nameof(gomeostas));
      Triggers = new ObservableCollection<EnvironmentTriggerRow>();
      SaveCommand = new RelayCommand(_ => SaveToDisk(), _ => IsEditingEnabled);
      ApplyFiltersCommand = new RelayCommand(_ => ApplyFilters());
      ResetFiltersCommand = new RelayCommand(_ => ResetFilters());

      GlobalTimer.PulsationStateChanged += OnPulsationStateChanged;
      ReloadFromDisk();
    }

    /// <summary>Строки таблицы.</summary>
    public ObservableCollection<EnvironmentTriggerRow> Triggers { get; }

    public string CurrentAgentTitle =>
        SymbiontPageTitleFormatter.Format("Триггеры среды", _currentAgentName, _currentAgentStage);

    public string FilterIdText
    {
      get => _filterId;
      set => _filterId = value ?? string.Empty;
    }

    public string FilterTitleText
    {
      get => _filterTitle;
      set => _filterTitle = value ?? string.Empty;
    }

    public ICommand SaveCommand { get; }
    public ICommand ApplyFiltersCommand { get; }
    public ICommand ResetFiltersCommand { get; }

    public bool IsStageZero => _currentAgentStage == 0;
    public bool HasAdapter => SymbiontEnvironmentGate.IsEnvironmentEditingAllowed();
    public bool IsEditingEnabled => HasAdapter && IsStageZero && !GlobalTimer.IsPulsationRunning;
    public bool IsReadOnlyMode => !IsEditingEnabled;
    public string PulseWarningMessage =>
        !HasAdapter
            ? "Укажите AdapterId в проекте (новый проект с выбором адаптера)"
            : !IsStageZero
                ? "[КРИТИЧНО] Редактирование доступно только в стадии 0"
                : GlobalTimer.IsPulsationRunning
                    ? "Редактирование доступно только при выключенной пульсации"
                    : string.Empty;
    public Brush WarningMessageColor => !HasAdapter || !IsStageZero ? Brushes.Red : Brushes.Gray;

    /// <summary>
    /// Перезагрузка с диска.
    /// </summary>
    public void ReloadFromDisk()
    {
      var agent = _gomeostas.GetAgentState();
      _currentAgentStage = agent?.EvolutionStage ?? 0;
      _currentAgentName = agent?.Name ?? string.Empty;

      _allRows.Clear();
      var errors = new List<string>();
      List<EnvironmentTriggerData> loaded = EnvironmentCatalogStorage.LoadTriggers(errors);

      foreach (EnvironmentTriggerData trigger in loaded)
      {
        EnvironmentTriggerRow row = EnvironmentTriggerMapper.ToRow(trigger);
        row.DetectSummary = BuildDetectSummary(row);
        _allRows.Add(row);
      }

      if (errors.Count > 0)
      {
        MessageBox.Show(
            string.Join(Environment.NewLine, errors.Take(8)),
            "Загрузка триггеров",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
      }

      ApplyFilters();
    }

    /// <summary>
    /// Регистрирует новую строку в полном списке (после AddingNewItem).
    /// </summary>
    public void RegisterNewRow(EnvironmentTriggerRow row)
    {
      if (row == null)
        return;

      if (!_allRows.Contains(row))
        _allRows.Add(row);
    }

    /// <summary>
    /// Создаёт строку по умолчанию для новой записи.
    /// </summary>
    public EnvironmentTriggerRow CreateNewRow()
    {
      int n = 1;
      string id;
      do
      {
        id = "trigger_" + n.ToString(CultureInfo.InvariantCulture);
        n++;
      }
      while (_allRows.Any(r => string.Equals(r.Id, id, StringComparison.OrdinalIgnoreCase)));

      return new EnvironmentTriggerRow
      {
        Id = id,
        DisplayName = "Новый триггер",
        InfluenceActionId = 0,
        DetectRules = new List<EnvironmentTriggerDetectRow>
        {
          new EnvironmentTriggerDetectRow
          {
            Kind = "command_before",
            Environment = "solidworks",
            Enabled = true
          },
          new EnvironmentTriggerDetectRow
          {
            Kind = "document_saved",
            Environment = "solidworks",
            Enabled = true
          }
        }
      };
    }

    /// <summary>
    /// Удаляет строки с подтверждением.
    /// </summary>
    public bool TryRemoveRows(IReadOnlyList<EnvironmentTriggerRow> rows)
    {
      if (rows == null || rows.Count == 0 || !IsEditingEnabled)
        return false;

      string msg = rows.Count == 1
          ? "Удалить триггер \"" + rows[0].DisplayName + "\"?"
          : "Удалить выбранные триггеры (" + rows.Count + ")?";

      if (MessageBox.Show(msg, "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        return false;

      foreach (EnvironmentTriggerRow row in rows)
      {
        _allRows.Remove(row);
        Triggers.Remove(row);
      }

      return true;
    }

    /// <summary>
    /// Обновляет краткое описание detect после редактирования.
    /// </summary>
    public void RefreshDetectSummary(EnvironmentTriggerRow row)
    {
      if (row != null)
        row.DetectSummary = BuildDetectSummary(row);
    }

    private void SaveToDisk()
    {
      try
      {
        var definitions = new List<EnvironmentTriggerData>();
        foreach (EnvironmentTriggerRow row in _allRows)
        {
          if (string.IsNullOrWhiteSpace(row?.Id))
            continue;

          definitions.Add(EnvironmentTriggerMapper.ToData(row));
        }

        EnvironmentCatalogStorage.SaveTriggers(definitions);
        MessageBox.Show("Триггеры среды сохранены.", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);
        ReloadFromDisk();
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message, "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void ApplyFilters()
    {
      IEnumerable<EnvironmentTriggerRow> q = _allRows;
      if (!string.IsNullOrWhiteSpace(_filterId))
      {
        string f = _filterId.Trim();
        q = q.Where(r => (r.Id ?? string.Empty).IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0);
      }

      if (!string.IsNullOrWhiteSpace(_filterTitle))
      {
        string f = _filterTitle.Trim();
        q = q.Where(r => (r.DisplayName ?? string.Empty).IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0);
      }

      Triggers.Clear();
      foreach (EnvironmentTriggerRow row in q)
        Triggers.Add(row);
    }

    private void ResetFilters()
    {
      _filterId = string.Empty;
      _filterTitle = string.Empty;
      ApplyFilters();
    }

    private static string BuildDetectSummary(EnvironmentTriggerRow row)
    {
      if (row?.DetectRules == null || row.DetectRules.Count == 0)
        return string.Empty;

      return string.Join(
          "; ",
          row.DetectRules.Select(r =>
              (r?.Kind ?? "?") +
              (string.IsNullOrWhiteSpace(r?.Environment) ? string.Empty : "@" + r.Environment)));
    }

    private void OnPulsationStateChanged()
    {
      Application.Current?.Dispatcher.Invoke(ApplyFilters);
    }

    /// <inheritdoc />
    public void Dispose()
    {
      GlobalTimer.PulsationStateChanged -= OnPulsationStateChanged;
    }
  }
}
