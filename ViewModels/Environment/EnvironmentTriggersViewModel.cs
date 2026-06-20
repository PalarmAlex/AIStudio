using AIStudio.Common;
using AIStudio.Common.Adapters;
using AIStudio.Common.SymbiontEnv;
using AIStudio.Dialogs;
using ISIDA.SymbiontEnv.Contract;
using ISIDA.Common;
using ISIDA.Gomeostas;
using ISIDA.Reflexes;
using ISIDA.Sensors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
  public sealed class EnvironmentTriggersViewModel : INotifyPropertyChanged
  {
    private readonly GomeostasSystem _gomeostas;
    private readonly GeneticReflexesSystem _geneticReflexes;
    private readonly SensorySystem _sensorySystem;
    private readonly List<EnvironmentTriggerRow> _allRows = new List<EnvironmentTriggerRow>();
    private AdapterEnvironmentSchema _schema = AdapterSchemaLoader.LoadForCurrentProject() ?? new AdapterEnvironmentSchema();
    private EnvironmentTriggerRow _selectedTrigger;
    private string _currentAgentName;
    private int _currentAgentStage;
    private string _filterId = string.Empty;
    private string _filterTitle = string.Empty;
    private int _validationIssueCount;
    private bool _showValidationErrors;

    public EnvironmentTriggersViewModel(
        GomeostasSystem gomeostas,
        GeneticReflexesSystem geneticReflexes,
        SensorySystem sensorySystem = null)
    {
      _gomeostas = gomeostas ?? throw new ArgumentNullException(nameof(gomeostas));
      _geneticReflexes = geneticReflexes;
      _sensorySystem = sensorySystem;
      Triggers = new ObservableCollection<EnvironmentTriggerRow>();
      TriggerIdCatalog = new ObservableCollection<AdapterSchemaTriggerCatalogEntry>();
      Links = new ObservableCollection<EnvironmentLinkItem>();
      EventSchema = new SchemaActionEditorViewModel(_schema);
      EventSchema.ValuesCommitted += OnEventSchemaCommitted;
      EventSchema.SelectedCatalogChanged += OnEventSchemaCatalogChanged;

      SaveCommand = new RelayCommand(_ => Save(), _ => IsEditingEnabled);
      AddTriggerCommand = new RelayCommand(_ => AddTrigger(), _ => IsEditingEnabled);
      DeleteSelectedCommand = new RelayCommand(_ => DeleteSelected(), _ => IsEditingEnabled && SelectedTrigger != null);
      ApplyFiltersCommand = new RelayCommand(_ => ApplyFilters());
      ResetFiltersCommand = new RelayCommand(_ => ResetFilters());
      RemoveAllCommand = new RelayCommand(RemoveAllTriggers, _ => IsEditingEnabled);
      PickTriggerIdFromCatalogCommand = new RelayCommand(_ => PickTriggerIdFromCatalog(), _ => IsEditingEnabled && SelectedTrigger != null);
      PickCommandPatternCommand = new RelayCommand(_ => PickCommandPattern(), _ => IsEditingEnabled && SelectedTrigger != null);
      EditHomeostasisDeltasCommand = new RelayCommand(_ => EditHomeostasisDeltas(), _ => IsEditingEnabled && SelectedTrigger != null);

      GlobalTimer.PulsationStateChanged += OnPulsationStateChanged;
      Reload();
    }

    public ObservableCollection<EnvironmentTriggerRow> Triggers { get; }
    public ObservableCollection<AdapterSchemaTriggerCatalogEntry> TriggerIdCatalog { get; }
    public ObservableCollection<EnvironmentLinkItem> Links { get; }
    public SchemaActionEditorViewModel EventSchema { get; }

    public string CurrentAgentTitle =>
        SymbiontPageTitleFormatter.Format("Триггеры среды", _currentAgentName, _currentAgentStage);

    public DescriptionWithLink CurrentAgentDescription { get; } = new DescriptionWithLink();

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

    public ICommand SaveCommand { get; }
    public ICommand AddTriggerCommand { get; }
    public ICommand DeleteSelectedCommand { get; }
    public ICommand ApplyFiltersCommand { get; }
    public ICommand ResetFiltersCommand { get; }
    public ICommand RemoveAllCommand { get; }
    public ICommand PickTriggerIdFromCatalogCommand { get; }
    public ICommand PickCommandPatternCommand { get; }
    public ICommand EditHomeostasisDeltasCommand { get; }

    public event PropertyChangedEventHandler PropertyChanged;

    public bool IsStageZero => _currentAgentStage == 0;
    public bool HasAdapter => SymbiontEnvironmentGate.IsEnvironmentEditingAllowed();
    public bool IsEditingEnabled => HasAdapter && IsStageZero && !GlobalTimer.IsPulsationRunning;

    public bool ShowValidationErrors
    {
      get => _showValidationErrors;
      private set
      {
        if (_showValidationErrors == value)
          return;
        _showValidationErrors = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(EventKindValidationMessage));
        OnPropertyChanged(nameof(HomeostasisDeltasValidationMessage));
        OnPropertyChanged(nameof(CommandPatternValidationMessage));
        OnPropertyChanged(nameof(HomeostasisOrCommandValidationMessage));
      }
    }

    public string EventKindValidationMessage =>
        ShowValidationErrors && SelectedTrigger != null && string.IsNullOrWhiteSpace(SelectedTrigger.EventKind)
            ? "Не заполнено поле"
            : string.Empty;

    public string HomeostasisDeltasValidationMessage =>
        ShowValidationErrors && SelectedTrigger != null && !HasHomeostasisDeltas(SelectedTrigger)
            ? "Не заполнено поле"
            : string.Empty;

    public string CommandPatternValidationMessage =>
        ShowValidationErrors && SelectedTrigger != null && (SelectedTrigger.ReflexTriggerCommandPatternId <= 0)
            ? "Не заполнено поле"
            : string.Empty;

    public string HomeostasisOrCommandValidationMessage =>
        ShowValidationErrors && SelectedTrigger != null && !HasHomeostasisOrCommand(SelectedTrigger)
            ? "Не заполнено поле"
            : string.Empty;

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
      var agent = _gomeostas.GetAgentState();
      _currentAgentStage = agent?.EvolutionStage ?? 0;
      _currentAgentName = agent?.Name ?? string.Empty;
      OnPropertyChanged(nameof(CurrentAgentTitle));
      _allRows.Clear();
      var errors = new List<string>();
      List<EnvironmentTriggerData> loaded = EnvironmentCatalogStorage.LoadTriggers(errors);

      // Если в YAML есть "битые" триггеры, строгий codec contract 3.1 их отбрасывает,
      // из-за чего редактор остаётся без SelectedTrigger и блокирует команды справочников.
      // Для UI поднимаем такие записи "мягко", чтобы их можно было исправить.
      var relaxedInvalidErrors = errors
          .Where(e => (e ?? string.Empty).IndexOf("нужен homeostasis_deltas", StringComparison.OrdinalIgnoreCase) >= 0)
          .ToList();
      if (relaxedInvalidErrors.Count > 0)
      {
        var relaxed = EnvironmentTriggersRelaxedReader.Read(EnvironmentPaths.TriggersFilePath);
        if (relaxed.Count > 0)
        {
          var known = new HashSet<string>(loaded.Select(t => t?.Id ?? string.Empty), StringComparer.OrdinalIgnoreCase);
          foreach (EnvironmentTriggerData t in relaxed)
          {
            if (t == null || string.IsNullOrWhiteSpace(t.Id))
              continue;
            if (!known.Contains(t.Id))
              loaded.Add(t);
          }

          // Эти проблемы показываем inline, без MessageBox.
          errors = errors.Except(relaxedInvalidErrors).ToList();
          ShowValidationErrors = true;
        }
      }
      RefreshSchema();
      foreach (EnvironmentTriggerData trigger in loaded)
      {
        EnvironmentTriggerRow row = EnvironmentTriggerMapper.ToRow(trigger);
        row.EventSummary = BuildEventSummary(row);
        ResolveCommandPatternText(row);
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
      // Показываем inline-валидацию сразу при открытии страницы,
      // чтобы пользователь видел незаполненные поля без "Сохранить" и без переоткрытия справочников.
      ShowValidationErrors = true;
      if (SelectedTrigger == null)
      {
        // Не оставляем UI без контекста: иначе команды справочников будут неактивны.
        EnvironmentTriggerRow row = CreateNewRow();
        _allRows.Add(row);
        ApplyFilters();
        SelectedTrigger = row;
      }
      RecalculateValidation();
    }

    public void Save()
    {
      try
      {
        ShowValidationErrors = true;

        EnvironmentTriggerRow firstInvalid = _allRows.FirstOrDefault(r => r != null && !IsValidForSave(r));
        if (firstInvalid != null)
        {
          SelectedTrigger = firstInvalid;
          RefreshTriggerLinks();
          return;
        }

        var definitions = new List<EnvironmentTriggerData>();
        foreach (EnvironmentTriggerRow row in _allRows)
        {
          if (row == null || string.IsNullOrWhiteSpace(row.Id))
            continue;
          definitions.Add(EnvironmentTriggerMapper.ToData(row));
        }
        EnvironmentCatalogStorage.SaveTriggers(definitions);
        MessageBox.Show(
            "Триггеры среды сохранены.",
            "Сохранение",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
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
      ShowValidationErrors = true;
      RefreshTriggerIdCatalog();
    }

    public void EditHomeostasisDeltas(Window owner = null)
    {
      if (SelectedTrigger == null || !IsEditingEnabled)
        return;
      var editor = new ActionInfluencesEditor(
          "homeostasis_deltas: " + (SelectedTrigger.DisplayName ?? SelectedTrigger.Id),
          _gomeostas.GetAllParameters().ToList(),
          new Dictionary<int, int>(SelectedTrigger.HomeostasisDeltas))
      {
        Owner = owner ?? Application.Current?.MainWindow
      };
      if (editor.ShowDialog() != true)
        return;

      SelectedTrigger.HomeostasisDeltas.Clear();
      foreach (KeyValuePair<int, int> kv in editor.SelectedInfluences)
        SelectedTrigger.HomeostasisDeltas[kv.Key] = GomeostasSystem.ClampInt(kv.Value, -10, 10);
      SelectedTrigger.NotifyHomeostasisDeltasChanged();
      MarkDirty();
      ShowValidationErrors = true;
      OnPropertyChanged(nameof(HomeostasisDeltasValidationMessage));
      OnPropertyChanged(nameof(HomeostasisOrCommandValidationMessage));
      RefreshTriggerLinks();
    }

    public void PickCommandPattern(Window owner = null)
    {
      if (SelectedTrigger == null || !IsEditingEnabled)
        return;
      var dialog = new CommandPatternRadioSelectionDialog(SelectedTrigger.ReflexTriggerCommandPatternId)
      {
        Owner = owner ?? Application.Current?.MainWindow
      };
      if (dialog.ShowDialog() != true)
        return;
      SelectedTrigger.ReflexTriggerCommandPatternId = dialog.SelectedCommandPatternId;
      ResolveCommandPatternText(SelectedTrigger);
      MarkDirty();
      ShowValidationErrors = true;
      OnPropertyChanged(nameof(CommandPatternValidationMessage));
      OnPropertyChanged(nameof(HomeostasisOrCommandValidationMessage));
      RefreshTriggerLinks();
    }

    private void PickCommandPattern() => PickCommandPattern(null);

    private void EditHomeostasisDeltas() => EditHomeostasisDeltas(null);

    private void ResolveCommandPatternText(EnvironmentTriggerRow row)
    {
      if (row == null)
        return;

      if (row.ReflexTriggerCommandPatternId <= 0)
      {
        row.ReflexTriggerCommandPatternText = string.Empty;
        return;
      }

      if (_sensorySystem?.CommandChannel != null)
      {
        row.ReflexTriggerCommandPatternText =
            _sensorySystem.CommandChannel.GetPhraseFromPhraseId(row.ReflexTriggerCommandPatternId) ?? string.Empty;
        return;
      }

      row.ReflexTriggerCommandPatternText = string.Empty;
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
      ShowValidationErrors = true;
      OnPropertyChanged(nameof(EventKindValidationMessage));
    }

    private void OnEventSchemaCatalogChanged()
    {
      if (SelectedTrigger == null)
        return;

      string catalogId = EventSchema.SelectedCatalogId ?? string.Empty;
      IDictionary<string, string> parameters = string.Equals(catalogId, SelectedTrigger.EventKind, StringComparison.OrdinalIgnoreCase)
          ? new Dictionary<string, string>(SelectedTrigger.EventParameters, StringComparer.OrdinalIgnoreCase)
          : null;
      EventSchema.ReloadCurrentCatalogParameters(parameters);
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
      RefreshTriggerLinks();
      OnPropertyChanged(nameof(EventKindValidationMessage));
      OnPropertyChanged(nameof(HomeostasisDeltasValidationMessage));
      OnPropertyChanged(nameof(CommandPatternValidationMessage));
      OnPropertyChanged(nameof(HomeostasisOrCommandValidationMessage));
    }

    private static bool HasHomeostasisDeltas(EnvironmentTriggerRow row)
    {
      if (row?.HomeostasisDeltas == null || row.HomeostasisDeltas.Count == 0)
        return false;
      return row.HomeostasisDeltas.Any(kv => kv.Key > 0 && kv.Value != 0);
    }

    private static bool HasHomeostasisOrCommand(EnvironmentTriggerRow row)
    {
      if (row == null)
        return false;
      if (row.ReflexTriggerCommandPatternId > 0)
        return true;
      return HasHomeostasisDeltas(row);
    }

    private static bool IsValidForSave(EnvironmentTriggerRow row)
    {
      if (row == null)
        return false;
      if (string.IsNullOrWhiteSpace(row.Id))
        return false;
      if (string.IsNullOrWhiteSpace(row.EventKind))
        return false;
      if (!HasHomeostasisOrCommand(row))
        return false;
      return true;
    }

    private void RefreshTriggerLinks()
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
      EventSchema.ReplaceSchema(_schema);
      EventSchema.LoadFromEvent(SelectedTrigger?.EventKind, SelectedTrigger?.EventParameters);
    }

    private void MarkDirty()
    {
      RecalculateValidation();
    }

    private void RecalculateValidation()
    {
      int count = _allRows.Count(r => string.IsNullOrWhiteSpace(r?.Id) || string.IsNullOrWhiteSpace(r?.EventKind));
      if (_validationIssueCount != count)
      {
        _validationIssueCount = count;
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
        OnPropertyChanged(nameof(PulseWarningMessage));
        OnPropertyChanged(nameof(WarningMessageColor));
        EventSchema.IsEditingEnabled = IsEditingEnabled;
      });
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
      public string Text { get; set; } = "Триггеры событий среды: mechanical path (homeostasis_deltas), Command pattern для genetic reflex и связь с рецептами. В Velum runtime SW-события идут через Command idle-flush, не через EA.";
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