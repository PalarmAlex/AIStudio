using AIStudio.Common;
using ISIDA.Common;
using ISIDA.Psychic.Automatism;
using ISIDA.Psychic.Understanding;
using System.Windows.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace AIStudio.ViewModels.Understanding
{
  /// <summary>
  /// Элемент дерева для отображения (обёртка над ProblemTreeNode)
  /// </summary>
  public class ProblemTreeNodeItem
  {
    public ProblemTreeNode Node { get; }
    public string TextDisplay { get; }
    public ObservableCollection<ProblemTreeNodeItem> Children { get; } = new ObservableCollection<ProblemTreeNodeItem>();

    public ProblemTreeNodeItem(ProblemTreeNode node, string textDisplay)
    {
      Node = node;
      TextDisplay = textDisplay ?? $"ID:{node?.ID ?? 0}";
    }
  }

  /// <summary>Строка панели свойств: подпись (жирным) и значение. ValueLines — для блока расшифровки (названия курсивом, «Образ действия» ещё жирным, значения синим).</summary>
  public class PropertyRow
  {
    public string Label { get; set; }
    public string Value { get; set; }
    /// <summary>Когда задан — показываются по строкам (для «Узел дерева автоматизмов»).</summary>
    public List<ExpansionLine> ValueLines { get; set; }
  }

  /// <summary>Строка блока расшифровки: название свойства (курсив, при необходимости жирный) и значение (синий). IsInActionBlock — дополнительный отступ слева для подпунктов блока «Образ действия».</summary>
  public class ExpansionLine
  {
    public string Name { get; set; }
    public string Value { get; set; }
    public bool IsBold { get; set; }
    public bool IsInActionBlock { get; set; }
  }

  /// <summary>
  /// ViewModel страницы дерева проблем
  /// </summary>
  public class ProblemTreeViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly ProblemTreeSystem _problemTree;
    private readonly AutomatizmTreeSystem _automatizmTree;
    private readonly SituationTypeSystem _situationTypeSystem;
    private readonly SituationImageSystem _situationImageSystem;
    /// <summary>Делегат для получения полной расшифровки узла дерева автоматизмов (условия + образ действия).</summary>
    private readonly Func<int, string> _getAutNodeDetails;
    private ProblemTreeNodeItem _selectedNode;

    public string CurrentAgentTitle => "Дерево проблем";

    public string CurrentAgentDescription =>
      "Дерево проблем — 4 уровня: AutTreeID, SituationTreeID, ThemeID, PurposeID. При клике на узел в правой панели отображаются свойства узла.";

    #region Дерево

    private ObservableCollection<ProblemTreeNodeItem> _treeItems = new ObservableCollection<ProblemTreeNodeItem>();
    public ObservableCollection<ProblemTreeNodeItem> TreeItems
    {
      get => _treeItems;
      set { _treeItems = value; OnPropertyChanged(nameof(TreeItems)); }
    }

    private ObservableCollection<PropertyRow> _selectedNodePropertyRows = new ObservableCollection<PropertyRow>();
    /// <summary>Строки панели свойств (подпись жирным + значение) для биндинга</summary>
    public ObservableCollection<PropertyRow> SelectedNodePropertyRows => _selectedNodePropertyRows;

    /// <summary>Выбранный узел дерева — для панели свойств</summary>
    public ProblemTreeNodeItem SelectedNode
    {
      get => _selectedNode;
      set
      {
        _selectedNode = value;
        OnPropertyChanged(nameof(SelectedNode));
        UpdateSelectedNodePropertyRows();
      }
    }

    private void UpdateSelectedNodePropertyRows()
    {
      _selectedNodePropertyRows.Clear();
      if (_selectedNode?.Node == null)
      {
        _selectedNodePropertyRows.Add(new PropertyRow { Label = "", Value = "Выберите узел в дереве" });
        OnPropertyChanged(nameof(SelectedNodePropertyRows));
        return;
      }
      var n = _selectedNode.Node;
      _selectedNodePropertyRows.Add(new PropertyRow { Label = "ID узла", Value = n.ID.ToString() });
      _selectedNodePropertyRows.Add(new PropertyRow { Label = "ID родителя", Value = n.ParentID.ToString() });

      if (n.AutTreeID > 0 && _getAutNodeDetails != null)
      {
        var details = _getAutNodeDetails(n.AutTreeID);
        if (string.IsNullOrEmpty(details))
          _selectedNodePropertyRows.Add(new PropertyRow { Label = "Узел дерева автоматизмов", Value = "—" });
        else
        {
          var rawLines = details.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
          var lines = new List<ExpansionLine>();
          bool inActionBlock = false;
          foreach (var line in rawLines)
          {
            var colon = line.IndexOf(": ", StringComparison.Ordinal);
            string name;
            string value;
            if (colon >= 0)
            {
              name = line.Substring(0, colon);
              value = line.Substring(colon + 2);
            }
            else
            {
              // Строка без ": " — если заканчивается на ":", убираем двоеточие из названия (чтобы в UI не было "Образ действия: :")
              name = line.TrimEnd().EndsWith(":") ? line.TrimEnd().TrimEnd(':').TrimEnd() : line;
              value = "";
            }

            // Сохраняем исходные пробелы в начале name
            bool isBold = name.TrimStart() == "Образ действия"; // Сравниваем без пробелов
            bool isInActionBlock = inActionBlock && !isBold;

            lines.Add(new ExpansionLine
            {
              Name = name, // Сохраняем исходную строку с пробелами
              Value = value.Trim(),
              IsBold = isBold,
              IsInActionBlock = isInActionBlock
            });

            if (isBold) inActionBlock = true;
          }
          _selectedNodePropertyRows.Add(new PropertyRow { Label = "Узел дерева автоматизмов", ValueLines = lines });
        }
      }
      else
        _selectedNodePropertyRows.Add(new PropertyRow { Label = "Узел дерева автоматизмов", Value = GetAutTreeIdDisplay(n.AutTreeID) });

      _selectedNodePropertyRows.Add(new PropertyRow { Label = "Образ ситуации", Value = GetSituationTreeIdDisplay(n.SituationTreeID) });
      _selectedNodePropertyRows.Add(new PropertyRow { Label = "Образ темы", Value = GetThemeIdDisplay(n.ThemeID) });
      _selectedNodePropertyRows.Add(new PropertyRow { Label = "Образ цели", Value = GetPurposeIdDisplay(n.PurposeID) });
      _selectedNodePropertyRows.Add(new PropertyRow { Label = "Дочерних узлов", Value = (n.Children?.Count ?? 0).ToString() });
      OnPropertyChanged(nameof(SelectedNodePropertyRows));
    }

    private string GetAutTreeIdDisplay(int autTreeId)
    {
      if (autTreeId <= 0) return "—";
      var node = _automatizmTree?.GetNodeById(autTreeId);
      if (node == null) return autTreeId.ToString();
      var baseText = GetBaseIdText(node.BaseID);
      if (node.EmotionID != 0)
        return $"{baseText}, эмоция {node.EmotionID}";
      return $"{baseText} (ID: {autTreeId})";
    }

    private static string GetBaseIdText(int baseId)
    {
      switch (baseId)
      {
        case -1: return "Плохо";
        case 0: return "Норма";
        case 1: return "Хорошо";
        default: return $"BaseID:{baseId}";
      }
    }

    private string GetSituationTreeIdDisplay(int situationTreeId)
    {
      if (situationTreeId <= 0) return "—";
      var rec = _situationImageSystem?.GetById(situationTreeId);
      if (rec == null) return situationTreeId.ToString();
      var typeRec = _situationTypeSystem?.GetById(rec.SituationTypeId);
      string namePart = (typeRec != null && !string.IsNullOrWhiteSpace(typeRec.Description))
        ? typeRec.Description
        : rec.SituationTypeId.ToString();
      return $"{namePart} (ID: {situationTreeId})";
    }

    private string GetThemeIdDisplay(int themeId)
    {
      if (themeId <= 0) return "—";
      if (!ThemeImageSystem.IsInitialized) return themeId.ToString();
      try
      {
        var rec = ThemeImageSystem.Instance.GetById(themeId);
        if (rec == null) return themeId.ToString();
        string typeDesc = ThemeImageSystem.Instance.GetThemeTypeDescription(rec.Type);
        string prefix = string.IsNullOrEmpty(typeDesc) ? "" : typeDesc + ", ";
        return $"{prefix}тип {rec.Type}, вес {rec.Weight} (ID: {themeId})";
      }
      catch { return themeId.ToString(); }
    }

    private string GetPurposeIdDisplay(int purposeId)
    {
      if (purposeId <= 0) return "—";
      if (!PurposeImageSystem.IsInitialized) return purposeId.ToString();
      try
      {
        var rec = PurposeImageSystem.Instance.GetById(purposeId);
        if (rec == null) return purposeId.ToString();
        var targetText = rec.Target == 1 ? "повторение" : rec.Target == 2 ? "улучшение" : rec.Target.ToString();
        return $"{targetText} (ID: {purposeId})";
      }
      catch { return purposeId.ToString(); }
    }

    #endregion

    #region Панель фильтров

    public List<KeyValuePair<int, string>> MaxNodesOptions { get; } = new List<KeyValuePair<int, string>>
    {
      new KeyValuePair<int, string>(100, "100"),
      new KeyValuePair<int, string>(250, "250"),
      new KeyValuePair<int, string>(500, "500"),
      new KeyValuePair<int, string>(1000, "1000"),
      new KeyValuePair<int, string>(2500, "2500"),
      new KeyValuePair<int, string>(5000, "5000"),
      new KeyValuePair<int, string>(0, "Все")
    };

    private int _selectedMaxNodes = 500;
    private string _filterAutTreeIdInput = "";
    private string _filterSituationTreeIdInput = "";
    private string _filterThemeIdInput = "";
    private string _filterPurposeIdInput = "";

    public int SelectedMaxNodes
    {
      get => _selectedMaxNodes;
      set { _selectedMaxNodes = value; OnPropertyChanged(nameof(SelectedMaxNodes)); }
    }

    public string FilterAutTreeIdInput
    {
      get => _filterAutTreeIdInput;
      set { _filterAutTreeIdInput = value ?? ""; OnPropertyChanged(nameof(FilterAutTreeIdInput)); }
    }

    public string FilterSituationTreeIdInput
    {
      get => _filterSituationTreeIdInput;
      set { _filterSituationTreeIdInput = value ?? ""; OnPropertyChanged(nameof(FilterSituationTreeIdInput)); }
    }

    public string FilterThemeIdInput
    {
      get => _filterThemeIdInput;
      set { _filterThemeIdInput = value ?? ""; OnPropertyChanged(nameof(FilterThemeIdInput)); }
    }

    public string FilterPurposeIdInput
    {
      get => _filterPurposeIdInput;
      set { _filterPurposeIdInput = value ?? ""; OnPropertyChanged(nameof(FilterPurposeIdInput)); }
    }

    public ICommand ApplyFiltersCommand { get; }
    public ICommand ClearFiltersCommand { get; }

    #endregion

    public bool IsProblemTreeAvailable =>
      _problemTree != null && ProblemTreeSystem.IsInitialized && AppGlobalState.EvolutionStage >= 4;

    public ProblemTreeViewModel(
        ProblemTreeSystem problemTree,
        AutomatizmTreeSystem automatizmTree = null,
        SituationTypeSystem situationTypeSystem = null,
        SituationImageSystem situationImageSystem = null,
        Func<int, string> getAutNodeDetails = null)
    {
      _problemTree = problemTree;
      _automatizmTree = automatizmTree;
      _situationTypeSystem = situationTypeSystem;
      _situationImageSystem = situationImageSystem;
      _getAutNodeDetails = getAutNodeDetails;
      ApplyFiltersCommand = new RelayCommand(ApplyFilters);
      ClearFiltersCommand = new RelayCommand(ClearFilters);
      LoadTree();
    }

    private void ApplyFilters(object parameter = null)
    {
      LoadTree();
    }

    private void ClearFilters(object parameter = null)
    {
      FilterAutTreeIdInput = "";
      FilterSituationTreeIdInput = "";
      FilterThemeIdInput = "";
      FilterPurposeIdInput = "";
      SelectedMaxNodes = 500;
      LoadTree();
    }

    /// <summary>Проверяет, что строка фильтра входит в строковое представление ID (пустой фильтр — не ограничивает).</summary>
    private static bool IdContainsFilter(int id, string filterInput)
    {
      if (string.IsNullOrWhiteSpace(filterInput)) return true;
      return id.ToString().Contains(filterInput.Trim());
    }

    private void LoadTree()
    {
      if (!IsProblemTreeAvailable)
      {
        TreeItems = new ObservableCollection<ProblemTreeNodeItem>();
        return;
      }

      var root = _problemTree.Tree;
      int limit = SelectedMaxNodes <= 0 ? int.MaxValue : SelectedMaxNodes;
      var result = new ObservableCollection<ProblemTreeNodeItem>();

      const int startDepth = 1;
      foreach (var child in root.Children ?? Enumerable.Empty<ProblemTreeNode>())
      {
        if (limit <= 0) break;
        var item = BuildNodeItem(child, ref limit, startDepth);
        if (item != null)
          result.Add(item);
      }

      TreeItems = result;
    }

    private ProblemTreeNodeItem BuildNodeItem(ProblemTreeNode node, ref int remainingLimit, int depth)
    {
      if (node == null || remainingLimit <= 0) return null;

      if (!PassesFilters(node))
      {
        bool anyChildPasses = false;
        int limitCopy = remainingLimit;
        foreach (var c in node.Children ?? Enumerable.Empty<ProblemTreeNode>())
        {
          var childItem = BuildNodeItem(c, ref limitCopy, depth + 1);
          if (childItem != null) { anyChildPasses = true; break; }
        }
        if (!anyChildPasses)
          return null;
      }

      string text = BuildNodeDisplayText(node, depth);
      var item = new ProblemTreeNodeItem(node, text);
      remainingLimit--;

      foreach (var c in node.Children ?? Enumerable.Empty<ProblemTreeNode>())
      {
        if (remainingLimit <= 0) break;
        var childItem = BuildNodeItem(c, ref remainingLimit, depth + 1);
        if (childItem != null)
          item.Children.Add(childItem);
      }

      return item;
    }

    /// <summary>Текст строки дерева: только ID и одно свойство по уровню. Уровень по данным узла: PurposeID→4, ThemeID→3, SituationTreeID→2, иначе Автоматизм.</summary>
    private static string BuildNodeDisplayText(ProblemTreeNode node, int depth)
    {
      if (node == null) return "—";
      if (node.ID == 0) return "Корень";
      int level = node.PurposeID != 0 ? 4 : node.ThemeID != 0 ? 3 : node.SituationTreeID != 0 ? 2 : 1;
      string levelPart = level == 1 ? $"Образ условий автоматизма: {node.AutTreeID}"
          : level == 2 ? $"Образ ситуации: {node.SituationTreeID}"
          : level == 3 ? $"Образ темы: {node.ThemeID}"
          : $"Образ цели: {node.PurposeID}";
      return $"ID: {node.ID} | {levelPart}";
    }

    private bool PassesFilters(ProblemTreeNode node)
    {
      if (!IdContainsFilter(node.AutTreeID, FilterAutTreeIdInput)) return false;
      if (!IdContainsFilter(node.SituationTreeID, FilterSituationTreeIdInput)) return false;
      if (!IdContainsFilter(node.ThemeID, FilterThemeIdInput)) return false;
      if (!IdContainsFilter(node.PurposeID, FilterPurposeIdInput)) return false;
      return true;
    }

    /// <summary>Обновить панель свойств (вызывать после смены выбранного узла)</summary>
    public void RefreshSelectedProperties()
    {
      UpdateSelectedNodePropertyRows();
    }
  }
}
