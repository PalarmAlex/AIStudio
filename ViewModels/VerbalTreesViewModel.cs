using AIStudio.Common;
using ISIDA.Common;
using ISIDA.Gomeostas;
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
using System.Windows.Threading;

namespace AIStudio.ViewModels
{
  public class VerbalTreesViewModel : INotifyPropertyChanged
  {
    #region Поля и свойства

    private readonly VerbalSensorChannel _verbalChannel;
    private readonly GomeostasSystem _gomeostas;

    private string _inputText;
    private string _wordSearchText;
    private string _phraseSearchText;
    private bool _authoritativeMode;
    private int _maxPhraseLength;
    private int _recognitionThreshold;
    private int _currentAgentStage;
    public bool IsStageZero => _currentAgentStage == 0;

    private ObservableCollection<WordNode> _visibleWordNodes;
    private ObservableCollection<PhraseNode> _visiblePhraseNodes;
    private IEnumerable<WordNode> _wordTreeStructure;
    private IEnumerable<PhraseNode> _phraseTreeStructure;
    private ObservableCollection<WordNode> _allWordNodes;
    private ObservableCollection<PhraseNode> _allPhraseNodes;
    private ObservableCollection<PhraseNode> _filteredPhraseNodes;
    private int? _wordDisplayLimit = 1000;
    private int? _phraseDisplayLimit = 1000;
    private SensorTypeFilterOption _selectedNodeTypeFilter;

    private const int VerbalTypeGroupId = -1000;
    private const int CommandTypeGroupId = -1001;

    #region Блокировка страницы в зависимости от стажа

    public bool IsEditingEnabled => IsStageZero && !GlobalTimer.IsPulsationRunning;
    public string PulseWarningMessage =>
        !IsStageZero ? "[КРИТИЧНО] Очистка сенсоров доступна только в стадии 0" : string.Empty;
    public Brush WarningMessageColor =>
        !IsStageZero ? Brushes.Red :
        Brushes.Gray;

    #endregion

    public string InputText
    {
      get => _inputText;
      set
      {
        if (_inputText != value)
        {
          _inputText = value;
          OnPropertyChanged(nameof(InputText));
        }
      }
    }

    public string WordSearchText
    {
      get => _wordSearchText;
      set
      {
        if (_wordSearchText != value)
        {
          _wordSearchText = value;
          OnPropertyChanged(nameof(WordSearchText));
        }
      }
    }

    public string PhraseSearchText
    {
      get => _phraseSearchText;
      set
      {
        if (_phraseSearchText != value)
        {
          _phraseSearchText = value;
          OnPropertyChanged(nameof(PhraseSearchText));
        }
      }
    }

    public bool AuthoritativeMode
    {
      get => _authoritativeMode;
      set
      {
        if (_authoritativeMode != value)
        {
          _authoritativeMode = value;
          _verbalChannel.AuthoritativeMode = value;
          OnPropertyChanged(nameof(AuthoritativeMode));
        }
      }
    }

    public int MaxPhraseLength
    {
      get => _maxPhraseLength;
      set
      {
        if (_maxPhraseLength != value && value > 0)
        {
          _maxPhraseLength = value;
          OnPropertyChanged(nameof(MaxPhraseLength));
        }
      }
    }

    public int RecognitionThreshold
    {
      get => _recognitionThreshold;
      set
      {
        if (_recognitionThreshold != value && value > 0)
        {
          _recognitionThreshold = value;
          _verbalChannel.RecognitionThreshold = value;
          OnPropertyChanged(nameof(RecognitionThreshold));
        }
      }
    }

    public ObservableCollection<WordNode> VisibleWordNodes
    {
      get => _visibleWordNodes;
      set
      {
        _visibleWordNodes = value;
        OnPropertyChanged(nameof(VisibleWordNodes));
      }
    }

    public ObservableCollection<PhraseNode> VisiblePhraseNodes
    {
      get => _visiblePhraseNodes;
      set
      {
        _visiblePhraseNodes = value;
        OnPropertyChanged(nameof(VisiblePhraseNodes));
      }
    }

    public IEnumerable<WordNode> WordTreeStructure
    {
      get => _wordTreeStructure;
      private set
      {
        if (_wordTreeStructure != value)
        {
          _wordTreeStructure = value;
          OnPropertyChanged(nameof(WordTreeStructure));
        }
      }
    }

    public IEnumerable<PhraseNode> PhraseTreeStructure
    {
      get => _phraseTreeStructure;
      private set
      {
        if (_phraseTreeStructure != value)
        {
          _phraseTreeStructure = value;
          OnPropertyChanged(nameof(PhraseTreeStructure));
        }
      }
    }

    public Dictionary<int, string> Words => _verbalChannel?.GetAllWords() ?? new Dictionary<int, string>();
    public Dictionary<int, string> Phrases
    {
      get
      {
        var result = new Dictionary<int, string>();
        try
        {
          if (_verbalChannel?.PhraseTree?.Nodes == null)
            return result;

          foreach (var node in _verbalChannel.PhraseTree.Nodes.Values)
          {
            if (node.Id == 0) continue; // пропускаем корневой узел

            string phraseText = _verbalChannel.GetPhraseFromPhraseId(node.Id);
            if (!string.IsNullOrEmpty(phraseText))
              result[node.Id] = phraseText;
          }
        }
        catch (Exception ex)
        {
          Logger.Error($"Ошибка получения паттернов: {ex.Message}");
        }
        return result;
      }
    }
    /// <summary>Варианты лимита отображаемых узлов (только для ускорения загрузки).</summary>
    public static IReadOnlyList<NodeLimitOption> DisplayLimitOptionsStatic { get; } = new List<NodeLimitOption>
    {
      new NodeLimitOption(100),
      new NodeLimitOption(500),
      new NodeLimitOption(1000),
      new NodeLimitOption(5000),
      new NodeLimitOption(10000),
      new NodeLimitOption(null) // Все
    };

    public IReadOnlyList<NodeLimitOption> DisplayLimitOptions => DisplayLimitOptionsStatic;

    public NodeLimitOption WordDisplayLimit
    {
      get => DisplayLimitOptionsStatic.FirstOrDefault(o => o.Value == _wordDisplayLimit) ?? DisplayLimitOptionsStatic[DisplayLimitOptionsStatic.Count - 1];
      set
      {
        if (value == null) return;
        if (_wordDisplayLimit == value.Value) return;
        _wordDisplayLimit = value.Value;
        OnPropertyChanged(nameof(WordDisplayLimit));
        ApplyWordDisplayLimit();
      }
    }

    public NodeLimitOption PhraseDisplayLimit
    {
      get => DisplayLimitOptionsStatic.FirstOrDefault(o => o.Value == _phraseDisplayLimit) ?? DisplayLimitOptionsStatic[DisplayLimitOptionsStatic.Count - 1];
      set
      {
        if (value == null) return;
        if (_phraseDisplayLimit == value.Value) return;
        _phraseDisplayLimit = value.Value;
        OnPropertyChanged(nameof(PhraseDisplayLimit));
        ApplyPhraseDisplayLimit();
      }
    }

    public int WordsCount => Words.Count;

    public int VerbalWordsCount =>
        Words.Count(w => GetWordNodeType(w.Key) == SensorNodeType.Verbal);

    public int CommandWordsCount =>
        Words.Count(w => GetWordNodeType(w.Key) == SensorNodeType.Command);

    public string WordsCountSummary =>
        $"Всего токенов: {WordsCount} (верб.: {VerbalWordsCount}, ком.: {CommandWordsCount})";

    public int PhrasesCount
    {
      get
      {
        try
        {
          return _verbalChannel.PhraseTree.Nodes.Count(n => n.Key != 0);
        }
        catch
        {
          return 0;
        }
      }
    }

    public int VerbalPhrasesCount
    {
      get
      {
        try
        {
          return _verbalChannel.PhraseTree.Nodes.Count(n =>
              n.Key != 0 && n.Value.NodeType == SensorNodeType.Verbal);
        }
        catch
        {
          return 0;
        }
      }
    }

    public int CommandPhrasesCount
    {
      get
      {
        try
        {
          return _verbalChannel.PhraseTree.Nodes.Count(n =>
              n.Key != 0 && BelongsToPatternForest(n.Value, SensorNodeType.Command));
        }
        catch
        {
          return 0;
        }
      }
    }

    public string PhrasesCountSummary =>
        $"Всего паттернов: {PhrasesCount} (верб.: {VerbalPhrasesCount}, ком.: {CommandPhrasesCount})";

    /// <summary>Варианты фильтра по типу узла сенсорного дерева.</summary>
    public static IReadOnlyList<SensorTypeFilterOption> NodeTypeFilterOptionsStatic { get; } =
        new List<SensorTypeFilterOption>
        {
          new SensorTypeFilterOption(null, "Все"),
          new SensorTypeFilterOption(SensorNodeType.Verbal, "Вербальные"),
          new SensorTypeFilterOption(SensorNodeType.Command, "Командные")
        };

    public IReadOnlyList<SensorTypeFilterOption> NodeTypeFilterOptions => NodeTypeFilterOptionsStatic;

    public SensorTypeFilterOption SelectedNodeTypeFilter
    {
      get => _selectedNodeTypeFilter ?? NodeTypeFilterOptionsStatic[0];
      set
      {
        if (value == null || _selectedNodeTypeFilter == value)
          return;
        _selectedNodeTypeFilter = value;
        OnPropertyChanged(nameof(SelectedNodeTypeFilter));
        ApplyNodeTypeFilter();
      }
    }

    #endregion

    #region Команды

    public ICommand AddTextCommand { get; }
    public ICommand SearchWordCommand { get; }
    public ICommand SearchPhraseCommand { get; }
    public ICommand RemoveAllCommand { get; }
    public ICommand ResetWordSearchCommand { get; }
    public ICommand ResetPhraseSearchCommand { get; }

    #endregion

    #region События

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPulsationStateChanged()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        OnPropertyChanged(nameof(IsEditingEnabled));
        OnPropertyChanged(nameof(PulseWarningMessage));
        OnPropertyChanged(nameof(WarningMessageColor));
      });
    }

    #endregion

    #region Конструктор и инициализация

    public VerbalTreesViewModel(GomeostasSystem gomeostas, VerbalSensorChannel verbalChannel)
    {
      _verbalChannel = verbalChannel ?? throw new ArgumentNullException(nameof(verbalChannel));
      _gomeostas = gomeostas ?? throw new ArgumentNullException(nameof(gomeostas));

      var agentInfo = _gomeostas.GetAgentState();
      _currentAgentStage = agentInfo?.EvolutionStage ?? 0;

      _authoritativeMode = _verbalChannel.AuthoritativeMode;
      _recognitionThreshold = _verbalChannel.RecognitionThreshold;
      _maxPhraseLength = _verbalChannel.MaxPhraseLength;

      _allWordNodes = new ObservableCollection<WordNode>(LoadInitialWordNodes(_wordDisplayLimit));
      _allPhraseNodes = new ObservableCollection<PhraseNode>(LoadInitialPhraseNodes(_phraseDisplayLimit));
      _filteredPhraseNodes = new ObservableCollection<PhraseNode>(_allPhraseNodes);

      VisibleWordNodes = _allWordNodes;
      VisiblePhraseNodes = _filteredPhraseNodes;

      AddTextCommand = new RelayCommand(AddText);
      SearchWordCommand = new RelayCommand(SearchWord);
      SearchPhraseCommand = new RelayCommand(SearchPhraseWrapper);
      RemoveAllCommand = new RelayCommand(RemoveAllTrees);
      ResetWordSearchCommand = new RelayCommand(ResetWordSearch);
      ResetPhraseSearchCommand = new RelayCommand(ResetPhraseSearch);

      GlobalTimer.PulsationStateChanged += OnPulsationStateChanged;

      InitializeTrees();

      CollapseAllWordNodes();
      CollapseAllPhraseNodes();
    }

    private void InitializeTrees()
    {
      VisibleWordNodes = new ObservableCollection<WordNode>(LoadInitialWordNodes(_wordDisplayLimit));
      VisiblePhraseNodes = new ObservableCollection<PhraseNode>(LoadInitialPhraseNodes(_phraseDisplayLimit));

      WordTreeStructure = GetFullWordTreeStructure();
    }

    private void ApplyWordDisplayLimit()
    {
      _allWordNodes = new ObservableCollection<WordNode>(LoadInitialWordNodes(_wordDisplayLimit));
      if (string.IsNullOrWhiteSpace(WordSearchText))
        VisibleWordNodes = _allWordNodes;
      else
        FilterWordNodes(WordSearchText);
      CollapseAllWordNodes();
      OnPropertyChanged(nameof(VisibleWordNodes));
    }

    private void ApplyPhraseDisplayLimit()
    {
      _allPhraseNodes = new ObservableCollection<PhraseNode>(LoadInitialPhraseNodes(_phraseDisplayLimit));
      _filteredPhraseNodes = new ObservableCollection<PhraseNode>(_allPhraseNodes);
      if (string.IsNullOrWhiteSpace(PhraseSearchText))
        VisiblePhraseNodes = _filteredPhraseNodes;
      else
        FilterPhraseNodes(PhraseSearchText);
      CollapseAllPhraseNodes();
      OnPropertyChanged(nameof(VisiblePhraseNodes));
    }

    private void ApplyNodeTypeFilter()
    {
      ApplyWordDisplayLimit();
      ApplyPhraseDisplayLimit();
      OnPropertyChanged(nameof(WordsCountSummary));
      OnPropertyChanged(nameof(PhrasesCountSummary));
      OnPropertyChanged(nameof(VerbalWordsCount));
      OnPropertyChanged(nameof(CommandWordsCount));
      OnPropertyChanged(nameof(VerbalPhrasesCount));
      OnPropertyChanged(nameof(CommandPhrasesCount));
    }

    private void UpdateAgentStage()
    {
      var agentInfo = _gomeostas.GetAgentState();
      var newStage = agentInfo?.EvolutionStage ?? 0;

      if (_currentAgentStage != newStage)
      {
        _currentAgentStage = newStage;

        Application.Current.Dispatcher.Invoke(() =>
        {
          OnPropertyChanged(nameof(IsStageZero));
          OnPropertyChanged(nameof(IsEditingEnabled));
          OnPropertyChanged(nameof(PulseWarningMessage));
          OnPropertyChanged(nameof(WarningMessageColor));
        });
      }
    }

    #endregion

    #region Методы работы с деревьями

    private SensorNodeType GetWordNodeType(int wordId)
    {
      if (_verbalChannel.WordTree.Nodes.TryGetValue(wordId, out var node))
        return node.NodeType;
      return SensorNodeType.Verbal;
    }

    /// <summary>
    /// Содержит ли паттерн командные токены (по типам токенов в WordTree).
    /// </summary>
    private SensorNodeType GetPatternNodeType(SensorTree<int, int>.TreeNode<int> node)
    {
      var current = node;
      while (current != null && current.Id != 0)
      {
        if (current.Element != 0 &&
            GetWordNodeType(current.Element) == SensorNodeType.Command)
          return SensorNodeType.Command;
        current = current.Parent;
      }
      return SensorNodeType.Verbal;
    }

    /// <summary>
    /// Принадлежность узла PhraseTree секции UI.
    /// Verbal — по NodeType ветки; Command — только ветки Command с реальными командными токенами
    /// (зеркальные Command-ветки с вербальными word id, как в VELUM, не показываются).
    /// </summary>
    private bool BelongsToPatternForest(SensorTree<int, int>.TreeNode<int> node, SensorNodeType forestType)
    {
      if (node.Id == 0)
        return false;

      if (forestType == SensorNodeType.Verbal)
        return node.NodeType == SensorNodeType.Verbal;

      return node.NodeType == SensorNodeType.Command &&
             GetPatternNodeType(node) == SensorNodeType.Command;
    }

    private bool ShouldShowType(SensorNodeType type)
    {
      var filter = SelectedNodeTypeFilter?.Value;
      return !filter.HasValue || filter.Value == type;
    }

    private IEnumerable<WordNode> LoadInitialWordNodes(int? maxNodes = null)
    {
      return BuildWordTreeRoots(_verbalChannel.GetAllWords(), maxNodes);
    }

    private List<WordNode> BuildWordTreeRoots(Dictionary<int, string> wordsSource, int? maxNodes)
    {
      var result = new List<WordNode>();
      int totalAdded = 0;

      if (ShouldShowType(SensorNodeType.Verbal))
      {
        var verbalEntries = wordsSource
            .Where(w => GetWordNodeType(w.Key) == SensorNodeType.Verbal)
            .OrderBy(w => w.Value)
            .ToList();
        var verbalGroup = CreateVerbalTypeGroup(verbalEntries, ref totalAdded, maxNodes);
        if (verbalGroup != null)
          result.Add(verbalGroup);
      }

      if (ShouldShowType(SensorNodeType.Command))
      {
        var commandEntries = wordsSource
            .Where(w => GetWordNodeType(w.Key) == SensorNodeType.Command)
            .OrderBy(w => w.Value)
            .ToList();
        var commandGroup = CreateCommandTypeGroup(commandEntries, ref totalAdded, maxNodes);
        if (commandGroup != null)
          result.Add(commandGroup);
      }

      return result;
    }

    private WordNode CreateVerbalTypeGroup(
        List<KeyValuePair<int, string>> verbalEntries,
        ref int totalAdded,
        int? maxNodes)
    {
      if (verbalEntries.Count == 0)
        return null;

      var typeGroup = new WordNode
      {
        Id = VerbalTypeGroupId,
        Text = "Вербальные",
        IsTypeGroup = true,
        HasChildren = true,
        NodeType = SensorNodeType.Verbal
      };

      foreach (var letterGroup in verbalEntries
          .GroupBy(w => char.ToUpper(GetFirstChar(w.Value)))
          .OrderBy(g => g.Key))
      {
        if (maxNodes.HasValue && totalAdded >= maxNodes.Value)
          break;

        var letterNode = new WordNode
        {
          Id = 0,
          Text = letterGroup.Key.ToString(),
          IsLetter = true,
          HasChildren = true,
          NodeType = SensorNodeType.Verbal
        };

        foreach (var word in letterGroup)
        {
          if (maxNodes.HasValue && totalAdded >= maxNodes.Value)
            break;
          letterNode.Children.Add(new WordNode
          {
            Id = word.Key,
            Text = word.Value,
            HasChildren = false,
            NodeType = SensorNodeType.Verbal
          });
          totalAdded++;
        }

        if (letterNode.Children.Count > 0)
          typeGroup.Children.Add(letterNode);
      }

      return typeGroup.Children.Count > 0 ? typeGroup : null;
    }

    private WordNode CreateCommandTypeGroup(
        List<KeyValuePair<int, string>> commandEntries,
        ref int totalAdded,
        int? maxNodes)
    {
      if (commandEntries.Count == 0)
        return null;

      var typeGroup = new WordNode
      {
        Id = CommandTypeGroupId,
        Text = "Командные",
        IsTypeGroup = true,
        HasChildren = true,
        NodeType = SensorNodeType.Command
      };

      foreach (var prefixGroup in commandEntries
          .GroupBy(w => GetCommandPrefix(w.Value))
          .OrderBy(g => g.Key))
      {
        if (maxNodes.HasValue && totalAdded >= maxNodes.Value)
          break;

        var prefixNode = new WordNode
        {
          Id = 0,
          Text = prefixGroup.Key,
          IsPrefixGroup = true,
          HasChildren = true,
          NodeType = SensorNodeType.Command
        };

        foreach (var word in prefixGroup.OrderBy(w => w.Value))
        {
          if (maxNodes.HasValue && totalAdded >= maxNodes.Value)
            break;
          prefixNode.Children.Add(new WordNode
          {
            Id = word.Key,
            Text = word.Value,
            HasChildren = false,
            NodeType = SensorNodeType.Command
          });
          totalAdded++;
        }

        if (prefixNode.Children.Count > 0)
          typeGroup.Children.Add(prefixNode);
      }

      return typeGroup.Children.Count > 0 ? typeGroup : null;
    }

    private static char GetFirstChar(string value)
    {
      if (string.IsNullOrEmpty(value))
        return '?';
      return value[0];
    }

    private static string GetCommandPrefix(string token)
    {
      if (string.IsNullOrEmpty(token))
        return "прочие";
      int idx = token.IndexOf(':');
      return idx >= 0
          ? token.Substring(0, idx + 1).ToLowerInvariant()
          : "прочие";
    }

    private ObservableCollection<PhraseNode> LoadInitialPhraseNodes(int? maxNodes = null)
    {
      var result = new ObservableCollection<PhraseNode>();

      if (ShouldShowType(SensorNodeType.Verbal))
      {
        var verbalForest = BuildPhraseForestForType(SensorNodeType.Verbal, maxNodes);
        if (verbalForest.Count > 0)
        {
          result.Add(CreatePhraseTypeGroup("Вербальные", VerbalTypeGroupId, SensorNodeType.Verbal, verbalForest));
        }
      }

      if (ShouldShowType(SensorNodeType.Command))
      {
        var commandForest = BuildPhraseForestForType(SensorNodeType.Command, maxNodes);
        if (commandForest.Count > 0)
        {
          result.Add(CreatePhraseTypeGroup("Командные", CommandTypeGroupId, SensorNodeType.Command, commandForest));
        }
      }

      return result;
    }

    private PhraseNode CreatePhraseTypeGroup(
        string title,
        int groupId,
        SensorNodeType nodeType,
        ObservableCollection<PhraseNode> children)
    {
      return new PhraseNode
      {
        Id = groupId,
        Text = title,
        IsTypeGroup = true,
        HasChildren = children.Count > 0,
        NodeType = nodeType,
        Children = children
      };
    }

    private ObservableCollection<PhraseNode> BuildPhraseForestForType(SensorNodeType nodeType, int? maxNodes)
    {
      var rootNodes = new ObservableCollection<PhraseNode>();

      try
      {
        var phraseNodes = _verbalChannel.PhraseTree.Nodes;
        var nodeDict = new Dictionary<int, PhraseNode>();

        foreach (var node in phraseNodes.Values)
        {
          if (!BelongsToPatternForest(node, nodeType))
            continue;

          string phraseText = _verbalChannel.GetPhraseFromPhraseId(node.Id);
          nodeDict[node.Id] = new PhraseNode
          {
            Id = node.Id,
            Text = GetLastWordFromPhrase(phraseText),
            FullPath = phraseText,
            HasChildren = node.Children.Any(c => BelongsToPatternForest(c, nodeType)),
            NodeType = nodeType
          };
        }

        var idsToInclude = (HashSet<int>)null;
        if (maxNodes.HasValue && nodeDict.Count > maxNodes.Value)
        {
          idsToInclude = new HashSet<int>();
          var roots = phraseNodes.Values
              .Where(n => BelongsToPatternForest(n, nodeType) &&
                          (n.Parent == null || n.Parent.Id == 0))
              .OrderBy(n => n.Id)
              .ToList();
          int count = 0;
          foreach (var r in roots)
          {
            if (count >= maxNodes.Value)
              break;
            CountAndAddIdsForType(r, idsToInclude, maxNodes.Value, ref count, nodeType);
          }
        }

        foreach (var node in phraseNodes.Values)
        {
          if (!BelongsToPatternForest(node, nodeType))
            continue;
          if (idsToInclude != null && !idsToInclude.Contains(node.Id))
            continue;

          var phraseNode = nodeDict[node.Id];

          if (node.Parent != null && node.Parent.Id != 0 &&
              BelongsToPatternForest(node.Parent, nodeType))
          {
            if (nodeDict.TryGetValue(node.Parent.Id, out var parentNode) &&
                (idsToInclude == null || idsToInclude.Contains(node.Parent.Id)))
            {
              parentNode.Children.Add(phraseNode);
            }
          }
          else if (node.Parent == null || node.Parent.Id == 0)
          {
            rootNodes.Add(phraseNode);
          }
        }

        var sortedRoots = rootNodes.OrderBy(n => n.Text).ToList();
        rootNodes.Clear();
        foreach (var node in sortedRoots)
          rootNodes.Add(node);
      }
      catch (Exception ex)
      {
        Logger.Error($"Ошибка загрузки дерева паттернов ({nodeType}): {ex.Message}");
      }

      return rootNodes;
    }

    private void CountAndAddIdsForType(
        SensorTree<int, int>.TreeNode<int> node,
        HashSet<int> ids,
        int max,
        ref int count,
        SensorNodeType nodeType)
    {
      if (count >= max || !BelongsToPatternForest(node, nodeType))
        return;
      if (ids.Contains(node.Id))
        return;
      ids.Add(node.Id);
      count++;
      foreach (var c in node.Children)
      {
        if (count >= max)
          return;
        CountAndAddIdsForType(c, ids, max, ref count, nodeType);
      }
    }

    /// <summary>
    /// Вспомогательный метод для получения последнего токена из паттерна
    /// </summary>
    private string GetLastWordFromPhrase(string phrase)
    {
      if (string.IsNullOrEmpty(phrase))
        return string.Empty;

      var words = phrase.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
      return words.Length > 0 ? words[words.Length - 1] : phrase;
    }

    public void LoadWordChildren(int parentId)
    {
      var parent = FindWordNode(VisibleWordNodes, parentId);
      if (parent == null || (parent.Children?.Count ?? 0) > 0)
        return;

      if (parent.IsLetter && parent.NodeType == SensorNodeType.Verbal)
      {
        var words = _verbalChannel.GetAllWords()
            .Where(w => GetWordNodeType(w.Key) == SensorNodeType.Verbal &&
                        char.ToUpper(GetFirstChar(w.Value)) == parent.Text[0])
            .Select(w => new WordNode
            {
              Id = w.Key,
              Text = w.Value,
              HasChildren = false,
              NodeType = SensorNodeType.Verbal
            });
        parent.Children = new List<WordNode>(words);
      }
      else if (parent.IsPrefixGroup && parent.NodeType == SensorNodeType.Command)
      {
        var words = _verbalChannel.GetAllWords()
            .Where(w => GetWordNodeType(w.Key) == SensorNodeType.Command &&
                        GetCommandPrefix(w.Value) == parent.Text)
            .Select(w => new WordNode
            {
              Id = w.Key,
              Text = w.Value,
              HasChildren = false,
              NodeType = SensorNodeType.Command
            });
        parent.Children = new List<WordNode>(words);
      }

      OnPropertyChanged(nameof(VisibleWordNodes));
    }

    private WordNode FindWordNode(IEnumerable<WordNode> nodes, int id)
    {
      foreach (var node in nodes)
      {
        if (node.Id == id)
          return node;
        var found = FindWordNode(node.Children, id);
        if (found != null)
          return found;
      }
      return null;
    }

    private PhraseNode FindNode(IEnumerable<PhraseNode> nodes, int id)
    {
      foreach (var node in nodes)
      {
        if (node.Id == id) return node;
        var found = FindNode(node.Children, id);
        if (found != null) return found;
      }
      return null;
    }

    private IEnumerable<WordNode> GetFullWordTreeStructure()
    {
      return BuildWordTreeRoots(_verbalChannel.GetAllWords(), null);
    }

    #endregion

    #region Методы работы с текстом

    private void AddText(object parameter)
    {
      if (string.IsNullOrWhiteSpace(InputText)) return;

      _verbalChannel.ProcessText(InputText, MaxPhraseLength);

      try
      {
        _verbalChannel.WordTree.Save();
        _verbalChannel.PhraseTree.Save();
        _verbalChannel.WordSandbox.Save();
        _verbalChannel.PhraseSandbox.Save();
      }
      catch (Exception ex)
      {
        Logger.Error(ex.Message);
        MessageBox.Show("Не удалось сохранить изменения на диск", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Warning);
      }

      InputText = string.Empty;
      RefreshAllCollections();
    }

    private void RemoveAllTrees(object parameter)
    {
      var result = MessageBox.Show(
          "Вы уверены, что хотите полностью очистить сенсорные деревья?\n\n" +
          "Это действие удалит все токены и паттерны из памяти агента и не может быть отменено.",
          "Подтверждение очистки",
          MessageBoxButton.YesNo,
          MessageBoxImage.Warning,
          MessageBoxResult.No);

      if (result != MessageBoxResult.Yes)
        return;

      try
      {
        // Очищаем все деревья через сенсорный канал
        _verbalChannel.ClearAllTrees();

        // Обновляем интерфейс
        RefreshAllCollections();

        MessageBox.Show("Сенсорные деревья успешно очищены.",
            "Очистка завершена",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
      }
      catch (Exception ex)
      {
        Logger.Info(ex.Message);
        MessageBox.Show($"Не удалось очистить деревья: {ex.Message}",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
      }
    }

    private void RefreshAllCollections()
    {
      UpdateAgentStage();

      // Полностью перезагружаем все коллекции (с учётом лимита отображения)
      _allWordNodes = new ObservableCollection<WordNode>(LoadInitialWordNodes(_wordDisplayLimit));
      _allPhraseNodes = new ObservableCollection<PhraseNode>(LoadInitialPhraseNodes(_phraseDisplayLimit));
      _filteredPhraseNodes = new ObservableCollection<PhraseNode>(_allPhraseNodes);

      // Применяем текущий фильтр
      if (string.IsNullOrWhiteSpace(WordSearchText))
        VisibleWordNodes = _allWordNodes;
      else
        FilterWordNodes(WordSearchText);

      if (string.IsNullOrWhiteSpace(PhraseSearchText))
        VisiblePhraseNodes = _allPhraseNodes;
      else
        FilterPhraseNodes(PhraseSearchText);

      WordTreeStructure = GetFullWordTreeStructure();

      CollapseAllWordNodes();
      CollapseAllPhraseNodes();

      // Принудительно обновляем свойства
      OnPropertyChanged(nameof(Words));
      OnPropertyChanged(nameof(Phrases));
      OnPropertyChanged(nameof(WordsCount));
      OnPropertyChanged(nameof(WordsCountSummary));
      OnPropertyChanged(nameof(PhrasesCount));
      OnPropertyChanged(nameof(PhrasesCountSummary));
      OnPropertyChanged(nameof(VerbalWordsCount));
      OnPropertyChanged(nameof(CommandWordsCount));
      OnPropertyChanged(nameof(VerbalPhrasesCount));
      OnPropertyChanged(nameof(CommandPhrasesCount));
      OnPropertyChanged(nameof(VisibleWordNodes));
      OnPropertyChanged(nameof(VisiblePhraseNodes));
    }

    #endregion

    #region Методы поиска

    private void FilterWordNodes(string searchText)
    {
      if (string.IsNullOrWhiteSpace(searchText))
      {
        VisibleWordNodes = new ObservableCollection<WordNode>(_allWordNodes);
        return;
      }

      var searchLower = searchText.ToLower();
      var matchingWords = _verbalChannel.GetAllWords()
          .Where(w => w.Value.ToLower().Contains(searchLower))
          .ToDictionary(w => w.Key, w => w.Value);

      VisibleWordNodes = new ObservableCollection<WordNode>(
          BuildWordTreeRoots(matchingWords, null));
    }

    private void FilterPhraseNodes(string searchText)
    {
      if (string.IsNullOrWhiteSpace(searchText))
      {
        VisiblePhraseNodes = new ObservableCollection<PhraseNode>(_allPhraseNodes);
        return;
      }

      // При поиске загружаем ВСЕ узлы паттернов (без display limit),
      // иначе узлы за пределами лимита не находятся фильтром
      var allPhraseNodesForSearch = LoadInitialPhraseNodes(null);

      var filtered = new ObservableCollection<PhraseNode>();
      var searchLower = searchText.ToLower();

      foreach (var rootNode in allPhraseNodesForSearch)
      {
        var filteredRoot = FilterPhraseNodeRecursive(rootNode, searchLower);
        if (filteredRoot != null)
        {
          filtered.Add(filteredRoot);
        }
      }

      VisiblePhraseNodes = filtered;
    }

    private PhraseNode FilterPhraseNodeRecursive(PhraseNode node, string searchText)
    {
      // Получаем полный текст паттерна для этого узла
      string fullPhraseText = string.Empty;
      if (node.Id > 0)
      {
        fullPhraseText = _verbalChannel.GetPhraseFromPhraseId(node.Id);
      }

      // Проверяем, содержит ли текущий узел искомый текст
      bool matches = !string.IsNullOrEmpty(fullPhraseText) &&
                     fullPhraseText.ToLower().Contains(searchText);

      // Также проверяем текст узла (для отображения)
      matches = matches || node.Text.ToLower().Contains(searchText);

      // Рекурсивно фильтруем дочерние узлы
      var filteredChildren = new ObservableCollection<PhraseNode>();
      foreach (var child in node.Children)
      {
        var filteredChild = FilterPhraseNodeRecursive(child, searchText);
        if (filteredChild != null)
        {
          filteredChildren.Add(filteredChild);
          matches = true;
        }
      }

      if (matches || filteredChildren.Count > 0)
      {
        return new PhraseNode
        {
          Id = node.Id,
          Text = node.Text,
          FullPath = node.FullPath ?? fullPhraseText,
          HasChildren = filteredChildren.Count > 0,
          IsTypeGroup = node.IsTypeGroup,
          NodeType = node.NodeType,
          Children = filteredChildren
        };
      }

      return null;
    }

    private void SearchWord(object parameter)
    {
      FilterWordNodes(WordSearchText);
      if (!string.IsNullOrEmpty(WordSearchText))
      {
        ExpandAllWordNodes();
      }
      else
      {
        CollapseAllWordNodes();
      }
    }

    private void SearchPhraseWrapper(object parameter)
    {
      FilterPhraseNodes(PhraseSearchText);
      if (!string.IsNullOrEmpty(PhraseSearchText))
      {
        ExpandAllPhraseNodes();
      }
      else
      {
        CollapseAllPhraseNodes();
      }
    }

    private void ResetWordSearch(object parameter)
    {
      WordSearchText = string.Empty;
      FilterWordNodes(string.Empty);
      CollapseAllWordNodes();
    }

    private void ResetPhraseSearch(object parameter)
    {
      PhraseSearchText = string.Empty;
      FilterPhraseNodes(string.Empty);
      CollapseAllPhraseNodes();
    }

    private void CollapseAllWordNodes()
    {
      foreach (var node in VisibleWordNodes)
      {
        node.IsExpanded = false;
        // Рекурсивно сворачиваем все дочерние узлы
        CollapseWordNodeRecursive(node);
      }
      OnPropertyChanged(nameof(VisibleWordNodes));
    }

    private void CollapseWordNodeRecursive(WordNode node)
    {
      foreach (var child in node.Children)
      {
        child.IsExpanded = false;
        CollapseWordNodeRecursive(child);
      }
    }

    private void CollapseAllPhraseNodes()
    {
      foreach (var node in VisiblePhraseNodes)
      {
        node.IsExpanded = false;
        // Рекурсивно сворачиваем все дочерние узлы
        CollapsePhraseNodeRecursive(node);
      }
      OnPropertyChanged(nameof(VisiblePhraseNodes));
    }

    private void CollapsePhraseNodeRecursive(PhraseNode node)
    {
      foreach (var child in node.Children)
      {
        child.IsExpanded = false;
        CollapsePhraseNodeRecursive(child);
      }
    }

    #endregion

    #region Вспомогательные методы

    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void ExpandAllWordNodes()
    {
      foreach (var node in VisibleWordNodes)
      {
        node.IsExpanded = true;
        ExpandWordNodeRecursive(node);
      }
      OnPropertyChanged(nameof(VisibleWordNodes));
    }

    private void ExpandWordNodeRecursive(WordNode node)
    {
      foreach (var child in node.Children)
      {
        child.IsExpanded = true;
        ExpandWordNodeRecursive(child);
      }
    }

    private void ExpandAllPhraseNodes()
    {
      foreach (var node in VisiblePhraseNodes)
      {
        node.IsExpanded = true;
        // Рекурсивно раскрываем все дочерние узлы
        ExpandPhraseNodeRecursive(node);
      }
      OnPropertyChanged(nameof(VisiblePhraseNodes));
    }

    private void ExpandPhraseNodeRecursive(PhraseNode node)
    {
      foreach (var child in node.Children)
      {
        child.IsExpanded = true;
        ExpandPhraseNodeRecursive(child);
      }
    }

    private void SetPhraseNodeExpanded(PhraseNode node, bool isExpanded)
    {
      node.IsExpanded = isExpanded;
      foreach (var child in node.Children)
      {
        SetPhraseNodeExpanded(child, isExpanded);
      }
    }

    #endregion

    #region Вложенные классы

    public class WordNode
    {
      public int Id { get; set; }
      public string Text { get; set; }
      public bool IsLetter { get; set; }
      public bool IsTypeGroup { get; set; }
      public bool IsPrefixGroup { get; set; }
      public bool IsBoldHeader => IsLetter || IsTypeGroup || IsPrefixGroup;
      public SensorNodeType NodeType { get; set; } = SensorNodeType.Verbal;
      public bool HasChildren { get; set; }
      public bool IsExpanded { get; set; }
      public List<WordNode> Children { get; set; } = new List<WordNode>();
    }

    public class PhraseNode
    {
      public int Id { get; set; }
      public string Text { get; set; }
      public string FullPath { get; set; }
      public bool IsTypeGroup { get; set; }
      public SensorNodeType NodeType { get; set; } = SensorNodeType.Verbal;
      public bool HasChildren { get; set; }
      public bool IsExpanded { get; set; }
      public ObservableCollection<PhraseNode> Children { get; set; } =
          new ObservableCollection<PhraseNode>();
    }

    public class TreeItemEventArgs : EventArgs
    {
      public int ItemId { get; }
      public TreeItemEventArgs(int itemId)
      {
        ItemId = itemId;
      }
    }

    /// <summary>Вариант лимита узлов для выпадающего списка (только ускорение загрузки).</summary>
    public class NodeLimitOption
    {
      public int? Value { get; }
      public string DisplayName => Value.HasValue ? Value.Value.ToString() : "Все";

      public NodeLimitOption(int? value) => Value = value;
    }

    /// <summary>Вариант фильтра по типу узла сенсорного дерева.</summary>
    public class SensorTypeFilterOption
    {
      public SensorNodeType? Value { get; }
      public string DisplayName { get; }

      public SensorTypeFilterOption(SensorNodeType? value, string displayName)
      {
        Value = value;
        DisplayName = displayName;
      }
    }

    #endregion
  }
}