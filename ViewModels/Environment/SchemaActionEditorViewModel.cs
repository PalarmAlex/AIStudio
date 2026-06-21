using AIStudio.Common.Adapters;
using AIStudio.Common.SymbiontEnv;
using AIStudio.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace AIStudio.ViewModels.SymbiontEnv
{
  /// <summary>Редактор handler + параметров по schema (шаги рецепта).</summary>
  public sealed class SchemaActionEditorViewModel : INotifyPropertyChanged
  {
    private AdapterEnvironmentSchema _schema;
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

    /// <summary>Подменяет schema после загрузки пакета адаптера.</summary>
    public void ReplaceSchema(AdapterEnvironmentSchema schema)
    {
      _schema = schema ?? new AdapterEnvironmentSchema();
    }

    public ObservableCollection<SchemaCatalogItemRow> CatalogItems { get; }
    public ObservableCollection<SchemaParamRow> Parameters { get; }

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

    public void PickTemplatePlaceholder(Window owner, SchemaParamRow row)
    {
      if (!IsEditingEnabled || row == null || !row.IsTemplateField)
        return;

      IReadOnlyList<RecipeStepCatalogPickItem> items = BuildTemplatePlaceholderPickItems(_schema);
      if (items == null || items.Count == 0)
        return;

      var dialog = new RecipeStepValueSelectionDialog(
          "Плейсхолдер шаблона",
          "Выберите маску для вставки. Между масками вводите разделитель вручную (пробел, дефис и т.д.).",
          items,
          row.Value)
      {
        Owner = owner
      };
      if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.SelectedValue))
        return;

      row.Value = string.IsNullOrWhiteSpace(row.Value)
          ? dialog.SelectedValue
          : row.Value + dialog.SelectedValue;
      NotifyParameterChanged();
    }

    public void PickPropertyName(Window owner, SchemaParamRow row)
    {
      if (!IsEditingEnabled || row == null || !row.IsPropertyNameField)
        return;

      IReadOnlyList<RecipeStepCatalogPickItem> items = BuildPropertyNamePickItems(_schema);
      if (items == null || items.Count == 0)
        return;

      var dialog = new RecipeStepValueSelectionDialog(
          "Имя свойства",
          "Выберите имя пользовательского свойства документа:",
          items,
          row.Value)
      {
        Owner = owner
      };
      if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.SelectedValue))
        return;

      row.Value = dialog.SelectedValue;
      NotifyParameterChanged();
    }

    public void ReloadCurrentCatalogParameters(IDictionary<string, string> args)
    {
      _suppressSideEffects = true;
      try
      {
        LoadHandlerParameters(_selectedCatalogId, SnapshotArgs(args));
        ValidationError = string.Empty;
      }
      finally
      {
        _suppressSideEffects = false;
      }
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
          EditorHint = arg.EditorHint ?? string.Empty,
          Required = arg.Required,
          AllowedValueOptions = BuildAllowedValueOptions(arg.Values),
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

    private static IList<KeyValuePair<string, string>> BuildAllowedValueOptions(
        IList<AdapterSchemaArgValueOption> values)
    {
      if (values == null || values.Count == 0)
        return null;

      var options = new List<KeyValuePair<string, string>>();
      foreach (AdapterSchemaArgValueOption option in values)
      {
        if (option == null || string.IsNullOrWhiteSpace(option.Key))
          continue;
        options.Add(new KeyValuePair<string, string>(option.Key, option.Display));
      }

      return options.Count > 0 ? options : null;
    }

    private static IReadOnlyList<RecipeStepCatalogPickItem> BuildTemplatePlaceholderPickItems(
        AdapterEnvironmentSchema schema)
    {
      if (schema?.RecipeTemplateCatalog?.Placeholders == null || schema.RecipeTemplateCatalog.Placeholders.Count == 0)
        return Array.Empty<RecipeStepCatalogPickItem>();

      var items = new List<RecipeStepCatalogPickItem>();
      foreach (AdapterSchemaTemplatePlaceholder placeholder in schema.RecipeTemplateCatalog.Placeholders)
      {
        if (placeholder == null || string.IsNullOrWhiteSpace(placeholder.Token))
          continue;
        items.Add(new RecipeStepCatalogPickItem
        {
          Value = placeholder.Token.Trim(),
          Description = BuildCatalogPickerDescription(placeholder.Label, placeholder.Description)
        });
      }

      return items;
    }

    private static IReadOnlyList<RecipeStepCatalogPickItem> BuildPropertyNamePickItems(
        AdapterEnvironmentSchema schema)
    {
      if (schema?.RecipeTemplateCatalog?.PropertyNames == null || schema.RecipeTemplateCatalog.PropertyNames.Count == 0)
        return Array.Empty<RecipeStepCatalogPickItem>();

      var items = new List<RecipeStepCatalogPickItem>();
      foreach (AdapterSchemaPropertyNameEntry entry in schema.RecipeTemplateCatalog.PropertyNames)
      {
        if (entry == null || string.IsNullOrWhiteSpace(entry.Name))
          continue;
        items.Add(new RecipeStepCatalogPickItem
        {
          Value = entry.Name.Trim(),
          Description = BuildCatalogPickerDescription(entry.Label, entry.Description)
        });
      }

      return items;
    }

    private static string BuildCatalogPickerDescription(string label, string description)
    {
      string labelText = (label ?? string.Empty).Trim();
      string descriptionText = (description ?? string.Empty).Trim();
      if (labelText.Length > 0 && descriptionText.Length > 0)
        return labelText + " — " + descriptionText;
      if (descriptionText.Length > 0)
        return descriptionText;
      return labelText;
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
