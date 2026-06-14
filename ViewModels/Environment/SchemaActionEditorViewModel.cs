using AIStudio.Common.Adapters;
using AIStudio.Common.SymbiontEnv;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>Редактор действия + параметров по schema (handler или event).</summary>
  public sealed class SchemaActionEditorViewModel : INotifyPropertyChanged
  {
    private readonly AdapterEnvironmentSchema _schema;
    private SchemaActionEditorMode _mode = SchemaActionEditorMode.RecipeHandler;
    private bool _isEditingEnabled = true;
    private string _catalogSearchText = string.Empty;
    private string _selectedCatalogId = string.Empty;
    private string _selectedDescription = string.Empty;
    private string _validationError = string.Empty;
    private bool _suppressSideEffects;
    private readonly List<SchemaCatalogItemRow> _allCatalogItems = new List<SchemaCatalogItemRow>();

    public SchemaActionEditorViewModel(AdapterEnvironmentSchema schema)
    {
      _schema = schema ?? new AdapterEnvironmentSchema();
      CatalogItems = new ObservableCollection<SchemaCatalogItemRow>();
      Parameters = new ObservableCollection<SchemaParamRow>();
      ApplyCatalogFilterCommand = new RelayCommand(_ => ApplyCatalogFilter());
      ShowHelpCommand = new RelayCommand(_ => { });
    }

    public ObservableCollection<SchemaCatalogItemRow> CatalogItems { get; }
    public ObservableCollection<SchemaParamRow> Parameters { get; }

    public SchemaActionEditorMode Mode
    {
      get => _mode;
      private set
      {
        if (_mode == value)
          return;
        _mode = value;
        OnPropertyChanged();
      }
    }

    public bool IsEditingEnabled
    {
      get => _isEditingEnabled;
      set
      {
        if (_isEditingEnabled == value)
          return;
        _isEditingEnabled = value;
        OnPropertyChanged();
      }
    }

    public string CatalogSearchText
    {
      get => _catalogSearchText;
      set
      {
        string normalized = value ?? string.Empty;
        if (_catalogSearchText == normalized)
          return;
        _catalogSearchText = normalized;
        OnPropertyChanged();
        ApplyCatalogFilter();
      }
    }

    public string SelectedCatalogId
    {
      get => _selectedCatalogId;
      set
      {
        string normalized = value ?? string.Empty;
        if (_selectedCatalogId == normalized)
          return;
        _selectedCatalogId = normalized;
        OnPropertyChanged();
        OnSelectedCatalogChanged();
      }
    }

    public string SelectedDescription
    {
      get => _selectedDescription;
      private set
      {
        string normalized = value ?? string.Empty;
        if (_selectedDescription == normalized)
          return;
        _selectedDescription = normalized;
        OnPropertyChanged();
      }
    }

    public string ValidationError
    {
      get => _validationError;
      private set
      {
        string normalized = value ?? string.Empty;
        if (_validationError == normalized)
          return;
        _validationError = normalized;
        OnPropertyChanged();
      }
    }

    public ICommand ApplyCatalogFilterCommand { get; }
    public ICommand ShowHelpCommand { get; }

    public event Action SelectedCatalogChanged;
    public event Action ParameterValueChanged;
    public event Action<Dictionary<string, string>> ValuesCommitted;

    public event PropertyChangedEventHandler PropertyChanged;

    public void LoadFromHandler(string handlerId, IDictionary<string, string> args)
    {
      _suppressSideEffects = true;
      try
      {
        Mode = SchemaActionEditorMode.RecipeHandler;
        _allCatalogItems.Clear();
        foreach (AdapterSchemaHandler handler in EnvironmentRecipeStepSchemaHelper.GetHandlers(_schema))
        {
          _allCatalogItems.Add(new SchemaCatalogItemRow
          {
            Id = handler.Id,
            Label = handler.Label,
            Description = handler.Description
          });
        }
        ApplyCatalogFilter();
        var argsSnapshot = SnapshotArgs(args);
        string normalizedHandlerId = handlerId ?? string.Empty;
        SelectedCatalogId = normalizedHandlerId;
        LoadHandlerParameters(normalizedHandlerId, argsSnapshot);
        ValidationError = string.Empty;
      }
      finally
      {
        _suppressSideEffects = false;
      }
    }

    public void LoadFromEvent(string eventKind, IDictionary<string, string> parameters)
    {
      _suppressSideEffects = true;
      try
      {
        Mode = SchemaActionEditorMode.TriggerEvent;
        _allCatalogItems.Clear();
        if (_schema.TriggerDetectKinds != null)
        {
          foreach (AdapterSchemaDetectKind kind in _schema.TriggerDetectKinds)
          {
            if (kind == null || string.IsNullOrWhiteSpace(kind.Kind))
              continue;
            _allCatalogItems.Add(new SchemaCatalogItemRow
            {
              Id = kind.Kind,
              Label = kind.Label,
              Description = kind.Kind
            });
          }
        }
        ApplyCatalogFilter();
        var paramsSnapshot = SnapshotArgs(parameters);
        string normalizedEventKind = eventKind ?? string.Empty;
        SelectedCatalogId = normalizedEventKind;
        LoadEventParameters(normalizedEventKind, paramsSnapshot);
        ValidationError = string.Empty;
      }
      finally
      {
        _suppressSideEffects = false;
      }
    }

    public bool TryCommit(out string error)
    {
      error = null;
      ValidationError = string.Empty;
      var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      foreach (SchemaParamRow row in Parameters)
      {
        if (row == null || string.IsNullOrWhiteSpace(row.Key))
          continue;
        if (row.Required && string.IsNullOrWhiteSpace(row.Value))
        {
          error = "Заполните поле \"" + (row.Label ?? row.Key) + "\".";
          ValidationError = error;
          return false;
        }
        if (!string.IsNullOrWhiteSpace(row.Value))
          values[row.Key] = row.Value.Trim();
      }

      ValuesCommitted?.Invoke(values);
      return true;
    }

    public void CommitCurrentValues()
    {
      if (TryCommit(out string error))
        return;
    }

    public void NotifyParameterChanged()
    {
      if (_suppressSideEffects)
        return;
      ParameterValueChanged?.Invoke();
      TryCommit(out _);
    }

    public void ReloadCurrentCatalogParameters(IDictionary<string, string> args)
    {
      _suppressSideEffects = true;
      try
      {
        var argsSnapshot = SnapshotArgs(args);
        if (Mode == SchemaActionEditorMode.RecipeHandler)
          LoadHandlerParameters(_selectedCatalogId, argsSnapshot);
        else
          LoadEventParameters(_selectedCatalogId, argsSnapshot);
        ValidationError = string.Empty;
      }
      finally
      {
        _suppressSideEffects = false;
      }
    }

    private Dictionary<string, string> BuildCurrentValues()
    {
      var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      foreach (SchemaParamRow row in Parameters)
      {
        if (row == null || string.IsNullOrWhiteSpace(row.Key) || string.IsNullOrWhiteSpace(row.Value))
          continue;
        values[row.Key] = row.Value.Trim();
      }
      return values;
    }

    private void ApplyCatalogFilter()
    {
      CatalogItems.Clear();
      IEnumerable<SchemaCatalogItemRow> q = _allCatalogItems;
      if (!string.IsNullOrWhiteSpace(_catalogSearchText))
      {
        string f = _catalogSearchText.Trim();
        q = q.Where(item =>
            (item.Id ?? string.Empty).IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0
            || (item.Label ?? string.Empty).IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0
            || (item.Description ?? string.Empty).IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0);
      }
      foreach (SchemaCatalogItemRow item in q)
        CatalogItems.Add(item);
    }

    private void OnSelectedCatalogChanged()
    {
      if (_suppressSideEffects)
        return;
      SelectedCatalogChanged?.Invoke();
    }

    private static Dictionary<string, string> SnapshotArgs(IDictionary<string, string> args)
    {
      if (args == null || args.Count == 0)
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      return new Dictionary<string, string>(args, StringComparer.OrdinalIgnoreCase);
    }

    private void ClearParameters()
    {
      foreach (SchemaParamRow row in Parameters)
      {
        if (row != null)
          row.PropertyChanged -= OnParameterRowChanged;
      }
      Parameters.Clear();
    }

    private void LoadHandlerParameters(string handlerId, IDictionary<string, string> existingArgs)
    {
      ClearParameters();
      AdapterSchemaHandler handler = _schema.Handlers?.FirstOrDefault(
          h => string.Equals(h?.Id, handlerId, StringComparison.OrdinalIgnoreCase));
      SelectedDescription = handler?.Description ?? handler?.Label ?? string.Empty;
      var existing = existingArgs ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      if (handler?.ArgsSchema == null)
        return;

      foreach (AdapterSchemaHandlerArg arg in handler.ArgsSchema)
      {
        if (arg == null || string.IsNullOrWhiteSpace(arg.Key))
          continue;
        string value = string.Empty;
        if (existing.TryGetValue(arg.Key, out string existingValue))
          value = existingValue ?? string.Empty;
        var row = new SchemaParamRow
        {
          Key = arg.Key,
          Label = string.IsNullOrWhiteSpace(arg.Label) ? arg.Key : arg.Label,
          Type = arg.Type ?? string.Empty,
          Required = arg.Required,
          AllowedValues = arg.Values,
          Value = value
        };
        row.PropertyChanged += OnParameterRowChanged;
        Parameters.Add(row);
      }
    }

    private void LoadEventParameters(string eventKind, IDictionary<string, string> existingParams)
    {
      ClearParameters();
      AdapterSchemaDetectKind detectKind = _schema.TriggerDetectKinds?.FirstOrDefault(
          k => string.Equals(k?.Kind, eventKind, StringComparison.OrdinalIgnoreCase));
      SelectedDescription = detectKind?.Label ?? eventKind ?? string.Empty;
      var existing = existingParams ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      if (detectKind?.Parameters == null)
        return;

      foreach (AdapterSchemaEventParameter param in detectKind.Parameters)
      {
        if (param == null || string.IsNullOrWhiteSpace(param.Key))
          continue;
        string value = string.Empty;
        if (existing.TryGetValue(param.Key, out string existingValue))
          value = existingValue ?? string.Empty;
        var row = new SchemaParamRow
        {
          Key = param.Key,
          Label = string.IsNullOrWhiteSpace(param.Label) ? param.Key : param.Label,
          Type = param.Type ?? string.Empty,
          Required = param.Required,
          AllowedValues = param.Values,
          Value = value
        };
        row.PropertyChanged += OnParameterRowChanged;
        Parameters.Add(row);
      }
    }

    private void OnParameterRowChanged(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName == nameof(SchemaParamRow.Value) || e.PropertyName == nameof(SchemaParamRow.BoolValue))
        NotifyParameterChanged();
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }

  /// <summary>Режим SchemaActionPanel.</summary>
  public enum SchemaActionEditorMode
  {
    RecipeHandler,
    TriggerEvent
  }
}
