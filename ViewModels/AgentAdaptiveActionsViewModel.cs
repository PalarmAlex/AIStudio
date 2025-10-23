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