using AIStudio.Common;
using AIStudio.Common.Adapters;
using AIStudio.Common.SymbiontEnv;
using ISIDA.Common;
using ISIDA.Gomeostas;
using ISIDA.SymbiontEnv.Contract;
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
  /// <summary>Реестр рецептов среды.</summary>
  public sealed class EnvironmentRecipesRegistryViewModel : INotifyPropertyChanged
  {
    private readonly GomeostasSystem _gomeostas;
    private readonly Action<EnvironmentRecipeEditorViewModel> _openEditor;
    private readonly List<EnvironmentRecipeListItem> _allItems = new List<EnvironmentRecipeListItem>();
    private readonly List<EnvironmentRecipeData> _allRecipes = new List<EnvironmentRecipeData>();
    private readonly AdapterEnvironmentSchema _schema;
    private int _currentAgentStage;
    private string _currentAgentName;
    private string _filterId = string.Empty;
    private string _filterTitle = string.Empty;

    public EnvironmentRecipesRegistryViewModel(
        GomeostasSystem gomeostas,
        Action<EnvironmentRecipeEditorViewModel> openEditor)
    {
      _gomeostas = gomeostas ?? throw new ArgumentNullException(nameof(gomeostas));
      _openEditor = openEditor ?? throw new ArgumentNullException(nameof(openEditor));
      _schema = AdapterSchemaLoader.LoadForCurrentProject();
      Items = new ObservableCollection<EnvironmentRecipeListItem>();
      RefreshCommand = new RelayCommand(_ => Reload());
      ApplyFiltersCommand = new RelayCommand(_ => ApplyFilters());
      ResetFiltersCommand = new RelayCommand(_ => ResetFilters());
      EditCommand = new RelayCommand(_ => EditSelected(), _ => Selected != null);
      DuplicateCommand = new RelayCommand(_ => DuplicateSelected(), _ => Selected != null && IsEditingEnabled);
      NewCommand = new RelayCommand(_ => CreateNew(), _ => IsEditingEnabled);
      GlobalTimer.PulsationStateChanged += OnPulsationStateChanged;
      Reload();
    }

    public string CurrentAgentTitle =>
        SymbiontPageTitleFormatter.Format("Рецепты поведения", _currentAgentName, _currentAgentStage);

    public DescriptionWithLink CurrentAgentDescription { get; } = new DescriptionWithLink();

    public ObservableCollection<EnvironmentRecipeListItem> Items { get; }
    public EnvironmentRecipeListItem Selected { get; set; }

    public string FilterIdText
    {
      get => _filterId;
      set
      {
        _filterId = value ?? string.Empty;
        OnPropertyChanged();
      }
    }

    public string FilterTitleText
    {
      get => _filterTitle;
      set
      {
        _filterTitle = value ?? string.Empty;
        OnPropertyChanged();
      }
    }

    public ICommand RefreshCommand { get; }
    public ICommand ApplyFiltersCommand { get; }
    public ICommand ResetFiltersCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DuplicateCommand { get; }
    public ICommand NewCommand { get; }

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
      OnPropertyChanged(nameof(CurrentAgentTitle));
      OnPropertyChanged(nameof(IsStageZero));
      OnPropertyChanged(nameof(IsEditingEnabled));
      OnPropertyChanged(nameof(PulseWarningMessage));
      OnPropertyChanged(nameof(WarningMessageColor));

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
      OnPropertyChanged(nameof(FilterIdText));
      OnPropertyChanged(nameof(FilterTitleText));
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
        model,
        isNew,
        SaveAllFromEditor);
      _openEditor(editorVm);
    }

    private bool SaveAllFromEditor(EnvironmentRecipeEditorModel model, bool isNew)
    {
      EnvironmentRecipeData def = EnvironmentRecipeMapper.ToData(model);
      int duplicateIdx = _allRecipes.FindIndex(
          r => string.Equals(r.Id, def.Id, StringComparison.OrdinalIgnoreCase));
      if (isNew && duplicateIdx >= 0)
      {
        MessageBox.Show("Рецепт с ID \"" + def.Id + "\" уже существует.", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
      }
      if (duplicateIdx >= 0)
        _allRecipes[duplicateIdx] = def;
      else
        _allRecipes.Add(def);
      if (!SaveAllToDisk())
        return false;
      ReloadFromDisk();
      return true;
    }

    private bool SaveAllToDisk()
    {
      try
      {
        EnvironmentCatalogStorage.SaveRecipes(_allRecipes);
        return true;
      }
      catch (Exception ex)
      {
        MessageBox.Show("Ошибка сохранения: " + ex.Message, "Рецепты среды", MessageBoxButton.OK, MessageBoxImage.Error);
        return false;
      }
    }

    private void OnPulsationStateChanged()
    {
      Application.Current?.Dispatcher.Invoke(() =>
      {
        OnPropertyChanged(nameof(IsEditingEnabled));
        OnPropertyChanged(nameof(PulseWarningMessage));
        OnPropertyChanged(nameof(WarningMessageColor));
        ApplyFilters();
      });
    }

    public void Dispose()
    {
      GlobalTimer.PulsationStateChanged -= OnPulsationStateChanged;
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
      public string Text { get; set; } = "Обзор рецептов поведения симбионта.";
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