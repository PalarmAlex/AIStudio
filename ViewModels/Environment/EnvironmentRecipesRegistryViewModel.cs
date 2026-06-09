using AIStudio.Common;
using AIStudio.Common.Adapters;
using AIStudio.Common.SymbiontEnv;
using ISIDA.SymbiontEnv.Contract;
using ISIDA.Common;
using ISIDA.Gomeostas;
using ISIDA.Reflexes;
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
  /// <summary>Реестр рецептов среды.</summary>
  public sealed class EnvironmentRecipesRegistryViewModel : IEnvironmentChildViewModel
  {
    private readonly GomeostasSystem _gomeostas;
    private readonly GeneticReflexesSystem _geneticReflexes;
    private readonly Action<EnvironmentRecipeEditorViewModel> _openEditor;
    private readonly List<EnvironmentRecipeListItem> _allItems = new List<EnvironmentRecipeListItem>();
    private readonly List<EnvironmentRecipeData> _allRecipes = new List<EnvironmentRecipeData>();
    private readonly AdapterEnvironmentSchema _schema;
    private string _currentAgentName;
    private int _currentAgentStage;
    private string _filterId = string.Empty;
    private string _filterTitle = string.Empty;

    public EnvironmentRecipesRegistryViewModel(
        GomeostasSystem gomeostas,
        GeneticReflexesSystem geneticReflexes,
        Action<EnvironmentRecipeEditorViewModel> openEditor)
    {
      _gomeostas = gomeostas ?? throw new ArgumentNullException(nameof(gomeostas));
      _geneticReflexes = geneticReflexes;
      _openEditor = openEditor ?? throw new ArgumentNullException(nameof(openEditor));
      _schema = AdapterSchemaLoader.LoadForCurrentProject();
      Items = new ObservableCollection<EnvironmentRecipeListItem>();
      RefreshCommand = new RelayCommand(_ => Reload());
      ApplyFiltersCommand = new RelayCommand(_ => ApplyFilters());
      ResetFiltersCommand = new RelayCommand(_ => ResetFilters());
      EditCommand = new RelayCommand(_ => EditSelected(), _ => Selected != null);
      DuplicateCommand = new RelayCommand(_ => DuplicateSelected(), _ => Selected != null);
      NewCommand = new RelayCommand(_ => CreateNew(), _ => IsEditingEnabled);
      RemoveAllCommand = new RelayCommand(RemoveAllRecipes, _ => IsEditingEnabled);
      GlobalTimer.PulsationStateChanged += OnPulsationStateChanged;
      Reload();
    }

    public ObservableCollection<EnvironmentRecipeListItem> Items { get; }
    public EnvironmentRecipeListItem Selected { get; set; }

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

    public ICommand RefreshCommand { get; }
    public ICommand ApplyFiltersCommand { get; }
    public ICommand ResetFiltersCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DuplicateCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand RemoveAllCommand { get; }

    public event Action DirtyChanged;
    public event Action<int> ValidationIssueCountChanged;

    public bool Dirty => false;
    public int ValidationIssueCount => Items.Sum(i => i.WarningCount);
    public bool CanSave => false;

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

    public void Save() { }

    public void Reload() => ReloadFromDisk();

    public void SelectAndOpen(string recipeId)
    {
      EnvironmentRecipeListItem item = _allItems.FirstOrDefault(
          i => string.Equals(i?.Id, recipeId, StringComparison.OrdinalIgnoreCase));
      if (item == null)
        return;
      Selected = item;
      EditSelected();
    }

    public bool TryDeleteSelected(IReadOnlyList<EnvironmentRecipeListItem> items)
    {
      if (items == null || items.Count == 0 || !IsEditingEnabled)
        return false;
      string msg = items.Count == 1
          ? "Удалить рецепт \"" + items[0].DisplayName + "\" (ID: " + items[0].Id + ")?"
          : "Удалить выбранные рецепты (" + items.Count + ")?";
      if (MessageBox.Show(msg, "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        return false;
      var ids = new HashSet<string>(items.Select(i => i.Id), StringComparer.Ordinal);
      _allRecipes.RemoveAll(r => ids.Contains(r.Id));
      SaveAllToDisk();
      ReloadFromDisk();
      return true;
    }

    private void ReloadFromDisk()
    {
      var agent = _gomeostas.GetAgentState();
      _currentAgentStage = agent?.EvolutionStage ?? 0;
      _currentAgentName = agent?.Name ?? string.Empty;
      _allRecipes.Clear();
      _allItems.Clear();
      var errors = new List<string>();
      List<EnvironmentRecipeData> loaded = EnvironmentCatalogStorage.LoadRecipes(errors);
      foreach (EnvironmentRecipeData recipe in loaded)
        _allRecipes.Add(recipe.Clone());
      foreach (EnvironmentRecipeData recipe in _allRecipes.OrderBy(r => r.Id, StringComparer.OrdinalIgnoreCase))
        _allItems.Add(EnvironmentRecipeMapper.ToListItem(recipe, _schema));
      if (errors.Count > 0)
      {
        MessageBox.Show(
            string.Join(Environment.NewLine, errors.Take(8)),
            "Загрузка рецептов",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
      }
      ApplyFilters();
      NotifyChildState();
    }

    private void NotifyChildState()
    {
      DirtyChanged?.Invoke();
      ValidationIssueCountChanged?.Invoke(ValidationIssueCount);
    }

    private void ApplyFilters()
    {
      IEnumerable<EnvironmentRecipeListItem> q = _allItems;
      if (!string.IsNullOrWhiteSpace(_filterId))
      {
        string f = _filterId.Trim();
        q = q.Where(i => (i.Id ?? string.Empty).IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0);
      }
      if (!string.IsNullOrWhiteSpace(_filterTitle))
      {
        string f = _filterTitle.Trim();
        q = q.Where(i => (i.DisplayName ?? string.Empty).IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0);
      }
      Items.Clear();
      foreach (EnvironmentRecipeListItem item in q)
        Items.Add(item);
    }

    private void ResetFilters()
    {
      _filterId = string.Empty;
      _filterTitle = string.Empty;
      ApplyFilters();
    }

    private void EditSelected()
    {
      if (Selected == null)
        return;
      EnvironmentRecipeData recipe = _allRecipes.FirstOrDefault(
          r => string.Equals(r.Id, Selected.Id, StringComparison.OrdinalIgnoreCase));
      if (recipe == null)
      {
        MessageBox.Show("Рецепт не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }
      OpenEditor(EnvironmentRecipeMapper.ToEditorModel(recipe), isNew: false);
    }

    private void CreateNew()
    {
      string newId = "recipe_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
      var model = new EnvironmentRecipeEditorModel { Id = newId, DisplayName = "Новый рецепт" };
      OpenEditor(model, isNew: true);
    }

    private void DuplicateSelected()
    {
      if (Selected == null || !IsEditingEnabled)
        return;
      EnvironmentRecipeData recipe = _allRecipes.FirstOrDefault(
          r => string.Equals(r.Id, Selected.Id, StringComparison.OrdinalIgnoreCase));
      if (recipe == null)
        return;
      EnvironmentRecipeEditorModel model = EnvironmentRecipeMapper.ToEditorModel(recipe);
      model.Id = model.Id + "_copy";
      model.DisplayName = (model.DisplayName ?? string.Empty) + " (копия)";
      OpenEditor(model, isNew: true);
    }

    private void OpenEditor(EnvironmentRecipeEditorModel model, bool isNew)
    {
      var editorVm = new EnvironmentRecipeEditorViewModel(
          _gomeostas,
          _geneticReflexes,
          model,
          isNew,
          SaveAllFromEditor);
      _openEditor(editorVm);
    }

    private void SaveAllFromEditor(EnvironmentRecipeEditorModel model, bool isNew)
    {
      EnvironmentRecipeData def = EnvironmentRecipeMapper.ToData(model);
      int duplicateIdx = _allRecipes.FindIndex(
          r => string.Equals(r.Id, def.Id, StringComparison.OrdinalIgnoreCase));
      if (isNew && duplicateIdx >= 0)
      {
        MessageBox.Show("Рецепт с ID \"" + def.Id + "\" уже существует.", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }
      if (duplicateIdx >= 0)
        _allRecipes[duplicateIdx] = def;
      else
        _allRecipes.Add(def);
      SaveAllToDisk();
      ReloadFromDisk();
    }

    public void RemoveAllRecipes(object parameter)
    {
      if (!IsEditingEnabled)
        return;
      if (MessageBox.Show(
              "Удалить ВСЕ рецепты среды?",
              "Подтверждение",
              MessageBoxButton.YesNo,
              MessageBoxImage.Warning) != MessageBoxResult.Yes)
        return;
      try
      {
        _allRecipes.Clear();
        EnvironmentCatalogStorage.SaveRecipes(new List<EnvironmentRecipeData>());
        ReloadFromDisk();
      }
      catch (Exception ex)
      {
        MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        ReloadFromDisk();
      }
    }

    private void SaveAllToDisk()
    {
      try
      {
        EnvironmentCatalogStorage.SaveRecipes(_allRecipes);
      }
      catch (Exception ex)
      {
        MessageBox.Show("Ошибка сохранения: " + ex.Message, "Рецепты среды", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void OnPulsationStateChanged()
    {
      Application.Current?.Dispatcher.Invoke(ApplyFilters);
    }

    public void Dispose()
    {
      GlobalTimer.PulsationStateChanged -= OnPulsationStateChanged;
    }
  }
}
