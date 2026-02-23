using AIStudio.Converters;
using AIStudio.Dialogs;
using AIStudio.Views;
using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Gomeostas;
using ISIDA.Psychic;
using ISIDA.Psychic.Automatism;
using ISIDA.Reflexes;
using ISIDA.Sensors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using static ISIDA.Gomeostas.GomeostasSystem;
using static ISIDA.Psychic.VerbalBrocaImagesSystem;

namespace AIStudio.ViewModels
{
  public class AutomatizmsViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly GomeostasSystem _gomeostas;
    private readonly AutomatizmSystem _automatizmSystem;
    private readonly AutomatizmTreeSystem _automatizmTreeSystem;
    private readonly ActionsImagesSystem _actionsImagesSystem;
    private readonly InfluenceActionsImagesSystem _influenceActionsImagesSystem;
    private readonly EmotionsImageSystem _emotionsImageSystem;
    private readonly SensorySystem _sensorySystem;
    private readonly InfluenceActionSystem _influenceActionSystem;
    private readonly AdaptiveActionsSystem _adaptiveActionsSystem;
    private readonly VerbalBrocaImagesSystem _verbalBrocaImages;
    private readonly ConditionedReflexToAutomatizmConverter _reflexConverter;
    private readonly AutomatizmFileLoader _automatizmFileLoader;

    private string _currentAgentName;
    private int _currentAgentStage;
    private bool _isCloningInProgress;

    private int? _selectedBaseConditionFilter;
    private string _selectedUsefulnessFilter;
    private int? _selectedBeliefFilter;
    private bool _isLoadingInProgress;

    private Dictionary<int, ActionsImagesSystem.ActionsImage> _actionsImageCache = new Dictionary<int, ActionsImagesSystem.ActionsImage>();
    private Dictionary<int, AutomatizmNode> _nodeCache = new Dictionary<int, AutomatizmNode>();
    private Dictionary<int, List<int>> _influenceActionsImagesCache = new Dictionary<int, List<int>>();
    private Dictionary<int, EmotionsImageSystem.EmotionsImage> _emotionImageCache = new Dictionary<int, EmotionsImageSystem.EmotionsImage>();

    public GomeostasSystem GomeostasSystem => _gomeostas;
    public bool IsStageTwoOrHigher => _currentAgentStage >= 2;

    public string CurrentAgentTitle => $"Автоматизмы Агента: {_currentAgentName ?? "Не определен"}";

    private ObservableCollection<AutomatizmDisplayItem> _allAutomatizms = new ObservableCollection<AutomatizmDisplayItem>();
    private ICollectionView _automatizmsView;
    public ICollectionView AutomatizmsView => _automatizmsView;

    public ICommand ClearFiltersCommand { get; }
    public ICommand RemoveAllCommand { get; }
    public ICommand CloneReflexesCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand LoadFromFileCommand { get; }

    public AutomatizmsViewModel(
        GomeostasSystem gomeostasSystem,
        AutomatizmSystem automatizmSystem,
        AutomatizmTreeSystem automatizmTreeSystem,
        ActionsImagesSystem actionsImagesSystem,
        InfluenceActionsImagesSystem influenceActionsImagesSystem,
        EmotionsImageSystem emotionsImageSystem,
        SensorySystem sensorySystem,
        InfluenceActionSystem influenceActionSystem,
        AdaptiveActionsSystem adaptiveActionsSystem,
        VerbalBrocaImagesSystem verbalBrocaImages,
        ConditionedReflexToAutomatizmConverter reflexConverter,
        AutomatizmFileLoader automatizmFileLoader)
    {
      _gomeostas = gomeostasSystem ?? throw new ArgumentNullException(nameof(gomeostasSystem));
      _automatizmSystem = automatizmSystem ?? throw new ArgumentNullException(nameof(automatizmSystem));
      _automatizmTreeSystem = automatizmTreeSystem ?? throw new ArgumentNullException(nameof(automatizmTreeSystem));
      _actionsImagesSystem = actionsImagesSystem ?? throw new ArgumentNullException(nameof(actionsImagesSystem));
      _influenceActionsImagesSystem = influenceActionsImagesSystem ?? throw new ArgumentNullException(nameof(influenceActionsImagesSystem));
      _emotionsImageSystem = emotionsImageSystem ?? throw new ArgumentNullException(nameof(emotionsImageSystem));
      _sensorySystem = sensorySystem ?? throw new ArgumentNullException(nameof(sensorySystem));
      _influenceActionSystem = influenceActionSystem ?? throw new ArgumentNullException(nameof(influenceActionSystem));
      _adaptiveActionsSystem = adaptiveActionsSystem ?? throw new ArgumentNullException(nameof(adaptiveActionsSystem));
      _verbalBrocaImages = verbalBrocaImages ?? throw new ArgumentNullException(nameof(verbalBrocaImages));
      _reflexConverter = reflexConverter ?? throw new ArgumentNullException(nameof(reflexConverter));
      _automatizmFileLoader = automatizmFileLoader ?? throw new ArgumentNullException(nameof(automatizmFileLoader));

      _automatizmsView = CollectionViewSource.GetDefaultView(_allAutomatizms);
      _automatizmsView.Filter = FilterAutomatizms;

      ClearFiltersCommand = new RelayCommand(ClearFilters);
      RemoveAllCommand = new RelayCommand(RemoveAllAutomatizms);
      CloneReflexesCommand = new RelayCommand(CloneReflexesToAutomatizms, CanCloneReflexes);

      GlobalTimer.PulsationStateChanged += OnPulsationStateChanged;
      LoadFromFileCommand = new RelayCommand(LoadFromFile, CanLoadFromFile);
      LoadAgentData();
    }

    public bool IsLoadingInProgress
    {
      get => _isLoadingInProgress;
      set
      {
        _isLoadingInProgress = value;
        OnPropertyChanged(nameof(IsLoadingInProgress));
        OnPropertyChanged(nameof(IsLoadFromFileEnabled));
        ((RelayCommand)LoadFromFileCommand).RaiseCanExecuteChanged();
      }
    }

    public bool IsLoadFromFileEnabled
    {
      get
      {
        return !IsLoadingInProgress &&
               _currentAgentStage == 2 &&
               !GlobalTimer.IsPulsationRunning &&
               PsychicSystem.IsInitialized;
      }
    }

    private bool CanLoadFromFile(object parameter)
    {
      return IsLoadFromFileEnabled;
    }


    private bool FilterAutomatizms(object item)
    {
      if (!(item is AutomatizmDisplayItem automatizm))
        return false;

      bool baseConditionMatch = !SelectedBaseConditionFilter.HasValue ||
                               automatizm.BaseCondition == SelectedBaseConditionFilter.Value;

      bool level2Match = !SelectedLevel2Filter.HasValue ||
                        (automatizm.EmotionIdList != null && automatizm.EmotionIdList.Contains(SelectedLevel2Filter.Value));

      bool usefulnessMatch = string.IsNullOrEmpty(SelectedUsefulnessFilter);
      if (!usefulnessMatch)
      {
        switch (SelectedUsefulnessFilter)
        {
          case "<0": usefulnessMatch = automatizm.Usefulness < 0; break;
          case "=0": usefulnessMatch = automatizm.Usefulness == 0; break;
          case ">0": usefulnessMatch = automatizm.Usefulness > 0; break;
        }
      }

      bool beliefMatch = !SelectedBeliefFilter.HasValue || automatizm.Belief == SelectedBeliefFilter.Value;

      return baseConditionMatch && level2Match && usefulnessMatch && beliefMatch;
    }

    private void OnPulsationStateChanged()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        OnPropertyChanged(nameof(IsStageTwoOrHigher));
        OnPropertyChanged(nameof(IsDeletionEnabled));
        OnPropertyChanged(nameof(PulseWarningMessage));
        OnPropertyChanged(nameof(WarningMessageColor));
        ((RelayCommand)SaveCommand)?.RaiseCanExecuteChanged();
      });
    }

    #region Блокировка страницы в зависимости от стажа

    /// <summary>Удаление автоматизмов разрешено на стадии 2 и 3.</summary>
    public bool IsDeletionEnabled =>
        IsStageTwoOrHigher &&
        (_currentAgentStage == 2 || _currentAgentStage == 3) &&
        !GlobalTimer.IsPulsationRunning;

    public string PulseWarningMessage =>
        !IsStageTwoOrHigher
            ? "[КРИТИЧНО] Управление автоматизмами доступно только в стадии 2"
            : _currentAgentStage != 2 && _currentAgentStage != 3
                ? "[КРИТИЧНО] Автоматизмы можно редактировать и удалять только на стадии 2 и 3"
                : GlobalTimer.IsPulsationRunning
                    ? "Управление автоматизмами доступно только при выключенной пульсации"
                    : _currentAgentStage == 3
                        ? "Редактирование только на стадии 2; удаление доступно на стадии 2 и 3."
                        : string.Empty;

    public Brush WarningMessageColor =>
        !IsStageTwoOrHigher || _currentAgentStage > 3
            ? Brushes.Red
            : Brushes.Gray;

    #endregion

    #region Фильтры

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
    public List<KeyValuePair<int?, string>> BeliefFilterOptions { get; } = new List<KeyValuePair<int?, string>>
    {
      new KeyValuePair<int?, string>(null, "Все"),
      new KeyValuePair<int?, string>(0, "Предположение"),
      new KeyValuePair<int?, string>(1, "Чужие сведения"),
      new KeyValuePair<int?, string>(2, "Проверенное знание")
    };
    public List<KeyValuePair<int?, string>> Level2FilterOptions { get; private set; } = new List<KeyValuePair<int?, string>>();

    public int? SelectedBaseConditionFilter
    {
      get => _selectedBaseConditionFilter;
      set
      {
        _selectedBaseConditionFilter = value;
        OnPropertyChanged(nameof(SelectedBaseConditionFilter));
        ApplyFilters();
      }
    }

    public string SelectedUsefulnessFilter
    {
      get => _selectedUsefulnessFilter;
      set
      {
        _selectedUsefulnessFilter = value;
        OnPropertyChanged(nameof(SelectedUsefulnessFilter));
        ApplyFilters();
      }
    }

    public int? SelectedBeliefFilter
    {
      get => _selectedBeliefFilter;
      set
      {
        _selectedBeliefFilter = value;
        OnPropertyChanged(nameof(SelectedBeliefFilter));
        ApplyFilters();
      }
    }

    private int? _selectedLevel2Filter;
    public int? SelectedLevel2Filter
    {
      get => _selectedLevel2Filter;
      set
      {
        _selectedLevel2Filter = value;
        OnPropertyChanged(nameof(SelectedLevel2Filter));
        ApplyFilters();
      }
    }

    public bool IsCloningInProgress
    {
      get => _isCloningInProgress;
      set
      {
        _isCloningInProgress = value;
        OnPropertyChanged(nameof(IsCloningInProgress));
        ((RelayCommand)CloneReflexesCommand).RaiseCanExecuteChanged();
      }
    }

    private void ApplyFilters()
    {
      _automatizmsView.Refresh();
    }

    private void ClearFilters(object parameter = null)
    {
      SelectedBaseConditionFilter = null;
      SelectedLevel2Filter = null;
      SelectedUsefulnessFilter = null;
      SelectedBeliefFilter = null;
    }

    #endregion

    private void LoadFromFile(object parameter)
    {
      if (!IsLoadFromFileEnabled) return;

      try
      {
        string bootDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "ISIDA", "BootData");

        var dialog = new AutomatizmLoadDialog(_gomeostas, bootDataFolder)
        {
          Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true && dialog.SelectedBaseState.HasValue)
        {
          // Файл уже мог быть изменен через редактор в диалоге
          int chainsLoaded = _automatizmFileLoader.LoadFromFile(
              dialog.SelectedBaseState.Value,
              dialog.SelectedStyleIds ?? new List<int>());

          RefreshAllCollections();

          MessageBox.Show(
              $"Автоматизмы загружены.\nОбработано цепочек: {chainsLoaded}",
              "Загрузка завершена",
              MessageBoxButton.OK,
              MessageBoxImage.Information);
        }
      }
      catch (ObjectDisposedException)
      {
        MessageBox.Show(
            "Загрузчик автоматизмов уже освобожден.",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void LoadAgentData()
    {
      try
      {
        RefreshAllCollections();
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка загрузки автоматизмов: {ex.Message}", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void RefreshAllCollections()
    {
      try
      {
        var agentInfo = _gomeostas.GetAgentState();
        _currentAgentStage = agentInfo?.EvolutionStage ?? 0;
        _currentAgentName = agentInfo.Name;

        _allAutomatizms.Clear();
        _actionsImageCache.Clear();
        _nodeCache.Clear();
        _influenceActionsImagesCache.Clear();
        _emotionImageCache.Clear();

        Dictionary<int, AutomatizmChainsSystem.AutomatizmChain> treeNodeToChainMap = new Dictionary<int, AutomatizmChainsSystem.AutomatizmChain>();
        Dictionary<int, List<AutomatizmChainsSystem.ChainLink>> chainLinksMap = new Dictionary<int, List<AutomatizmChainsSystem.ChainLink>>();

        if (AutomatizmChainsSystem.IsInitialized)
        {
          var allChains = AutomatizmChainsSystem.Instance.GetAllAutomatizmChains();
          foreach (var chain in allChains.Values)
          {
            if (chain.TreeNodeId > 0)
            {
              treeNodeToChainMap[chain.TreeNodeId] = chain;
              chainLinksMap[chain.ID] = chain.Links?.ToList() ?? new List<AutomatizmChainsSystem.ChainLink>();
            }
          }
        }

        foreach (var automatizm in _automatizmSystem.GetAllAutomatizms().OrderBy(a => a.ID))
        {
          var treeNode = GetTreeNode(automatizm.BranchID);
          var actionsImage = GetActionsImage(automatizm.ActionsImageID);

          List<int> emotionIdList = new List<int>();
          if (treeNode?.EmotionID > 0)
            emotionIdList = GetEmotionIdsFromEmotionImage(treeNode.EmotionID);

          List<int> influenceActionIds = new List<int>();
          if (treeNode?.ActivityID > 0)
            influenceActionIds = GetInfluenceActionIds(treeNode.ActivityID);

          string toneMoodText = string.Empty;
          if (treeNode?.ToneMoodID > 0)
          {
            try
            {
              toneMoodText = PsychicSystem.GetToneMoodString(treeNode.ToneMoodID);
            }
            catch { }
          }

          string verbalText = string.Empty;
          if (treeNode?.VerbID > 0)
          {
            try
            {
              var verbalImage = _verbalBrocaImages.GetVerbalBrocaImage(treeNode.VerbID);
              if (verbalImage != null && verbalImage.PhraseIdList != null && verbalImage.PhraseIdList.Any())
              {
                var phraseTexts = new List<string>();
                foreach (var phraseId in verbalImage.PhraseIdList)
                {
                  string phraseText = _sensorySystem.VerbalChannel.GetPhraseFromPhraseId(phraseId);
                  if (!string.IsNullOrEmpty(phraseText))
                    phraseTexts.Add($"\"{phraseText}\"");
                  else
                    phraseTexts.Add($"[ID:{phraseId}]");
                }

                if (phraseTexts.Any())
                  verbalText = string.Join(" ", phraseTexts);
              }
            }
            catch (Exception ex)
            {
              Logger.Error($"Ошибка получения текста фразы: {ex.Message}");
            }
          }

          // Определяем информацию о цепочке для этого узла дерева
          string chainInfo = string.Empty;
          int chainId = 0;

          if (treeNode != null && treeNodeToChainMap.ContainsKey(treeNode.ID))
          {
            var chain = treeNodeToChainMap[treeNode.ID];
            chainId = chain.ID;

            // Формируем информацию о цепочке
            if (chainLinksMap.ContainsKey(chainId))
            {
              var links = chainLinksMap[chainId];
              var linkCount = links.Count;
              var linkDescriptions = links.Take(3).Select(l => l.Description ?? $"Звено {l.ID}");

              chainInfo = $"Цепочка {chainId}: {linkCount} звеньев";
              if (chain.Name?.Length > 0)
                chainInfo = $"{chain.Name} ({chainInfo})";

              // Добавляем описание первого звена для tooltip
              if (links.Any())
              {
                chainInfo += $"\nПервые звенья: {string.Join(" → ", linkDescriptions)}";
                if (linkCount > 3)
                  chainInfo += $" ... и еще {linkCount - 3}";
              }
            }
          }

          var displayItem = new AutomatizmDisplayItem
          {
            ID = automatizm.ID,
            BranchID = automatizm.BranchID,
            Usefulness = automatizm.Usefulness,
            ActionsImageID = automatizm.ActionsImageID,
            Belief = automatizm.Belief,
            Energy = automatizm.Energy,
            Count = automatizm.Count,
            NextID = automatizm.NextID,
            ChainID = chainId,
            ChainInfo = chainInfo,
            ChainDetailedInfo = GetChainDetailedInfo(chainId),

            BaseCondition = treeNode?.BaseID ?? 0,
            EmotionIdList = emotionIdList,
            ActivityID = treeNode?.ActivityID ?? 0,
            InfluenceActionIds = influenceActionIds,
            ToneMoodID = treeNode?.ToneMoodID ?? 0,
            SimbolID = treeNode?.SimbolID ?? 0,
            VerbID = treeNode?.VerbID ?? 0,

            // Текстовые описания для tooltip
            BaseConditionText = GetBaseConditionText(treeNode?.BaseID ?? 0),
            EmotionText = GetEmotionText(emotionIdList),
            InfluenceActionsText = GetInfluenceActionsText(influenceActionIds),
            ToneMoodText = toneMoodText,
            VerbalText = verbalText,

            ActionsImageDisplay = actionsImage != null ? new ActionsImageDisplay
            {
              ActIdList = actionsImage.ActIdList ?? new List<int>(),
              PhraseIdList = actionsImage.PhraseIdList ?? new List<int>(),
              ToneId = actionsImage.ToneId,
              MoodId = actionsImage.MoodId
            } : null
          };

          _allAutomatizms.Add(displayItem);
        }

        LoadLevel2FilterOptions();

        OnPropertyChanged(nameof(IsStageTwoOrHigher));
        OnPropertyChanged(nameof(IsDeletionEnabled));
        OnPropertyChanged(nameof(PulseWarningMessage));
        OnPropertyChanged(nameof(WarningMessageColor));
        OnPropertyChanged(nameof(CurrentAgentTitle));
        ((RelayCommand)SaveCommand)?.RaiseCanExecuteChanged();
      }
      catch (Exception ex)
      {
        Logger.Error(ex.Message);
      }
    }

    private void LoadLevel2FilterOptions()
    {
      Level2FilterOptions = new List<KeyValuePair<int?, string>> { new KeyValuePair<int?, string>(null, "Все контексты") };
      var level2Items = _gomeostas?.GetAllBehaviorStyles()?.Values?.ToList() ?? new List<BehaviorStyle>();
      Level2FilterOptions.AddRange(level2Items.Select(x => new KeyValuePair<int?, string>(x.Id, x.Name)));
    }

    private string GetBaseConditionText(int baseId)
    {
      switch (baseId)
      {
        case -1: return "Плохо";
        case 0: return "Норма";
        case 1: return "Хорошо";
        default: return $"Неизвестное ({baseId})";
      }
    }

    private string GetEmotionText(List<int> emotionIds)
    {
      if (emotionIds == null || !emotionIds.Any())
        return "Нет эмоций";

      try
      {
        var behaviorStyles = _gomeostas.GetAllBehaviorStyles();
        var names = emotionIds
            .Where(id => behaviorStyles.ContainsKey(id))
            .Select(id => behaviorStyles[id].Name)
            .ToList();

        return names.Any() ? string.Join(", ", names) : $"Стили: {string.Join(", ", emotionIds)}";
      }
      catch
      {
        return $"Стили: {string.Join(", ", emotionIds)}";
      }
    }

    private string GetInfluenceActionsText(List<int> actionIds)
    {
      if (actionIds == null || !actionIds.Any())
        return "Нет воздействий";

      try
      {
        var allActions = _influenceActionSystem.GetAllInfluenceActions();
        var names = actionIds
            .Where(id => allActions.Any(a => a.Id == id))
            .Select(id => allActions.First(a => a.Id == id).Name)
            .ToList();

        return names.Any() ? string.Join(", ", names) : $"Действия: {string.Join(", ", actionIds)}";
      }
      catch
      {
        return $"Действия: {string.Join(", ", actionIds)}";
      }
    }

    private List<int> GetEmotionIdsFromEmotionImage(int emotionImageId)
    {
      if (emotionImageId <= 0)
        return new List<int>();

      if (_emotionImageCache.TryGetValue(emotionImageId, out var cachedEmotionImage))
        return cachedEmotionImage.BaseStylesList?.ToList() ?? new List<int>();

      try
      {
        var emotionImage = _emotionsImageSystem.GetEmotionsImage(emotionImageId);
        if (emotionImage != null)
        {
          _emotionImageCache[emotionImageId] = emotionImage;
          return emotionImage.BaseStylesList?.ToList() ?? new List<int>();
        }
      }
      catch (Exception ex)
      {
        Logger.Error($"Ошибка получения эмоций из образа ID={emotionImageId}: {ex.Message}");
      }
      return new List<int>();
    }

    private List<int> GetInfluenceActionIds(int activityId)
    {
      if (activityId <= 0)
        return new List<int>();

      if (_influenceActionsImagesCache.TryGetValue(activityId, out var cachedIds))
        return cachedIds;

      try
      {
        var influenceActions = _influenceActionsImagesSystem.GetInfluenceActionIds(activityId);
        var ids = influenceActions?.ToList() ?? new List<int>();
        _influenceActionsImagesCache[activityId] = ids;
        return ids;
      }
      catch (Exception ex)
      {
        Logger.Error($"Ошибка получения воздействий для ActivityID={activityId}: {ex.Message}");
        return new List<int>();
      }
    }

    public string GetTreeNodeConditionsInfo(int branchId)
    {
      try
      {
        var treeNode = GetTreeNode(branchId);
        if (treeNode == null)
          return $"Узел дерева ID:{branchId} не найден";

        // Используем существующий конвертер для формирования подсказки
        var converter = new TreeNodeConditionsToTooltipConverter();
        return converter.Convert(treeNode, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture) as string;
      }
      catch (Exception ex)
      {
        return $"Ошибка загрузки информации об узле: {ex.Message}";
      }
    }

    private AutomatizmNode GetTreeNode(int branchId)
    {
      // Для BranchID < 1000000 это ID узла дерева
      if (branchId < 1000000 && branchId > 0)
      {
        if (_nodeCache.TryGetValue(branchId, out var cachedNode))
          return cachedNode;

        var node = _automatizmTreeSystem.GetNodeById(branchId);
        if (node != null)
        {
          _nodeCache[branchId] = node;
        }
        return node;
      }

      // Для BranchID >= 1000000 и < 2000000 - это привязка к действиям
      // Для BranchID >= 2000000 - это привязка к фразам
      return null;
    }

    private ActionsImagesSystem.ActionsImage GetActionsImage(int actionsImageId)
    {
      if (actionsImageId <= 0)
        return null;

      if (_actionsImageCache.TryGetValue(actionsImageId, out var cachedImage))
        return cachedImage;

      var image = _actionsImagesSystem.GetActionsImage(actionsImageId);
      if (image != null)
        _actionsImageCache[actionsImageId] = image;

      return image;
    }

    public void RemoveSelectedAutomatizm(object parameter)
    {
      if (parameter is AutomatizmDisplayItem automatizm)
      {
        if (_currentAgentStage != 2 && _currentAgentStage != 3)
        {
          MessageBox.Show(
              $"Удаление автоматизмов доступно только на стадии 2 и 3 (текущая стадия: {_currentAgentStage})",
              "Удаление недоступно",
              MessageBoxButton.OK,
              MessageBoxImage.Warning);
          return;
        }
        try
        {
          if (_allAutomatizms.Contains(automatizm))
            _allAutomatizms.Remove(automatizm);

          if (automatizm.ID > 0)
          {
            _automatizmSystem.DeleteAutomatizm(automatizm.ID);

            var (success, error) = _automatizmSystem.SaveAutomatizm();
            if (!success)
            {
              MessageBox.Show($"Не удалось сохранить удаление: {error}", "Ошибка",
                  MessageBoxButton.OK, MessageBoxImage.Error);
            }
          }
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
              MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }
    }

    public void RemoveAllAutomatizms(object parameter)
    {
      if (_currentAgentStage != 2 && _currentAgentStage != 3)
      {
        MessageBox.Show(
            $"Удаление автоматизмов доступно только на стадии 2 и 3 (текущая стадия: {_currentAgentStage})",
            "Удаление недоступно",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
      }

      var result = MessageBox.Show(
          $"Вы действительно хотите удалить ВСЕ автоматизмы агента? Это действие нельзя будет отменить.",
          "Подтверждение удаления",
          MessageBoxButton.YesNo,
          MessageBoxImage.Warning);

      if (result == MessageBoxResult.Yes)
      {
        try
        {
          var success = _automatizmSystem.DeleteAllAutomatizm();

          if (success)
          {
            _allAutomatizms.Clear();

            var (saveSuccess, error) = _automatizmSystem.SaveAutomatizm();
            if (saveSuccess)
            {
              MessageBox.Show("Все автоматизмы агента успешно удалены",
                  "Удаление завершено",
                  MessageBoxButton.OK,
                  MessageBoxImage.Information);
            }
            else
            {
              MessageBox.Show($"Не удалось сохранить изменения после удаления:\n{error}",
                  "Ошибка сохранения",
                  MessageBoxButton.OK,
                  MessageBoxImage.Error);
            }
          }
          else
          {
            MessageBox.Show($"Не удалось удалить все автоматизмы.",
                "Ошибка удаления",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
          }
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Ошибка удаления автоматизмов агента: {ex.Message}",
              "Ошибка",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
        }
      }
    }

    private bool CanCloneReflexes(object parameter)
    {
      return !IsCloningInProgress &&
             _currentAgentStage == 2 &&
             !GlobalTimer.IsPulsationRunning &&
             _reflexConverter != null;
    }

    private void CloneReflexesToAutomatizms(object parameter)
    {
      if (!ConditionedReflexToAutomatizmConverter.IsInitialized)
      {
        MessageBox.Show(
            "Конвертер условных рефлексов не инициализирован.",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
      }

      var result = MessageBox.Show(
          "Вы действительно хотите создать автоматизмы на основе всех условных рефлексов?\n" +
          "Существующие автоматизмы останутся без изменений.",
          "Подтверждение клонирования",
          MessageBoxButton.YesNo,
          MessageBoxImage.Question);

      if (result != MessageBoxResult.Yes)
        return;

      IsCloningInProgress = true;

      try
      {
        System.Threading.Tasks.Task.Run(() =>
        {
          var (newCount, existingCount, totalCount, duplicateCount, chainsCreated, errors) =
              ConditionedReflexToAutomatizmConverter.Instance.CloneAllConditionedReflexesToAutomatisms();

          Application.Current.Dispatcher.Invoke(() =>
          {
            IsCloningInProgress = false;

            string message;
            if (totalCount == 0)
            {
              message = "Нет условных рефлексов для клонирования.";
            }
            else
            {
              message = $"Обработано {totalCount} условных рефлексов:\n" +
                        $"• Создано новых автоматизмов: {newCount}\n" +
                        $"• Уже существовало: {existingCount}\n" +
                        $"• Пропущено (дубликаты): {duplicateCount}\n" +
                        $"• Создано цепочек автоматизмов: {chainsCreated}";

              if (errors != null && errors.Any())
              {
                var errorCount = errors.Count;
                var errorDetails = string.Join("\n", errors.Take(5));
                if (errors.Count > 5)
                  errorDetails += $"\n... и еще {errors.Count - 5} ошибок";

                message += $"\n\nОшибки ({errorCount}):\n{errorDetails}";
              }
            }

            MessageBox.Show(
                message,
                "Результат клонирования",
                MessageBoxButton.OK,
                newCount > 0 || chainsCreated > 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);

            RefreshAllCollections();
          });
        });
      }
      catch (Exception ex)
      {
        IsCloningInProgress = false;
        MessageBox.Show(
            $"Ошибка при клонировании рефлексов: {ex.Message}",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
      }
    }

    public string GetChainDetailedInfo(int chainId)
    {
      if (chainId <= 0 || !AutomatizmChainsSystem.IsInitialized)
        return "Цепочка не привязана";

      try
      {
        var chainSystem = AutomatizmChainsSystem.Instance;
        var chain = chainSystem.GetChain(chainId);
        if (chain == null)
          return $"Цепочка {chainId} не найдена";

        var links = chainSystem.GetChainLinks(chainId);
        if (!links.Any())
          return $"Цепочка {chainId} не содержит звеньев";

        var sb = new StringBuilder();
        sb.AppendLine($"Цепочка: {chain.Name ?? $"ID {chainId}"}");
        if (!string.IsNullOrEmpty(chain.Description))
          sb.AppendLine($"Описание: {chain.Description}");

        sb.AppendLine($"Всего звеньев: {links.Count}");
        sb.AppendLine();

        foreach (var link in links.OrderBy(l => l.ID))
        {
          sb.AppendLine($"Звено {link.ID}:");

          // Получаем информацию об образе действий
          var actionsImage = _actionsImagesSystem.GetActionsImage(link.ActionsImageId);
          if (actionsImage != null)
          {
            if (actionsImage.ActIdList?.Any() == true)
            {
              var allActions = _adaptiveActionsSystem.GetAllAdaptiveActions();
              var actionNames = actionsImage.ActIdList
                  .Select(id => allActions.FirstOrDefault(a => a.Id == id)?.Name ?? $"Действие {id}")
                  .ToList();
              sb.AppendLine($"  Действия: {string.Join(", ", actionNames)}");
            }
          }

          if (!string.IsNullOrEmpty(link.Description))
            sb.AppendLine($"  Описание: {link.Description}");

          if (link.SuccessNextLink > 0)
            sb.AppendLine($"  При успехе → звено {link.SuccessNextLink}");
          if (link.FailureNextLink > 0)
            sb.AppendLine($"  При неудаче → звено {link.FailureNextLink}");

          //sb.AppendLine();
        }

        return sb.ToString();
      }
      catch (Exception ex)
      {
        return $"Ошибка получения информации о цепочке: {ex.Message}";
      }
    }

    public class AutomatizmDisplayItem
    {
      public int ID { get; set; }
      public int BranchID { get; set; }
      public int Usefulness { get; set; }
      public int ActionsImageID { get; set; }
      public int Belief { get; set; }
      public int Energy { get; set; }
      public int Count { get; set; }
      public int NextID { get; set; }
      public int ChainID { get; set; }

      // Поля из узла дерева (условия запуска)
      public int BaseCondition { get; set; }
      public List<int> EmotionIdList { get; set; }
      public int ActivityID { get; set; }
      public List<int> InfluenceActionIds { get; set; }
      public int ToneMoodID { get; set; }
      public int SimbolID { get; set; }
      public int VerbID { get; set; }

      // Текстовые описания для tooltip
      public string BaseConditionText { get; set; }
      public string EmotionText { get; set; }
      public string InfluenceActionsText { get; set; }
      public string ToneMoodText { get; set; }
      public string VerbalText { get; set; }

      // Информация о цепочке (если есть)
      public string ChainInfo { get; set; }
      public string ChainDetailedInfo { get; set; }

      // Образ действия
      public ActionsImageDisplay ActionsImageDisplay { get; set; }
    }

    public class ActionsImageDisplay
    {
      public List<int> ActIdList { get; set; } = new List<int>();
      public List<int> PhraseIdList { get; set; } = new List<int>();
      public int ToneId { get; set; }
      public int MoodId { get; set; }
    }

    public class DescriptionWithLink
    {
      public string Text { get; set; }
      public string LinkText { get; set; } = "Подробнее...";
      public string Url { get; set; } = "https://scorcher.ru/isida/iadaptive_agents_guide.php#automatism";
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

    public DescriptionWithLink CurrentAgentDescription
    {
      get
      {
        return new DescriptionWithLink
        {
          Text = "Редактор автоматизмов, доступен только для просмотра и удаления. Автоматизмы создаются на основе действий безусловных, условных рефлексов, и отзеркаленных действий Оператора."
        };
      }
    }
  }
}