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
    public Dictionary<int, string> Phrases => _verbalChannel?.GetAllPhrases() ?? new Dictionary<int, string>();
    public int WordsCount => Words.Count;
    public int PhrasesCount => Phrases.Count;

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

      _allWordNodes = new ObservableCollection<WordNode>(LoadInitialWordNodes());
      _allPhraseNodes = new ObservableCollection<PhraseNode>(LoadInitialPhraseNodes());
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
      VisibleWordNodes = new ObservableCollection<WordNode>(LoadInitialWordNodes());
      VisiblePhraseNodes = new ObservableCollection<PhraseNode>(LoadInitialPhraseNodes());

      WordTreeStructure = GetFullWordTreeStructure();
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

    private IEnumerable<WordNode> LoadInitialWordNodes()
    {
      var words = _verbalChannel.GetAllWords();

      var grouped = words.Values
          .OrderBy(w => w)
          .GroupBy(w => char.ToUpper(w.FirstOrDefault()))
          .OrderBy(g => g.Key);

      var result = new ObservableCollection<WordNode>();

      foreach (var group in grouped)
      {
        var letterNode = new WordNode
        {
          Id = 0,
          Text = group.Key.ToString(),
          IsLetter = true,
          HasChildren = group.Any()
        };

        foreach (var word in group)
        {
          var wordId = words.FirstOrDefault(x => x.Value == word).Key;
          letterNode.Children.Add(new WordNode
          {
            Id = wordId,
            Text = word,
            HasChildren = false
          });
        }

        result.Add(letterNode);
      }

      return result;
    }

    private ObservableCollection<PhraseNode> LoadInitialPhraseNodes()
    {
      var allPhrases = _verbalChannel.GetAllPhrases();
      var rootNodes = new ObservableCollection<PhraseNode>();

      // Создаем полное дерево фраз
      foreach (var phrase in allPhrases.OrderBy(p => p.Value))
      {
        var words = phrase.Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) continue;

        // Находим или создаем корневой узел
        var firstWord = words[0];
        var rootNode = rootNodes.FirstOrDefault(n =>
            n.Text.Equals(firstWord, StringComparison.OrdinalIgnoreCase));

        if (rootNode == null)
        {
          rootNode = new PhraseNode
          {
            Id = 0,
            Text = firstWord,
            FullPath = firstWord,
            HasChildren = words.Length > 1
          };
          rootNodes.Add(rootNode);
        }

        // Строим полный путь для фразы
        var currentNode = rootNode;
        var currentPath = firstWord;

        for (int i = 1; i < words.Length; i++)
        {
          var word = words[i];
          currentPath += " " + word;

          // Ищем существующий дочерний узел
          var childNode = currentNode.Children.FirstOrDefault(n =>
              n.Text.Equals(word, StringComparison.OrdinalIgnoreCase));

          if (childNode == null)
          {
            childNode = new PhraseNode
            {
              Id = i == words.Length - 1 ? phrase.Key : 0,
              Text = word,
              FullPath = currentPath,
              HasChildren = i < words.Length - 1
            };
            currentNode.Children.Add(childNode);
          }

          currentNode = childNode;
        }
      }

      return rootNodes;
    }

    public void LoadWordChildren(int parentId)
    {
      var parent = VisibleWordNodes.FirstOrDefault(n => n.Id == parentId);
      if (parent == null || (parent.Children?.Count ?? 0) > 0) return;

      var words = _verbalChannel.GetAllWords()
          .Where(w => char.ToUpper(w.Value[0]) == parent.Text[0])
          .Select(w => new WordNode
          {
            Id = w.Key,
            Text = w.Value,
            HasChildren = false
          });

      parent.Children = new List<WordNode>(words);
      OnPropertyChanged(nameof(VisibleWordNodes));
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
      var words = _verbalChannel.GetAllWords();
      var grouped = words
          .GroupBy(w => char.ToUpper(w.Value.FirstOrDefault()))
          .OrderBy(g => g.Key);

      foreach (var group in grouped)
      {
        var letterNode = new WordNode
        {
          Id = 0,
          Text = group.Key.ToString(),
          IsLetter = true
        };

        foreach (var word in group.OrderBy(w => w.Value))
        {
          letterNode.Children.Add(new WordNode
          {
            Id = word.Key,
            Text = word.Value
          });
        }

        yield return letterNode;
      }
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
        Debug.WriteLine($"Ошибка при сохранении данных: {ex.Message}");
        MessageBox.Show("Не удалось сохранить изменения на диск", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Warning);
      }

      InputText = string.Empty;
      RefreshAllCollections();
    }

    private void RemoveAllTrees(object parameter)
    {
      // Запрос подтверждения
      var result = MessageBox.Show(
          "Вы уверены, что хотите полностью очистить все вербальные деревья?\n\n" +
          "Это действие удалит все слова и фразы из памяти агента и не может быть отменено.",
          "Подтверждение очистки",
          MessageBoxButton.YesNo,
          MessageBoxImage.Warning,
          MessageBoxResult.No);

      if (result != MessageBoxResult.Yes)
        return;

      try
      {
        // Очищаем все деревья через вербальный канал
        _verbalChannel.ClearAllTrees();

        // Обновляем интерфейс
        RefreshAllCollections();

        MessageBox.Show("Все вербальные деревья успешно очищены.",
            "Очистка завершена",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Ошибка при очистке деревьев: {ex.Message}");
        MessageBox.Show($"Не удалось очистить деревья: {ex.Message}",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
      }
    }

    private void RefreshAllCollections()
    {
      UpdateAgentStage();

      // Полностью перезагружаем все коллекции
      _allWordNodes = new ObservableCollection<WordNode>(LoadInitialWordNodes());
      _allPhraseNodes = new ObservableCollection<PhraseNode>(LoadInitialPhraseNodes());
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
      OnPropertyChanged(nameof(PhrasesCount));
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

      var filtered = new ObservableCollection<WordNode>();
      var searchLower = searchText.ToLower();

      foreach (var letterNode in _allWordNodes)
      {
        var filteredLetterNode = new WordNode
        {
          Id = letterNode.Id,
          Text = letterNode.Text,
          IsLetter = letterNode.IsLetter,
          HasChildren = letterNode.HasChildren
        };

        // Фильтруем слова в этой буквенной группе
        foreach (var wordNode in letterNode.Children)
        {
          if (wordNode.Text.ToLower().Contains(searchLower))
          {
            filteredLetterNode.Children.Add(wordNode);
            filteredLetterNode.HasChildren = true;
          }
        }

        // Добавляем буквенную группу только если в ней есть слова
        if (filteredLetterNode.Children.Count > 0)
        {
          filtered.Add(filteredLetterNode);
        }
      }

      VisibleWordNodes = filtered;
    }

    private void FilterPhraseNodes(string searchText)
    {
      if (string.IsNullOrWhiteSpace(searchText))
      {
        VisiblePhraseNodes = new ObservableCollection<PhraseNode>(_allPhraseNodes);
        return;
      }

      var filtered = new ObservableCollection<PhraseNode>();
      var searchLower = searchText.ToLower();

      foreach (var rootNode in _allPhraseNodes)
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
      // Проверяем, содержит ли текущий узел искомый текст
      bool matches = node.Text.ToLower().Contains(searchText);

      // Рекурсивно фильтруем дочерние узлы
      var filteredChildren = new ObservableCollection<PhraseNode>();
      foreach (var child in node.Children)
      {
        var filteredChild = FilterPhraseNodeRecursive(child, searchText);
        if (filteredChild != null)
        {
          filteredChildren.Add(filteredChild);
          matches = true; // Если есть подходящие дети, показываем родителя
        }
      }

      // Если узел или его дети подходят под фильтр
      if (matches || filteredChildren.Count > 0)
      {
        return new PhraseNode
        {
          Id = node.Id,
          Text = node.Text,
          FullPath = node.FullPath,
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

    public class TreeItemEventArgs : EventArgs
    {
      public int ItemId { get; }
      public TreeItemEventArgs(int itemId)
      {
        ItemId = itemId;
      }
    }

    #endregion
  }
}