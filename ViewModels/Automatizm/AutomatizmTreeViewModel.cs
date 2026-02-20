using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Gomeostas;
using ISIDA.Psychic;
using ISIDA.Psychic.Automatism;
using ISIDA.Sensors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Input;

namespace AIStudio.ViewModels
{
  /// <summary>
  /// Элемент строки автоматизма для отображения в списке у конечного узла.
  /// </summary>
  /// <summary>
  /// Элемент блока автоматизма: многострочный текст (ID, Действие, Фраза, Уверенность, Кол-во использований).
  /// </summary>
  public class AutomatizmLineItem : INotifyPropertyChanged
  {
    private string _displayText;

    public AutomatizmLineItem() { }

    public AutomatizmLineItem(string displayText, bool isFirstInList)
    {
      _displayText = displayText;
      IsFirstInList = isFirstInList;
    }

    public string DisplayText
    {
      get => _displayText;
      set { _displayText = value; OnPropertyChanged(nameof(DisplayText)); }
    }

    /// <summary>Выделить первую строку жирным, когда автоматизмов больше одного.</summary>
    public bool IsFirstInList { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }

  /// <summary>
  /// Элемент дерева: узел (Плохо/Норма/Хорошо или дочерний) с опциональным списком автоматизмов у листа.
  /// </summary>
  public class AutomatizmTreeNodeItem : INotifyPropertyChanged
  {
    public int NodeId { get; set; }
    public string Name { get; set; }
    public int BaseID { get; set; }
    public int EmotionID { get; set; }
    public int ActivityID { get; set; }
    public int ToneMoodID { get; set; }
    public int SimbolID { get; set; }
    public int VerbID { get; set; }
    public HashSet<int> StyleIdsInPath { get; set; } = new HashSet<int>();

    private readonly ObservableCollection<AutomatizmTreeNodeItem> _children = new ObservableCollection<AutomatizmTreeNodeItem>();
    public ObservableCollection<AutomatizmTreeNodeItem> Children => _children;

    private readonly ObservableCollection<AutomatizmLineItem> _automatizmLines = new ObservableCollection<AutomatizmLineItem>();
    public ObservableCollection<AutomatizmLineItem> AutomatizmLines => _automatizmLines;

    public bool IsLeaf => _children.Count == 0;
    public bool HasAutomatizms => _automatizmLines.Count > 0;

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }

  public class AutomatizmTreeViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private readonly AutomatizmTreeSystem _automatizmTreeSystem;
    private readonly AutomatizmSystem _automatizmSystem;
    private readonly ActionsImagesSystem _actionsImagesSystem;
    private readonly SensorySystem _sensorySystem;
    private readonly AdaptiveActionsSystem _adaptiveActionsSystem;
    private readonly GomeostasSystem _gomeostas;
    private readonly EmotionsImageSystem _emotionsImageSystem;
    private readonly InfluenceActionsImagesSystem _influenceActionsImagesSystem;
    private readonly InfluenceActionSystem _influenceActionSystem;
    private readonly VerbalBrocaImagesSystem _verbalBrocaImages;

    private readonly ObservableCollection<AutomatizmTreeNodeItem> _rootNodes = new ObservableCollection<AutomatizmTreeNodeItem>();
    public ObservableCollection<AutomatizmTreeNodeItem> RootNodes => _rootNodes;

    private int? _selectedStyleFilter;
    private int _selectedPerceptionActionFilterId;
    private int _selectedActionFilterId;
    private string _filterPhrasePerception = string.Empty;
    private string _filterPhrase = string.Empty;

    public string PageTitle => "Дерево автоматизмов";

    public List<KeyValuePair<int?, string>> StyleFilterOptions { get; } = new List<KeyValuePair<int?, string>>();
    public List<KeyValuePair<int, string>> PerceptionActionFilterOptions { get; } = new List<KeyValuePair<int, string>>();
    public List<KeyValuePair<int, string>> ActionFilterOptions { get; } = new List<KeyValuePair<int, string>>();

    public int? SelectedStyleFilter
    {
      get => _selectedStyleFilter;
      set
      {
        _selectedStyleFilter = value;
        OnPropertyChanged(nameof(SelectedStyleFilter));
        RebuildTreeWithFilters();
      }
    }

    public int SelectedPerceptionActionFilterId
    {
      get => _selectedPerceptionActionFilterId;
      set
      {
        _selectedPerceptionActionFilterId = value;
        OnPropertyChanged(nameof(SelectedPerceptionActionFilterId));
        RebuildTreeWithFilters();
      }
    }

    public int SelectedActionFilterId
    {
      get => _selectedActionFilterId;
      set
      {
        _selectedActionFilterId = value;
        OnPropertyChanged(nameof(SelectedActionFilterId));
        RebuildTreeWithFilters();
      }
    }

    public string FilterPhrasePerception
    {
      get => _filterPhrasePerception;
      set
      {
        _filterPhrasePerception = value ?? string.Empty;
        OnPropertyChanged(nameof(FilterPhrasePerception));
        RebuildTreeWithFilters();
      }
    }

    public string FilterPhrase
    {
      get => _filterPhrase;
      set
      {
        _filterPhrase = value ?? string.Empty;
        OnPropertyChanged(nameof(FilterPhrase));
        RebuildTreeWithFilters();
      }
    }

    public ICommand ClearFiltersCommand { get; }

    public AutomatizmTreeViewModel(
        AutomatizmTreeSystem automatizmTreeSystem,
        AutomatizmSystem automatizmSystem,
        ActionsImagesSystem actionsImagesSystem,
        SensorySystem sensorySystem,
        AdaptiveActionsSystem adaptiveActionsSystem,
        GomeostasSystem gomeostas,
        EmotionsImageSystem emotionsImageSystem,
        InfluenceActionsImagesSystem influenceActionsImagesSystem,
        InfluenceActionSystem influenceActionSystem,
        VerbalBrocaImagesSystem verbalBrocaImages)
    {
      _automatizmTreeSystem = automatizmTreeSystem ?? throw new ArgumentNullException(nameof(automatizmTreeSystem));
      _automatizmSystem = automatizmSystem ?? throw new ArgumentNullException(nameof(automatizmSystem));
      _actionsImagesSystem = actionsImagesSystem ?? throw new ArgumentNullException(nameof(actionsImagesSystem));
      _sensorySystem = sensorySystem ?? throw new ArgumentNullException(nameof(sensorySystem));
      _adaptiveActionsSystem = adaptiveActionsSystem ?? throw new ArgumentNullException(nameof(adaptiveActionsSystem));
      _gomeostas = gomeostas ?? throw new ArgumentNullException(nameof(gomeostas));
      _emotionsImageSystem = emotionsImageSystem ?? throw new ArgumentNullException(nameof(emotionsImageSystem));
      _influenceActionsImagesSystem = influenceActionsImagesSystem ?? throw new ArgumentNullException(nameof(influenceActionsImagesSystem));
      _influenceActionSystem = influenceActionSystem ?? throw new ArgumentNullException(nameof(influenceActionSystem));
      _verbalBrocaImages = verbalBrocaImages ?? throw new ArgumentNullException(nameof(verbalBrocaImages));

      StyleFilterOptions.Add(new KeyValuePair<int?, string>(null, "Все эмоции"));
      var styles = _gomeostas?.GetAllBehaviorStyles()?.Values?.ToList() ?? new List<GomeostasSystem.BehaviorStyle>();
      foreach (var s in styles)
        StyleFilterOptions.Add(new KeyValuePair<int?, string>(s.Id, s.Name));

      PerceptionActionFilterOptions.Add(new KeyValuePair<int, string>(0, "Все действия"));
      var influenceActions = _influenceActionSystem?.GetAllInfluenceActions();
      if (influenceActions != null)
        foreach (var a in influenceActions)
          PerceptionActionFilterOptions.Add(new KeyValuePair<int, string>(a.Id, a.Name));

      ActionFilterOptions.Add(new KeyValuePair<int, string>(0, "Все действия"));
      var actionsCollection = _adaptiveActionsSystem?.GetAllAdaptiveActions();
      var actionsList = actionsCollection != null ? actionsCollection.ToList() : new List<AdaptiveActionsSystem.AdaptiveAction>();
      foreach (var a in actionsList)
        ActionFilterOptions.Add(new KeyValuePair<int, string>(a.Id, a.Name));

      ClearFiltersCommand = new RelayCommand(ClearFilters);
      LoadTree();
    }

    private void ClearFilters(object _ = null)
    {
      SelectedStyleFilter = null;
      SelectedPerceptionActionFilterId = 0;
      SelectedActionFilterId = 0;
      FilterPhrasePerception = string.Empty;
      FilterPhrase = string.Empty;
    }

    private void LoadTree()
    {
      _rootNodes.Clear();
      var tree = _automatizmTreeSystem?.Tree;
      if (tree?.Children == null) return;

      foreach (var child in tree.Children.OrderBy(c => c.BaseID))
      {
        var name = GetBaseConditionName(child.BaseID);
        var item = new AutomatizmTreeNodeItem
        {
          NodeId = child.ID,
          Name = name,
          BaseID = child.BaseID,
          EmotionID = child.EmotionID,
          ActivityID = child.ActivityID,
          ToneMoodID = child.ToneMoodID,
          SimbolID = child.SimbolID,
          VerbID = child.VerbID,
          StyleIdsInPath = CollectStyleIdsInPath(child)
        };
        FillChildren(item, child);
        // Показываем только столбцы, у которых есть ветки (не пустые)
        if (item.Children.Count > 0 || item.AutomatizmLines.Count > 0)
          _rootNodes.Add(item);
      }
    }

    private string GetBaseConditionName(int baseId)
    {
      switch (baseId)
      {
        case -1: return "Плохо";
        case 0: return "Норма";
        case 1: return "Хорошо";
        default: return $"Состояние {baseId}";
      }
    }

    private HashSet<int> CollectStyleIdsInPath(AutomatizmNode node)
    {
      var set = new HashSet<int>();
      if (node.EmotionID > 0 && _emotionsImageSystem != null)
      {
        try
        {
          var img = _emotionsImageSystem.GetEmotionsImage(node.EmotionID);
          if (img?.BaseStylesList != null)
            foreach (var id in img.BaseStylesList) set.Add(id);
        }
        catch { }
      }
      var current = node.ParentNode;
      while (current != null && current.ID != 0)
      {
        if (current.EmotionID > 0)
        {
          try
          {
            var img = _emotionsImageSystem.GetEmotionsImage(current.EmotionID);
            if (img?.BaseStylesList != null)
              foreach (var id in img.BaseStylesList) set.Add(id);
          }
          catch { }
        }
        current = current.ParentNode;
      }
      return set;
    }

    private (int activityId, int verbId) GetPathActivityAndVerbId(AutomatizmNode node)
    {
      int activityId = 0, verbId = 0;
      var current = node;
      while (current != null)
      {
        if (activityId == 0 && current.ActivityID > 0) activityId = current.ActivityID;
        if (verbId == 0 && current.VerbID > 0) verbId = current.VerbID;
        current = current.ParentNode;
      }
      return (activityId, verbId);
    }

    private void FillChildren(AutomatizmTreeNodeItem item, AutomatizmNode node)
    {
      if (node.Children == null || node.Children.Count == 0)
      {
        var (pathActivityId, pathVerbId) = GetPathActivityAndVerbId(node);
        FillAutomatizmLines(item, node.ID, pathActivityId, pathVerbId);
        return;
      }
      foreach (var ch in node.Children)
      {
        var name = GetNodeDisplayDescription(ch, node);
        var childItem = new AutomatizmTreeNodeItem
        {
          NodeId = ch.ID,
          Name = name,
          BaseID = ch.BaseID,
          EmotionID = ch.EmotionID,
          ActivityID = ch.ActivityID,
          ToneMoodID = ch.ToneMoodID,
          SimbolID = ch.SimbolID,
          VerbID = ch.VerbID,
          StyleIdsInPath = CollectStyleIdsInPath(ch)
        };
        FillChildren(childItem, ch);
        // Добавляем только ветки, в которых есть хотя бы один автоматизм
        if (NodeHasAnyAutomatizms(childItem))
          item.Children.Add(childItem);
      }
    }

    /// <summary>Есть ли в узле или в любом потомке хотя бы один автоматизм.</summary>
    private static bool NodeHasAnyAutomatizms(AutomatizmTreeNodeItem node)
    {
      if (node == null) return false;
      if (node.AutomatizmLines != null && node.AutomatizmLines.Count > 0) return true;
      if (node.Children == null) return false;
      return node.Children.Any(NodeHasAnyAutomatizms);
    }

    /// <summary>Текстовое описание узла по его полям (эмоции, триггер, тон/настроение, символ, верб. образ).</summary>
    private string GetNodeDisplayDescription(AutomatizmNode node, AutomatizmNode parent)
    {
      if (node.ParentID == 0)
        return GetBaseConditionName(node.BaseID);

      if (node.EmotionID > 0 && (parent == null || parent.EmotionID != node.EmotionID))
      {
        var names = GetEmotionStyleNames(node.EmotionID);
        return string.IsNullOrEmpty(names) ? $"Эмоции: ID {node.EmotionID}" : "Эмоции: " + names;
      }
      if (node.ActivityID > 0 && (parent == null || parent.ActivityID != node.ActivityID))
        return GetTriggerText(node.ActivityID, node.VerbID);
      if (node.ToneMoodID > 0 && (parent == null || parent.ToneMoodID != node.ToneMoodID))
      {
        try
        {
          var s = PsychicSystem.GetToneMoodString(node.ToneMoodID);
          if (string.IsNullOrEmpty(s)) return $"Тон / Настроение: ID {node.ToneMoodID}";
          s = s.Replace(" - ", " / ");
          return "Тон / Настроение: " + s;
        }
        catch { return $"Тон / Настроение: ID {node.ToneMoodID}"; }
      }
      if (node.SimbolID > 0 && (parent == null || parent.SimbolID != node.SimbolID))
        return GetSimbolDisplay(node.SimbolID);
      if (node.VerbID > 0 && (parent == null || parent.VerbID != node.VerbID))
        return GetTriggerText(node.ActivityID, node.VerbID);

      return "Узел " + node.ID;
    }

    private string GetEmotionStyleNames(int emotionImageId)
    {
      if (emotionImageId <= 0 || _emotionsImageSystem == null || _gomeostas == null) return string.Empty;
      try
      {
        var img = _emotionsImageSystem.GetEmotionsImage(emotionImageId);
        if (img?.BaseStylesList == null || !img.BaseStylesList.Any()) return string.Empty;
        var styles = _gomeostas.GetAllBehaviorStyles();
        var names = img.BaseStylesList
            .Where(id => styles.ContainsKey(id))
            .Select(id => styles[id].Name)
            .ToList();
        return names.Any() ? string.Join(" + ", names) : string.Empty;
      }
      catch { return string.Empty; }
    }

    private string GetTriggerText(int activityId, int verbId)
    {
      var actionPart = "Нет";
      if (activityId > 0 && _influenceActionsImagesSystem != null && _influenceActionSystem != null)
      {
        try
        {
          var actionIds = _influenceActionsImagesSystem.GetInfluenceActionIds(activityId)?.ToList();
          if (actionIds != null && actionIds.Any())
          {
            var all = _influenceActionSystem.GetAllInfluenceActions();
            var names = actionIds.Where(id => all.Any(a => a.Id == id)).Select(id => all.First(a => a.Id == id).Name).ToList();
            actionPart = names.Any() ? string.Join(", ", names) : "Нет";
          }
        }
        catch { }
      }
      var phrasePart = "Нет";
      if (verbId > 0 && _verbalBrocaImages != null && _sensorySystem != null)
      {
        try
        {
          var verbal = _verbalBrocaImages.GetVerbalBrocaImage(verbId);
          if (verbal?.PhraseIdList != null && verbal.PhraseIdList.Any())
          {
            var phrases = _sensorySystem.VerbalChannel?.GetAllPhrases();
            if (phrases != null)
            {
              var texts = verbal.PhraseIdList.Where(id => phrases.ContainsKey(id)).Select(id => "\"" + phrases[id] + "\"").ToList();
              if (texts.Any()) phrasePart = string.Join(", ", texts);
            }
          }
        }
        catch { }
      }
      return $"Триггер: Действие: {actionPart} | Фраза: {phrasePart}";
    }

    private string GetSimbolDisplay(int simbolId)
    {
      if (simbolId <= 0) return "Первый символ: Нет";
      var letter = _sensorySystem?.VerbalChannel?.GetPrimarySensorSymbol(simbolId) ?? '\0';
      return letter != '\0' ? "Первый символ: " + letter : "Первый символ: ID " + simbolId;
    }

    private void FillAutomatizmLines(AutomatizmTreeNodeItem item, int nodeId, int pathActivityId, int pathVerbId)
    {
      item.AutomatizmLines.Clear();
      var list = _automatizmSystem.GetMotorsAutomatizmListFromTreeId(nodeId);
      if (list == null || list.Count == 0) return;

      var standard = _automatizmSystem.GetBelief2AutomatizmFromTreeId(nodeId);
      // Сортировка: первый штатный (Belief==2), остальные по убыванию уверенности (Belief)
      var sorted = list
          .OrderBy(a => a.Belief == 2 ? 0 : 1)
          .ThenByDescending(a => a.Belief)
          .ToList();

      var toAdd = new List<AutomatizmLineItem>();
      foreach (var a in sorted)
      {
        if (!AutomatizmPassesFilters(a.ActionsImageID, item.StyleIdsInPath, pathActivityId, pathVerbId))
          continue;
        var actionText = GetActionText(a.ActionsImageID);
        var phraseText = GetPhraseText(a.ActionsImageID, inQuotes: true);
        var beliefText = a.Belief == 0 ? "Предположение" : a.Belief == 1 ? "Чужие сведения" : "Проверенное знание";
        var block = new StringBuilder();
        block.AppendLine("ID: " + a.ID);
        block.AppendLine("Действие: " + actionText);
        block.AppendLine("Фраза: " + phraseText);
        block.AppendLine("Уверенность: " + beliefText);
        block.Append("Кол-во использований: " + a.Count);
        toAdd.Add(new AutomatizmLineItem(block.ToString(), false));
      }
      for (int i = 0; i < toAdd.Count; i++)
      {
        if (i == 0 && toAdd.Count > 1)
          toAdd[i] = new AutomatizmLineItem(toAdd[i].DisplayText, true);
        item.AutomatizmLines.Add(toAdd[i]);
      }
    }

    private string GetActionText(int actionsImageId)
    {
      if (actionsImageId <= 0) return "Нет";
      var img = _actionsImagesSystem.GetActionsImage(actionsImageId);
      if (img?.ActIdList == null || !img.ActIdList.Any()) return "Нет";
      var actionsCollection = _adaptiveActionsSystem?.GetAllAdaptiveActions();
      var actionsList = actionsCollection != null ? actionsCollection.ToList() : new List<AdaptiveActionsSystem.AdaptiveAction>();
      var names = img.ActIdList
          .Select(id => actionsList.FirstOrDefault(a => a.Id == id)?.Name ?? $"#{id}")
          .ToList();
      return string.Join(", ", names);
    }

    private string GetPhraseText(int actionsImageId, bool inQuotes = false)
    {
      if (actionsImageId <= 0) return "Нет";
      var img = _actionsImagesSystem.GetActionsImage(actionsImageId);
      if (img?.PhraseIdList == null || !img.PhraseIdList.Any()) return "Нет";
      var phrases = _sensorySystem?.VerbalChannel?.GetAllPhrases();
      if (phrases == null) return "Нет";
      var texts = img.PhraseIdList
          .Where(id => phrases.ContainsKey(id))
          .Select(id => inQuotes ? "\"" + phrases[id] + "\"" : phrases[id])
          .ToList();
      return texts.Any() ? string.Join(", ", texts) : "Нет";
    }

    private bool AutomatizmPassesFilters(int actionsImageId, HashSet<int> styleIdsInPath, int pathActivityId, int pathVerbId)
    {
      if (SelectedStyleFilter.HasValue && SelectedStyleFilter.Value != 0)
      {
        if (styleIdsInPath == null || !styleIdsInPath.Contains(SelectedStyleFilter.Value))
          return false;
      }
      if (SelectedPerceptionActionFilterId != 0 && pathActivityId > 0)
      {
        var actionIds = _influenceActionsImagesSystem?.GetInfluenceActionIds(pathActivityId)?.ToList();
        if (actionIds == null || !actionIds.Contains(SelectedPerceptionActionFilterId))
          return false;
      }
      else if (SelectedPerceptionActionFilterId != 0)
        return false;
      if (SelectedActionFilterId != 0)
      {
        var img = actionsImageId > 0 ? _actionsImagesSystem.GetActionsImage(actionsImageId) : null;
        if (img?.ActIdList == null || !img.ActIdList.Contains(SelectedActionFilterId))
          return false;
      }
      if (!string.IsNullOrWhiteSpace(FilterPhrasePerception))
      {
        var phraseText = GetPerceptionPhraseText(pathVerbId);
        if (string.IsNullOrEmpty(phraseText) ||
            phraseText.IndexOf(FilterPhrasePerception.Trim(), StringComparison.OrdinalIgnoreCase) < 0)
          return false;
      }
      if (!string.IsNullOrWhiteSpace(FilterPhrase))
      {
        var phraseText = GetPhraseText(actionsImageId, inQuotes: false);
        if (string.IsNullOrEmpty(phraseText) ||
            phraseText.IndexOf(FilterPhrase.Trim(), StringComparison.OrdinalIgnoreCase) < 0)
          return false;
      }
      return true;
    }

    private string GetPerceptionPhraseText(int verbId)
    {
      if (verbId <= 0 || _verbalBrocaImages == null || _sensorySystem == null) return string.Empty;
      try
      {
        var verbal = _verbalBrocaImages.GetVerbalBrocaImage(verbId);
        if (verbal?.PhraseIdList == null || !verbal.PhraseIdList.Any()) return string.Empty;
        var phrases = _sensorySystem.VerbalChannel?.GetAllPhrases();
        if (phrases == null) return string.Empty;
        return string.Join(" ", verbal.PhraseIdList.Where(id => phrases.ContainsKey(id)).Select(id => phrases[id]));
      }
      catch { return string.Empty; }
    }

    private void RebuildTreeWithFilters()
    {
      LoadTree();
    }
  }
}
