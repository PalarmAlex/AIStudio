using AIStudio.Common;
using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Gomeostas;
using ISIDA.Psychic;
using ISIDA.Psychic.Automatism;
using ISIDA.Psychic.Memory.Episodic;
using ISIDA.Psychic.Understanding;
using ISIDA.Sensors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace AIStudio.ViewModels.Episodic
{
  /// <summary>
  /// Элемент дерева для отображения (обёртка над EpisodicMemoryNode с текстовым обозначением)
  /// </summary>
  public class EpisodicTreeNodeItem
  {
    public EpisodicMemoryNode Node { get; }
    public string TextDisplay { get; }
    public string TooltipText { get; }
    /// <summary>Цвет для узла: зелёный для положительного эффекта, красный для отрицательного, тёмно-жёлтый для нуля, иначе по умолчанию.</summary>
    public Brush EffectBrush { get; }
    /// <summary>Строка узла «Акция» (с эффектом) — выделять жирным.</summary>
    public bool IsActionRow { get; }
    /// <summary>Уровень узла: 0=Base, 1=Эмоция, 2=Understanding, 3=NodePID, 4=Триггер, 5=Акция (см. <see cref="EpisodicMemoryTree.BranchKeyLength"/>).</summary>
    public int Level { get; }
    public FontWeight RowFontWeight => IsActionRow ? FontWeights.Bold : FontWeights.Normal;
    public ObservableCollection<EpisodicTreeNodeItem> Children { get; } = new ObservableCollection<EpisodicTreeNodeItem>();

    public EpisodicTreeNodeItem(EpisodicMemoryNode node, string textDisplay, string tooltipText, Brush effectBrush = null, bool isActionRow = false, int level = 0)
    {
      Node = node;
      TextDisplay = textDisplay ?? $"ID:{node?.ID ?? 0}";
      TooltipText = tooltipText ?? TextDisplay;
      EffectBrush = effectBrush ?? Brushes.Black;
      IsActionRow = isActionRow;
      Level = level;
    }
  }

  /// <summary>
  /// Элемент отображения одного кадра истории эпизодов (плашка в ленте 100 кадров)
  /// </summary>
  public class HistoryFrameItem
  {
    public string DisplayText { get; }
    public Brush BackgroundBrush { get; }
    public string TooltipText { get; }

    public HistoryFrameItem(string displayText, Brush backgroundBrush, string tooltipText = null)
    {
      DisplayText = displayText ?? "";
      BackgroundBrush = backgroundBrush ?? Brushes.White;
      TooltipText = tooltipText ?? displayText ?? "";
    }
  }

  /// <summary>
  /// Описание с ссылкой
  /// </summary>
  public class DescriptionWithLink
  {
    public string Text { get; set; }
    public string LinkText { get; set; } = "Подробнее...";
    public string Url { get; set; } = "https://scorcher.ru/isida/iadaptive_agents_guide.php#episodic";
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

  /// <summary>
  /// ViewModel страницы дерева эпизодической памяти
  /// </summary>
  public class EpisodicMemoryTreeViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly EpisodicMemorySystem _episodicMemory;
    private readonly GomeostasSystem _gomeostas;
    private readonly EmotionsImageSystem _emotionsImage;
    private readonly InfluenceActionSystem _influenceAction;
    private readonly AdaptiveActionsSystem _adaptiveActions;
    private readonly ProblemTreeSystem _problemTree;
    private readonly InfluenceActionsImagesSystem _influenceActionsImages;
    private readonly ActionsImagesSystem _actionsImages;
    private readonly SensorySystem _sensorySystem;
    private readonly EpisodicMemoryNodePresentation _nodePresentation;

    private string _filterPhrasePerception = string.Empty;
    private string _filterPhrase = string.Empty;
    private string _filterPhrasePerceptionInput = string.Empty;
    private string _filterPhraseInput = string.Empty;

    public string CurrentAgentTitle => "Дерево эпизодической памяти";

    public DescriptionWithLink CurrentAgentDescription =>
      new DescriptionWithLink
      {
        Text = "Дерево только для чтения. Три дерева эпизодов по базовым состояниям: Плохо, Норма, Хорошо. Узлы: Эмоция → Understanding → NodePID → Триггер → Акция (эффект). "
      };

    #region Деревья для трёх колонок

    private ObservableCollection<EpisodicTreeNodeItem> _treeBad = new ObservableCollection<EpisodicTreeNodeItem>();
    public ObservableCollection<EpisodicTreeNodeItem> TreeBad
    {
      get => _treeBad;
      set { _treeBad = value; OnPropertyChanged(nameof(TreeBad)); }
    }

    private ObservableCollection<EpisodicTreeNodeItem> _treeNormal = new ObservableCollection<EpisodicTreeNodeItem>();
    public ObservableCollection<EpisodicTreeNodeItem> TreeNormal
    {
      get => _treeNormal;
      set { _treeNormal = value; OnPropertyChanged(nameof(TreeNormal)); }
    }

    private ObservableCollection<EpisodicTreeNodeItem> _treeGood = new ObservableCollection<EpisodicTreeNodeItem>();
    public ObservableCollection<EpisodicTreeNodeItem> TreeGood
    {
      get => _treeGood;
      set { _treeGood = value; OnPropertyChanged(nameof(TreeGood)); }
    }

    #endregion

    #region Сворачивание узлов (Эмоции, Understanding, NodePID, Триггер) — по умолчанию все отжаты

    private bool _collapseEmotionsBad;
    private bool _collapseUnderstandingBad;
    private bool _collapseNodePidBad;
    private bool _collapseTriggerBad;
    private bool _collapseEmotionsNormal;
    private bool _collapseUnderstandingNormal;
    private bool _collapseNodePidNormal;
    private bool _collapseTriggerNormal;
    private bool _collapseEmotionsGood;
    private bool _collapseUnderstandingGood;
    private bool _collapseNodePidGood;
    private bool _collapseTriggerGood;

    public bool CollapseEmotionsBad { get => _collapseEmotionsBad; set { _collapseEmotionsBad = value; OnPropertyChanged(nameof(CollapseEmotionsBad)); } }
    public bool CollapseUnderstandingBad { get => _collapseUnderstandingBad; set { _collapseUnderstandingBad = value; OnPropertyChanged(nameof(CollapseUnderstandingBad)); } }
    public bool CollapseNodePidBad { get => _collapseNodePidBad; set { _collapseNodePidBad = value; OnPropertyChanged(nameof(CollapseNodePidBad)); } }
    public bool CollapseTriggerBad { get => _collapseTriggerBad; set { _collapseTriggerBad = value; OnPropertyChanged(nameof(CollapseTriggerBad)); } }
    public bool CollapseEmotionsNormal { get => _collapseEmotionsNormal; set { _collapseEmotionsNormal = value; OnPropertyChanged(nameof(CollapseEmotionsNormal)); } }
    public bool CollapseUnderstandingNormal { get => _collapseUnderstandingNormal; set { _collapseUnderstandingNormal = value; OnPropertyChanged(nameof(CollapseUnderstandingNormal)); } }
    public bool CollapseNodePidNormal { get => _collapseNodePidNormal; set { _collapseNodePidNormal = value; OnPropertyChanged(nameof(CollapseNodePidNormal)); } }
    public bool CollapseTriggerNormal { get => _collapseTriggerNormal; set { _collapseTriggerNormal = value; OnPropertyChanged(nameof(CollapseTriggerNormal)); } }
    public bool CollapseEmotionsGood { get => _collapseEmotionsGood; set { _collapseEmotionsGood = value; OnPropertyChanged(nameof(CollapseEmotionsGood)); } }
    public bool CollapseUnderstandingGood { get => _collapseUnderstandingGood; set { _collapseUnderstandingGood = value; OnPropertyChanged(nameof(CollapseUnderstandingGood)); } }
    public bool CollapseNodePidGood { get => _collapseNodePidGood; set { _collapseNodePidGood = value; OnPropertyChanged(nameof(CollapseNodePidGood)); } }
    public bool CollapseTriggerGood { get => _collapseTriggerGood; set { _collapseTriggerGood = value; OnPropertyChanged(nameof(CollapseTriggerGood)); } }

    #endregion

    #region Фильтры (порядок как в AutomatizmsView, без NodePID)

    public List<KeyValuePair<int?, string>> BaseConditionFilterOptions { get; } = new List<KeyValuePair<int?, string>>
    {
      new KeyValuePair<int?, string>(null, "Все состояния"),
      new KeyValuePair<int?, string>(-1, "Плохо"),
      new KeyValuePair<int?, string>(0, "Норма"),
      new KeyValuePair<int?, string>(1, "Хорошо")
    };

    public List<KeyValuePair<string, string>> UsefulnessFilterOptions { get; } = new List<KeyValuePair<string, string>>
    {
      new KeyValuePair<string, string>(null, "Все"),
      new KeyValuePair<string, string>("<0", "Вредные (<0)"),
      new KeyValuePair<string, string>("=0", "Нейтральные (=0)"),
      new KeyValuePair<string, string>(">0", "Полезные (>0)")
    };

    public List<KeyValuePair<int, string>> PerceptionActionFilterOptions { get; } = new List<KeyValuePair<int, string>>();
    public List<KeyValuePair<int, string>> ActionFilterOptions { get; } = new List<KeyValuePair<int, string>>();
    public List<KeyValuePair<int?, string>> Level2FilterOptions { get; private set; } = new List<KeyValuePair<int?, string>>();

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

    private int? _selectedBaseConditionFilter;
    private int? _selectedLevel2Filter;
    private string _selectedUsefulnessFilter;
    private int _selectedPerceptionActionFilterId;
    private int _selectedActionFilterId;
    private int _selectedMaxNodes = 500;

    public int? SelectedBaseConditionFilter { get => _selectedBaseConditionFilter; set { _selectedBaseConditionFilter = value; OnPropertyChanged(nameof(SelectedBaseConditionFilter)); } }
    public int? SelectedLevel2Filter { get => _selectedLevel2Filter; set { _selectedLevel2Filter = value; OnPropertyChanged(nameof(SelectedLevel2Filter)); } }
    public string SelectedUsefulnessFilter { get => _selectedUsefulnessFilter; set { _selectedUsefulnessFilter = value; OnPropertyChanged(nameof(SelectedUsefulnessFilter)); } }
    public int SelectedPerceptionActionFilterId { get => _selectedPerceptionActionFilterId; set { _selectedPerceptionActionFilterId = value; OnPropertyChanged(nameof(SelectedPerceptionActionFilterId)); } }
    public int SelectedActionFilterId { get => _selectedActionFilterId; set { _selectedActionFilterId = value; OnPropertyChanged(nameof(SelectedActionFilterId)); } }
    public int SelectedMaxNodes { get => _selectedMaxNodes; set { _selectedMaxNodes = value; OnPropertyChanged(nameof(SelectedMaxNodes)); } }

    public string FilterPhrasePerceptionInput { get => _filterPhrasePerceptionInput; set { _filterPhrasePerceptionInput = value ?? string.Empty; OnPropertyChanged(nameof(FilterPhrasePerceptionInput)); } }
    public string FilterPhraseInput { get => _filterPhraseInput; set { _filterPhraseInput = value ?? string.Empty; OnPropertyChanged(nameof(FilterPhraseInput)); } }

    public ICommand ApplyFiltersCommand { get; }
    public ICommand ClearFiltersCommand { get; }

    #endregion

    public bool IsEpisodicMemoryAvailable =>
      _episodicMemory != null && EpisodicMemorySystem.IsInitialized && AppGlobalState.EvolutionStage >= 4;

    public EpisodicMemoryTreeViewModel(
      EpisodicMemorySystem episodicMemory,
      GomeostasSystem gomeostas,
      EmotionsImageSystem emotionsImage,
      InfluenceActionSystem influenceAction,
      AdaptiveActionsSystem adaptiveActions,
      ProblemTreeSystem problemTree,
      InfluenceActionsImagesSystem influenceActionsImages = null,
      ActionsImagesSystem actionsImages = null,
      SensorySystem sensorySystem = null,
      AutomatizmTreeSystem automatizmTree = null,
      VerbalBrocaImagesSystem verbalBroca = null)
    {
      _episodicMemory = episodicMemory;
      _gomeostas = gomeostas;
      _emotionsImage = emotionsImage;
      _influenceAction = influenceAction;
      _adaptiveActions = adaptiveActions;
      _problemTree = problemTree;
      _influenceActionsImages = influenceActionsImages;
      _actionsImages = actionsImages;
      _sensorySystem = sensorySystem;
      _nodePresentation = new EpisodicMemoryNodePresentation(
        _gomeostas, _emotionsImage, _influenceAction, _adaptiveActions, _problemTree,
        _influenceActionsImages, _actionsImages, _sensorySystem,
        automatizmTree, verbalBroca);

      PerceptionActionFilterOptions.Add(new KeyValuePair<int, string>(0, "Все действия"));
      var influenceActions = _influenceAction?.GetAllInfluenceActions();
      if (influenceActions != null)
        foreach (var a in influenceActions)
          PerceptionActionFilterOptions.Add(new KeyValuePair<int, string>(a.Id, a.Name));

      ActionFilterOptions.Add(new KeyValuePair<int, string>(0, "Все действия"));
      var actionsList = _adaptiveActions?.GetAllAdaptiveActions()?.ToList() ?? new List<AdaptiveActionsSystem.AdaptiveAction>();
      foreach (var a in actionsList)
        ActionFilterOptions.Add(new KeyValuePair<int, string>(a.Id, a.Name));

      LoadLevel2FilterOptions();

      ApplyFiltersCommand = new RelayCommand(ApplyFilters);
      ClearFiltersCommand = new RelayCommand(ClearFilters);

      LoadTrees();
    }

    private void LoadLevel2FilterOptions()
    {
      Level2FilterOptions = new List<KeyValuePair<int?, string>> { new KeyValuePair<int?, string>(null, "Все контексты") };
      var dict = _gomeostas?.GetAllBehaviorStyles();
      if (dict != null)
        foreach (var kv in dict)
          Level2FilterOptions.Add(new KeyValuePair<int?, string>(kv.Key, kv.Value.Name));
    }

    private void ApplyFilters(object parameter = null)
    {
      _filterPhrasePerception = (FilterPhrasePerceptionInput ?? string.Empty).Trim();
      _filterPhrase = (FilterPhraseInput ?? string.Empty).Trim();
      LoadTrees();
    }

    private void ClearFilters(object parameter = null)
    {
      SelectedBaseConditionFilter = null;
      SelectedLevel2Filter = null;
      SelectedUsefulnessFilter = null;
      SelectedPerceptionActionFilterId = 0;
      SelectedActionFilterId = 0;
      FilterPhrasePerceptionInput = string.Empty;
      FilterPhraseInput = string.Empty;
      _filterPhrasePerception = string.Empty;
      _filterPhrase = string.Empty;
      SelectedMaxNodes = 500;
      LoadTrees();
    }

    private void LoadTrees()
    {
      if (!IsEpisodicMemoryAvailable)
      {
        TreeBad = new ObservableCollection<EpisodicTreeNodeItem>();
        TreeNormal = new ObservableCollection<EpisodicTreeNodeItem>();
        TreeGood = new ObservableCollection<EpisodicTreeNodeItem>();
        return;
      }

      var root = _episodicMemory.Tree;
      int limit = _selectedMaxNodes <= 0 ? int.MaxValue : _selectedMaxNodes;
      var empty = new ObservableCollection<EpisodicTreeNodeItem>();

      // Интегральное состояние: при выборе конкретного состояния показываем только соответствующую колонку
      if (_selectedBaseConditionFilter.HasValue)
      {
        int baseId = _selectedBaseConditionFilter.Value;
        int limitSingle = limit;
        if (baseId == -1)
        {
          TreeBad = BuildTreeFromChildren(root.Children, -1, ref limitSingle, 0, true);
          TreeNormal = empty;
          TreeGood = empty;
        }
        else if (baseId == 0)
        {
          TreeBad = empty;
          TreeNormal = BuildTreeFromChildren(root.Children, 0, ref limitSingle, 0, true);
          TreeGood = empty;
        }
        else
        {
          TreeBad = empty;
          TreeNormal = empty;
          TreeGood = BuildTreeFromChildren(root.Children, 1, ref limitSingle, 0, true);
        }
        return;
      }

      int limitBad = limit;
      TreeBad = BuildTreeFromChildren(root.Children, -1, ref limitBad, 0, true);
      int limitNorm = limit;
      TreeNormal = BuildTreeFromChildren(root.Children, 0, ref limitNorm, 0, true);
      int limitGood = limit;
      TreeGood = BuildTreeFromChildren(root.Children, 1, ref limitGood, 0, true);
    }

    /// <summary>Ключ группировки на уровне дерева эпизодов (совпадает с <see cref="EpisodicMemoryTree"/>).</summary>
    private static int GetLevelGroupKey(EpisodicMemoryNode node, int depth)
    {
      switch (depth)
      {
        case 0: return node.BaseID;
        case 1: return node.EmotionID;
        case 2: return node.UnderstandingNodeId;
        case 3: return node.NodePID;
        case 4: return node.TriggerId;
        case 5: return node.ActionId;
        default: return node?.ID ?? 0;
      }
    }

    private ObservableCollection<EpisodicTreeNodeItem> BuildTreeFromChildren(
      List<EpisodicMemoryNode> children,
      int targetBaseId,
      ref int remainingLimit,
      int depth,
      bool filterByBaseId)
    {
      var result = new ObservableCollection<EpisodicTreeNodeItem>();
      if (children == null || remainingLimit <= 0) return result;

      // На уровнях 1 (Эмоция) и 2 (Understanding) объединяем узлы с одинаковым ключом (как в BOT).
      var filtered = children.Where(c => !filterByBaseId || c.BaseID == targetBaseId).ToList();
      var grouped = (depth >= 1 && depth <= 2)
        ? filtered.GroupBy(c => GetLevelGroupKey(c, depth)).ToList()
        : null;

      if (grouped != null)
      {
        foreach (var grp in grouped)
        {
          if (remainingLimit <= 0) break;
          var list = grp.ToList();
          var first = list.First();
          var mergedChildren = list.Count == 1
            ? (first.Children ?? new List<EpisodicMemoryNode>())
            : list.SelectMany(n => n.Children ?? new List<EpisodicMemoryNode>()).ToList();

          int limitBeforeSub = remainingLimit;
          ObservableCollection<EpisodicTreeNodeItem> sub = null;
          if (depth < EpisodicMemoryTree.LeafLevelIndex && mergedChildren.Count > 0 && remainingLimit > 0)
            sub = BuildTreeFromChildren(mergedChildren, targetBaseId, ref remainingLimit, depth + 1, false);

          bool nodePasses = PassesFilters(first);
          bool hasPassingDescendants = sub != null && sub.Count > 0;
          if (!nodePasses && !hasPassingDescendants)
          {
            remainingLimit = limitBeforeSub;
            continue;
          }

          var (text, tooltip, effectBrush) = _nodePresentation.GetNodeDisplayAndTooltip(first, depth);
          var item = new EpisodicTreeNodeItem(first, text, tooltip, effectBrush, isActionRow: depth == EpisodicMemoryTree.LeafLevelIndex, level: depth);
          remainingLimit--;
          if (sub != null)
            foreach (var c in sub)
              item.Children.Add(c);
          result.Add(item);
        }
        return result;
      }

      foreach (var child in filtered)
      {
        if (remainingLimit <= 0) break;

        // Сначала строим потомков — чтобы скрывать узлы, у которых нет подходящих по фильтру потомков
        int limitBeforeSub = remainingLimit;
        ObservableCollection<EpisodicTreeNodeItem> sub = null;
        if (depth < EpisodicMemoryTree.LeafLevelIndex && child.Children != null && child.Children.Count > 0 && remainingLimit > 0)
        {
          sub = BuildTreeFromChildren(child.Children, targetBaseId, ref remainingLimit, depth + 1, false);
        }

        // Включаем узел только если он проходит фильтры ИЛИ есть хотя бы один подходящий потомок
        bool nodePasses = PassesFilters(child);
        bool hasPassingDescendants = sub != null && sub.Count > 0;
        if (!nodePasses && !hasPassingDescendants)
        {
          remainingLimit = limitBeforeSub; // не добавляем ветку — восстанавливаем лимит
          continue;
        }

        var (text, tooltip, effectBrush) = _nodePresentation.GetNodeDisplayAndTooltip(child, depth);
        var item = new EpisodicTreeNodeItem(child, text, tooltip, effectBrush, isActionRow: depth == EpisodicMemoryTree.LeafLevelIndex, level: depth);
        remainingLimit--;

        if (sub != null)
          foreach (var c in sub)
            item.Children.Add(c);

        result.Add(item);
      }

      return result;
    }

    private bool PassesFilters(EpisodicMemoryNode node)
    {
      // Контексты реагирования: при активном фильтре узлы без эмоции не считаются подходящими — попадут в дерево только как предки подходящих
      if (SelectedLevel2Filter.HasValue)
      {
        if (node.EmotionID == 0) return false;
        var img = _emotionsImage?.GetEmotionsImage(node.EmotionID);
        if (img?.BaseStylesList == null || !img.BaseStylesList.Contains(SelectedLevel2Filter.Value))
          return false;
      }

      // Полезность (эффект): при активном фильтре узлы без Params не считаются подходящими — попадут в дерево только как предки подходящих
      if (!string.IsNullOrEmpty(SelectedUsefulnessFilter))
      {
        if (node.Params == null) return false;
        switch (SelectedUsefulnessFilter)
        {
          case "<0": if (EpisodicMemoryNodePresentation.GetSignedOutcome(node.Params) >= 0) return false; break;
          case "=0": if (EpisodicMemoryNodePresentation.GetSignedOutcome(node.Params) != 0) return false; break;
          case ">0": if (EpisodicMemoryNodePresentation.GetSignedOutcome(node.Params) <= 0) return false; break;
        }
      }

      if (SelectedPerceptionActionFilterId != 0)
      {
        if (node.TriggerId == 0) return false;
        var actIds = _influenceActionsImages?.GetInfluenceActionIds(node.TriggerId);
        if (actIds != null && actIds.Contains(SelectedPerceptionActionFilterId)) { /* ok */ }
        else
        {
          var actImg = _actionsImages?.GetActionsImage(node.TriggerId);
          if (actImg?.ActIdList == null || !actImg.ActIdList.Contains(SelectedPerceptionActionFilterId))
            return false;
        }
      }

      if (SelectedActionFilterId != 0)
      {
        if (node.ActionId == 0) return false;
        var actImg = _actionsImages?.GetActionsImage(node.ActionId);
        if (actImg?.ActIdList == null || !actImg.ActIdList.Contains(SelectedActionFilterId))
          return false;
      }

      // Фраза триггера: при активном фильтре узлы без триггера не считаются подходящими — попадут в дерево только как предки подходящих
      if (!string.IsNullOrWhiteSpace(_filterPhrasePerception))
      {
        if (node.TriggerId == 0) return false;
        string phrase = _nodePresentation.GetTriggerPhraseText(node.TriggerId);
        if (string.IsNullOrEmpty(phrase) || phrase.IndexOf(_filterPhrasePerception, StringComparison.OrdinalIgnoreCase) < 0)
          return false;
      }

      // Фраза акции: при активном фильтре узлы без акции не считаются подходящими — попадут в дерево только как предки подходящих
      if (!string.IsNullOrWhiteSpace(_filterPhrase))
      {
        if (node.ActionId == 0) return false;
        string phrase = _nodePresentation.GetActionPhraseText(node.ActionId);
        if (string.IsNullOrEmpty(phrase) || phrase.IndexOf(_filterPhrase, StringComparison.OrdinalIgnoreCase) < 0)
          return false;
      }

      return true;
    }
  }
}
