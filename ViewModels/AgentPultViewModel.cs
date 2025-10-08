using AIStudio.Common;
using ISIDA.Actions;
using ISIDA.Gomeostas;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace AIStudio.ViewModels
{
  public class AgentPultViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly InfluenceActionSystem _influenceActionSystem;
    private ObservableCollection<InfluenceActionItem> _influenceActions;
    private ObservableCollection<InfluenceActionItem> _column1Actions;
    private ObservableCollection<InfluenceActionItem> _column2Actions;
    private AntagonistManager _antagonistManager;

    private ICommand _applyInfluenceCommand;
    public ICommand ApplyInfluenceCommand => _applyInfluenceCommand ??
        (_applyInfluenceCommand = new RelayCommand(ApplyInfluenceActions));

    public AgentPultViewModel()
    {
      _influenceActionSystem = InfluenceActionSystem.Instance;
      _influenceActions = new ObservableCollection<InfluenceActionItem>();
      _column1Actions = new ObservableCollection<InfluenceActionItem>();
      _column2Actions = new ObservableCollection<InfluenceActionItem>();

      LoadInfluenceActions();
    }

    public ObservableCollection<InfluenceActionItem> Column1Actions
    {
      get => _column1Actions;
      set
      {
        _column1Actions = value;
        OnPropertyChanged();
      }
    }

    public ObservableCollection<InfluenceActionItem> Column2Actions
    {
      get => _column2Actions;
      set
      {
        _column2Actions = value;
        OnPropertyChanged();
      }
    }

    public void LoadInfluenceActions()
    {
      _influenceActions.Clear();

      var column1 = new ObservableCollection<InfluenceActionItem>();
      var column2 = new ObservableCollection<InfluenceActionItem>();

      try
      {
        var allActions = _influenceActionSystem.GetAllInfluenceActions();

        int index = 0;
        foreach (var action in allActions)
        {
          var item = new InfluenceActionItem
          {
            Id = action.Id,
            Name = action.Name,
            Description = action.Description,
            IsSelected = false,
            AntagonistIds = new List<int>(action.AntagonistInfluences ?? new List<int>())
          };

          _influenceActions.Add(item);

          // Распределяем по столбцам
          if (index % 2 == 0)
            column1.Add(item);
          else
            column2.Add(item);

          index++;
        }

        // Инициализируем менеджер антагонистов
        _antagonistManager = new AntagonistManager(_influenceActions.Cast<AntagonistItem>().ToList());

        Column1Actions = column1;
        Column2Actions = column2;
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Ошибка загрузки воздействий: {ex.Message}");
      }
    }
    public List<int> GetSelectedInfluenceActions()
    {
      return _influenceActions
          .Where(a => a.IsSelected)
          .Select(a => a.Id)
          .ToList();
    }

    public void ApplyInfluenceActions(object parameter)
    {
      var selectedActions = GetSelectedInfluenceActions();
      if (selectedActions.Count == 0)
      {
        MessageBox.Show("Не выбрано ни одного воздействия",
            "Внимание",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        return;
      }

      // Получаем все выбранные действия
      var actionsToApply = _influenceActionSystem.GetAllInfluenceActions()
          .Where(a => selectedActions.Contains(a.Id))
          .ToList();

      // Применяем воздействия к системе гомеостаза
      foreach (var action in actionsToApply)
      {
        var (success, error) = _influenceActionSystem.ApplyInfluenceAction(action.Id);
        if (!success)
        {
          MessageBox.Show($"Не удалось применить гомеостатическое воздействие: {error}",
              "Ошибка изменения параметров гомеостаза агента",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
          return;
        }
      }
    }

    public void Dispose()
    {
      _antagonistManager?.Dispose();
    }
  }

  public class InfluenceActionItem : AntagonistItem
  {
    // доп свойства
  }
}