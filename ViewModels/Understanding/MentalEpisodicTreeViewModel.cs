using ISIDA.Common;
using ISIDA.Psychic.Thinking;
using ISIDA.Psychic.Understanding;
using AIStudio.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace AIStudio.ViewModels.Understanding
{
  /// <summary>Элемент дерева: папка контекста или лист-правило.</summary>
  public sealed class MentalEpisodicTreeItem
  {
    public MentalEpisodicContextSnapshot Context { get; }
    public MentalEpisodicRuleSnapshot Rule { get; }
    public string TextDisplay { get; }
    public ObservableCollection<MentalEpisodicTreeItem> Children { get; } = new ObservableCollection<MentalEpisodicTreeItem>();

    /// <summary>Папка контекста с дочерними правилами.</summary>
    public MentalEpisodicTreeItem(MentalEpisodicContextSnapshot ctx, string text, IEnumerable<MentalEpisodicTreeItem> ruleItems)
    {
      Context = ctx ?? throw new ArgumentNullException(nameof(ctx));
      Rule = null;
      TextDisplay = text ?? "";
      if (ruleItems != null)
      {
        foreach (var c in ruleItems)
          Children.Add(c);
      }
    }

    /// <summary>Лист: сохранённая цепочка ИФ.</summary>
    public MentalEpisodicTreeItem(MentalEpisodicContextSnapshot parentContext, MentalEpisodicRuleSnapshot rule, string text)
    {
      Context = parentContext;
      Rule = rule ?? throw new ArgumentNullException(nameof(rule));
      TextDisplay = text ?? "";
    }

    public bool IsContextFolder => Rule == null;
  }

  /// <summary>Страница дерева ментальной эпизодики (контекст → правила с цепочками ИФ).</summary>
  public sealed class MentalEpisodicTreeViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly MentalEpisodicTreeSystem _mentalTree;
    private MentalEpisodicTreeItem _selectedNode;

    public string CurrentAgentTitle => "Ментальные цепочки";

    public ProblemTreeViewModel.DescriptionWithLink CurrentAgentDescription => new ProblemTreeViewModel.DescriptionWithLink
    {
      Text = "Дерево ментальной эпизодики (файл mental_episodic_tree.dat): контекст — узел проблемы (NodePID), образ темы и цели; под ним — сохранённые цепочки инфо-функций с эффектом после оценки решения цикла. Выберите узел слева, справа — детали. "
    };

    private ObservableCollection<MentalEpisodicTreeItem> _treeItems = new ObservableCollection<MentalEpisodicTreeItem>();
    public ObservableCollection<MentalEpisodicTreeItem> TreeItems
    {
      get => _treeItems;
      private set { _treeItems = value; OnPropertyChanged(nameof(TreeItems)); }
    }

    private ObservableCollection<PropertyRow> _selectedRows = new ObservableCollection<PropertyRow>();
    public ObservableCollection<PropertyRow> SelectedNodePropertyRows => _selectedRows;

    public MentalEpisodicTreeItem SelectedNode
    {
      get => _selectedNode;
      set
      {
        _selectedNode = value;
        OnPropertyChanged(nameof(SelectedNode));
        UpdateSelectedRows();
      }
    }

    public bool IsMentalTreeAvailable =>
      _mentalTree != null && MentalEpisodicTreeSystem.IsInitialized && AppGlobalState.EvolutionStage >= 4;

    public List<KeyValuePair<int, string>> MaxContextsOptions { get; } = new List<KeyValuePair<int, string>>
    {
      new KeyValuePair<int, string>(50, "50"),
      new KeyValuePair<int, string>(100, "100"),
      new KeyValuePair<int, string>(250, "250"),
      new KeyValuePair<int, string>(500, "500"),
      new KeyValuePair<int, string>(0, "Все")
    };

    private int _selectedMaxContexts = 100;
    public int SelectedMaxContexts
    {
      get => _selectedMaxContexts;
      set { _selectedMaxContexts = value; OnPropertyChanged(nameof(SelectedMaxContexts)); }
    }

    private string _filterNodePid = "";
    private string _filterThemeId = "";
    private string _filterPurposeId = "";
    private string _filterEffect = "";
    private string _filterChain = "";

    public string FilterNodePidInput
    {
      get => _filterNodePid;
      set { _filterNodePid = value ?? ""; OnPropertyChanged(nameof(FilterNodePidInput)); }
    }

    public string FilterThemeIdInput
    {
      get => _filterThemeId;
      set { _filterThemeId = value ?? ""; OnPropertyChanged(nameof(FilterThemeIdInput)); }
    }

    public string FilterPurposeIdInput
    {
      get => _filterPurposeId;
      set { _filterPurposeId = value ?? ""; OnPropertyChanged(nameof(FilterPurposeIdInput)); }
    }

    public string FilterEffectInput
    {
      get => _filterEffect;
      set { _filterEffect = value ?? ""; OnPropertyChanged(nameof(FilterEffectInput)); }
    }

    public string FilterChainInput
    {
      get => _filterChain;
      set { _filterChain = value ?? ""; OnPropertyChanged(nameof(FilterChainInput)); }
    }

    public ICommand ApplyFiltersCommand { get; }
    public ICommand ClearFiltersCommand { get; }

    public MentalEpisodicTreeViewModel(MentalEpisodicTreeSystem mentalTree)
    {
      _mentalTree = mentalTree;
      ApplyFiltersCommand = new RelayCommand(_ => LoadTree());
      ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
      LoadTree();
    }

    private void ClearFilters()
    {
      FilterNodePidInput = "";
      FilterThemeIdInput = "";
      FilterPurposeIdInput = "";
      FilterEffectInput = "";
      FilterChainInput = "";
      SelectedMaxContexts = 100;
      LoadTree();
    }

    private static bool IdContainsFilter(int id, string filterInput)
    {
      if (string.IsNullOrWhiteSpace(filterInput)) return true;
      return id.ToString().Contains(filterInput.Trim());
    }

    private void LoadTree()
    {
      if (!IsMentalTreeAvailable)
      {
        TreeItems = new ObservableCollection<MentalEpisodicTreeItem>();
        return;
      }

      var snap = _mentalTree.GetDisplaySnapshot();
      int limit = SelectedMaxContexts <= 0 ? int.MaxValue : SelectedMaxContexts;
      var roots = new ObservableCollection<MentalEpisodicTreeItem>();
      int added = 0;

      foreach (var ctx in snap)
      {
        if (added >= limit) break;
        if (!IdContainsFilter(ctx.NodePid, FilterNodePidInput)) continue;
        if (!IdContainsFilter(ctx.ThemeId, FilterThemeIdInput)) continue;
        if (!IdContainsFilter(ctx.PurposeId, FilterPurposeIdInput)) continue;

        var ruleItems = new List<MentalEpisodicTreeItem>();
        foreach (var rule in ctx.Rules.OrderBy(r => r.Id))
        {
          if (!string.IsNullOrWhiteSpace(FilterEffectInput))
          {
            if (!rule.Effect.ToString().Contains(FilterEffectInput.Trim())) continue;
          }
          if (!string.IsNullOrWhiteSpace(FilterChainInput))
          {
            var chainText = string.Join(",", rule.InfoFuncIds);
            if (!chainText.Contains(FilterChainInput.Trim())) continue;
          }
          ruleItems.Add(new MentalEpisodicTreeItem(ctx, rule, BuildRuleLine(rule)));
        }

        if (ruleItems.Count == 0 &&
            (!string.IsNullOrWhiteSpace(FilterEffectInput) || !string.IsNullOrWhiteSpace(FilterChainInput)))
          continue;

        string ctxText = BuildContextLine(ctx);
        roots.Add(new MentalEpisodicTreeItem(ctx, ctxText, ruleItems));
        added++;
      }

      TreeItems = roots;
    }

    private static string BuildContextLine(MentalEpisodicContextSnapshot ctx)
    {
      return $"Контекст id={ctx.Id} | узел {ctx.NodePid} | тема {ctx.ThemeId} | цель {ctx.PurposeId} | правил: {ctx.Rules.Count}";
    }

    private static string BuildRuleLine(MentalEpisodicRuleSnapshot rule)
    {
      var chain = rule.InfoFuncIds == null || rule.InfoFuncIds.Count == 0
        ? "—"
        : string.Join(",", rule.InfoFuncIds);
      return $"Правило id={rule.Id} | эффект {rule.Effect} | n={rule.Count} | ИФ: {chain}";
    }

    private void UpdateSelectedRows()
    {
      _selectedRows.Clear();
      if (_selectedNode == null)
      {
        _selectedRows.Add(new PropertyRow { Label = "", Value = "Выберите узел в дереве" });
        OnPropertyChanged(nameof(SelectedNodePropertyRows));
        return;
      }

      if (_selectedNode.IsContextFolder && _selectedNode.Context != null)
      {
        var c = _selectedNode.Context;
        _selectedRows.Add(new PropertyRow { Label = "Тип", Value = "Папка контекста" });
        _selectedRows.Add(new PropertyRow { Label = "Id узла", Value = c.Id.ToString() });
        _selectedRows.Add(new PropertyRow { Label = "NodePID (узел дерева проблем)", Value = c.NodePid.ToString() });
        _selectedRows.Add(new PropertyRow { Label = "ThemeID", Value = FormatTheme(c.ThemeId) });
        _selectedRows.Add(new PropertyRow { Label = "PurposeID", Value = FormatPurpose(c.PurposeId) });
        _selectedRows.Add(new PropertyRow { Label = "Дочерних правил", Value = c.Rules.Count.ToString() });
      }
      else if (_selectedNode.Rule != null)
      {
        var r = _selectedNode.Rule;
        var parent = _selectedNode.Context;
        _selectedRows.Add(new PropertyRow { Label = "Тип", Value = "Сохранённая цепочка (правило)" });
        _selectedRows.Add(new PropertyRow { Label = "Id правила", Value = r.Id.ToString() });
        _selectedRows.Add(new PropertyRow { Label = "Id папки контекста", Value = r.ParentContextId.ToString() });
        if (parent != null)
        {
          _selectedRows.Add(new PropertyRow { Label = "Контекст: NodePID", Value = parent.NodePid.ToString() });
          _selectedRows.Add(new PropertyRow { Label = "Контекст: ThemeID", Value = FormatTheme(parent.ThemeId) });
          _selectedRows.Add(new PropertyRow { Label = "Контекст: PurposeID", Value = FormatPurpose(parent.PurposeId) });
        }
        _selectedRows.Add(new PropertyRow { Label = "Effect", Value = r.Effect.ToString() });
        _selectedRows.Add(new PropertyRow { Label = "Count (усреднений)", Value = r.Count.ToString() });
        _selectedRows.Add(new PropertyRow { Label = "Цепочка ИФ (id)", Value = r.InfoFuncIds.Count == 0 ? "—" : string.Join(", ", r.InfoFuncIds) });
        _selectedRows.Add(new PropertyRow { Label = "Цепочка (названия)", Value = FormatChainNames(r.InfoFuncIds) });
      }
      else
        _selectedRows.Add(new PropertyRow { Label = "", Value = "—" });

      OnPropertyChanged(nameof(SelectedNodePropertyRows));
    }

    private static string FormatTheme(int themeId)
    {
      if (themeId <= 0) return "0 (не задано)";
      if (!ThemeImageSystem.IsInitialized) return themeId.ToString();
      try
      {
        var rec = ThemeImageSystem.Instance.GetById(themeId);
        if (rec == null) return themeId.ToString();
        string typeDesc = ThemeImageSystem.Instance.GetThemeTypeDescription(rec.Type);
        string prefix = string.IsNullOrEmpty(typeDesc) ? "" : typeDesc + ", ";
        return $"{prefix}тип {rec.Type}, вес {rec.Weight} (id {themeId})";
      }
      catch { return themeId.ToString(); }
    }

    private static string FormatPurpose(int purposeId)
    {
      if (purposeId <= 0) return "0 (не задано)";
      if (!PurposeImageSystem.IsInitialized) return purposeId.ToString();
      try
      {
        var rec = PurposeImageSystem.Instance.GetById(purposeId);
        if (rec == null) return purposeId.ToString();
        var targetText = rec.Target == 1 ? "повторение" : rec.Target == 2 ? "улучшение" : rec.Target.ToString();
        return $"{targetText} (id {purposeId})";
      }
      catch { return purposeId.ToString(); }
    }

    private static string FormatChainNames(IReadOnlyList<int> ids)
    {
      if (ids == null || ids.Count == 0) return "—";
      var parts = new List<string>();
      foreach (var id in ids)
      {
        var e = InfoFunctionsCatalog.GetById(id);
        string name = e?.Name ?? "?";
        parts.Add($"{id}:{name}");
      }
      return string.Join(" → ", parts);
    }
  }
}
