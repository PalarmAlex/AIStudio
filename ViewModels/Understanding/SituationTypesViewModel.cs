using AIStudio.Common;
using ISIDA.Actions;
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
  public class SituationTypesViewModel : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private readonly SituationTypeSystem _situationTypeSystem;
    private readonly GomeostasSystem _gomeostas;
    private int _currentAgentStage;

    public bool IsStageFour => _currentAgentStage == 4;

    public string CurrentAgentTitle => "Справочник типов ситуаций";

    public DescriptionWithLink CurrentAgentDescription => new DescriptionWithLink
    {
      Text = "Справочник типов ситуаций: слоты событий (1–20), настроения (21–40), воздействия (41–60). Коды событий, настроений и воздействий заданы в движке и справочниках (только просмотр); редактируется привязка типа темы (ThemeTypeId) в каждом слоте. "
    };

    private ObservableCollection<SituationTypeRecord> _eventRecords = new ObservableCollection<SituationTypeRecord>();
    private ObservableCollection<SituationTypeRecord> _moodRecords = new ObservableCollection<SituationTypeRecord>();
    private ObservableCollection<SituationTypeRecord> _influenceRecords = new ObservableCollection<SituationTypeRecord>();

    /// <summary>Слоты событий с привязкой темы (ID 1–20)</summary>
    public ObservableCollection<SituationTypeRecord> EventRecords => _eventRecords;
    public ObservableCollection<SituationTypeRecord> MoodRecords => _moodRecords;
    public ObservableCollection<SituationTypeRecord> InfluenceRecords => _influenceRecords;

    public ICommand SaveAllCommand { get; }
    public ICommand ClearAllCommand { get; }
    public ICommand FillDefaultsCommand { get; }

    public SituationTypesViewModel(
      GomeostasSystem gomeostasSystem,
      SituationTypeSystem situationTypeSystem)
    {
      _gomeostas = gomeostasSystem ?? throw new ArgumentNullException(nameof(gomeostasSystem));
      _situationTypeSystem = situationTypeSystem;

      SaveAllCommand = new RelayCommand(SaveAll);
      ClearAllCommand = new RelayCommand(ClearAll);
      FillDefaultsCommand = new RelayCommand(FillDefaults);

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

    #region Опции для ComboBox (только тема)

    /// <summary>Темы для привязки в слотах (ThemeTypeId)</summary>
    public List<KeyValuePair<int, string>> ThemeSlotCellOptions { get; private set; } = new List<KeyValuePair<int, string>>();

    private void LoadCellOptions()
    {
      var allThemes = ThemeImageSystem.GetDefaultThemeTypesForSettings();
      ThemeSlotCellOptions = new List<KeyValuePair<int, string>> { new KeyValuePair<int, string>(-1, "—") };
      if (allThemes != null)
      {
        foreach (var t in allThemes.OrderBy(x => x.Id))
          ThemeSlotCellOptions.Add(new KeyValuePair<int, string>(t.Id, $"{t.Id}: {t.Description}"));
      }

      OnPropertyChanged(nameof(ThemeSlotCellOptions));
    }

    #endregion

    /// <summary>
    /// Слоты 41–60 изначально часто без InfluenceId (-1): «Заполнить по умолчанию» их не трогает.
    /// Подставляем ID воздействий из справочника по порядку (как строки в ExterInalInfluencesView: OrderBy Id),
    /// чтобы колонка «Воздействие» и движок GetIdByInfluenceId имели согласованные коды. Сохраняем файл при изменении.
    /// </summary>
    private void SyncEmptyInfluenceSlotsFromCatalogAndSave()
    {
      if (_situationTypeSystem == null || !SituationTypeSystem.IsInitialized || !InfluenceActionSystem.IsInitialized)
        return;
      var list = InfluenceActionSystem.Instance.GetAllInfluenceActions();
      if (list == null || list.Count == 0)
        return;
      var sorted = list.OrderBy(x => x.Id).ToList();
      bool changed = false;
      foreach (var r in _influenceRecords)
      {
        if (r == null || r.Id < 41 || r.Id > 60)
          continue;
        if (r.InfluenceId >= 0)
          continue;
        int idx = r.Id - 41;
        if (idx < 0 || idx >= sorted.Count)
          continue;
        r.InfluenceId = sorted[idx].Id;
        changed = true;
      }
      if (!changed)
        return;
      _situationTypeSystem.UpdateFromRecords(_influenceRecords);
      var (ok, err) = _situationTypeSystem.Save();
      if (!ok)
      {
        MessageBox.Show(
            $"Не удалось сохранить автоподстановку воздействий для слотов 41–60:\n{err}",
            "Справочник типов ситуаций",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
      }
      RefreshObservableCollectionInPlace(_influenceRecords);
    }

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

          _eventRecords.Clear();
          for (int id = 1; id <= 20; id++)
          {
            _eventRecords.Add(all.TryGetValue(id, out var r) ? r : new SituationTypeRecord { Id = id, MoodId = SituationTypeSystem.EmptySlotValue, InfluenceId = SituationTypeSystem.EmptySlotValue, ThemeTypeId = -1, EventAgentCode = -1 });
          }

          _moodRecords.Clear();
          for (int id = 21; id <= 40; id++)
          {
            _moodRecords.Add(all.TryGetValue(id, out var r) ? r : new SituationTypeRecord { Id = id, MoodId = SituationTypeSystem.EmptySlotValue, InfluenceId = SituationTypeSystem.EmptySlotValue, ThemeTypeId = -1, EventAgentCode = -1 });
          }

          _influenceRecords.Clear();
          for (int id = 41; id <= 60; id++)
          {
            _influenceRecords.Add(all.TryGetValue(id, out var r) ? r : new SituationTypeRecord { Id = id, MoodId = SituationTypeSystem.EmptySlotValue, InfluenceId = SituationTypeSystem.EmptySlotValue, ThemeTypeId = -1, EventAgentCode = -1 });
          }

          SyncEmptyInfluenceSlotsFromCatalogAndSave();
        }

        LoadCellOptions();

        OnPropertyChanged(nameof(EventRecords));
        OnPropertyChanged(nameof(MoodRecords));
        OnPropertyChanged(nameof(InfluenceRecords));
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

    public void RefreshData() => LoadData();

    private void FillDefaults(object parameter)
    {
      if (MessageBox.Show(
          "Подставить рекомендуемые привязки из кода движка (AgentEventsCatalog)?\n\n"
          + "• События: слоты 1–8 — код события и тема; слоты 9–20 очищаются.\n"
          + "• Настроение: слоты 21–28 — настроения 0–7 и темы; слоты 29–40 очищаются.\n"
          + "• Воздействия (41–60) не меняются.\n\n"
          + "Запись в файл — только после «Сохранить».",
          "Заполнить по умолчанию",
          MessageBoxButton.YesNo,
          MessageBoxImage.Question) != MessageBoxResult.Yes)
        return;

      AgentEventsCatalog.ApplyDefaultSituationSlotBindings(_eventRecords, _moodRecords);
      RefreshObservableCollectionInPlace(_eventRecords);
      RefreshObservableCollectionInPlace(_moodRecords);
    }

    private static void RefreshObservableCollectionInPlace(ObservableCollection<SituationTypeRecord> col)
    {
      var copy = col.ToList();
      col.Clear();
      foreach (var x in copy)
        col.Add(x);
    }

    private void SaveAll(object parameter)
    {
      SaveAndReload("все слоты (1–60)");
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

        _situationTypeSystem.UpdateFromRecords(_eventRecords);
        _situationTypeSystem.UpdateFromRecords(_moodRecords);
        _situationTypeSystem.UpdateFromRecords(_influenceRecords);

        var (validLink, linkErr) = _situationTypeSystem.ValidateThemeRequiresLinkField(_situationTypeSystem.GetAll());
        if (!validLink)
        {
          MessageBox.Show(linkErr, "Тема без связи",
              MessageBoxButton.OK, MessageBoxImage.Warning);
          return;
        }

        var allWithTheme = _situationTypeSystem.GetAll()
          .Where(r => r != null && r.Id >= 1 && r.Id <= 60 && r.ThemeTypeId > 0)
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

    private void ClearAll(object parameter)
    {
      if (MessageBox.Show(
          "Очистить все настраиваемые привязки?\n\n"
          + "• События 11–20: тема и код события (слоты 1–10 не меняются).\n"
          + "• Настроение 21–40: настроение и тема.\n"
          + "• Воздействие 41–60: воздействие и тема.",
          "Подтверждение",
          MessageBoxButton.YesNo,
          MessageBoxImage.Question) != MessageBoxResult.Yes)
        return;

      foreach (var r in _eventRecords)
      {
        if (r.Id >= 11 && r.Id <= 20)
        {
          r.ThemeTypeId = -1;
          r.EventAgentCode = -1;
        }
      }

      foreach (var r in _moodRecords)
      {
        r.MoodId = SituationTypeSystem.EmptySlotValue;
        r.InfluenceId = SituationTypeSystem.EmptySlotValue;
        r.ThemeTypeId = -1;
      }

      foreach (var r in _influenceRecords)
      {
        r.MoodId = SituationTypeSystem.EmptySlotValue;
        r.InfluenceId = SituationTypeSystem.EmptySlotValue;
        r.ThemeTypeId = -1;
      }

      SaveAndReload("все слоты (очищены)");
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
