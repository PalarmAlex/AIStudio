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

    public ObservableCollection<AdaptiveAction> ElementaryActions { get; } = new ObservableCollection<AdaptiveAction>();
    public ObservableCollection<AdaptiveAction> GeneticReflexActions { get; } = new ObservableCollection<AdaptiveAction>();
    public ObservableCollection<AdaptiveAction> ConditionedReflexActions { get; } = new ObservableCollection<AdaptiveAction>();

    private readonly AdaptiveActionsSystem _adaptiveActionsSystem;
    private readonly SensorySystem _sensorySystem;

    public AgentAdaptiveActionsViewModel()
    {
      _adaptiveActionsSystem = AdaptiveActionsSystem.Instance;
      _sensorySystem = SensorySystem.Instance;
    }

    private string _reflexWordsText = string.Empty;
    public string ReflexWordsText
    {
      get => _reflexWordsText;
      private set => SetProperty(ref _reflexWordsText, value);
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
      UpdateReflexWordsText();
    }

    private void UpdateGroupedActions()
    {
      ElementaryActions.Clear();
      GeneticReflexActions.Clear();
      ConditionedReflexActions.Clear();

      // Группируем действия по типам источников
      foreach (var action in CurrentActiveActions)
      {
        switch (action.ActivationSource)
        {
          case ActionActivationSource.Elementary:
            ElementaryActions.Add(action);
            break;
          case ActionActivationSource.GeneticReflex:
            GeneticReflexActions.Add(action);
            break;
          case ActionActivationSource.ConditionedReflex:
            ConditionedReflexActions.Add(action);
            break;
        }
      }

      OnPropertyChanged(nameof(ElementaryActions));
      OnPropertyChanged(nameof(GeneticReflexActions));
      OnPropertyChanged(nameof(ConditionedReflexActions));
    }

    private void UpdateReflexWordsText()
    {
      try
      {
        var reflexWords = new List<string>();

        // Собираем слова только для рефлекторных действий (не элементарных)
        foreach (var action in CurrentActiveActions)
        {
          if (!action.IsElementary)
          {
            // Получаем слово, связанное с действием
            int wordId = _adaptiveActionsSystem.GetWordIdForAction(action.Id);
            string wordText = GetWordText(wordId);

            if (!string.IsNullOrEmpty(wordText))
            {
              reflexWords.Add(wordText);
            }
          }
        }

        // Формируем текст для отображения
        if (reflexWords.Any())
          ReflexWordsText = $"{string.Join(", ", reflexWords)}";
        else
          ReflexWordsText = "";
      }
      catch (Exception ex)
      {
        ReflexWordsText = $"Ошибка получения рефлексов: {ex.Message}";
      }
    }

    private string GetWordText(int wordId)
    {
      if (wordId <= 0) return string.Empty;

      try
      {
        var allWords = _sensorySystem.VerbalChannel.GetAllWords();
        if (allWords.TryGetValue(wordId, out string wordText))
        {
          return wordText;
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Ошибка получения слова {wordId}: {ex.Message}");
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