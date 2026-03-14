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

    public string CurrentAgentTitle => $"Справочник типов ситуаций: {_gomeostas?.GetAgentState()?.Name ?? "Не определен"}";

    private ObservableCollection<SituationTypeRecord> _moodRecords = new ObservableCollection<SituationTypeRecord>();
    private ObservableCollection<SituationTypeRecord> _influenceRecords = new ObservableCollection<SituationTypeRecord>();

    public ObservableCollection<SituationTypeRecord> MoodRecords => _moodRecords;
    public ObservableCollection<SituationTypeRecord> InfluenceRecords => _influenceRecords;

    private string _defaultRecordsText = "";

    /// <summary>Текстовый блок: записи по умолчанию (ID 1–10)</summary>
    public string DefaultRecordsText
    {
      get => _defaultRecordsText;
      private set { _defaultRecordsText = value; OnPropertyChanged(nameof(DefaultRecordsText)); }
    }

    public ICommand SaveMoodCommand { get; }
    public ICommand SaveInfluenceCommand { get; }
    public ICommand ClearMoodCommand { get; }
    public ICommand ClearInfluenceCommand { get; }

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

      OnPropertyChanged(nameof(MoodCellOptions));
      OnPropertyChanged(nameof(InfluenceCellOptions));
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
          var all = _situationTypeSystem.GetAll().ToDictionary(r => r.Id);

          _moodRecords.Clear();
          for (int id = 11; id <= 20; id++)
          {
            _moodRecords.Add(all.TryGetValue(id, out var r) ? r : new SituationTypeRecord { Id = id, MoodId = SituationTypeSystem.EmptySlotValue, InfluenceId = SituationTypeSystem.EmptySlotValue, Description = "" });
          }

          _influenceRecords.Clear();
          for (int id = 21; id <= 40; id++)
          {
            _influenceRecords.Add(all.TryGetValue(id, out var r) ? r : new SituationTypeRecord { Id = id, MoodId = SituationTypeSystem.EmptySlotValue, InfluenceId = SituationTypeSystem.EmptySlotValue, Description = "" });
          }

          BuildDefaultRecordsText(all);
        }

        LoadCellOptions();

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

    private void BuildDefaultRecordsText(Dictionary<int, SituationTypeRecord> byId)
    {
      var sb = new StringBuilder();
      sb.AppendLine("Записи по умолчанию (ID 1–10):");
      for (int id = 1; id <= 10; id++)
      {
        if (byId.TryGetValue(id, out var r))
          sb.AppendLine($"  {r.Id}: MoodId={r.MoodId}, InfluenceId={r.InfluenceId}, {r.Description ?? ""}");
        else
          sb.AppendLine($"  {id}: (нет)");
      }
      DefaultRecordsText = sb.ToString().TrimEnd();
    }

    public void RefreshData() => LoadData();

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

        var (valid, dupError) = _situationTypeSystem.ValidateRecordsNoDuplicates(_moodRecords, _influenceRecords);
        if (!valid)
        {
          MessageBox.Show($"Невозможно сохранить: {dupError}", "Дубликаты",
              MessageBoxButton.OK, MessageBoxImage.Warning);
          return;
        }

        _situationTypeSystem.UpdateFromRecords(_moodRecords);
        _situationTypeSystem.UpdateFromRecords(_influenceRecords);
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
  }
}
