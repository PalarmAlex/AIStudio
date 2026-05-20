using AIStudio.Common;
using ISIDA.Common;
using ISIDA.Gomeostas;
using ISIDA.Sensors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace AIStudio.ViewModels
{
  public class VerbalTreesViewModel : INotifyPropertyChanged
  {
    #region Поля и свойства

    private readonly VerbalSensorChannel _channel;
    private readonly SensorTreesPageLabels _labels;
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
    private ObservableCollection<WordNode> _allWordNodes;
    private ObservableCollection<PhraseNode> _allPhraseNodes;
    private ObservableCollection<PhraseNode> _filteredPhraseNodes;
    private int? _wordDisplayLimit = 1000;
    private int? _phraseDisplayLimit = 1000;

    #region Блокировка страницы в зависимости от стажа

    public bool IsEditingEnabled => IsStageZero && !GlobalTimer.IsPulsationRunning;
    public string PulseWarningMessage =>
        !IsStageZero ? "[КРИТИЧНО] Очистка сенсоров доступна только в стадии 0" : string.Empty;
    public Brush WarningMessageColor =>
        !IsStageZero ? Brushes.Red :
        Brushes.Gray;

    #endregion

    public string TokenTreeHeader => _labels.TokenTreeHeader;
    public string PatternTreeHeader => _labels.PatternTreeHeader;

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
          _channel.AuthoritativeMode = value;
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
          _channel.RecognitionThreshold = value;
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

    public Dictionary<int, string> Words => _channel?.GetAllWords() ?? new Dictionary<int, string>();
    public Dictionary<int, string> Phrases
    {
      get
      {
        var result = new Dictionary<int, string>();
        try
        {
          if (_channel?.PhraseTree?.Nodes == null)
            return result;

          foreach (var node in _channel.PhraseTree.Nodes.Values)
          {
            if (node.Id == 0)
              continue;

            string phraseText = _channel.GetPhraseFromPhraseId(node.Id);
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

    public static IReadOnlyList<NodeLimitOption> DisplayLimitOptionsStatic { get; } = new List<NodeLimitOption>
    {
      new NodeLimitOption(100),
      new NodeLimitOption(500),
      new NodeLimitOption(1000),
      new NodeLimitOption(5000),
      new NodeLimitOption(10000),
      new NodeLimitOption(null)
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

    public string WordsCountSummary =>
        $"Всего {_labels.TokenCountWord}: {WordsCount}";

    public int PhrasesCount
    {
      get
      {
        try
        {
          return _channel.PhraseTree.Nodes.Count(n => n.Key != 0);
        }
        catch
        {
          return 0;
        }
      }
    }

    public string PhrasesCountSummary =>
        $"Всего {_labels.PatternCountWord}: {PhrasesCount}";

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

    public VerbalTreesViewModel(
        GomeostasSystem gomeostas,
        VerbalSensorChannel channel,
        SensorTreesPageLabels labels)
    {
      _channel = channel ?? throw new ArgumentNullException(nameof(channel));
      _labels = labels ?? throw new ArgumentNullException(nameof(labels));
      _gomeostas = gomeostas ?? throw new ArgumentNullException(nameof(gomeostas));

      var agentInfo = _gomeostas.GetAgentState();
      _currentAgentStage = agentInfo?.EvolutionStage ?? 0;

      _authoritativeMode = _channel.AuthoritativeMode;
      _recognitionThreshold = _channel.RecognitionThreshold;
      _maxPhraseLength = _channel.MaxPhraseLength;

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

    private IEnumerable<WordNode> LoadInitialWordNodes(int? maxNodes = null)
    {
      return BuildWordTreeRoots(_channel.GetAllWords(), maxNodes);
    }

    private List<WordNode> BuildWordTreeRoots(Dictionary<int, string> wordsSource, int? maxNodes)
    {
      var entries = wordsSource.OrderBy(w => w.Value).ToList();
      return _channel.UsesAtomicTokens
          ? BuildAtomicWordTreeRoots(entries, maxNodes)
          : BuildCharWordTreeRoots(entries, maxNodes);
    }

    private List<WordNode> BuildCharWordTreeRoots(
        List<KeyValuePair<int, string>> entries,
        int? maxNodes)
    {
      var result = new List<WordNode>();
      int totalAdded = 0;

      foreach (var letterGroup in entries
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
          HasChildren = true
        };

        foreach (var word in letterGroup)
        {
          if (maxNodes.HasValue && totalAdded >= maxNodes.Value)
            break;
          letterNode.Children.Add(new WordNode
          {
            Id = word.Key,
            Text = word.Value,
            HasChildren = false
          });
          totalAdded++;
        }

        if (letterNode.Children.Count > 0)
          result.Add(letterNode);
      }

      return result;
    }

    private List<WordNode> BuildAtomicWordTreeRoots(
        List<KeyValuePair<int, string>> entries,
        int? maxNodes)
    {
      var result = new List<WordNode>();
      int totalAdded = 0;

      foreach (var prefixGroup in entries
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
          HasChildren = true
        };

        foreach (var word in prefixGroup.OrderBy(w => w.Value))
        {
          if (maxNodes.HasValue && totalAdded >= maxNodes.Value)
            break;
          prefixNode.Children.Add(new WordNode
          {
            Id = word.Key,
            Text = word.Value,
            HasChildren = false
          });
          totalAdded++;
        }

        if (prefixNode.Children.Count > 0)
          result.Add(prefixNode);
      }

      return result;
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
      return BuildPhraseForest(maxNodes);
    }

    private ObservableCollection<PhraseNode> BuildPhraseForest(int? maxNodes)
    {
      var rootNodes = new ObservableCollection<PhraseNode>();

      try
      {
        var phraseNodes = _channel.PhraseTree.Nodes;
        var nodeDict = new Dictionary<int, PhraseNode>();

        foreach (var node in phraseNodes.Values)
        {
          if (node.Id == 0)
            continue;

          string phraseText = _channel.GetPhraseFromPhraseId(node.Id);
          nodeDict[node.Id] = new PhraseNode
          {
            Id = node.Id,
            Text = GetLastWordFromPhrase(phraseText),
            FullPath = phraseText,
            HasChildren = node.Children.Any(c => c.Id != 0)
          };
        }

        var idsToInclude = (HashSet<int>)null;
        if (maxNodes.HasValue && nodeDict.Count > maxNodes.Value)
        {
          idsToInclude = new HashSet<int>();
          var roots = phraseNodes.Values
              .Where(n => n.Id != 0 && (n.Parent == null || n.Parent.Id == 0))
              .OrderBy(n => n.Id)
              .ToList();
          int count = 0;
          foreach (var r in roots)
          {
            if (count >= maxNodes.Value)
              break;
            CountAndAddIds(r, idsToInclude, maxNodes.Value, ref count);
          }
        }

        foreach (var node in phraseNodes.Values)
        {
          if (node.Id == 0)
            continue;
          if (idsToInclude != null && !idsToInclude.Contains(node.Id))
            continue;

          var phraseNode = nodeDict[node.Id];

          if (node.Parent != null && node.Parent.Id != 0)
          {
            if (nodeDict.TryGetValue(node.Parent.Id, out var parentNode) &&
                (idsToInclude == null || idsToInclude.Contains(node.Parent.Id)))
            {
              parentNode.Children.Add(phraseNode);
            }
          }
          else
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
        Logger.Error($"Ошибка загрузки дерева паттернов: {ex.Message}");
      }

      return rootNodes;
    }

    private void CountAndAddIds(
        SensorTree<int, int>.TreeNode<int> node,
        HashSet<int> ids,
        int max,
        ref int count)
    {
      if (count >= max || node.Id == 0)
        return;
      if (ids.Contains(node.Id))
        return;
      ids.Add(node.Id);
      count++;
      foreach (var c in node.Children)
      {
        if (count >= max)
          return;
        CountAndAddIds(c, ids, max, ref count);
      }
    }

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

      if (parent.IsLetter)
      {
        var words = _channel.GetAllWords()
            .Where(w => char.ToUpper(GetFirstChar(w.Value)) == parent.Text[0])
            .Select(w => new WordNode
            {
              Id = w.Key,
              Text = w.Value,
              HasChildren = false
            });
        parent.Children = new List<WordNode>(words);
      }
      else if (parent.IsPrefixGroup)
      {
        var words = _channel.GetAllWords()
            .Where(w => GetCommandPrefix(w.Value) == parent.Text)
            .Select(w => new WordNode
            {
              Id = w.Key,
              Text = w.Value,
              HasChildren = false
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

    private IEnumerable<WordNode> GetFullWordTreeStructure()
    {
      return BuildWordTreeRoots(_channel.GetAllWords(), null);
    }

    #endregion

    #region Методы работы с текстом

    private void AddText(object parameter)
    {
      if (string.IsNullOrWhiteSpace(InputText))
        return;

      _channel.ProcessText(InputText, MaxPhraseLength);

      try
      {
        if (_channel.UsesAtomicTokens)
          _channel.AtomicWordTree?.Save();
        else
          _channel.WordTree?.Save();
        _channel.PhraseTree.Save();
        _channel.WordSandbox.Save();
        _channel.PhraseSandbox.Save();
        _channel.PhraseTextSandbox.Save();
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
        _channel.ClearAllTrees();
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

      _allWordNodes = new ObservableCollection<WordNode>(LoadInitialWordNodes(_wordDisplayLimit));
      _allPhraseNodes = new ObservableCollection<PhraseNode>(LoadInitialPhraseNodes(_phraseDisplayLimit));
      _filteredPhraseNodes = new ObservableCollection<PhraseNode>(_allPhraseNodes);

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

      OnPropertyChanged(nameof(Words));
      OnPropertyChanged(nameof(Phrases));
      OnPropertyChanged(nameof(WordsCount));
      OnPropertyChanged(nameof(WordsCountSummary));
      OnPropertyChanged(nameof(PhrasesCount));
      OnPropertyChanged(nameof(PhrasesCountSummary));
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
      var matchingWords = _channel.GetAllWords()
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

      var allPhraseNodesForSearch = LoadInitialPhraseNodes(null);

      var filtered = new ObservableCollection<PhraseNode>();
      var searchLower = searchText.ToLower();

      foreach (var rootNode in allPhraseNodesForSearch)
      {
        var filteredRoot = FilterPhraseNodeRecursive(rootNode, searchLower);
        if (filteredRoot != null)
          filtered.Add(filteredRoot);
      }

      VisiblePhraseNodes = filtered;
    }

    private PhraseNode FilterPhraseNodeRecursive(PhraseNode node, string searchText)
    {
      string fullPhraseText = string.Empty;
      if (node.Id > 0)
        fullPhraseText = _channel.GetPhraseFromPhraseId(node.Id);

      bool matches = !string.IsNullOrEmpty(fullPhraseText) &&
                     fullPhraseText.ToLower().Contains(searchText);
      matches = matches || node.Text.ToLower().Contains(searchText);

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
          Children = filteredChildren
        };
      }

      return null;
    }

    private void SearchWord(object parameter)
    {
      FilterWordNodes(WordSearchText);
      if (!string.IsNullOrEmpty(WordSearchText))
        ExpandAllWordNodes();
      else
        CollapseAllWordNodes();
    }

    private void SearchPhraseWrapper(object parameter)
    {
      FilterPhraseNodes(PhraseSearchText);
      if (!string.IsNullOrEmpty(PhraseSearchText))
        ExpandAllPhraseNodes();
      else
        CollapseAllPhraseNodes();
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

    #endregion

    #region Вложенные классы

    public class WordNode
    {
      public int Id { get; set; }
      public string Text { get; set; }
      public bool IsLetter { get; set; }
      public bool IsPrefixGroup { get; set; }
      public bool IsBoldHeader => IsLetter || IsPrefixGroup;
      public bool HasChildren { get; set; }
      public bool IsExpanded { get; set; }
      public List<WordNode> Children { get; set; } = new List<WordNode>();
    }

    public class PhraseNode
    {
      public int Id { get; set; }
      public string Text { get; set; }
      public string FullPath { get; set; }
      public bool HasChildren { get; set; }
      public bool IsExpanded { get; set; }
      public ObservableCollection<PhraseNode> Children { get; set; } =
          new ObservableCollection<PhraseNode>();
    }

    public class NodeLimitOption
    {
      public int? Value { get; }
      public string DisplayName => Value.HasValue ? Value.Value.ToString() : "Все";

      public NodeLimitOption(int? value) => Value = value;
    }

    #endregion
  }
}
