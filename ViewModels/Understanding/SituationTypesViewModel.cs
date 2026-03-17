using AIStudio.Common;
using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Gomeostas;
using ISIDA.Psychic.Automatism;
using ISIDA.Psychic.Understanding;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace AIStudio.ViewModels
{
  public class SituationTypesViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly SituationTypeSystem _situationTypeSystem;
    private readonly InfluenceActionSystem _influenceActionSystem;
    private readonly GomeostasSystem _gomeostas;
    private int _currentAgentStage;

    public bool IsStageFour => _currentAgentStage == 4;

    public string CurrentAgentTitle => "Справочник типов ситуаций";

    public DescriptionWithLink CurrentAgentDescription => new DescriptionWithLink
    {
      Text = "Справочник типов ситуаций: слоты по настроению (ID 11–20), воздействию (ID 21–40), привязки тем (ID 41–60). "
    };

    private ObservableCollection<SituationTypeRecord> _moodRecords = new ObservableCollection<SituationTypeRecord>();
    private ObservableCollection<SituationTypeRecord> _influenceRecords = new ObservableCollection<SituationTypeRecord>();
    private ObservableCollection<SituationTypeRecord> _themeRecords = new ObservableCollection<SituationTypeRecord>();

    public ObservableCollection<SituationTypeRecord> MoodRecords => _moodRecords;
    public ObservableCollection<SituationTypeRecord> InfluenceRecords => _influenceRecords;
    /// <summary>Привязка тем к кодам ID 41–60</summary>
    public ObservableCollection<SituationTypeRecord> ThemeRecords => _themeRecords;

    private string _defaultRecordsText = "";

    /// <summary>Текстовый блок: записи по умолчанию (ID 1–10), три столбца — ID, Привязка, Описание</summary>
    public string DefaultRecordsText
    {
      get => _defaultRecordsText;
      private set { _defaultRecordsText = value; OnPropertyChanged(nameof(DefaultRecordsText)); }
    }

    public ICommand SaveMoodCommand { get; }
    public ICommand SaveInfluenceCommand { get; }
    public ICommand ClearMoodCommand { get; }
    public ICommand ClearInfluenceCommand { get; }
    public ICommand SaveThemeCommand { get; }
    public ICommand ClearThemeCommand { get; }

    public SituationTypesViewModel(
      GomeostasSystem gomeostasSystem,
      SituationTypeSystem situationTypeSystem,
      InfluenceActionSystem influenceActionSystem)
    {
      _gomeostas = gomeostasSystem ?? throw new ArgumentNullException(nameof(gomeostasSystem));
      _situationTypeSystem = situationTypeSystem;
      _influenceActionSystem = influenceActionSystem;

      SaveMoodCommand = new RelayCommand(SaveMood);
      SaveInfluenceCommand = new RelayCommand(SaveInfluence);
      ClearMoodCommand = new RelayCommand(ClearMood);
      ClearInfluenceCommand = new RelayCommand(ClearInfluence);
      SaveThemeCommand = new RelayCommand(SaveTheme);
      ClearThemeCommand = new RelayCommand(ClearTheme);

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
            ? "[КРИТИЧНО] Редактирование справочника типов ситуаций доступно только в стадии 4"
            : GlobalTimer.IsPulsationRunning
                ? "Редактирование доступно только при выключенной пульсации"
                : string.Empty;

    public Brush WarningMessageColor =>
        !IsStageFour ? Brushes.Red : Brushes.Gray;

    #endregion

    #region Опции для ComboBox

    public List<KeyValuePair<int, string>> MoodCellOptions { get; private set; } = new List<KeyValuePair<int, string>>();
    public List<KeyValuePair<int, string>> InfluenceCellOptions { get; private set; } = new List<KeyValuePair<int, string>>();
    /// <summary>Темы для слотов 41–60 (темы, не занятые в 1–10, + «—»)</summary>
    public List<KeyValuePair<int, string>> ThemeSlotCellOptions { get; private set; } = new List<KeyValuePair<int, string>>();

    private void LoadCellOptions()
    {
      MoodCellOptions = new List<KeyValuePair<int, string>> { new KeyValuePair<int, string>(SituationTypeSystem.EmptySlotValue, "—") };
      if (ActionsImagesSystem.IsInitialized)
      {
        var moods = ActionsImagesSystem.GetMoodList();
        if (moods != null)
        {
          foreach (var kv in moods.OrderBy(x => x.Key))
            MoodCellOptions.Add(new KeyValuePair<int, string>(kv.Key, $"{kv.Key}: {kv.Value}"));
        }
      }

      InfluenceCellOptions = new List<KeyValuePair<int, string>> { new KeyValuePair<int, string>(SituationTypeSystem.EmptySlotValue, "—") };
      if (_influenceActionSystem != null)
      {
        var influences = _influenceActionSystem.GetAllInfluenceActions();
        if (influences != null)
        {
          foreach (var inf in influences.OrderBy(x => x.Id))
            InfluenceCellOptions.Add(new KeyValuePair<int, string>(inf.Id, $"{inf.Id}: {inf.Name}"));
        }
      }

      var allThemes = ThemeImageSystem.GetDefaultThemeTypesForSettings();
      var usedInDefaults = _situationTypeSystem != null && SituationTypeSystem.IsInitialized
        ? SituationTypeSystem.Instance.GetThemeTypeIdsUsedInDefaultSlots().ToList()
        : new List<int>();
      ThemeSlotCellOptions = new List<KeyValuePair<int, string>> { new KeyValuePair<int, string>(-1, "—") };
      if (allThemes != null)
      {
        foreach (var t in allThemes.OrderBy(x => x.Id))
        {
          if (!usedInDefaults.Contains(t.Id))
            ThemeSlotCellOptions.Add(new KeyValuePair<int, string>(t.Id, $"{t.Id}: {t.Description}"));
        }
      }

      OnPropertyChanged(nameof(MoodCellOptions));
      OnPropertyChanged(nameof(InfluenceCellOptions));
      OnPropertyChanged(nameof(ThemeSlotCellOptions));
    }

    #endregion

    private void LoadData()
    {
      try
      {
        var agentInfo = _gomeostas.GetAgentState();
        _currentAgentStage = agentInfo?.EvolutionStage ?? 0;

        if (_situationTypeSystem != null && ISIDA.Psychic.Understanding.SituationTypeSystem.IsInitialized)
        {
          _situationTypeSystem.EnsureSlotsAndSaveIfNeeded();
          var all = _situationTypeSystem.GetAll().ToDictionary(r => r.Id);

          _moodRecords.Clear();
          for (int id = 11; id <= 20; id++)
          {
            _moodRecords.Add(all.TryGetValue(id, out var r) ? r : new SituationTypeRecord { Id = id, MoodId = SituationTypeSystem.EmptySlotValue, InfluenceId = SituationTypeSystem.EmptySlotValue, ThemeTypeId = -1, Description = "" });
          }

          _influenceRecords.Clear();
          for (int id = 21; id <= 40; id++)
          {
            var r = all.TryGetValue(id, out var rec) ? rec : new SituationTypeRecord { Id = id, MoodId = SituationTypeSystem.EmptySlotValue, InfluenceId = SituationTypeSystem.EmptySlotValue, ThemeTypeId = -1, Description = "" };
            _influenceRecords.Add(r);
          }

          _themeRecords.Clear();
          for (int id = 41; id <= 60; id++)
          {
            var r = all.TryGetValue(id, out var rec) ? rec : new SituationTypeRecord { Id = id, MoodId = SituationTypeSystem.EmptySlotValue, InfluenceId = SituationTypeSystem.EmptySlotValue, ThemeTypeId = -1, Description = "" };
            _themeRecords.Add(r);
          }

          BuildDefaultRecordsText(all);
        }

        LoadCellOptions();

        OnPropertyChanged(nameof(MoodRecords));
        OnPropertyChanged(nameof(InfluenceRecords));
        OnPropertyChanged(nameof(DefaultRecordsText));
        OnPropertyChanged(nameof(ThemeRecords));
        OnPropertyChanged(nameof(IsStageFour));
        OnPropertyChanged(nameof(IsEditingEnabled));
        OnPropertyChanged(nameof(PulseWarningMessage));
        OnPropertyChanged(nameof(WarningMessageColor));
        OnPropertyChanged(nameof(CurrentAgentTitle));
        OnPropertyChanged(nameof(IsReadOnlyMode));
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка загрузки справочника типов ситуаций: {ex.Message}", "Ошибка",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void BuildDefaultRecordsText(Dictionary<int, SituationTypeRecord> byId)
    {
      var sb = new StringBuilder();
      for (int id = 1; id <= 10; id++)
      {
        if (byId.TryGetValue(id, out var r))
        {
          var line = $"{r.Id}: {r.Description ?? ""}";
          if (r.ThemeTypeId > 0)
            line += $" (ID: {r.ThemeTypeId})";
          sb.AppendLine(line);
        }
        else
          sb.AppendLine($"{id}: (нет)");
      }
      DefaultRecordsText = sb.ToString().TrimEnd();
    }

    public void RefreshData() => LoadData();

    private void SaveTheme(object parameter)
    {
      SaveWithThemeValidation("привязки тем (41–60)");
    }

    private void SaveWithThemeValidation(string context)
    {
      try
      {
        if (_situationTypeSystem == null || !SituationTypeSystem.IsInitialized)
        {
          MessageBox.Show("Система типов ситуаций не инициализирована", "Ошибка",
              MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        _situationTypeSystem.UpdateFromRecords(_moodRecords);
        _situationTypeSystem.UpdateFromRecords(_influenceRecords);
        _situationTypeSystem.UpdateFromRecords(_themeRecords);

        var allWithTheme = _situationTypeSystem.GetAll()
          .Where(r => r != null && ((r.Id >= 1 && r.Id <= 10) || (r.Id >= 41 && r.Id <= 60)))
          .ToList();
        var (validTheme, themeErr) = _situationTypeSystem.ValidateThemeTypeIdUniqueness(allWithTheme);
        if (!validTheme)
        {
          MessageBox.Show(themeErr, "Дубликат темы",
              MessageBoxButton.OK, MessageBoxImage.Warning);
          return;
        }

        var (valid, dupError) = _situationTypeSystem.ValidateRecordsNoDuplicates(_moodRecords, _influenceRecords);
        if (!valid)
        {
          MessageBox.Show($"Невозможно сохранить: {dupError}", "Дубликаты",
              MessageBoxButton.OK, MessageBoxImage.Warning);
          return;
        }

        var (success, error) = _situationTypeSystem.Save();

        if (success)
        {
          LoadData();
          MessageBox.Show($"Справочник ({context}) успешно сохранён",
              "Сохранение завершено",
              MessageBoxButton.OK,
              MessageBoxImage.Information);
        }
        else
        {
          MessageBox.Show($"Не удалось сохранить справочник:\n{error}",
              "Ошибка сохранения",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при сохранении:\n{ex.Message}",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
      }
    }

    private void ClearTheme(object parameter)
    {
      if (MessageBox.Show(
          "Очистить привязки тем (ThemeTypeId и описание) для всех 20 слотов (ID 41–60)?",
          "Подтверждение",
          MessageBoxButton.YesNo,
          MessageBoxImage.Question) != MessageBoxResult.Yes)
        return;

      foreach (var r in _themeRecords)
      {
        r.ThemeTypeId = -1;
        r.Description = "";
      }
      SaveWithThemeValidation("привязки тем (очищены)");
    }

    private void SaveMood(object parameter)
    {
      SaveAndReload("слоты по настроению");
    }

    private void SaveInfluence(object parameter)
    {
      SaveAndReload("слоты по воздействию");
    }

    private void SaveAndReload(string context)
    {
      try
      {
        if (_situationTypeSystem == null || !ISIDA.Psychic.Understanding.SituationTypeSystem.IsInitialized)
        {
          MessageBox.Show("Система типов ситуаций не инициализирована", "Ошибка",
              MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        _situationTypeSystem.UpdateFromRecords(_moodRecords);
        _situationTypeSystem.UpdateFromRecords(_influenceRecords);
        _situationTypeSystem.UpdateFromRecords(_themeRecords);

        var allWithTheme = _situationTypeSystem.GetAll()
          .Where(r => r != null && ((r.Id >= 1 && r.Id <= 10) || (r.Id >= 41 && r.Id <= 60)))
          .ToList();
        var (validTheme, themeErr) = _situationTypeSystem.ValidateThemeTypeIdUniqueness(allWithTheme);
        if (!validTheme)
        {
          MessageBox.Show(themeErr, "Дубликат темы",
              MessageBoxButton.OK, MessageBoxImage.Warning);
          return;
        }

        var (valid, dupError) = _situationTypeSystem.ValidateRecordsNoDuplicates(_moodRecords, _influenceRecords);
        if (!valid)
        {
          MessageBox.Show($"Невозможно сохранить: {dupError}", "Дубликаты",
              MessageBoxButton.OK, MessageBoxImage.Warning);
          return;
        }

        var (success, error) = _situationTypeSystem.Save();

        if (success)
        {
          LoadData();
          MessageBox.Show($"Справочник ({context}) успешно сохранён",
              "Сохранение завершено",
              MessageBoxButton.OK,
              MessageBoxImage.Information);
        }
        else
        {
          MessageBox.Show($"Не удалось сохранить справочник:\n{error}",
              "Ошибка сохранения",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при сохранении:\n{ex.Message}",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
      }
    }

    private void ClearMood(object parameter)
    {
      if (MessageBox.Show(
          "Очистить значения (MoodId и описание) для всех 10 слотов настроения?",
          "Подтверждение",
          MessageBoxButton.YesNo,
          MessageBoxImage.Question) != MessageBoxResult.Yes)
        return;

      foreach (var r in _moodRecords)
      {
        r.MoodId = SituationTypeSystem.EmptySlotValue;
        r.InfluenceId = SituationTypeSystem.EmptySlotValue;
        r.Description = "";
      }
      SaveAndReload("слоты по настроению (очищены)");
    }

    private void ClearInfluence(object parameter)
    {
      if (MessageBox.Show(
          "Очистить значения (InfluenceId и описание) для всех 20 слотов воздействия?",
          "Подтверждение",
          MessageBoxButton.YesNo,
          MessageBoxImage.Question) != MessageBoxResult.Yes)
        return;

      foreach (var r in _influenceRecords)
      {
        r.MoodId = SituationTypeSystem.EmptySlotValue;
        r.InfluenceId = SituationTypeSystem.EmptySlotValue;
        r.Description = "";
      }
      SaveAndReload("слоты по воздействию (очищены)");
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
