using AIStudio.Common;
using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Gomeostas;
using ISIDA.Psychic;
using ISIDA.Psychic.Automatism;
using ISIDA.Psychic.Understanding;
using ISIDA.Psychic.Memory.Episodic;
using ISIDA.Sensors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace AIStudio.ViewModels.Episodic
{
  public class MotorEpisodicRuleRow
  {
    public int Id { get; set; }
    public int BaseId { get; set; }
    public int EmotionId { get; set; }
    public int NodePid { get; set; }
    public int TriggerId { get; set; }
    public int ActionId { get; set; }
    public int Effect { get; set; }
    public int Count { get; set; }
    public int StimulsEffect { get; set; }
    /// <summary>Учительское правило (оценка в StimulsEffect).</summary>
    public bool IsTeacher { get; set; }
    /// <summary>Текст столбца «Эффект»: для учителя — «—», иначе число.</summary>
    public string EffectColumnText => IsTeacher ? "—" : Effect.ToString();
    /// <summary>Для окраски столбца «Эффект»: у учителя нейтральный 0.</summary>
    public int EffectColorValue => IsTeacher ? 0 : Effect;
    /// <summary>Контексты реагирования (Level2) для отображения через IdListToNamesConverter.</summary>
    public List<int> EmotionContextIds { get; set; }
    public string TriggerTooltipText { get; set; }
    public string ActionTooltipText { get; set; }
    public string NodePidTooltipText { get; set; }
  }

  public class MotorRulesViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private readonly EpisodicMemorySystem _episodicMemory;
    private readonly EpisodicMemoryNodePresentation _presentation;
    private readonly EmotionsImageSystem _emotionsImage;
    private readonly InfluenceActionsImagesSystem _influenceActionsImages;
    private readonly ActionsImagesSystem _actionsImages;
    private readonly GomeostasSystem _gomeostas;
    private List<MotorEpisodicRuleRow> _allRows = new List<MotorEpisodicRuleRow>();

    private ObservableCollection<MotorEpisodicRuleRow> _rules = new ObservableCollection<MotorEpisodicRuleRow>();
    public ObservableCollection<MotorEpisodicRuleRow> Rules
    {
      get => _rules;
      set { _rules = value; OnPropertyChanged(nameof(Rules)); OnPropertyChanged(nameof(DisplayCountText)); }
    }

    private ObservableCollection<HistoryFrameItem> _historyFrames = new ObservableCollection<HistoryFrameItem>();
    public ObservableCollection<HistoryFrameItem> HistoryFrames
    {
      get => _historyFrames;
      set { _historyFrames = value; OnPropertyChanged(nameof(HistoryFrames)); }
    }

    public string CurrentAgentTitle => "Моторные правила";

    public bool IsMotorRulesAvailable =>
      _episodicMemory != null && EpisodicMemorySystem.IsInitialized && AppGlobalState.EvolutionStage >= 4;

    public List<KeyValuePair<int?, string>> BaseConditionFilterOptions { get; } = new List<KeyValuePair<int?, string>>
    {
      new KeyValuePair<int?, string>(null, "Все состояния"),
      new KeyValuePair<int?, string>(-1, "Плохо"),
      new KeyValuePair<int?, string>(0, "Норма"),
      new KeyValuePair<int?, string>(1, "Хорошо")
    };

    public List<KeyValuePair<int?, string>> Level2FilterOptions { get; private set; } = new List<KeyValuePair<int?, string>>();

    public List<KeyValuePair<int, string>> PerceptionActionFilterOptions { get; } = new List<KeyValuePair<int, string>>();
    public List<KeyValuePair<int, string>> ActionFilterOptions { get; } = new List<KeyValuePair<int, string>>();

    public List<KeyValuePair<int?, string>> PageSizeOptions { get; } = new List<KeyValuePair<int?, string>>
    {
      new KeyValuePair<int?, string>(100, "100"),
      new KeyValuePair<int?, string>(500, "500"),
      new KeyValuePair<int?, string>(1000, "1000"),
      new KeyValuePair<int?, string>(5000, "5000"),
      new KeyValuePair<int?, string>(10000, "10000"),
      new KeyValuePair<int?, string>(null, "Все")
    };

    private int? _selectedBaseConditionFilter;
    private int? _selectedLevel2Filter;
    private int _selectedPerceptionActionFilterId;
    private int _selectedActionFilterId;
    private int? _selectedPageSize = 100;

    public int? SelectedBaseConditionFilter
    {
      get => _selectedBaseConditionFilter;
      set { _selectedBaseConditionFilter = value; OnPropertyChanged(nameof(SelectedBaseConditionFilter)); }
    }

    public int? SelectedLevel2Filter
    {
      get => _selectedLevel2Filter;
      set { _selectedLevel2Filter = value; OnPropertyChanged(nameof(SelectedLevel2Filter)); }
    }

    public int SelectedPerceptionActionFilterId
    {
      get => _selectedPerceptionActionFilterId;
      set { _selectedPerceptionActionFilterId = value; OnPropertyChanged(nameof(SelectedPerceptionActionFilterId)); }
    }

    public int SelectedActionFilterId
    {
      get => _selectedActionFilterId;
      set { _selectedActionFilterId = value; OnPropertyChanged(nameof(SelectedActionFilterId)); }
    }

    public int? SelectedPageSize
    {
      get => _selectedPageSize;
      set
      {
        _selectedPageSize = value;
        OnPropertyChanged(nameof(SelectedPageSize));
        RebuildFiltered();
      }
    }

    public string FilterPhrasePerceptionInput { get => _filterPhrasePerceptionInput; set { _filterPhrasePerceptionInput = value ?? string.Empty; OnPropertyChanged(nameof(FilterPhrasePerceptionInput)); } }
    public string FilterPhraseActionInput { get => _filterPhraseActionInput; set { _filterPhraseActionInput = value ?? string.Empty; OnPropertyChanged(nameof(FilterPhraseActionInput)); } }

    private string _filterPhrasePerceptionInput = string.Empty;
    private string _filterPhraseActionInput = string.Empty;

    public string FilterNodePid { get => _filterNodePid; set { _filterNodePid = value ?? string.Empty; OnPropertyChanged(nameof(FilterNodePid)); } }
    public string FilterEffect { get => _filterEffect; set { _filterEffect = value ?? string.Empty; OnPropertyChanged(nameof(FilterEffect)); } }
    public string FilterCount { get => _filterCount; set { _filterCount = value ?? string.Empty; OnPropertyChanged(nameof(FilterCount)); } }
    public string FilterStimulsEffect { get => _filterStimulsEffect; set { _filterStimulsEffect = value ?? string.Empty; OnPropertyChanged(nameof(FilterStimulsEffect)); } }

    private string _filterNodePid = string.Empty;
    private string _filterEffect = string.Empty;
    private string _filterCount = string.Empty;
    private string _filterStimulsEffect = string.Empty;

    private string _appliedNodePid = string.Empty;
    private string _appliedEffect = string.Empty;
    private string _appliedCount = string.Empty;
    private string _appliedStimulsEffect = string.Empty;

    private int? _appliedBaseCondition;
    private int? _appliedLevel2;
    private int _appliedPerceptionActionFilterId;
    private int _appliedActionFilterId;
    private string _appliedPhrasePerception = string.Empty;
    private string _appliedPhraseAction = string.Empty;

    private int _lastFilteredTotal;

    public string DisplayCountText
    {
      get
      {
        int filtered = _lastFilteredTotal;
        int shown = Rules?.Count ?? 0;
        return filtered == shown ? $"Показано: {shown}" : $"Показано: {shown} из {filtered}";
      }
    }

    public ICommand ApplyFiltersCommand { get; }
    public ICommand ClearFiltersCommand { get; }
    public ICommand RefreshCommand { get; }

    public MotorRulesViewModel(
      EpisodicMemorySystem episodicMemory,
      GomeostasSystem gomeostas,
      EmotionsImageSystem emotionsImage,
      InfluenceActionSystem influenceAction,
      AdaptiveActionsSystem adaptiveActions,
      ProblemTreeSystem problemTree,
      InfluenceActionsImagesSystem influenceActionsImages,
      ActionsImagesSystem actionsImages,
      SensorySystem sensorySystem,
      AutomatizmTreeSystem automatizmTree,
      VerbalBrocaImagesSystem verbalBrocaImages)
    {
      _episodicMemory = episodicMemory;
      _gomeostas = gomeostas;
      _emotionsImage = emotionsImage;
      _influenceActionsImages = influenceActionsImages;
      _actionsImages = actionsImages;
      _presentation = new EpisodicMemoryNodePresentation(
        gomeostas, emotionsImage, influenceAction, adaptiveActions, problemTree,
        influenceActionsImages, actionsImages, sensorySystem,
        automatizmTree, verbalBrocaImages);

      LoadLevel2FilterOptions();

      PerceptionActionFilterOptions.Add(new KeyValuePair<int, string>(0, "Все действия"));
      var influenceActions = influenceAction?.GetAllInfluenceActions();
      if (influenceActions != null)
        foreach (var a in influenceActions)
          PerceptionActionFilterOptions.Add(new KeyValuePair<int, string>(a.Id, a.Name));

      ActionFilterOptions.Add(new KeyValuePair<int, string>(0, "Все действия"));
      var actionsList = adaptiveActions?.GetAllAdaptiveActions()?.ToList() ?? new List<AdaptiveActionsSystem.AdaptiveAction>();
      foreach (var a in actionsList)
        ActionFilterOptions.Add(new KeyValuePair<int, string>(a.Id, a.Name));

      ApplyFiltersCommand = new RelayCommand(_ => ApplyFilters());
      ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
      RefreshCommand = new RelayCommand(_ => ReloadAll());

      ReloadAll();
    }

    private void LoadLevel2FilterOptions()
    {
      Level2FilterOptions = new List<KeyValuePair<int?, string>> { new KeyValuePair<int?, string>(null, "Все контексты") };
      var dict = _gomeostas?.GetAllBehaviorStyles();
      if (dict != null)
        foreach (var kv in dict)
          Level2FilterOptions.Add(new KeyValuePair<int?, string>(kv.Key, kv.Value.Name));
      OnPropertyChanged(nameof(Level2FilterOptions));
    }

    private void ApplyFilters()
    {
      _appliedNodePid = (FilterNodePid ?? string.Empty).Trim();
      _appliedEffect = (FilterEffect ?? string.Empty).Trim();
      _appliedCount = (FilterCount ?? string.Empty).Trim();
      _appliedStimulsEffect = (FilterStimulsEffect ?? string.Empty).Trim();
      _appliedBaseCondition = SelectedBaseConditionFilter;
      _appliedLevel2 = SelectedLevel2Filter;
      _appliedPerceptionActionFilterId = SelectedPerceptionActionFilterId;
      _appliedActionFilterId = SelectedActionFilterId;
      _appliedPhrasePerception = (FilterPhrasePerceptionInput ?? string.Empty).Trim();
      _appliedPhraseAction = (FilterPhraseActionInput ?? string.Empty).Trim();
      RebuildFiltered();
    }

    private void ClearFilters()
    {
      FilterNodePid = FilterEffect = FilterCount = FilterStimulsEffect = string.Empty;
      FilterPhrasePerceptionInput = FilterPhraseActionInput = string.Empty;
      SelectedBaseConditionFilter = null;
      SelectedLevel2Filter = null;
      SelectedPerceptionActionFilterId = 0;
      SelectedActionFilterId = 0;
      _appliedNodePid = _appliedEffect = _appliedCount = _appliedStimulsEffect = string.Empty;
      _appliedPhrasePerception = _appliedPhraseAction = string.Empty;
      _appliedBaseCondition = null;
      _appliedLevel2 = null;
      _appliedPerceptionActionFilterId = 0;
      _appliedActionFilterId = 0;
      RebuildFiltered();
    }

    private void ReloadAll()
    {
      _allRows.Clear();
      if (IsMotorRulesAvailable && _episodicMemory?.Tree != null)
      {
        foreach (var child in _episodicMemory.Tree.Children ?? Enumerable.Empty<EpisodicMemoryNode>())
          CollectRuleRows(child, _allRows);
      }

      HistoryFrames = EpisodicHistoryFramesLoader.Load(_episodicMemory, _presentation);
      RebuildFiltered();
    }

    private void CollectRuleRows(EpisodicMemoryNode node, List<MotorEpisodicRuleRow> acc)
    {
      if (node == null) return;
      if (node.Params != null)
      {
        var emoImg = _emotionsImage?.GetEmotionsImage(node.EmotionID);
        acc.Add(new MotorEpisodicRuleRow
        {
          Id = node.ID,
          BaseId = node.BaseID,
          EmotionId = node.EmotionID,
          NodePid = node.NodePID,
          TriggerId = node.TriggerId,
          ActionId = node.ActionId,
          Effect = node.Params.Effect,
          Count = node.Params.Count,
          StimulsEffect = node.Params.StimulsEffect,
          IsTeacher = node.Params.IsTeacher,
          EmotionContextIds = emoImg?.BaseStylesList != null ? new List<int>(emoImg.BaseStylesList) : new List<int>(),
          TriggerTooltipText = _presentation.GetTriggerTooltip(node.TriggerId),
          ActionTooltipText = _presentation.GetActionTooltip(node.ActionId),
          NodePidTooltipText = _presentation.GetNodePidConditionsTooltip(node.NodePID)
        });
      }
      if (node.Children != null)
      {
        foreach (var c in node.Children)
          CollectRuleRows(c, acc);
      }
    }

    private bool RowMatchesAppliedFilters(MotorEpisodicRuleRow r)
    {
      if (_appliedBaseCondition.HasValue && r.BaseId != _appliedBaseCondition.Value)
        return false;

      if (_appliedLevel2.HasValue)
      {
        if (r.EmotionId == 0) return false;
        var img = _emotionsImage?.GetEmotionsImage(r.EmotionId);
        if (img?.BaseStylesList == null || !img.BaseStylesList.Contains(_appliedLevel2.Value))
          return false;
      }

      if (_appliedPerceptionActionFilterId != 0)
      {
        if (r.TriggerId == 0) return false;
        var actIds = _influenceActionsImages?.GetInfluenceActionIds(r.TriggerId);
        if (actIds != null && actIds.Contains(_appliedPerceptionActionFilterId)) { }
        else
        {
          var actImg = _actionsImages?.GetActionsImage(r.TriggerId);
          if (actImg?.ActIdList == null || !actImg.ActIdList.Contains(_appliedPerceptionActionFilterId))
            return false;
        }
      }

      if (_appliedActionFilterId != 0)
      {
        if (r.ActionId == 0) return false;
        var actImg = _actionsImages?.GetActionsImage(r.ActionId);
        if (actImg?.ActIdList == null || !actImg.ActIdList.Contains(_appliedActionFilterId))
          return false;
      }

      if (!string.IsNullOrWhiteSpace(_appliedPhrasePerception))
      {
        if (r.TriggerId == 0) return false;
        string phrase = _presentation.GetTriggerPhraseText(r.TriggerId);
        if (string.IsNullOrEmpty(phrase) || phrase.IndexOf(_appliedPhrasePerception, StringComparison.OrdinalIgnoreCase) < 0)
          return false;
      }

      if (!string.IsNullOrWhiteSpace(_appliedPhraseAction))
      {
        if (r.ActionId == 0) return false;
        string phrase = _presentation.GetActionPhraseText(r.ActionId);
        if (string.IsNullOrEmpty(phrase) || phrase.IndexOf(_appliedPhraseAction, StringComparison.OrdinalIgnoreCase) < 0)
          return false;
      }

      return true;
    }

    private void RebuildFiltered()
    {
      IEnumerable<MotorEpisodicRuleRow> q = _allRows;
      if (!string.IsNullOrEmpty(_appliedNodePid))
        q = q.Where(r => MatchNum(r.NodePid, _appliedNodePid));
      if (!string.IsNullOrEmpty(_appliedEffect))
        q = q.Where(r => MatchNum(r.Effect, _appliedEffect));
      if (!string.IsNullOrEmpty(_appliedCount))
        q = q.Where(r => MatchNum(r.Count, _appliedCount));
      if (!string.IsNullOrEmpty(_appliedStimulsEffect))
        q = q.Where(r => MatchNum(r.StimulsEffect, _appliedStimulsEffect));

      q = q.Where(RowMatchesAppliedFilters);

      var list = q.ToList();
      _lastFilteredTotal = list.Count;
      int cap = SelectedPageSize ?? int.MaxValue;
      if (cap < list.Count)
        list = list.Take(cap).ToList();

      Rules = new ObservableCollection<MotorEpisodicRuleRow>(list);
      OnPropertyChanged(nameof(DisplayCountText));
    }

    private static bool MatchNum(int value, string filter)
    {
      var s = value.ToString();
      return s.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
    }
  }
}
