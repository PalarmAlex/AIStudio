using AIStudio.Common;
using AIStudio.Common.Adapters;
using AIStudio.Common.SymbiontEnv;
using AIStudio.Dialogs;
using ISIDA.SymbiontEnv.Contract;
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
  /// <summary>
  /// Редактор триггеров среды (master-detail + SchemaActionPanel).
  /// </summary>
  public sealed class EnvironmentTriggersViewModel : IEnvironmentChildViewModel, INotifyPropertyChanged
  {
    private readonly GomeostasSystem _gomeostas;
    private readonly GeneticReflexesSystem _geneticReflexes;
    private readonly List<EnvironmentTriggerRow> _allRows = new List<EnvironmentTriggerRow>();
    private AdapterEnvironmentSchema _schema = new AdapterEnvironmentSchema();
    private EnvironmentTriggerRow _selectedTrigger;
    private string _currentAgentName;
    private int _currentAgentStage;
    private string _filterId = string.Empty;
    private string _filterTitle = string.Empty;
    private bool _dirty;
    private int _validationIssueCount;
    public EnvironmentTriggersViewModel(GomeostasSystem gomeostas, GeneticReflexesSystem geneticReflexes)
    {
      _gomeostas = gomeostas ?? throw new ArgumentNullException(nameof(gomeostas));
      _geneticReflexes = geneticReflexes;
      Triggers = new ObservableCollection<EnvironmentTriggerRow>();
      TriggerIdCatalog = new ObservableCollection<AdapterSchemaTriggerCatalogEntry>();
      Links = new ObservableCollection<EnvironmentLinkItem>();
      EventSchema = new SchemaActionEditorViewModel(_schema);
      EventSchema.ValuesCommitted += OnEventSchemaCommitted;
      EventSchema.SelectedCatalogChanged += OnEventSchemaCatalogChanged;

      SaveCommand = new RelayCommand(_ => Save(), _ => CanSave);
      AddTriggerCommand = new RelayCommand(_ => AddTrigger(), _ => IsEditingEnabled);
      DeleteSelectedCommand = new RelayCommand(_ => DeleteSelected(), _ => IsEditingEnabled && SelectedTrigger != null);
      ApplyFiltersCommand = new RelayCommand(_ => ApplyFilters());
      ResetFiltersCommand = new RelayCommand(_ => ResetFilters());
      RemoveAllCommand = new RelayCommand(RemoveAllTriggers, _ => IsEditingEnabled);
      PickTriggerIdFromCatalogCommand = new RelayCommand(_ => PickTriggerIdFromCatalog(), _ => IsEditingEnabled && SelectedTrigger != null);
      PickInfluenceActionCommand = new RelayCommand(_ => PickInfluenceAction(), _ => IsEditingEnabled && SelectedTrigger != null);
      NavigateToRecipesCommand = new RelayCommand(_ => NavigateToRecipes(), _ => SelectedTrigger != null);
      NavigateToReflexesCommand = new RelayCommand(_ => NavigateToReflexes(), _ => SelectedTrigger != null);

      GlobalTimer.PulsationStateChanged += OnPulsationStateChanged;
      Reload();
    }

    public ObservableCollection<EnvironmentTriggerRow> Triggers { get; }
    public ObservableCollection<AdapterSchemaTriggerCatalogEntry> TriggerIdCatalog { get; }
    public ObservableCollection<EnvironmentLinkItem> Links { get; }
    public SchemaActionEditorViewModel EventSchema { get; }

    public EnvironmentTriggerRow SelectedTrigger
    {
      get => _selectedTrigger;
      set
      {
        if (_selectedTrigger == value)
          return;
        _selectedTrigger = value;
        OnPropertyChanged();
        LoadSelectedTriggerDetails();
      }
    }

    public string FilterIdText
    {
      get => _filterId;
      set { _filterId = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string FilterTitleText
    {
      get => _filterTitle;
      set { _filterTitle = value ?? string.Empty; OnPropertyChanged(); }
    }

    public bool Dirty => _dirty;
    public int ValidationIssueCount => _validationIssueCount;
    public bool CanSave => IsEditingEnabled && _dirty;

    public ICommand SaveCommand { get; }
    public ICommand AddTriggerCommand { get; }
    public ICommand DeleteSelectedCommand { get; }
    public ICommand ApplyFiltersCommand { get; }
    public ICommand ResetFiltersCommand { get; }
    public ICommand RemoveAllCommand { get; }
    public ICommand PickTriggerIdFromCatalogCommand { get; }
    public ICommand PickInfluenceActionCommand { get; }
    public ICommand NavigateToRecipesCommand { get; }
    public ICommand NavigateToReflexesCommand { get; }

    public event Action DirtyChanged;
    public event Action<int> ValidationIssueCountChanged;
    public event Action<EnvironmentNavigationRequest> NavigateRequest;
    public event PropertyChangedEventHandler PropertyChanged;

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

    public void Reload()
    {
      _dirty = false;
      DirtyChanged?.Invoke();
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
        row.EventSummary = BuildEventSummary(row);
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
      RefreshTriggerIdCatalog();
      ApplyFilters();
      SelectedTrigger = Triggers.FirstOrDefault();
      RecalculateValidation();
      OnPropertyChanged(nameof(CanSave));
    }

    public void Save()
    {
      try
      {
        var definitions = new List<EnvironmentTriggerData>();
        foreach (EnvironmentTriggerRow row in _allRows)
        {
          if (string.IsNullOrWhiteSpace(row?.Id))
            continue;
          if (string.IsNullOrWhiteSpace(row.EventKind))
          {
            MessageBox.Show(
                "Укажите событие для триггера \"" + row.DisplayName + "\".",
                "Сохранение",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
          }
          definitions.Add(EnvironmentTriggerMapper.ToData(row));
        }
        EnvironmentCatalogStorage.SaveTriggers(definitions);
        _dirty = false;
        DirtyChanged?.Invoke();
        OnPropertyChanged(nameof(CanSave));
        Reload();
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message, "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    public void RegisterNewRow(EnvironmentTriggerRow row)
    {
      if (row == null)
        return;
      if (!_allRows.Contains(row))
        _allRows.Add(row);
      MarkDirty();
      RefreshTriggerIdCatalog();
    }

    public EnvironmentTriggerRow CreateNewRow()
    {
      string defaultId = TriggerIdCatalog.FirstOrDefault()?.Id;
      if (string.IsNullOrWhiteSpace(defaultId))
      {
        int n = 1;
        do
        {
          defaultId = "trigger_" + n.ToString(CultureInfo.InvariantCulture);
          n++;
        }
        while (_allRows.Any(r => string.Equals(r.Id, defaultId, StringComparison.OrdinalIgnoreCase)));
      }

      string defaultEvent = _schema.TriggerDetectKinds?.FirstOrDefault()?.Kind ?? "document_saved";
      AdapterSchemaTriggerCatalogEntry catalogEntry = TriggerIdCatalog.FirstOrDefault(
          e => string.Equals(e?.Id, defaultId, StringComparison.OrdinalIgnoreCase));
      var row = new EnvironmentTriggerRow
      {
        Id = defaultId,
        DisplayName = catalogEntry?.Label ?? "Новый триггер",
        InfluenceActionId = 0,
        EventKind = defaultEvent
      };
      row.EventSummary = BuildEventSummary(row);
      return row;
    }

    public void Dispose()
    {
      GlobalTimer.PulsationStateChanged -= OnPulsationStateChanged;
      EventSchema.ValuesCommitted -= OnEventSchemaCommitted;
      EventSchema.SelectedCatalogChanged -= OnEventSchemaCatalogChanged;
    }

    private void AddTrigger()
    {
      EnvironmentTriggerRow row = CreateNewRow();
      _allRows.Add(row);
      ApplyFilters();
      SelectedTrigger = row;
      MarkDirty();
    }

    private void DeleteSelected()
    {
      if (SelectedTrigger == null || !IsEditingEnabled)
        return;
      if (MessageBox.Show(
              "Удалить триггер \"" + SelectedTrigger.DisplayName + "\"?",
              "Подтверждение",
              MessageBoxButton.YesNo,
              MessageBoxImage.Question) != MessageBoxResult.Yes)
        return;
      _allRows.Remove(SelectedTrigger);
      Triggers.Remove(SelectedTrigger);
      SelectedTrigger = Triggers.FirstOrDefault();
      MarkDirty();
      RefreshTriggerIdCatalog();
    }

    private void PickTriggerIdFromCatalog()
    {
      if (SelectedTrigger == null || !IsEditingEnabled)
        return;
      var options = TriggerIdCatalog.Select(e => new TriggerCatalogItem
      {
        Id = e.Id,
        Label = e.Label,
        Description = e.Description
      }).ToList();
      if (options.Count == 0)
      {
        MessageBox.Show("Каталог trigger-catalog.json пуст.", "Выбор ID", MessageBoxButton.OK, MessageBoxImage.Information);
        return;
      }
      var dialog = new TriggerIdSelectionDialog(SelectedTrigger.Id, options);
      if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.SelectedTriggerId))
        return;
      SelectedTrigger.Id = dialog.SelectedTriggerId;
      if (!string.IsNullOrWhiteSpace(dialog.SelectedDisplayName))
        SelectedTrigger.DisplayName = dialog.SelectedDisplayName;
      MarkDirty();
      RefreshTriggerIdCatalog();
    }

    private void PickInfluenceAction()
    {
      if (SelectedTrigger == null || !IsEditingEnabled)
        return;
      var dialog = new InfluenceActionRadioSelectionDialog(SelectedTrigger.InfluenceActionId);
      if (dialog.ShowDialog() != true || dialog.SelectedInfluenceActionId <= 0)
        return;
      SelectedTrigger.InfluenceActionId = dialog.SelectedInfluenceActionId;
      MarkDirty();
      RefreshLinks();
    }

    private void NavigateToRecipes()
    {
      if (SelectedTrigger == null)
        return;
      NavigateRequest?.Invoke(new EnvironmentNavigationRequest
      {
        Tab = EnvironmentShellTab.Recipes,
        TriggerId = SelectedTrigger.Id,
        InfluenceActionId = SelectedTrigger.InfluenceActionId
      });
    }

    private void NavigateToReflexes()
    {
      NavigateRequest?.Invoke(new EnvironmentNavigationRequest
      {
        Tab = EnvironmentShellTab.Overview
      });
    }

    private void OnEventSchemaCommitted(Dictionary<string, string> values)
    {
      if (SelectedTrigger == null)
        return;
      SelectedTrigger.EventKind = EventSchema.SelectedCatalogId;
      SelectedTrigger.EventParameters.Clear();
      if (values != null)
      {
        foreach (KeyValuePair<string, string> kv in values)
          SelectedTrigger.EventParameters[kv.Key] = kv.Value;
      }
      SelectedTrigger.EventSummary = BuildEventSummary(SelectedTrigger);
      MarkDirty();
      RefreshLinks();
    }

    private void OnEventSchemaCatalogChanged()
    {
      if (SelectedTrigger == null)
        return;
      SelectedTrigger.EventKind = EventSchema.SelectedCatalogId;
      SelectedTrigger.EventSummary = BuildEventSummary(SelectedTrigger);
      MarkDirty();
    }

    private void LoadSelectedTriggerDetails()
    {
      EventSchema.IsEditingEnabled = IsEditingEnabled;
      if (SelectedTrigger == null)
      {
        Links.Clear();
        return;
      }
      EventSchema.LoadFromEvent(SelectedTrigger.EventKind, SelectedTrigger.EventParameters);
      RefreshLinks();
    }

    private void RefreshLinks()
    {
      Links.Clear();
      if (SelectedTrigger == null)
        return;
      var errors = new List<string>();
      IList<EnvironmentRecipeData> recipes = EnvironmentCatalogStorage.LoadRecipes(errors);
      foreach (EnvironmentLinkItem item in EnvironmentLinksService.BuildTriggerLinks(SelectedTrigger, recipes, _geneticReflexes))
        Links.Add(item);
    }

    private void RemoveAllTriggers(object parameter)
    {
      if (!IsEditingEnabled)
        return;
      if (MessageBox.Show(
              "Удалить ВСЕ триггеры среды?",
              "Подтверждение",
              MessageBoxButton.YesNo,
              MessageBoxImage.Warning) != MessageBoxResult.Yes)
        return;
      _allRows.Clear();
      Triggers.Clear();
      EnvironmentCatalogStorage.SaveTriggers(new List<EnvironmentTriggerData>());
      Reload();
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
      OnPropertyChanged(nameof(FilterIdText));
      OnPropertyChanged(nameof(FilterTitleText));
      ApplyFilters();
    }

    private void RefreshTriggerIdCatalog()
    {
      TriggerIdCatalog.Clear();
      var knownIds = new HashSet<string>(StringComparer.Ordinal);
      foreach (AdapterSchemaTriggerCatalogEntry entry in AdapterSchemaLoader.LoadTriggerCatalogForCurrentProject())
      {
        if (entry == null || string.IsNullOrWhiteSpace(entry.Id) || !knownIds.Add(entry.Id))
          continue;
        TriggerIdCatalog.Add(entry);
      }
      foreach (EnvironmentTriggerRow row in _allRows.OrderBy(r => r?.Id ?? string.Empty, StringComparer.Ordinal))
      {
        string id = (row?.Id ?? string.Empty).Trim();
        if (id.Length == 0 || !knownIds.Add(id))
          continue;
        TriggerIdCatalog.Add(new AdapterSchemaTriggerCatalogEntry
        {
          Id = id,
          Label = row.DisplayName ?? id
        });
      }
    }

    private void RefreshSchema()
    {
      _schema = AdapterSchemaLoader.LoadForCurrentProject() ?? new AdapterEnvironmentSchema();
      EventSchema.LoadFromEvent(SelectedTrigger?.EventKind, SelectedTrigger?.EventParameters);
    }

    private void MarkDirty()
    {
      _dirty = true;
      DirtyChanged?.Invoke();
      OnPropertyChanged(nameof(CanSave));
      OnPropertyChanged(nameof(Dirty));
      RecalculateValidation();
    }

    private void RecalculateValidation()
    {
      int count = _allRows.Count(r => string.IsNullOrWhiteSpace(r?.Id) || string.IsNullOrWhiteSpace(r?.EventKind) || r.InfluenceActionId <= 0);
      if (_validationIssueCount != count)
      {
        _validationIssueCount = count;
        ValidationIssueCountChanged?.Invoke(count);
        OnPropertyChanged(nameof(ValidationIssueCount));
      }
    }

    private string BuildEventSummary(EnvironmentTriggerRow row)
    {
      if (row == null || string.IsNullOrWhiteSpace(row.EventKind))
        return string.Empty;
      string label = ResolveEventKindLabel(row.EventKind);
      if (row.EventParameters == null || row.EventParameters.Count == 0)
        return label;
      var parts = row.EventParameters
          .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
          .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
          .Select(kv => kv.Key + "=" + kv.Value)
          .ToList();
      return parts.Count > 0 ? label + " (" + string.Join(", ", parts) + ")" : label;
    }

    private string ResolveEventKindLabel(string kind)
    {
      AdapterSchemaDetectKind match = _schema.TriggerDetectKinds?.FirstOrDefault(
          k => string.Equals(k?.Kind, kind, StringComparison.OrdinalIgnoreCase));
      return match != null && !string.IsNullOrWhiteSpace(match.Label) ? match.Label : kind;
    }

    private void OnPulsationStateChanged()
    {
      Application.Current?.Dispatcher.Invoke(() =>
      {
        OnPropertyChanged(nameof(IsEditingEnabled));
        OnPropertyChanged(nameof(CanSave));
        EventSchema.IsEditingEnabled = IsEditingEnabled;
      });
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
