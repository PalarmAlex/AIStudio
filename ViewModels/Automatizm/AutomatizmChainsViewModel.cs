using AIStudio.Views;
using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Psychic.Automatism;
using ISIDA.Sensors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace AIStudio.ViewModels
{
  public class AutomatizmChainsViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly AutomatizmSystem _automatizmSystem;
    private readonly AutomatizmChainsSystem _chainsSystem;
    private readonly ActionsImagesSystem _actionsImagesSystem;
    private readonly AdaptiveActionsSystem _adaptiveActionsSystem;
    private readonly SensorySystem _sensorySystem;

    private ObservableCollection<ChainDisplayItem> _allChains = new ObservableCollection<ChainDisplayItem>();
    private ObservableCollection<ChainDisplayItem> _displayChains = new ObservableCollection<ChainDisplayItem>();
    private ICollectionView _chainsView;
    public ICollectionView ChainsView => _chainsView;
    public ICommand ClearFiltersCommand { get; }

    /// <summary>Варианты количества записей на страницу: 100, 500, 1000, 5000, 10000, Все.</summary>
    public List<KeyValuePair<int?, string>> PageSizeOptions { get; } = new List<KeyValuePair<int?, string>>
    {
      new KeyValuePair<int?, string>(100, "100"),
      new KeyValuePair<int?, string>(500, "500"),
      new KeyValuePair<int?, string>(1000, "1000"),
      new KeyValuePair<int?, string>(5000, "5000"),
      new KeyValuePair<int?, string>(10000, "10000"),
      new KeyValuePair<int?, string>(null, "Все")
    };

    private int? _selectedPageSize = 100;
    public int? SelectedPageSize
    {
      get => _selectedPageSize;
      set
      {
        _selectedPageSize = value;
        OnPropertyChanged(nameof(SelectedPageSize));
        RefreshDisplay();
      }
    }

    /// <summary>Текст вида "Показано 100 из 500" для отображения в интерфейсе.</summary>
    public string DisplayCountText
    {
      get
      {
        int filtered = _allChains.Count(FilterChains);
        int shown = Math.Min(filtered, SelectedPageSize ?? int.MaxValue);
        return filtered == shown ? $"Показано: {shown}" : $"Показано: {shown} из {filtered}";
      }
    }

    private string _filterAutomatizmId;
    private string _filterChainId;
    private int _selectedActionFilterId;
    private string _filterPhrases;
    private string _filterImageKind = string.Empty;

    public string FilterAutomatizmId
    {
      get => _filterAutomatizmId;
      set
      {
        _filterAutomatizmId = value;
        OnPropertyChanged(nameof(FilterAutomatizmId));
        ApplyFilters();
      }
    }

    public string FilterChainId
    {
      get => _filterChainId;
      set
      {
        _filterChainId = value;
        OnPropertyChanged(nameof(FilterChainId));
        ApplyFilters();
      }
    }

    public int SelectedActionFilterId
    {
      get => _selectedActionFilterId;
      set
      {
        _selectedActionFilterId = value;
        OnPropertyChanged(nameof(SelectedActionFilterId));
        ApplyFilters();
      }
    }

    public string FilterPhrases
    {
      get => _filterPhrases;
      set
      {
        _filterPhrases = value;
        OnPropertyChanged(nameof(FilterPhrases));
        ApplyFilters();
      }
    }
   
    public string FilterImageKind
    {
      get => _filterImageKind;
      set
      {
        _filterImageKind = value;
        OnPropertyChanged(nameof(FilterImageKind));
        ApplyFilters();
      }
    }

    public List<KeyValuePair<string, string>> ImageKindOptions { get; } = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("", "Все типы"),
            new KeyValuePair<string, string>("0", "Объективное действие"),
            new KeyValuePair<string, string>("1", "Субъективное предположение")
        };

    public List<KeyValuePair<int, string>> ActionFilterOptions { get; } = new List<KeyValuePair<int, string>>();

    public AutomatizmChainsViewModel(
        AutomatizmSystem automatizmSystem,
        AutomatizmChainsSystem chainsSystem,
        ActionsImagesSystem actionsImagesSystem,
        AdaptiveActionsSystem adaptiveActionsSystem,
        SensorySystem sensorySystem)
    {
      _automatizmSystem = automatizmSystem ?? throw new ArgumentNullException(nameof(automatizmSystem));
      _chainsSystem = chainsSystem ?? throw new ArgumentNullException(nameof(chainsSystem));
      _actionsImagesSystem = actionsImagesSystem ?? throw new ArgumentNullException(nameof(actionsImagesSystem));
      _adaptiveActionsSystem = adaptiveActionsSystem ?? throw new ArgumentNullException(nameof(adaptiveActionsSystem));
      _sensorySystem = sensorySystem ?? throw new ArgumentNullException(nameof(sensorySystem));

      _chainsView = CollectionViewSource.GetDefaultView(_displayChains);

      ClearFiltersCommand = new RelayCommand(ClearFilters);

      InitializeActionFilterOptions();
      FilterImageKind = string.Empty;
      LoadChainsData();
    }

    private void InitializeActionFilterOptions()
    {
      ActionFilterOptions.Clear();
      ActionFilterOptions.Add(new KeyValuePair<int, string>(0, "Все действия"));

      var allActions = _adaptiveActionsSystem.GetAllAdaptiveActions();
      foreach (var action in allActions.OrderBy(a => a.Name))
      {
        ActionFilterOptions.Add(new KeyValuePair<int, string>(action.Id, action.Name));
      }
    }

    private void LoadChainsData()
    {
      try
      {
        _allChains.Clear();

        var allAutomatizms = _automatizmSystem.GetAllAutomatizms();
        var allChains = _chainsSystem.GetAllAutomatizmChains();

        foreach (var automatizm in allAutomatizms)
        {
          int chainId = 0;
          if (AutomatizmChainsSystem.IsInitialized)
            chainId = automatizm.NextID;

          if (chainId > 0)
          {
            var chain = _chainsSystem.GetChain(chainId);
            if (chain != null)
            {
              foreach (var link in chain.Links.OrderBy(l => l.ID))
              {
                var displayItem = CreateChainDisplayItem(automatizm, chain, link);
                if (displayItem != null)
                  _allChains.Add(displayItem);
              }
            }
          }
        }

        foreach (var chain in allChains.Values)
        {
          var chainAutomatizms = allAutomatizms
              .Where(a => _chainsSystem.GetChainByActionsImage(a.ActionsImageID) == chain.ID)
              .ToList();

          if (!chainAutomatizms.Any())
          {
            foreach (var link in chain.Links.OrderBy(l => l.ID))
            {
              var displayItem = CreateChainDisplayItem(null, chain, link);
              if (displayItem != null)
                _allChains.Add(displayItem);
            }
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Error(ex.Message);
      }

      RefreshDisplay();
    }

    private void RefreshDisplay()
    {
      var filtered = _allChains.Where(FilterChains).ToList();
      int take = SelectedPageSize ?? int.MaxValue;
      _displayChains.Clear();
      foreach (var item in filtered.Take(take))
        _displayChains.Add(item);
      OnPropertyChanged(nameof(DisplayCountText));
    }

    private ChainDisplayItem CreateChainDisplayItem(
        Automatizm automatizm,
        AutomatizmChainsSystem.AutomatizmChain chain,
        AutomatizmChainsSystem.ChainLink link)
    {
      try
      {
        if (automatizm == null)
          return null;

        int automatizmId = automatizm.ID;
        int automatizmActionsImageId = automatizm.ActionsImageID;
        int chainId = chain?.ID ?? 0;
        int linkId = link?.ID ?? 0;
        int linkActionsImageId = link?.ActionsImageId ?? 0;

        var automatizmActionsImage = automatizmActionsImageId > 0 ?
            _actionsImagesSystem.GetActionsImage(automatizmActionsImageId) : null;

        var linkActionsImage = linkActionsImageId > 0 ?
            _actionsImagesSystem.GetActionsImage(linkActionsImageId) : null;

        var displayImage = linkActionsImage ?? automatizmActionsImage;

        if (displayImage == null && automatizmActionsImage == null && linkActionsImage == null)
          return null;

        string actionsText = GetActionsText(displayImage?.ActIdList);
        var actionIds = displayImage?.ActIdList?.ToList() ?? new List<int>();
        string phrasesText = GetPhrasesText(displayImage?.PhraseIdList);
        string toneMoodText = GetToneMoodText(displayImage?.ToneId ?? 0, displayImage?.MoodId ?? 0);
        string imageKindText = GetImageKindText(displayImage?.Kind ?? 0);
        string imageKindTooltip = GetImageKindTooltip(displayImage?.Kind ?? 0);

        return new ChainDisplayItem
        {
          AutomatizmId = automatizmId,
          AutomatizmActionsImageId = automatizmActionsImageId,
          ChainId = chainId,
          LinkId = linkId,
          LinkActionsImageId = linkActionsImageId,
          ImageKindText = imageKindText,
          ImageKindTooltip = imageKindTooltip,
          ActionIds = actionIds,
          ActionsText = actionsText,
          PhrasesText = phrasesText,
          ToneMoodText = toneMoodText,
          SuccessNextLink = link?.SuccessNextLink ?? 0,
          FailureNextLink = link?.FailureNextLink ?? 0,
          ChainUsefulness = link?.ChainUsefulness ?? 1
        };
      }
      catch (Exception ex)
      {
        Logger.Error(ex.Message);
        return null;
      }
    }

    private string GetActionsText(List<int> actionIds)
    {
      if (actionIds == null || !actionIds.Any())
        return string.Empty;

      try
      {
        var allActions = _adaptiveActionsSystem.GetAllAdaptiveActions();
        var actionNames = new List<string>();

        foreach (var actionId in actionIds)
        {
          var action = allActions.FirstOrDefault(a => a.Id == actionId);
          if (action != null)
            actionNames.Add(action.Name);
          else
            actionNames.Add($"Действие #{actionId}");
        }

        return string.Join(", ", actionNames);
      }
      catch (Exception ex)
      {
        Logger.Error(ex.Message);
        return $"Действия: {string.Join(", ", actionIds)}";
      }
    }

    private string GetPhrasesText(List<int> phraseIds)
    {
      if (phraseIds == null || !phraseIds.Any())
        return string.Empty;

      try
      {
        var phraseTexts = new List<string>();

        foreach (var phraseId in phraseIds)
        {
          // Используем прямой метод получения фразы по ID
          string phraseText = _sensorySystem.VerbalChannel.GetPhraseFromPhraseId(phraseId);

          if (!string.IsNullOrEmpty(phraseText))
            phraseTexts.Add($"\"{phraseText}\"");
          else
            phraseTexts.Add($"[ID:{phraseId}]");
        }

        return string.Join(", ", phraseTexts);
      }
      catch (Exception ex)
      {
        Logger.Error(ex.Message);
        return $"Фразы: {string.Join(", ", phraseIds)}";
      }
    }

    private string GetToneMoodText(int toneId, int moodId)
    {
      if (toneId == 0 && moodId == 0)
        return string.Empty;

      var toneText = ActionsImagesSystem.GetToneText(toneId);
      var moodText = ActionsImagesSystem.GetMoodText(moodId);

      if (!string.IsNullOrEmpty(toneText) && !string.IsNullOrEmpty(moodText))
        return $"{toneText} - {moodText}";
      else if (!string.IsNullOrEmpty(toneText))
        return toneText;
      else if (!string.IsNullOrEmpty(moodText))
        return moodText;
      else
        return string.Empty;
    }

    private string GetImageKindText(int kind)
    {
      switch (kind)
      {
        case 0:
          return "Объективное";
        case 1:
          return "Субъективное";
        default:
          return $"Тип {kind}";
      }
    }

    private string GetImageKindTooltip(int kind)
    {
      switch (kind)
      {
        case 0:
          return "Объективное действие (реально воспринятое из Пульта)";
        case 1:
          return "Субъективное предположение (Правило из сновидения)";
        default:
          return $"Неизвестный тип образа: {kind}";
      }
    }

    private bool FilterChains(object item)
    {
      if (!(item is ChainDisplayItem chainItem))
        return false;

      // Фильтр по ID автоматизма
      if (!string.IsNullOrEmpty(FilterAutomatizmId) &&
          !string.IsNullOrWhiteSpace(FilterAutomatizmId))
      {
        if (!int.TryParse(FilterAutomatizmId, out int filterId) ||
            chainItem.AutomatizmId != filterId)
          return false;
      }

      // Фильтр по ID цепочки
      if (!string.IsNullOrEmpty(FilterChainId) &&
          !string.IsNullOrWhiteSpace(FilterChainId))
      {
        if (!int.TryParse(FilterChainId, out int filterId) ||
            chainItem.ChainId != filterId)
          return false;
      }

      // Фильтр по действиям
      if (SelectedActionFilterId > 0)
      {
        if (chainItem.ActionIds == null || !chainItem.ActionIds.Contains(SelectedActionFilterId))
          return false;
      }

      // Фильтр по фразам (поиск подстроки)
      if (!string.IsNullOrEmpty(FilterPhrases) &&
          !string.IsNullOrWhiteSpace(FilterPhrases))
      {
        if (string.IsNullOrEmpty(chainItem.PhrasesText) ||
            chainItem.PhrasesText.IndexOf(FilterPhrases, StringComparison.OrdinalIgnoreCase) < 0)
          return false;
      }

      // Фильтр по типу образа
      if (!string.IsNullOrEmpty(FilterImageKind) &&
          !string.IsNullOrWhiteSpace(FilterImageKind))
      {
        string expectedKind;
        if (FilterImageKind == "0")
          expectedKind = "Объективное";
        else if (FilterImageKind == "1")
          expectedKind = "Субъективное";
        else
          expectedKind = FilterImageKind;

        if (!chainItem.ImageKindText.Equals(expectedKind, StringComparison.OrdinalIgnoreCase))
          return false;
      }

      return true;
    }

    private void ApplyFilters()
    {
      RefreshDisplay();
    }

    private void ClearFilters(object parameter = null)
    {
      FilterAutomatizmId = string.Empty;
      FilterChainId = string.Empty;
      SelectedActionFilterId = 0;
      FilterPhrases = string.Empty;
      FilterImageKind = string.Empty;
    }

    public class ChainDisplayItem
    {
      public int AutomatizmId { get; set; }
      public int AutomatizmActionsImageId { get; set; }
      public int ChainId { get; set; }
      public int LinkId { get; set; }
      public int LinkActionsImageId { get; set; }
      public string ImageKindText { get; set; }
      public string ImageKindTooltip { get; set; }
      public List<int> ActionIds { get; set; }
      public string ActionsText { get; set; }
      public string PhrasesText { get; set; }
      public string ToneMoodText { get; set; }
      public int SuccessNextLink { get; set; }
      public int FailureNextLink { get; set; }
      public int ChainUsefulness { get; set; }
    }

  }
}