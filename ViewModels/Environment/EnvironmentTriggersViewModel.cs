using AIStudio.Common;
using AIStudio.Common.Adapters;
using AIStudio.Common.SymbiontEnv;
using AIStudio.Dialogs;
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
    private readonly List<TriggerCatalogItem> _triggerIdOptions = new List<TriggerCatalogItem>();
    private AdapterEnvironmentSchema _schema = new AdapterEnvironmentSchema();
    private IReadOnlyList<AdapterSchemaDetectKind> _detectKinds = new List<AdapterSchemaDetectKind>();
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
      RemoveAllCommand = new RelayCommand(RemoveAllTriggers, _ => IsEditingEnabled);
      ApplyFiltersCommand = new RelayCommand(_ => ApplyFilters());
      ResetFiltersCommand = new RelayCommand(_ => ResetFilters());
      GlobalTimer.PulsationStateChanged += OnPulsationStateChanged;
      ReloadFromDisk();
    }

    /// <summary>Строки таблицы.</summary>
    public ObservableCollection<EnvironmentTriggerRow> Triggers { get; }

    /// <summary>Справочник ID триггеров для диалога выбора.</summary>
    public IReadOnlyList<TriggerCatalogItem> TriggerIdOptions => _triggerIdOptions;

    /// <summary>Типы detect из schema адаптера.</summary>
    public IReadOnlyList<AdapterSchemaDetectKind> DetectKindOptions => _detectKinds;
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
    public ICommand RemoveAllCommand { get; }
    public ICommand ApplyFiltersCommand { get; }
    public ICommand ResetFiltersCommand { get; }
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
      RefreshSchema();
      foreach (EnvironmentTriggerData trigger in loaded)
      {
        EnvironmentTriggerRow row = EnvironmentTriggerMapper.ToRow(trigger);
        EnvironmentTriggerFilterSchemaHelper.Initialize(row, trigger, _schema);
        row.DetectSummary = BuildDetectSummary(row);
        row.FilterSummary = BuildFilterSummary(row);
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
      RefreshTriggerIdOptions();
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
      RefreshTriggerIdOptions();
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
      var row = new EnvironmentTriggerRow
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
      EnvironmentTriggerFilterSchemaHelper.Initialize(row, null, _schema, applyNewDefaults: true);
      row.FilterSummary = BuildFilterSummary(row);
      return row;
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

    /// <summary>
    /// Выбор ID триггера из справочника проекта.
    /// </summary>
    public bool TryPickTriggerId(Window owner, EnvironmentTriggerRow row)
    {
      if (row == null || !IsEditingEnabled)
        return false;

      if (_triggerIdOptions.Count == 0)
      {
        MessageBox.Show(
            "Справочник ID пуст. Введите новый ID вручную в ячейке таблицы.",
            "Выбор ID триггера",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        return false;
      }

      var dialog = new TriggerIdSelectionDialog(row.Id, _triggerIdOptions)
      {
        Owner = owner
      };
      if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.SelectedTriggerId))
        return false;

      row.Id = dialog.SelectedTriggerId;
      if (!string.IsNullOrWhiteSpace(dialog.SelectedDisplayName)
          && (string.IsNullOrWhiteSpace(row.DisplayName) || string.Equals(row.DisplayName, "Новый триггер", StringComparison.Ordinal)))
        row.DisplayName = dialog.SelectedDisplayName;

      RefreshTriggerIdOptions();
      return true;
    }

    /// <summary>
    /// Выбор воздействия (InfluenceAction) через радио-диалог.
    /// </summary>
    public bool TryPickInfluenceAction(Window owner, EnvironmentTriggerRow row)
    {
      if (row == null || !IsEditingEnabled)
        return false;

      var dialog = new InfluenceActionRadioSelectionDialog(row.InfluenceActionId)
      {
        Owner = owner
      };
      if (dialog.ShowDialog() != true || dialog.SelectedInfluenceActionId <= 0)
        return false;

      row.InfluenceActionId = dialog.SelectedInfluenceActionId;
      return true;
    }

    /// <summary>
    /// Редактирование фильтра контекста триггера.
    /// </summary>
    public bool TryEditFilterFields(Window owner, EnvironmentTriggerRow row)
    {
      if (row == null || !IsEditingEnabled)
        return false;

      var editor = new EnvironmentTriggerFilterEditorDialog(row.FilterFields)
      {
        Owner = owner
      };
      if (editor.ShowDialog() != true)
        return false;

      EnvironmentSchemaFieldsHelper.ReplaceFields(row.FilterFields, editor.FilterFields);
      row.FilterSummary = BuildFilterSummary(row);
      return true;
    }

    /// <summary>
    /// Редактирование правил детекции.
    /// </summary>
    public bool TryEditDetectRules(Window owner, EnvironmentTriggerRow row)
    {
      if (row == null || !IsEditingEnabled)
        return false;

      var editor = new EnvironmentTriggerDetectEditorDialog(row.DetectRules, _detectKinds)
      {
        Owner = owner
      };
      if (editor.ShowDialog() != true)
        return false;

      row.DetectRules = editor.ResultRules;
      RefreshDetectSummary(row);
      return true;
    }

    public void RemoveAllTriggers(object parameter)
    {
      if (!IsEditingEnabled)
        return;

      MessageBoxResult result = MessageBox.Show(
          "Вы действительно хотите удалить ВСЕ триггеры среды? Это действие нельзя будет отменить.",
          "Подтверждение удаления",
          MessageBoxButton.YesNo,
          MessageBoxImage.Warning);
      if (result != MessageBoxResult.Yes)
        return;

      try
      {
        _allRows.Clear();
        Triggers.Clear();
        EnvironmentCatalogStorage.SaveTriggers(new List<EnvironmentTriggerData>());
        MessageBox.Show(
            "Все триггеры среды успешно удалены",
            "Удаление завершено",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        ReloadFromDisk();
      }
      catch (Exception ex)
      {
        MessageBox.Show(
            "Ошибка удаления триггеров среды: " + ex.Message,
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        ReloadFromDisk();
      }
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

    private string BuildDetectSummary(EnvironmentTriggerRow row)
    {
      if (row?.DetectRules == null || row.DetectRules.Count == 0)
        return string.Empty;
      return string.Join(
          "; ",
          row.DetectRules.Select(r =>
          {
            string kind = r?.Kind ?? "?";
            string label = ResolveDetectKindLabel(kind);
            string summary = label;
            if (!string.IsNullOrWhiteSpace(r?.Environment))
              summary += "@" + r.Environment;
            if (string.Equals(kind, "command_before", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(r?.CommandIdsText))
              summary += " [" + r.CommandIdsText + "]";
            if (r != null && !r.Enabled)
              summary += " (выкл.)";
            return summary;
          }));
    }

    private string ResolveDetectKindLabel(string kind)
    {
      if (string.IsNullOrWhiteSpace(kind))
        return "?";

      AdapterSchemaDetectKind match = _detectKinds.FirstOrDefault(
          k => string.Equals(k?.Kind, kind, StringComparison.OrdinalIgnoreCase));
      if (match != null && !string.IsNullOrWhiteSpace(match.Label))
        return match.Label;

      return kind;
    }

    private void RefreshTriggerIdOptions()
    {
      _triggerIdOptions.Clear();
      var knownIds = new HashSet<string>(StringComparer.Ordinal);
      foreach (EnvironmentTriggerRow row in _allRows.OrderBy(r => r?.Id ?? string.Empty, StringComparer.Ordinal))
      {
        string id = (row?.Id ?? string.Empty).Trim();
        if (id.Length == 0 || !knownIds.Add(id))
          continue;
        _triggerIdOptions.Add(new TriggerCatalogItem
        {
          Id = id,
          Label = row.DisplayName ?? id,
          Description = string.Empty
        });
      }
    }

    private void RefreshSchema()
    {
      _schema = AdapterSchemaLoader.LoadForCurrentProject() ?? new AdapterEnvironmentSchema();
      IList<AdapterSchemaDetectKind> kinds = _schema.TriggerDetectKinds;
      _detectKinds = kinds != null
          ? kinds.ToList()
          : new List<AdapterSchemaDetectKind>();
    }

    private static string BuildFilterSummary(EnvironmentTriggerRow row)
    {
      return EnvironmentSchemaFieldsHelper.BuildSummary(row?.FilterFields);
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
