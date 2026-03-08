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
using System.Windows.Input;
using System.Windows.Media;

namespace AIStudio.ViewModels.Episodic
{
  /// <summary>
  /// Элемент дерева для отображения (обёртка над EpisodicMemoryNode с текстовым обозначением)
  /// </summary>
  public class EpisodicTreeNodeItem : INotifyPropertyChanged
  {
    public EpisodicMemoryNode Node { get; }
    public string TextDisplay { get; }
    public string TooltipText { get; }
    /// <summary>Цвет для узла: зелёный для положительного эффекта, красный для отрицательного, иначе по умолчанию.</summary>
    public Brush EffectBrush { get; }
    public ObservableCollection<EpisodicTreeNodeItem> Children { get; } = new ObservableCollection<EpisodicTreeNodeItem>();

    public EpisodicTreeNodeItem(EpisodicMemoryNode node, string textDisplay, string tooltipText, Brush effectBrush = null)
    {
      Node = node;
      TextDisplay = textDisplay ?? $"ID:{node?.ID ?? 0}";
      TooltipText = tooltipText ?? TextDisplay;
      EffectBrush = effectBrush ?? Brushes.Black;
    }

    public event PropertyChangedEventHandler PropertyChanged;
  }

  /// <summary>
  /// Описание с ссылкой (как в AutomatizmsViewModel)
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

    private string _filterPhrasePerception = string.Empty;
    private string _filterPhrase = string.Empty;
    private string _filterPhrasePerceptionInput = string.Empty;
    private string _filterPhraseInput = string.Empty;

    public string CurrentAgentTitle => "Дерево эпизодической памяти";

    public DescriptionWithLink CurrentAgentDescription =>
      new DescriptionWithLink
      {
        Text = "Дерево только для чтения. Три дерева эпизодов по базовым состояниям: Плохо (-1), Норма (0), Хорошо (1). Узлы отображаются в формате NodePID → Эмоция → Тригггер → Акция → Эффект. "
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
      SensorySystem sensorySystem = null)
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

      int limitBad = limit;
      TreeBad = BuildTreeFromChildren(root.Children, -1, ref limitBad, 0, true);
      int limitNorm = limit;
      TreeNormal = BuildTreeFromChildren(root.Children, 0, ref limitNorm, 0, true);
      int limitGood = limit;
      TreeGood = BuildTreeFromChildren(root.Children, 1, ref limitGood, 0, true);
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

      foreach (var child in children)
      {
        if (remainingLimit <= 0) break;
        if (filterByBaseId && child.BaseID != targetBaseId) continue;
        if (!PassesFilters(child)) continue;

        var (text, tooltip, effectBrush) = GetNodeDisplayAndTooltip(child, depth);
        var item = new EpisodicTreeNodeItem(child, text, tooltip, effectBrush);
        remainingLimit--;

        if (child.Children != null && child.Children.Count > 0 && remainingLimit > 0)
        {
          var sub = BuildTreeFromChildren(child.Children, targetBaseId, ref remainingLimit, depth + 1, false);
          foreach (var c in sub)
            item.Children.Add(c);
        }

        result.Add(item);
      }

      return result;
    }

    private bool PassesFilters(EpisodicMemoryNode node)
    {
      if (SelectedLevel2Filter.HasValue)
      {
        if (node.EmotionID == 0) return false;
        var img = _emotionsImage?.GetEmotionsImage(node.EmotionID);
        if (img?.BaseStylesList == null || !img.BaseStylesList.Contains(SelectedLevel2Filter.Value))
          return false;
      }

      if (!string.IsNullOrEmpty(SelectedUsefulnessFilter) && (node.Params == null))
        return false;
      if (!string.IsNullOrEmpty(SelectedUsefulnessFilter) && node.Params != null)
      {
        switch (SelectedUsefulnessFilter)
        {
          case "<0": if (node.Params.Effect >= 0) return false; break;
          case "=0": if (node.Params.Effect != 0) return false; break;
          case ">0": if (node.Params.Effect <= 0) return false; break;
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

      if (!string.IsNullOrWhiteSpace(_filterPhrasePerception) && node.TriggerId != 0)
      {
        string phrase = GetTriggerPhraseText(node.TriggerId);
        if (string.IsNullOrEmpty(phrase) || phrase.IndexOf(_filterPhrasePerception, StringComparison.OrdinalIgnoreCase) < 0)
          return false;
      }

      if (!string.IsNullOrWhiteSpace(_filterPhrase) && node.ActionId != 0)
      {
        string phrase = GetActionPhraseText(node.ActionId);
        if (string.IsNullOrEmpty(phrase) || phrase.IndexOf(_filterPhrase, StringComparison.OrdinalIgnoreCase) < 0)
          return false;
      }

      return true;
    }

    private string GetTriggerPhraseText(int triggerId)
    {
      var img = _actionsImages?.GetActionsImage(triggerId);
      if (img?.PhraseIdList == null || img.PhraseIdList.Count == 0) return null;
      return GetPhraseTextFromImage(img);
    }

    private string GetActionPhraseText(int actionId)
    {
      var img = _actionsImages?.GetActionsImage(actionId);
      if (img?.PhraseIdList == null || img.PhraseIdList.Count == 0) return null;
      return GetPhraseTextFromImage(img);
    }

    private string GetPhraseTextFromImage(ActionsImagesSystem.ActionsImage img)
    {
      if (_sensorySystem?.VerbalChannel == null) return null;
      var vc = _sensorySystem.VerbalChannel;
      var list = img.PhraseIdList.Select(pid => vc.GetPhraseFromPhraseId(pid)).Where(s => !string.IsNullOrEmpty(s)).ToList();
      return list.Any() ? string.Join(" ", list) : null;
    }

    private (string text, string tooltip, Brush effectBrush) GetNodeDisplayAndTooltip(EpisodicMemoryNode node, int depth)
    {
      if (node == null) return ("—", "—", null);

      string text;
      string tooltip;
      Brush effectBrush = null;

      switch (depth)
      {
        case 0:
          text = GetBaseIdText(node.BaseID);
          tooltip = $"BaseID: {node.BaseID}";
          break;
        case 1:
          text = "Эмоция: " + GetEmotionText(node.EmotionID);
          tooltip = GetEmotionText(node.EmotionID) + $"\nEmotionID: {node.EmotionID}";
          break;
        case 2:
          text = $"NodePID: {node.NodePID}";
          tooltip = GetNodePidTooltip(node.NodePID);
          break;
        case 3:
          text = $"Тригггер: {node.TriggerId}";
          tooltip = GetTriggerTooltip(node.TriggerId);
          break;
        case 4:
          text = node.Params != null
            ? $"Акция: {node.ActionId}, Эффект: {FormatEffect(node.Params.Effect)}"
            : $"Акция: {node.ActionId}";
          tooltip = GetActionTooltip(node.ActionId);
          if (node.Params != null)
            tooltip += $"\nЭффект: {node.Params.Effect}, Count: {node.Params.Count}";
          if (node.Params != null)
            effectBrush = node.Params.Effect > 0 ? Brushes.DarkGreen : (node.Params.Effect < 0 ? Brushes.DarkRed : null);
          break;
        default:
          text = BuildCompositeLabel(node);
          tooltip = $"ID: {node.ID}";
          break;
      }

      return (text ?? $"ID:{node.ID}", tooltip ?? text, effectBrush);
    }

    private static string FormatEffect(int effect)
    {
      if (effect > 0) return "+" + effect;
      return effect.ToString();
    }

    private string GetBaseIdText(int baseId)
    {
      switch (baseId)
      {
        case -1: return "Плохо";
        case 0: return "Норма";
        case 1: return "Хорошо";
        default: return $"BaseID:{baseId}";
      }
    }

    private string BuildCompositeLabel(EpisodicMemoryNode node)
    {
      if (node.NodePID != 0) return $"NodePID: {node.NodePID}";
      if (node.EmotionID != 0) return "Эмоция: " + GetEmotionText(node.EmotionID);
      if (node.TriggerId != 0) return $"Тригггер: {node.TriggerId}";
      if (node.ActionId != 0) return $"Акция: {node.ActionId}";
      return GetBaseIdText(node.BaseID);
    }

    private string GetEmotionText(int emotionId)
    {
      if (emotionId <= 0) return "—";
      try
      {
        var img = _emotionsImage?.GetEmotionsImage(emotionId);
        if (img?.BaseStylesList == null || !img.BaseStylesList.Any()) return emotionId.ToString();
        var styles = _gomeostas?.GetAllBehaviorStyles();
        if (styles == null) return emotionId.ToString();
        var names = img.BaseStylesList.Where(id => styles.ContainsKey(id)).Select(id => styles[id].Name).ToList();
        return names.Any() ? string.Join(", ", names) : emotionId.ToString();
      }
      catch { return emotionId.ToString(); }
    }

    private string GetNodePidTooltip(int nodePid)
    {
      if (nodePid <= 0) return $"NodePID: {nodePid}";
      var pn = FindProblemNodeById(_problemTree?.Tree, nodePid);
      if (pn == null) return $"NodePID: {nodePid}";
      return $"NodePID: {nodePid}\nAutTreeID: {pn.AutTreeID}\nSituationTreeID: {pn.SituationTreeID}\nThemeID: {pn.ThemeID}\nPurposeID: {pn.PurposeID}";
    }

    private ProblemTreeNode FindProblemNodeById(ProblemTreeNode root, int id)
    {
      if (root == null) return null;
      if (root.ID == id) return root;
      foreach (var c in root.Children ?? Enumerable.Empty<ProblemTreeNode>())
      {
        var found = FindProblemNodeById(c, id);
        if (found != null) return found;
      }
      return null;
    }

    private string GetTriggerTooltip(int triggerId)
    {
      if (triggerId <= 0) return "—";
      var actImg = _actionsImages?.GetActionsImage(triggerId);
      if (actImg != null)
        return BuildImageTooltip(actImg);
      var infImg = _influenceActionsImages?.GetInfluenceActionsImage(triggerId);
      if (infImg?.ActIdList != null && infImg.ActIdList.Count > 0 && _influenceAction != null)
      {
        var names = infImg.ActIdList
          .Where(id => _influenceAction.GetAllInfluenceActions().Any(a => a.Id == id))
          .Select(id => _influenceAction.GetAllInfluenceActions().First(a => a.Id == id).Name)
          .ToList();
        return $"Действие: {(names.Any() ? string.Join(", ", names) : "Нет")}\nФраза: Нет\nТон/Настроение: —";
      }
      return $"Триггер ID: {triggerId}";
    }

    private string GetActionTooltip(int actionId)
    {
      if (actionId <= 0) return "—";
      var actImg = _actionsImages?.GetActionsImage(actionId);
      return actImg != null ? BuildImageTooltip(actImg) : $"Акция ID: {actionId}";
    }

    private string BuildImageTooltip(ActionsImagesSystem.ActionsImage actImg)
    {
      var sb = new System.Text.StringBuilder();
      string actionText = "Нет";
      if (actImg.ActIdList != null && actImg.ActIdList.Count > 0 && _adaptiveActions != null)
      {
        var names = actImg.ActIdList
          .Where(id => _adaptiveActions.GetAllAdaptiveActions().Any(a => a.Id == id))
          .Select(id => _adaptiveActions.GetAllAdaptiveActions().First(a => a.Id == id).Name)
          .ToList();
        actionText = names.Any() ? string.Join(", ", names) : "Нет";
      }
      else if (actImg.ActIdList != null && actImg.ActIdList.Count > 0 && _influenceAction != null)
      {
        var names = actImg.ActIdList
          .Where(id => _influenceAction.GetAllInfluenceActions().Any(a => a.Id == id))
          .Select(id => _influenceAction.GetAllInfluenceActions().First(a => a.Id == id).Name)
          .ToList();
        actionText = names.Any() ? string.Join(", ", names) : "Нет";
      }
      sb.AppendLine($"Действие: {actionText}");

      string phraseText = "Нет";
      if (actImg.PhraseIdList != null && actImg.PhraseIdList.Count > 0 && _sensorySystem?.VerbalChannel != null)
      {
        var phrases = actImg.PhraseIdList.Select(pid => _sensorySystem.VerbalChannel.GetPhraseFromPhraseId(pid)).Where(s => !string.IsNullOrEmpty(s)).ToList();
        phraseText = phrases.Any() ? string.Join(" ", phrases) : "Нет";
      }
      sb.AppendLine($"Фраза: {phraseText}");

      string tone = ActionsImagesSystem.GetToneText(actImg.ToneId);
      string mood = ActionsImagesSystem.GetMoodText(actImg.MoodId);
      sb.AppendLine(string.IsNullOrEmpty(tone) && string.IsNullOrEmpty(mood) ? "Тон/Настроение: —" : $"Тон/Настроение: {tone ?? "—"} - {mood ?? "—"}");
      return sb.ToString().TrimEnd();
    }
  }
}
