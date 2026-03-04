using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Psychic.Automatism;
using ISIDA.Sensors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using static ISIDA.Actions.AdaptiveActionsSystem;

namespace AIStudio.ViewModels
{
  /// <summary>Элемент для отображения вербальной части автоматизма (тон, настроение, фраза).</summary>
  public sealed class ReflexPhraseBlock
  {
    public string ToneText { get; set; } = "";
    public string MoodText { get; set; } = "";
    public string PhraseText { get; set; } = "";
    /// <summary>Синий (false) или зелёный (true) — для различия одинаковых слогов.</summary>
    public bool UseGreenColor { get; set; }
    /// <summary>Размер шрифта фразы: 16 или 18 — чередуется при одинаковых фразах.</summary>
    public double PhraseFontSize { get; set; } = 16;
  }

  public class AgentAdaptiveActionsViewModel : INotifyPropertyChanged
  {
    private ObservableCollection<AdaptiveAction> _currentActiveActions = new ObservableCollection<AdaptiveAction>();
    public ObservableCollection<AdaptiveAction> CurrentActiveActions
    {
      get => _currentActiveActions;
      private set
      {
        _currentActiveActions = value;
        OnPropertyChanged();
        UpdateGroupedActions();
      }
    }

    public ObservableCollection<AdaptiveAction> GeneticReflexActions { get; } = new ObservableCollection<AdaptiveAction>();
    public ObservableCollection<AdaptiveAction> ConditionedReflexActions { get; } = new ObservableCollection<AdaptiveAction>();
    public ObservableCollection<AdaptiveAction> AutomatizmActions { get; } = new ObservableCollection<AdaptiveAction>();

    private readonly AdaptiveActionsSystem _adaptiveActionsSystem;
    private readonly SensorySystem _sensorySystem;

    public AgentAdaptiveActionsViewModel()
    {
      _adaptiveActionsSystem = AdaptiveActionsSystem.Instance;
      _sensorySystem = SensorySystem.Instance;
    }

    private string _reflexPhrasesText = string.Empty;
    public string ReflexPhrasesText
    {
      get => _reflexPhrasesText;
      private set => SetProperty(ref _reflexPhrasesText, value);
    }

    public ObservableCollection<ReflexPhraseBlock> ReflexPhraseBlocks { get; } = new ObservableCollection<ReflexPhraseBlock>();

    /// <summary>Количество блоков фраз (для привязки видимости в UI).</summary>
    public int ReflexPhraseBlocksCount => ReflexPhraseBlocks.Count;

    /// <summary>Фразы на предыдущем пульсе (для сравнения с текущими).</summary>
    private List<string> _lastPhraseList = new List<string>();
    /// <summary>Число пульсов подряд, когда текущая фраза совпадает с фразой на предыдущем пульсе.</summary>
    private int _pulseCount;
    /// <summary>Сейчас показываем выделение (зелёный, +2px): true = зелёный/18, false = синий/16.</summary>
    private bool _useHighlightStyle;

    private int _minSignificance = 1;
    private int _maxSignificance = 1;

    public int MinSignificance
    {
      get => _minSignificance;
      private set => SetProperty(ref _minSignificance, value);
    }

    public int MaxSignificance
    {
      get => _maxSignificance;
      private set => SetProperty(ref _maxSignificance, value);
    }

    public void UpdateActions(IList<AdaptiveAction> active)
    {
      // Обновляем активные действия
      CurrentActiveActions = new ObservableCollection<AdaptiveAction>(active ?? new List<AdaptiveAction>());

      // Пересчитываем значимость только для активных действий
      var activeSignificances = CurrentActiveActions.Select(a => a.GetSignificance()).ToList();
      MinSignificance = activeSignificances.Any() ? activeSignificances.Min() : 1;
      MaxSignificance = activeSignificances.Any() ? activeSignificances.Max() : 1;
      UpdateReflexPhrasesText();
    }

    private void UpdateGroupedActions()
    {
      GeneticReflexActions.Clear();
      ConditionedReflexActions.Clear();
      AutomatizmActions.Clear();

      int defaultActionId = _adaptiveActionsSystem.DefaultAdaptiveActionId;

      foreach (var action in CurrentActiveActions)
      {
        switch (action.ActivationSource)
        {
          case ActionActivationSource.GeneticReflex:
            GeneticReflexActions.Add(action);
            break;
          case ActionActivationSource.ConditionedReflex:
            ConditionedReflexActions.Add(action);
            break;
          case ActionActivationSource.Automatizm:
            AutomatizmActions.Add(action);
            break;
          case ActionActivationSource.AutomatizmVerbalResponse:
            // Смешанный образ (действие + фраза): невербальная часть — в секцию «Автоматизмы»
            if (action.Id != defaultActionId)
              AutomatizmActions.Add(action);
            break;
        }
      }
      OnPropertyChanged(nameof(GeneticReflexActions));
      OnPropertyChanged(nameof(ConditionedReflexActions));
      OnPropertyChanged(nameof(AutomatizmActions));
    }

    private void UpdateReflexPhrasesText()
    {
      try
      {
        var textBlocks = new List<string>();
        var currentPhraseTexts = new List<string>();

        foreach (var action in CurrentActiveActions.Where(a => a.ActivationSource == ActionActivationSource.AutomatizmVerbalResponse))
        {
          int phraseId = _adaptiveActionsSystem.GetPhraseIdForAction(action.Id);
          string phraseText = GetPhraseText(phraseId);
          if (string.IsNullOrEmpty(phraseText))
            continue;

          currentPhraseTexts.Add(phraseText);
        }

        if (currentPhraseTexts.Count == 0)
        {
          _pulseCount = 0;
          _useHighlightStyle = false;
          _lastPhraseList.Clear();
          ReflexPhraseBlocks.Clear();
          ReflexPhrasesText = "";
          OnPropertyChanged(nameof(ReflexPhraseBlocksCount));
          return;
        }

        bool sameAsPrevious = _lastPhraseList.Count == currentPhraseTexts.Count
          && _lastPhraseList.Zip(currentPhraseTexts, (a, b) => a == b).All(x => x);

        if (sameAsPrevious)
        {
          _pulseCount++;
          if (_pulseCount >= AppConfig.ReflexActionDisplayDuration)
          {
            _useHighlightStyle = !_useHighlightStyle;
            _pulseCount = 0;
          }
        }
        else
          _pulseCount = 0;

        _lastPhraseList = new List<string>(currentPhraseTexts);

        double phraseFontSize = _useHighlightStyle ? 18 : 16;
        ReflexPhraseBlocks.Clear();
        foreach (var action in CurrentActiveActions.Where(a => a.ActivationSource == ActionActivationSource.AutomatizmVerbalResponse))
        {
          int phraseId = _adaptiveActionsSystem.GetPhraseIdForAction(action.Id);
          string phraseText = GetPhraseText(phraseId);
          if (string.IsNullOrEmpty(phraseText))
            continue;

          int displayImageId = _adaptiveActionsSystem.GetActionImageIdForPhraseDisplay(action.Id);
          if (displayImageId <= 0)
            displayImageId = action.Id;
          var (toneText, moodText) = GetToneAndMoodForActionImage(displayImageId);
          ReflexPhraseBlocks.Add(new ReflexPhraseBlock
          {
            ToneText = toneText,
            MoodText = moodText,
            PhraseText = phraseText,
            UseGreenColor = _useHighlightStyle,
            PhraseFontSize = phraseFontSize
          });
          textBlocks.Add($"Тон: {toneText}\nНастроение: {moodText}\nФраза: {phraseText}");
        }

        ReflexPhrasesText = textBlocks.Any() ? string.Join("\n\n", textBlocks) : "";
        OnPropertyChanged(nameof(ReflexPhraseBlocksCount));
      }
      catch (Exception ex)
      {
        ReflexPhrasesText = $"Ошибка получения рефлексов: {ex.Message}";
        OnPropertyChanged(nameof(ReflexPhraseBlocksCount));
      }
    }

    /// <summary>
    /// Возвращает тон и настроение из образа действий автоматизма (action image id).
    /// </summary>
    private static (string tone, string mood) GetToneAndMoodForActionImage(int actionImageId)
    {
      if (!ActionsImagesSystem.IsInitialized || actionImageId <= 0)
        return ("Нормальный", "Нормальное");

      var image = ActionsImagesSystem.Instance.GetActionsImage(actionImageId);
      if (image == null)
        return ("Нормальный", "Нормальное");

      string toneText = ActionsImagesSystem.GetToneText(image.ToneId);
      string moodText = ActionsImagesSystem.GetMoodText(image.MoodId);
      if (string.IsNullOrEmpty(toneText)) toneText = "Нормальный";
      if (string.IsNullOrEmpty(moodText)) moodText = "Нормальное";
      return (toneText, moodText);
    }

    private string GetPhraseText(int phraseId)
    {
      if (phraseId <= 0) return string.Empty;

      try
      {
        return _sensorySystem.VerbalChannel.GetPhraseFromPhraseId(phraseId);
      }
      catch (Exception ex)
      {
        Logger.Error(ex.Message);
      }

      return string.Empty;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
      if (Equals(field, value)) return false;
      field = value;
      OnPropertyChanged(propertyName);
      return true;
    }
  }
}