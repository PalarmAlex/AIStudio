using ISIDA.Actions;
using ISIDA.Sensors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using static ISIDA.Actions.AdaptiveActionsSystem;

namespace AIStudio.ViewModels
{
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

      // Группируем действия по типам источников
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
        }
      }
      OnPropertyChanged(nameof(GeneticReflexActions));
      OnPropertyChanged(nameof(ConditionedReflexActions));
    }

    private void UpdateReflexPhrasesText()
    {
      try
      {
        var reflexPhrases = new List<string>();

        // Собираем фразы
        foreach (var action in CurrentActiveActions)
        {

          // Получаем фразу, связанную с действием
          int phraseId = _adaptiveActionsSystem.GetPhraseIdForAction(action.Id);
          string phraseText = GetPhraseText(phraseId);

          if (!string.IsNullOrEmpty(phraseText))
            reflexPhrases.Add(phraseText);
        }

        // Формируем текст для отображения
        if (reflexPhrases.Any())
          ReflexPhrasesText = $"{string.Join(", ", reflexPhrases)}";
        else
          ReflexPhrasesText = "";
      }
      catch (Exception ex)
      {
        ReflexPhrasesText = $"Ошибка получения рефлексов: {ex.Message}";
      }
    }

    private string GetPhraseText(int phraseId)
    {
      if (phraseId <= 0) return string.Empty;

      try
      {
        var allphrases = _sensorySystem.VerbalChannel.GetAllPhrases();
        if (allphrases.TryGetValue(phraseId, out string phraseText))
        {
          return phraseText;
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Ошибка получения фразы {phraseId}: {ex.Message}");
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