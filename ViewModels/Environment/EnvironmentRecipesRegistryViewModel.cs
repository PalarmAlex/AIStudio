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
  /// Реестр рецептов среды (список из <see cref="EnvironmentPaths.RecipesFilePath"/>).
  /// </summary>
  public sealed class EnvironmentRecipesRegistryViewModel : IDisposable
  {
    private readonly GomeostasSystem _gomeostas;
    private readonly Action<EnvironmentRecipeEditorViewModel> _openEditor;
    private readonly List<EnvironmentRecipeListItem> _allItems = new List<EnvironmentRecipeListItem>();
    private readonly List<EnvironmentRecipeData> _allRecipes = new List<EnvironmentRecipeData>();
    private string _currentAgentName;
    private int _currentAgentStage;
    private string _filterId = string.Empty;
    private string _filterTitle = string.Empty;
    /// <summary>
    /// Создаёт модель реестра.
    /// </summary>
    public EnvironmentRecipesRegistryViewModel(
        GomeostasSystem gomeostas,
        Action<EnvironmentRecipeEditorViewModel> openEditor)
    {
      _gomeostas = gomeostas ?? throw new ArgumentNullException(nameof(gomeostas));
      _openEditor = openEditor ?? throw new ArgumentNullException(nameof(openEditor));
      Items = new ObservableCollection<EnvironmentRecipeListItem>();
      RefreshCommand = new RelayCommand(_ => ReloadFromDisk());
      ApplyFiltersCommand = new RelayCommand(_ => ApplyFilters());
      ResetFiltersCommand = new RelayCommand(_ => ResetFilters());
      EditCommand = new RelayCommand(_ => EditSelected(), _ => Selected != null);
      DuplicateCommand = new RelayCommand(_ => DuplicateSelected(), _ => Selected != null);
      NewCommand = new RelayCommand(_ => CreateNew(), _ => IsEditingEnabled);
      RemoveAllCommand = new RelayCommand(RemoveAllRecipes, _ => IsEditingEnabled);
      GlobalTimer.PulsationStateChanged += OnPulsationStateChanged;
      ReloadFromDisk();
    }

    /// <summary>Элементы таблицы (после фильтра).</summary>
    public ObservableCollection<EnvironmentRecipeListItem> Items { get; }
    /// <summary>Выбранная строка.</summary>
    public EnvironmentRecipeListItem Selected { get; set; }
    /// <summary>Заголовок страницы.</summary>
    public string CurrentAgentTitle =>
        SymbiontPageTitleFormatter.Format("Рецепты среды", _currentAgentName, _currentAgentStage);
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
    /// <summary>
    /// Удаляет выбранные рецепты с подтверждением.
    /// </summary>
    public bool TryDeleteSelected(IReadOnlyList<EnvironmentRecipeListItem> items)
    {
      if (items == null || items.Count == 0 || !IsEditingEnabled)
        return false;
      string msg = items.Count == 1
          ? "Удалить рецепт \"" + items[0].DisplayName + "\" (ID: " + items[0].Id + ")?"
          : "Удалить выбранные рецепты (" + items.Count + ")?";
      if (MessageBox.Show(msg, "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        return false;
      var ids = new HashSet<string>(items.Select(i => i.Id), StringComparer.OrdinalIgnoreCase);
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
        _allItems.Add(EnvironmentRecipeMapper.ToListItem(recipe));
      if (errors.Count > 0)
      {
        MessageBox.Show(
            string.Join(Environment.NewLine, errors.Take(8)),
            "Загрузка рецептов",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
      }
      ApplyFilters();
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
      var editorVm = new EnvironmentRecipeEditorViewModel(
          _gomeostas,
          EnvironmentRecipeMapper.ToEditorModel(recipe),
          isNew: false,
          onSaveAll: SaveAllFromEditor,
          sourceRecipe: recipe);
      _openEditor(editorVm);
    }

    private void CreateNew()
    {
      string newId = "recipe_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
      var model = new EnvironmentRecipeEditorModel
      {
        Id = newId,
        DisplayName = "Новый рецепт"
      };
      var editorVm = new EnvironmentRecipeEditorViewModel(
          _gomeostas,
          model,
          isNew: true,
          onSaveAll: SaveAllFromEditor);
      _openEditor(editorVm);
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
      var editorVm = new EnvironmentRecipeEditorViewModel(
          _gomeostas,
          model,
          isNew: true,
          onSaveAll: SaveAllFromEditor,
          sourceRecipe: recipe);
      _openEditor(editorVm);
    }

    private void SaveAllFromEditor(EnvironmentRecipeEditorModel model, bool isNew)
    {
      EnvironmentRecipeData def = EnvironmentRecipeMapper.ToData(model);
      int duplicateIdx = _allRecipes.FindIndex(
          r => string.Equals(r.Id, def.Id, StringComparison.OrdinalIgnoreCase));
      if (isNew && duplicateIdx >= 0)
      {
        MessageBox.Show(
            "Рецепт с ID \"" + def.Id + "\" уже существует.",
            "Сохранение",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
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

      MessageBoxResult result = MessageBox.Show(
          "Вы действительно хотите удалить ВСЕ рецепты среды? Это действие нельзя будет отменить.",
          "Подтверждение удаления",
          MessageBoxButton.YesNo,
          MessageBoxImage.Warning);
      if (result != MessageBoxResult.Yes)
        return;

      try
      {
        _allRecipes.Clear();
        _allItems.Clear();
        Items.Clear();
        EnvironmentCatalogStorage.SaveRecipes(new List<EnvironmentRecipeData>());
        MessageBox.Show(
            "Все рецепты среды успешно удалены",
            "Удаление завершено",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        ReloadFromDisk();
      }
      catch (Exception ex)
      {
        MessageBox.Show(
            "Ошибка удаления рецептов среды: " + ex.Message,
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
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

    /// <inheritdoc />
    public void Dispose()
    {
      GlobalTimer.PulsationStateChanged -= OnPulsationStateChanged;
    }
  }
}
