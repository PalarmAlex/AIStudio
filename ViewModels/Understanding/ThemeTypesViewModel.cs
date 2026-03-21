using AIStudio.Common;
using ISIDA.Common;
using ISIDA.Gomeostas;
using ISIDA.Psychic.Understanding;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace AIStudio.ViewModels
{
  /// <summary>Строка таблицы типов тем: Id и описание задаются в коде движка; в theme_types.dat — вес и список инфо-функций.</summary>
  public class ThemeTypeItem : INotifyPropertyChanged
  {
    private int _id;
    private string _description;
    private int _defaultWeight = 2;
    private List<int> _allowedInfoFuncIds = new List<int>();

    public int Id { get => _id; set { if (_id != value) { _id = value; OnPropertyChanged(nameof(Id)); } } }
    public string Description { get => _description; set { if (_description != value) { _description = value; OnPropertyChanged(nameof(Description)); } } }
    public int DefaultWeight { get => _defaultWeight; set { if (_defaultWeight != value) { _defaultWeight = value; OnPropertyChanged(nameof(DefaultWeight)); } } }

    public IReadOnlyList<int> AllowedInfoFuncIds => _allowedInfoFuncIds;

    public void SetAllowedInfoFuncIds(IEnumerable<int> ids)
    {
      _allowedInfoFuncIds = ids?.Where(x => x > 0).Distinct().OrderBy(x => x).ToList() ?? new List<int>();
      OnPropertyChanged(nameof(AllowedInfoFuncIds));
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
  }

  public class ThemeTypesViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly GomeostasSystem _gomeostas;
    private int _currentAgentStage;

    public bool IsStageFour => _currentAgentStage == 4;
    public string CurrentAgentTitle => "Темы мышления";

    public DescriptionWithLink CurrentAgentDescription => new DescriptionWithLink
    {
      Text = "Темы мышления агента. Состав тем и их Id фиксированы в коде движка; здесь задаются только вес по умолчанию и списки инфо-функций. "
    };

    public ObservableCollection<ThemeTypeItem> ThemeTypes { get; } = new ObservableCollection<ThemeTypeItem>();

    public ICommand SaveCommand { get; }

    public ThemeTypesViewModel(GomeostasSystem gomeostas)
    {
      _gomeostas = gomeostas ?? throw new ArgumentNullException(nameof(gomeostas));
      SaveCommand = new RelayCommand(SaveData);
      GlobalTimer.PulsationStateChanged += OnPulsationStateChanged;
      LoadData();
    }

    private void OnPulsationStateChanged()
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        OnPropertyChanged(nameof(IsEditingEnabled));
        OnPropertyChanged(nameof(PulseWarningMessage));
        OnPropertyChanged(nameof(WarningMessageColor));
        OnPropertyChanged(nameof(IsReadOnlyMode));
      });
    }

    #region Блокировка страницы

    public bool IsEditingEnabled => IsStageFour && !GlobalTimer.IsPulsationRunning;
    public bool IsReadOnlyMode => !IsEditingEnabled;

    public string PulseWarningMessage =>
        !IsStageFour
            ? "[КРИТИЧНО] Редактирование тем мышления доступно только в стадии 4"
            : GlobalTimer.IsPulsationRunning
                ? "Редактирование доступно только при выключенной пульсации"
                : string.Empty;

    public Brush WarningMessageColor =>
        !IsStageFour ? Brushes.Red : Brushes.Gray;

    #endregion

    private void LoadData()
    {
      try
      {
        var agentInfo = _gomeostas.GetAgentState();
        _currentAgentStage = agentInfo?.EvolutionStage ?? 0;

        ThemeTypes.Clear();
        if (ThemeImageSystem.IsInitialized)
        {
          foreach (var t in ThemeImageSystem.Instance.GetFixedCatalogThemeTypesForUi())
          {
            var item = new ThemeTypeItem
            {
              Id = t.Id,
              Description = t.Description ?? "",
              DefaultWeight = t.DefaultWeight
            };
            item.SetAllowedInfoFuncIds(t.AllowedInfoFuncIds);
            ThemeTypes.Add(item);
          }
        }

        OnPropertyChanged(nameof(ThemeTypes));
        OnPropertyChanged(nameof(IsStageFour));
        OnPropertyChanged(nameof(IsEditingEnabled));
        OnPropertyChanged(nameof(PulseWarningMessage));
        OnPropertyChanged(nameof(WarningMessageColor));
        OnPropertyChanged(nameof(CurrentAgentTitle));
        OnPropertyChanged(nameof(IsReadOnlyMode));
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка загрузки тем мышления: {ex.Message}", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void SaveData(object parameter)
    {
      if (!ThemeImageSystem.IsInitialized)
      {
        MessageBox.Show("Система тем мышления не инициализирована.", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      var rows = ThemeTypes
          .Select(t => (t.Id, t.DefaultWeight, (IReadOnlyList<int>)t.AllowedInfoFuncIds.ToList()))
          .ToList();
      var (success, error) = ThemeImageSystem.Instance.SaveFixedCatalogThemeTypes(rows);

      if (success)
      {
        LoadData();
        MessageBox.Show("Темы мышления успешно сохранены.", "Сохранение завершено",
            MessageBoxButton.OK, MessageBoxImage.Information);
      }
      else
      {
        MessageBox.Show($"Не удалось сохранить темы мышления:\n{error}",
            "Ошибка сохранения",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    public class DescriptionWithLink
    {
      public string Text { get; set; }
      public string LinkText { get; set; } = "Подробнее...";
      public string Url { get; set; } = "https://scorcher.ru/isida/iadaptive_agents_guide.php#understanding";
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
  }
}
