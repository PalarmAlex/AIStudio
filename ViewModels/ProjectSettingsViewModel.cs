using ISIDA.Common;
using ISIDA.Psychic.Understanding;
using ISIDA.Reflexes;
using ISIDA.Actions;
using ISIDA.Gomeostas;
using AIStudio.Common;
using AIStudio.Windows;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;

namespace AIStudio.ViewModels
{
  public class ProjectSettingsViewModel : INotifyPropertyChanged
  {
    private readonly GomeostasSystem _gomeostas;
    private readonly AdaptiveActionsSystem _actionsSystem;
    /// <summary>При смене корня проекта вызывается после подстановки путей и параметров из XML — перезагрузка движка ISIDA в AIStudio.</summary>
    private readonly Action<ProjectSettingsViewModel> _reloadRuntimeAfterProjectRootSwitch;

    private bool _isInitialized = false;
    private bool _bulkApplyingProjectSettings = false;
    private bool _logEnabled = false;
    private string _settingsPath;
    private string _dataGomeostasFolderPath;
    private string _dataActionsFolderPath;
    private string _sensorsFolderPath;
    private string _reflexesFolderPath;
    private string _psychicDataFolderPath;
    private string _logsFolderPath;
    private string _bootDataFolderPath;
    private string _scenarioReportsFolderPath;
    private int _defaultStileId;
    private int _defaultAdaptiveActionId;
    private int _defaultThemeTypeId;
    private int _defaultFormatLog;
    private int _waitingPeriodForActionsVal;
    private int _thinkingCycleDecayAgeDivisor;
    private int _thinkingCycleDecayBase;
    private int _thinkingCycleMainMaxAgePulses;
    private int _noOperatorStimulusSilencePulses;

    private bool _homeostasisPulseSpeedDriftEnabled;

    private int _recognitionThreshold;
    private int _previousRecognitionThreshold;

    private int _compareLevel;
    private int _previousCompareLevel;

    private float _difSensorPar;
    private string _difSensorParText;

    private int _dynamicTime;
    private int _previousDynamicTime;

    private int _reflexActionDisplayDuration;
    private int _previousReflexActionDisplayDuration;

    private bool _settingsPathNotMatchingTemplate;
    private bool _dataGomeostasNotMatchingTemplate;
    private bool _dataActionsNotMatchingTemplate;
    private bool _sensorsNotMatchingTemplate;
    private bool _reflexesNotMatchingTemplate;
    private bool _psychicNotMatchingTemplate;
    private bool _logsNotMatchingTemplate;
    private bool _bootDataNotMatchingTemplate;
    private bool _scenarioReportsNotMatchingTemplate;

    /// <summary>Видимость предупреждения рядом с путём «Каталог настроек».</summary>
    public bool SettingsPathNotMatchingTemplate
    {
      get => _settingsPathNotMatchingTemplate;
      set { _settingsPathNotMatchingTemplate = value; OnPropertyChanged(nameof(SettingsPathNotMatchingTemplate)); }
    }

    /// <summary>Видимость предупреждения для каталога данных гомеостаза.</summary>
    public bool DataGomeostasNotMatchingTemplate
    {
      get => _dataGomeostasNotMatchingTemplate;
      set { _dataGomeostasNotMatchingTemplate = value; OnPropertyChanged(nameof(DataGomeostasNotMatchingTemplate)); }
    }

    /// <summary>Видимость предупреждения для каталога адаптивных действий.</summary>
    public bool DataActionsNotMatchingTemplate
    {
      get => _dataActionsNotMatchingTemplate;
      set { _dataActionsNotMatchingTemplate = value; OnPropertyChanged(nameof(DataActionsNotMatchingTemplate)); }
    }

    /// <summary>Видимость предупреждения для каталога вербальных сенсоров.</summary>
    public bool SensorsNotMatchingTemplate
    {
      get => _sensorsNotMatchingTemplate;
      set { _sensorsNotMatchingTemplate = value; OnPropertyChanged(nameof(SensorsNotMatchingTemplate)); }
    }

    /// <summary>Видимость предупреждения для каталога рефлексов.</summary>
    public bool ReflexesNotMatchingTemplate
    {
      get => _reflexesNotMatchingTemplate;
      set { _reflexesNotMatchingTemplate = value; OnPropertyChanged(nameof(ReflexesNotMatchingTemplate)); }
    }

    /// <summary>Видимость предупреждения для каталога психики.</summary>
    public bool PsychicNotMatchingTemplate
    {
      get => _psychicNotMatchingTemplate;
      set { _psychicNotMatchingTemplate = value; OnPropertyChanged(nameof(PsychicNotMatchingTemplate)); }
    }

    /// <summary>Видимость предупреждения для каталога логов.</summary>
    public bool LogsNotMatchingTemplate
    {
      get => _logsNotMatchingTemplate;
      set { _logsNotMatchingTemplate = value; OnPropertyChanged(nameof(LogsNotMatchingTemplate)); }
    }

    /// <summary>Видимость предупреждения для каталога загрузочных данных.</summary>
    public bool BootDataNotMatchingTemplate
    {
      get => _bootDataNotMatchingTemplate;
      set { _bootDataNotMatchingTemplate = value; OnPropertyChanged(nameof(BootDataNotMatchingTemplate)); }
    }

    /// <summary>Видимость предупреждения для каталога отчётов сценариев.</summary>
    public bool ScenarioReportsNotMatchingTemplate
    {
      get => _scenarioReportsNotMatchingTemplate;
      set { _scenarioReportsNotMatchingTemplate = value; OnPropertyChanged(nameof(ScenarioReportsNotMatchingTemplate)); }
    }

    public string SettingsPath
    {
      get => _settingsPath;
      set
      {
        _settingsPath = value;
        OnPropertyChanged(nameof(SettingsPath));
      }
    }
    public string DataGomeostasFolderPath
    {
      get => _dataGomeostasFolderPath;
      set
      {
        _dataGomeostasFolderPath = value;
        OnPropertyChanged(nameof(DataGomeostasFolderPath));
      }
    }
    public string DataActionsFolderPath
    {
      get => _dataActionsFolderPath;
      set
      {
        _dataActionsFolderPath = value;
        OnPropertyChanged(nameof(DataActionsFolderPath));
      }
    }
    public string SensorsFolderPath
    {
      get => _sensorsFolderPath;
      set
      {
        _sensorsFolderPath = value;
        OnPropertyChanged(nameof(SensorsFolderPath));
      }
    }
    public string ReflexesFolderPath
    {
      get => _reflexesFolderPath;
      set
      {
        _reflexesFolderPath = value;
        OnPropertyChanged(nameof(ReflexesFolderPath));
      }
    }
    public string PsychicDataFolderPath
    {
      get => _psychicDataFolderPath;
      set
      {
        _psychicDataFolderPath = value;
        OnPropertyChanged(nameof(PsychicDataFolderPath));
      }
    }
    public string LogsFolderPath
    {
      get => _logsFolderPath;
      set
      {
        _logsFolderPath = value;
        OnPropertyChanged(nameof(LogsFolderPath));
      }
    }
    public string BootDataFolderPath
    {
      get => _bootDataFolderPath;
      set
      {
        _bootDataFolderPath = value;
        OnPropertyChanged(nameof(BootDataFolderPath));
      }
    }

    public string ScenarioReportsFolderPath
    {
      get => _scenarioReportsFolderPath;
      set
      {
        _scenarioReportsFolderPath = value;
        OnPropertyChanged(nameof(ScenarioReportsFolderPath));
      }
    }

    public int DefaultStileId
    {
      get => _defaultStileId;
      set
      {
        _defaultStileId = value;
        OnPropertyChanged(nameof(DefaultStileId));
      }
    }

    public int WaitingPeriodForActionsVal
    {
      get => _waitingPeriodForActionsVal;
      set
      {
        _waitingPeriodForActionsVal = value;
        OnPropertyChanged(nameof(WaitingPeriodForActionsVal));
      }
    }

    public int ThinkingCycleDecayAgeDivisor
    {
      get => _thinkingCycleDecayAgeDivisor;
      set
      {
        if (_bulkApplyingProjectSettings)
        {
          _thinkingCycleDecayAgeDivisor = value;
          OnPropertyChanged(nameof(ThinkingCycleDecayAgeDivisor));
          return;
        }

        if (value < 1)
        {
          MessageBox.Show("Делитель возраста цикла (A) должен быть не меньше 1.", "Ошибка ввода");
          return;
        }
        _thinkingCycleDecayAgeDivisor = value;
        OnPropertyChanged(nameof(ThinkingCycleDecayAgeDivisor));
      }
    }

    public int ThinkingCycleDecayBase
    {
      get => _thinkingCycleDecayBase;
      set
      {
        if (_bulkApplyingProjectSettings)
        {
          _thinkingCycleDecayBase = value;
          OnPropertyChanged(nameof(ThinkingCycleDecayBase));
          return;
        }

        if (value < 0)
        {
          MessageBox.Show("Базовое снятие веса (B) не может быть отрицательным.", "Ошибка ввода");
          return;
        }
        _thinkingCycleDecayBase = value;
        OnPropertyChanged(nameof(ThinkingCycleDecayBase));
      }
    }

    public int ThinkingCycleMainMaxAgePulses
    {
      get => _thinkingCycleMainMaxAgePulses;
      set
      {
        if (_bulkApplyingProjectSettings)
        {
          _thinkingCycleMainMaxAgePulses = value;
          OnPropertyChanged(nameof(ThinkingCycleMainMaxAgePulses));
          return;
        }

        if (value < 1)
        {
          MessageBox.Show("Максимальный возраст главного цикла (пульсов) должен быть не меньше 1.", "Ошибка ввода");
          return;
        }
        _thinkingCycleMainMaxAgePulses = value;
        OnPropertyChanged(nameof(ThinkingCycleMainMaxAgePulses));
      }
    }

    /// <summary>Пульсов без стимула с пульта до события «долго без оператора» (тема мышления).</summary>
    public int NoOperatorStimulusSilencePulses
    {
      get => _noOperatorStimulusSilencePulses;
      set
      {
        if (_bulkApplyingProjectSettings)
        {
          _noOperatorStimulusSilencePulses = value;
          OnPropertyChanged(nameof(NoOperatorStimulusSilencePulses));
          return;
        }

        if (value < 1)
        {
          MessageBox.Show("Порог тишины для события «долго без оператора» должен быть не меньше 1 пульса.", "Ошибка ввода");
          return;
        }
        _noOperatorStimulusSilencePulses = value;
        OnPropertyChanged(nameof(NoOperatorStimulusSilencePulses));
      }
    }

    /// <summary>При true — сдвиг параметров по Speed на каждом пульсе; при false — только при воздействии.</summary>
    public bool HomeostasisPulseSpeedDriftEnabled
    {
      get => _homeostasisPulseSpeedDriftEnabled;
      set
      {
        _homeostasisPulseSpeedDriftEnabled = value;
        OnPropertyChanged(nameof(HomeostasisPulseSpeedDriftEnabled));
      }
    }

    public bool LogEnabled
    {
      get => _logEnabled;
      set
      {
        _logEnabled = value;
        OnPropertyChanged(nameof(LogEnabled));
      }
    }
    public int DefaultAdaptiveActionId
    {
      get => _defaultAdaptiveActionId;
      set
      {
        _defaultAdaptiveActionId = value;
        OnPropertyChanged(nameof(DefaultAdaptiveActionId));
      }
    }
    public int DefaultThemeTypeId
    {
      get => _defaultThemeTypeId;
      set
      {
        _defaultThemeTypeId = value;
        OnPropertyChanged(nameof(DefaultThemeTypeId));
      }
    }
    public int RecognitionThreshold
    {
      get => _recognitionThreshold;
      set
      {
        if (!_isInitialized)
        {
          _recognitionThreshold = value;
          _previousRecognitionThreshold = value;
          return;
        }

        var validation = SettingsValidator.ValidateRecognitionThreshold(value);
        if (validation.isValid)
        {
          _previousRecognitionThreshold = _recognitionThreshold;
          _recognitionThreshold = value;
          OnPropertyChanged(nameof(RecognitionThreshold));
        }
        else
        {
          MessageBox.Show(validation.errorMessage, "Ошибка ввода");
          _recognitionThreshold = _previousRecognitionThreshold;
          OnPropertyChanged(nameof(RecognitionThreshold));
        }
      }
    }
    public int CompareLevel
    {
      get => _compareLevel;
      set
      {
        if (!_isInitialized)
        {
          _compareLevel = value;
          _previousCompareLevel = value;
          return;
        }

        var validation = SettingsValidator.ValidateCompareLevel(value);
        if (validation.isValid)
        {
          _previousCompareLevel = _compareLevel;
          _compareLevel = value;
          OnPropertyChanged(nameof(CompareLevel));
        }
        else
        {
          MessageBox.Show(validation.errorMessage, "Ошибка ввода");
          _compareLevel = _previousCompareLevel;
          OnPropertyChanged(nameof(CompareLevel));
        }
      }
    }
    public string DifSensorParText
    {
      get => _difSensorParText ?? _difSensorPar.ToString(CultureInfo.InvariantCulture);
      set
      {
        if (string.IsNullOrEmpty(value))
          return;

        string normalizedValue = value.Replace(',', '.');

        if (float.TryParse(normalizedValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
        {
          var validation = SettingsValidator.ValidateDifSensorPar(result);
          if (validation.isValid)
          {
            _difSensorParText = normalizedValue;
            _difSensorPar = result;
            OnPropertyChanged(nameof(DifSensorPar));
          }
          else
          {
            MessageBox.Show(validation.errorMessage, "Ошибка ввода");
            _difSensorParText = _difSensorPar.ToString(CultureInfo.InvariantCulture);
            OnPropertyChanged(nameof(DifSensorParText));
          }
        }
        else
          OnPropertyChanged(nameof(DifSensorParText));
      }
    }
    public float DifSensorPar
    {
      get => _difSensorPar;
      set
      {
        var validation = SettingsValidator.ValidateDifSensorPar(value);
        if (_bulkApplyingProjectSettings)
        {
          if (!validation.isValid)
            return;
          _difSensorPar = value;
          _difSensorParText = value.ToString(CultureInfo.InvariantCulture);
          OnPropertyChanged(nameof(DifSensorPar));
          OnPropertyChanged(nameof(DifSensorParText));
          return;
        }

        if (validation.isValid)
        {
          _difSensorPar = value;
          _difSensorParText = value.ToString(CultureInfo.InvariantCulture);
          OnPropertyChanged(nameof(DifSensorPar));
          OnPropertyChanged(nameof(DifSensorParText));
        }
        else
          MessageBox.Show(validation.errorMessage, "Ошибка ввода");
      }
    }
    public int DynamicTime
    {
      get => _dynamicTime;
      set
      {
        if (!_isInitialized)
        {
          _dynamicTime = value;
          _previousDynamicTime = value;
          return;
        }

        var validation = SettingsValidator.ValidateDynamicTime(value);
        if (validation.isValid)
        {
          _previousDynamicTime = _dynamicTime;
          _dynamicTime = value;
          OnPropertyChanged(nameof(DynamicTime));
        }
        else
        {
          MessageBox.Show(validation.errorMessage, "Ошибка ввода");
          _dynamicTime = _previousDynamicTime;
          OnPropertyChanged(nameof(DynamicTime));
        }
      }
    }
    public int ReflexActionDisplayDuration
    {
      get => _reflexActionDisplayDuration;
      set
      {
        if (!_isInitialized)
        {
          _reflexActionDisplayDuration = value;
          _previousReflexActionDisplayDuration = value;
          return;
        }

        if (value < _dynamicTime)
        {
          _previousReflexActionDisplayDuration = _reflexActionDisplayDuration;
          _reflexActionDisplayDuration = value;
          OnPropertyChanged(nameof(ReflexActionDisplayDuration));
        }
        else
        {
          MessageBox.Show("Время удержания рефлексов не может быть больше или равно времени удержания состояний", "Ошибка ввода");
          _reflexActionDisplayDuration = _previousReflexActionDisplayDuration;
          OnPropertyChanged(nameof(ReflexActionDisplayDuration));
        }
      }
    }
    public int DefaultFormatLog
    {
      get => _defaultFormatLog;
      set
      {
        _defaultFormatLog = value;
        OnPropertyChanged(nameof(DefaultFormatLog));
      }
    }
    public ICommand BrowseFolderCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    /// <summary>Выбор корня проекта данных и подстановка путей по шаблону ISIDA.</summary>
    /// <summary>Открывает текстовое описание шаблона каталогов в редакторе по умолчанию.</summary>
    public ICommand OpenProjectDirectoryTemplateCommand { get; }

    public ObservableCollection<SelectableItem> BehaviorStylesWithNone { get; } = new ObservableCollection<SelectableItem>();
    public ObservableCollection<SelectableItem> AdaptiveActionsWithNone { get; } = new ObservableCollection<SelectableItem>();
    public ObservableCollection<SelectableItem> ThemeTypesWithNone { get; } = new ObservableCollection<SelectableItem>();
    public ObservableCollection<SelectableItem> GeneticReflexesWithNone { get; } = new ObservableCollection<SelectableItem>();
    public ObservableCollection<SelectableItem> FormatLog { get; } = new ObservableCollection<SelectableItem>();

    public class SelectableItem
    {
      public int Id { get; set; }
      public string Name { get; set; }
    }

    public ProjectSettingsViewModel(
        GomeostasSystem gomeostas,
        Action<ProjectSettingsViewModel> reloadRuntimeAfterProjectRootSwitch = null)
    {
      _reloadRuntimeAfterProjectRootSwitch = reloadRuntimeAfterProjectRootSwitch;
      SettingsPath = AppConfig.SettingsPath;
      DataGomeostasFolderPath = AppConfig.DataGomeostasFolderPath;
      DataActionsFolderPath = AppConfig.DataActionsFolderPath;
      SensorsFolderPath = AppConfig.SensorsFolderPath;
      ReflexesFolderPath = AppConfig.ReflexesFolderPath;
      PsychicDataFolderPath = AppConfig.PsychicDataFolderPath;
      LogsFolderPath = AppConfig.LogsFolderPath;
      BootDataFolderPath = AppConfig.BootDataFolderPath;
      ScenarioReportsFolderPath = AppConfig.ScenarioReportsFolderPath;
      DefaultStileId = AppConfig.DefaultStileId;
      WaitingPeriodForActionsVal = AppConfig.WaitingPeriodForActionsVal;
      _thinkingCycleDecayAgeDivisor = AppConfig.ThinkingCycleDecayAgeDivisor;
      _thinkingCycleDecayBase = AppConfig.ThinkingCycleDecayBase;
      _thinkingCycleMainMaxAgePulses = AppConfig.ThinkingCycleMainMaxAgePulses;
      _noOperatorStimulusSilencePulses = AppConfig.NoOperatorStimulusSilencePulses;
      _homeostasisPulseSpeedDriftEnabled = AppConfig.HomeostasisPulseSpeedDriftEnabled;
      DefaultAdaptiveActionId = AppConfig.DefaultAdaptiveActionId;
      DefaultThemeTypeId = AppConfig.DefaultThemeTypeId;
      LogEnabled = AppConfig.LogEnabled;
      _defaultFormatLog = (int)AppConfig.LogFormat;
      _recognitionThreshold = AppConfig.RecognitionThreshold;
      _previousRecognitionThreshold = _recognitionThreshold;
      _compareLevel = AppConfig.CompareLevel;
      _previousCompareLevel = _compareLevel;
      _difSensorPar = AppConfig.DifSensorPar;
      _dynamicTime = AppConfig.DynamicTime;
      _previousDynamicTime = _dynamicTime;
      _reflexActionDisplayDuration = AppConfig.ReflexActionDisplayDuration;
      _previousReflexActionDisplayDuration = _reflexActionDisplayDuration;
      _gomeostas = gomeostas;

      try
      {
        if (!AdaptiveActionsSystem.IsInitialized)
        {
          AdaptiveActionsSystem.InitializeInstance(_gomeostas,
              DataActionsFolderPath);
        }
        _actionsSystem = AdaptiveActionsSystem.Instance;

        if (!GeneticReflexesSystem.IsInitialized)
          GeneticReflexesSystem.InitializeInstance(_gomeostas, ReflexesFolderPath);
      }
      catch (Exception ex)
      {
        var errorDialog = new TaskDialog
        {
          WindowTitle = "Ошибка",
          MainInstruction = "Ошибка инициализации системы действий",
          Content = ex.Message,
          MainIcon = TaskDialogIcon.Error,
          Buttons = { new TaskDialogButton(ButtonType.Ok) }
        };
        errorDialog.ShowDialog();
      }

      LoadBehaviorStylesWithNone();
      LoadAdaptiveActionsWithNone();
      LoadThemeTypesWithNone();
      LoadLogFormats();

      DefaultFormatLog = (int)AppConfig.LogFormat;
      BrowseFolderCommand = new RelayCommand(BrowseFolderWithParameter);
      SaveSettingsCommand = new RelayCommand(SaveSettingsWithParameter);
      OpenProjectDirectoryTemplateCommand = new RelayCommand(OpenProjectDirectoryTemplate);

      _isInitialized = true;
      OnPropertyChanged(nameof(ThinkingCycleDecayAgeDivisor));
      OnPropertyChanged(nameof(ThinkingCycleDecayBase));
      OnPropertyChanged(nameof(ThinkingCycleMainMaxAgePulses));
      OnPropertyChanged(nameof(NoOperatorStimulusSilencePulses));

      RefreshProjectPathTemplateWarnings();
    }
    private void LoadLogFormats()
    {
      FormatLog.Clear();

      FormatLog.Add(new SelectableItem { Id = (int)ResearchLogger.LogFormat.None, Name = "Нет" });
      FormatLog.Add(new SelectableItem { Id = (int)ResearchLogger.LogFormat.JsonL, Name = "JSON" });
      FormatLog.Add(new SelectableItem { Id = (int)ResearchLogger.LogFormat.Csv, Name = "CSV" });
      FormatLog.Add(new SelectableItem { Id = (int)ResearchLogger.LogFormat.All, Name = "Все" });
    }

    private void LoadBehaviorStylesWithNone()
    {
      BehaviorStylesWithNone.Clear();
      BehaviorStylesWithNone.Add(new SelectableItem { Id = 0, Name = "Нет" });

      if (_gomeostas?.GetAllBehaviorStyles() != null)
      {
        foreach (var style in _gomeostas.GetAllBehaviorStyles().Values.OrderBy(s => s.Id))
        {
          BehaviorStylesWithNone.Add(new SelectableItem { Id = style.Id, Name = style.Name });
        }
      }
    }

    private void LoadAdaptiveActionsWithNone()
    {
      AdaptiveActionsWithNone.Clear();
      AdaptiveActionsWithNone.Add(new SelectableItem { Id = 0, Name = "Нет" });

      if (_actionsSystem?.GetAllAdaptiveActions() != null)
      {
        foreach (var action in _actionsSystem.GetAllAdaptiveActions().OrderBy(a => a.Id))
        {
          AdaptiveActionsWithNone.Add(new SelectableItem { Id = action.Id, Name = action.Name });
        }
      }
    }

    private void LoadThemeTypesWithNone()
    {
      ThemeTypesWithNone.Clear();
      ThemeTypesWithNone.Add(new SelectableItem { Id = 0, Name = "Нет темы" });
      foreach (var (id, description) in ThemeImageSystem.GetDefaultThemeTypesForSettings())
      {
        ThemeTypesWithNone.Add(new SelectableItem { Id = id, Name = description });
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void BrowseFolderWithParameter(object parameter)
    {
      if (!(parameter is string paramStr)) return;

      var dialog = new VistaFolderBrowserDialog
      {
        Description = $"Выберите папку для {paramStr}",
        UseDescriptionForTitle = true // Это сделает заголовок окна таким же как Description
      };

      // Устанавливаем начальный путь если он существует
      string initialPath;
      switch (paramStr)
      {
        case nameof(SettingsPath):
          initialPath = Directory.Exists(SettingsPath) ? SettingsPath : "";
          break;
        case nameof(DataGomeostasFolderPath):
          initialPath = Directory.Exists(DataGomeostasFolderPath) ? DataGomeostasFolderPath : "";
          break;
        case nameof(DataActionsFolderPath):
          initialPath = Directory.Exists(DataActionsFolderPath) ? DataActionsFolderPath : "";
          break;
        case nameof(SensorsFolderPath):
          initialPath = Directory.Exists(SensorsFolderPath) ? SensorsFolderPath : "";
          break;
        case nameof(ReflexesFolderPath):
          initialPath = Directory.Exists(ReflexesFolderPath) ? ReflexesFolderPath : "";
          break;
        case nameof(PsychicDataFolderPath):
          initialPath = Directory.Exists(PsychicDataFolderPath) ? PsychicDataFolderPath : "";
          break;
        case nameof(LogsFolderPath):
          initialPath = Directory.Exists(LogsFolderPath) ? LogsFolderPath : "";
          break;
        case nameof(BootDataFolderPath):
          initialPath = Directory.Exists(BootDataFolderPath) ? BootDataFolderPath : "";
          break;
        case nameof(ScenarioReportsFolderPath):
          initialPath = Directory.Exists(ScenarioReportsFolderPath) ? ScenarioReportsFolderPath : "";
          break;
        default:
          initialPath = "";
          break;
      }

      dialog.SelectedPath = initialPath;

      if (dialog.ShowDialog() == true)
      {
        switch (paramStr)
        {
          case nameof(SettingsPath):
            SettingsPath = dialog.SelectedPath;
            RefreshProjectPathTemplateWarnings();
            break;
          case nameof(DataGomeostasFolderPath):
            DataGomeostasFolderPath = dialog.SelectedPath;
            RefreshProjectPathTemplateWarnings();
            break;
          case nameof(DataActionsFolderPath):
            DataActionsFolderPath = dialog.SelectedPath;
            RefreshProjectPathTemplateWarnings();
            break;
          case nameof(SensorsFolderPath):
            SensorsFolderPath = dialog.SelectedPath;
            RefreshProjectPathTemplateWarnings();
            break;
          case nameof(ReflexesFolderPath):
            ReflexesFolderPath = dialog.SelectedPath;
            RefreshProjectPathTemplateWarnings();
            break;
          case nameof(PsychicDataFolderPath):
            PsychicDataFolderPath = dialog.SelectedPath;
            RefreshProjectPathTemplateWarnings();
            break;
          case nameof(LogsFolderPath):
            LogsFolderPath = dialog.SelectedPath;
            RefreshProjectPathTemplateWarnings();
            break;
          case nameof(BootDataFolderPath):
            BootDataFolderPath = dialog.SelectedPath;
            RefreshProjectPathTemplateWarnings();
            break;
          case nameof(ScenarioReportsFolderPath):
            ScenarioReportsFolderPath = dialog.SelectedPath;
            RefreshProjectPathTemplateWarnings();
            break;
        }
      }
    }

    private void RefreshProjectPathTemplateWarnings()
    {
      string root;
      if (!SettingsValidator.TryInferProjectRoot(SettingsPath, DataGomeostasFolderPath, out root))
      {
        SettingsPathNotMatchingTemplate = false;
        DataGomeostasNotMatchingTemplate = false;
        DataActionsNotMatchingTemplate = false;
        SensorsNotMatchingTemplate = false;
        ReflexesNotMatchingTemplate = false;
        PsychicNotMatchingTemplate = false;
        LogsNotMatchingTemplate = false;
        BootDataNotMatchingTemplate = false;
        ScenarioReportsNotMatchingTemplate = false;
        return;
      }

      SettingsPathNotMatchingTemplate = !SettingsValidator.IsFolderPathMatchingProjectTemplate(root, SettingsPath, nameof(SettingsPath));
      DataGomeostasNotMatchingTemplate = !SettingsValidator.IsFolderPathMatchingProjectTemplate(root, DataGomeostasFolderPath, nameof(DataGomeostasFolderPath));
      DataActionsNotMatchingTemplate = !SettingsValidator.IsFolderPathMatchingProjectTemplate(root, DataActionsFolderPath, nameof(DataActionsFolderPath));
      SensorsNotMatchingTemplate = !SettingsValidator.IsFolderPathMatchingProjectTemplate(root, SensorsFolderPath, nameof(SensorsFolderPath));
      ReflexesNotMatchingTemplate = !SettingsValidator.IsFolderPathMatchingProjectTemplate(root, ReflexesFolderPath, nameof(ReflexesFolderPath));
      PsychicNotMatchingTemplate = !SettingsValidator.IsFolderPathMatchingProjectTemplate(root, PsychicDataFolderPath, nameof(PsychicDataFolderPath));
      LogsNotMatchingTemplate = !SettingsValidator.IsFolderPathMatchingProjectTemplate(root, LogsFolderPath, nameof(LogsFolderPath));
      BootDataNotMatchingTemplate = !SettingsValidator.IsFolderPathMatchingProjectTemplate(root, BootDataFolderPath, nameof(BootDataFolderPath));
      ScenarioReportsNotMatchingTemplate = !SettingsValidator.IsFolderPathMatchingProjectTemplate(root, ScenarioReportsFolderPath, nameof(ScenarioReportsFolderPath));
    }

    private void OpenProjectDirectoryTemplate(object _)
    {
      try
      {
        var w = new ProjectDirectoryTemplateWindow
        {
          Owner = Application.Current != null ? Application.Current.MainWindow : null
        };
        w.ShowDialog();
      }
      catch (Exception ex)
      {
        MessageBox.Show("Не удалось открыть шаблон: " + ex.Message, "Ошибка");
      }
    }

    /// <summary>
    /// Открывает диалог выбора корня проекта. По умолчанию — <see cref="ProjectBootstrap.DefaultProjectsParentPath"/>.
    /// </summary>
    /// <param name="projectRoot">Выбранный каталог при успехе.</param>
    /// <returns>True, если пользователь выбрал каталог.</returns>
    public bool TryPickProjectRootFolder(out string projectRoot)
    {
      projectRoot = null;

      string initialPath = ProjectBootstrap.GetDefaultProjectsFolderDialogPath();
      if (SettingsValidator.TryInferProjectRoot(SettingsPath, DataGomeostasFolderPath, out string currentRoot)
          && Directory.Exists(currentRoot))
      {
        initialPath = ProjectBootstrap.ToFolderDialogInitialPath(currentRoot);
      }

      var dialog = new VistaFolderBrowserDialog
      {
        Description = "Укажите корневой каталог данных проекта...",
        UseDescriptionForTitle = true,
        SelectedPath = initialPath
      };

      if (dialog.ShowDialog() != true)
        return false;

      projectRoot = dialog.SelectedPath;
      return !string.IsNullOrWhiteSpace(projectRoot);
    }

    /// <summary>
    /// Применяет корень проекта: пути, настройки из Settings.xml, при наличии колбэка — перезагрузка движка.
    /// </summary>
    /// <param name="projectRoot">Корневой каталог проекта данных.</param>
    public void ApplyProjectRoot(string projectRoot)
    {
      if (string.IsNullOrWhiteSpace(projectRoot))
        return;
      List<string> missingRoots;
      if (!SettingsValidator.MandatoryProjectRootFoldersExist(projectRoot, out missingRoots))
      {
        MessageBox.Show(
            "В выбранном каталоге отсутствуют обязательные папки шаблона: " + string.Join(", ", missingRoots) + ".",
            "Структура проекта");
        return;
      }

      if (SettingsValidator.TryInferProjectRoot(SettingsPath, DataGomeostasFolderPath, out string currentProjectRoot)
          && PathsReferToSameDirectory(projectRoot, currentProjectRoot))
      {
        MessageBox.Show(
            "Указан тот же каталог данных проекта, что уже задан на этой странице. Переключение и перезагрузка не выполняются.",
            "Переключение проекта",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        return;
      }

      var errors = new List<string>();
      string[] pathKeys =
      {
        nameof(SettingsPath),
        nameof(LogsFolderPath),
        nameof(BootDataFolderPath),
        nameof(DataGomeostasFolderPath),
        nameof(DataActionsFolderPath),
        nameof(SensorsFolderPath),
        nameof(ReflexesFolderPath),
        nameof(PsychicDataFolderPath),
        nameof(ScenarioReportsFolderPath)
      };

      var newPaths = new Dictionary<string, string>();
      for (int i = 0; i < pathKeys.Length; i++)
      {
        string key = pathKeys[i];
        string expected;
        try
        {
          expected = SettingsValidator.GetExpectedFolderPathForSetting(projectRoot, key);
        }
        catch
        {
          errors.Add(GetPathSettingDisplayName(key) + ": ошибка вычисления пути.");
          continue;
        }

        if (Directory.Exists(expected))
          newPaths[key] = expected;
        else
          errors.Add(GetPathSettingDisplayName(key) + " (каталог): " + expected);
      }

      string projectSettingsXml = Path.Combine(projectRoot, "Settings", AppConfig.StudioSettingsFileName);
      if (!File.Exists(projectSettingsXml))
      {
        string legacyProjectSettings = Path.Combine(projectRoot, "Settings", AppConfig.LegacyStudioSettingsFileName);
        if (File.Exists(legacyProjectSettings))
          projectSettingsXml = legacyProjectSettings;
      }

      XElement appSettings = null;
      if (File.Exists(projectSettingsXml))
      {
        try
        {
          var doc = XDocument.Load(projectSettingsXml);
          appSettings = doc.Root?.Element("AppSettings");
        }
        catch (Exception ex)
        {
          errors.Add("Не удалось прочитать файл настроек проекта: " + ex.Message);
        }
      }
      else
      {
        errors.Add("Файл «" + AppConfig.StudioSettingsFileName + "» (или «" + AppConfig.LegacyStudioSettingsFileName + "») в каталоге Settings не найден; параметры со страницы не загружены из проекта.");
      }

      bool wasInit = _isInitialized;
      _isInitialized = false;
      _bulkApplyingProjectSettings = true;

      try
      {
        foreach (var kv in newPaths)
          ApplyPathSetting(kv.Key, kv.Value);

        if (appSettings != null)
          ApplyScalarSettingsFromAppSettings(appSettings, errors);
      }
      finally
      {
        _bulkApplyingProjectSettings = false;
        _isInitialized = wasInit;
      }

      bool useRuntimeReload = _reloadRuntimeAfterProjectRootSwitch != null;
      bool reloadExecuted = false;

      if (useRuntimeReload)
      {
        if (!ValidateAllSettings())
        {
          RefreshProjectPathTemplateWarnings();
          LoadBehaviorStylesWithNone();
          LoadAdaptiveActionsWithNone();
          LoadThemeTypesWithNone();
          LoadLogFormats();
          OnPropertyChanged(nameof(DefaultFormatLog));
          OnPropertyChanged(nameof(DifSensorParText));
          return;
        }

        if (MessageBox.Show(
                "Сейчас будет записан файл конфигурации студии и полностью перезагружен движок ISIDA из файлов выбранного проекта. Продолжить?",
                "Переключение проекта",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
          RefreshProjectPathTemplateWarnings();
          LoadBehaviorStylesWithNone();
          LoadAdaptiveActionsWithNone();
          LoadThemeTypesWithNone();
          LoadLogFormats();
          OnPropertyChanged(nameof(DefaultFormatLog));
          OnPropertyChanged(nameof(DifSensorParText));
        }
        else
        {
          try
          {
            _reloadRuntimeAfterProjectRootSwitch.Invoke(this);
            reloadExecuted = true;
          }
          catch (Exception ex)
          {
            MessageBox.Show(
                "Не удалось перезагрузить данные движка из нового проекта: " + ex.Message,
                "Переключение проекта",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            RefreshProjectPathTemplateWarnings();
            LoadBehaviorStylesWithNone();
            LoadAdaptiveActionsWithNone();
            LoadThemeTypesWithNone();
            LoadLogFormats();
            OnPropertyChanged(nameof(DefaultFormatLog));
            OnPropertyChanged(nameof(DifSensorParText));
            return;
          }
        }
      }
      else
      {
        RefreshProjectPathTemplateWarnings();
        LoadBehaviorStylesWithNone();
        LoadAdaptiveActionsWithNone();
        LoadThemeTypesWithNone();
        LoadLogFormats();
        OnPropertyChanged(nameof(DefaultFormatLog));
        OnPropertyChanged(nameof(DifSensorParText));
      }

      if (errors.Count > 0)
      {
        var sb = new StringBuilder();
        sb.AppendLine("По указанному пути не обнаружены следующие настройки или не удалось их применить:");
        for (int i = 0; i < errors.Count; i++)
          sb.AppendLine(errors[i]);

        MessageBox.Show(sb.ToString(), "Переключение проекта", MessageBoxButton.OK, MessageBoxImage.Warning);
      }
      else if (reloadExecuted)
      {
        MessageBox.Show(
            "Каталоги и параметры проекта применены; данные движка перезагружены из файлов нового проекта. Настройки студии записаны в файл конфигурации.",
            "Переключение проекта",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
      }
      else if (useRuntimeReload)
      {
        MessageBox.Show(
            "Каталоги и параметры из файла проекта применены к странице. Перезагрузка движка отменена. Сохраните настройки и при необходимости перезапустите приложение, чтобы применить пути к работающему движку.",
            "Переключение проекта",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
      }
      else
      {
        MessageBox.Show(
            "Каталоги и параметры из файла проекта применены к текущей странице. Сохраните настройки и при необходимости перезапустите приложение.",
            "Переключение проекта",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
      }
    }

    /// <summary>Сравнивает пути к каталогам с учётом полной канонизации и регистра (Windows).</summary>
    private static bool PathsReferToSameDirectory(string pathA, string pathB)
    {
      if (string.IsNullOrWhiteSpace(pathA) || string.IsNullOrWhiteSpace(pathB))
        return false;
      try
      {
        string fullA = Path.GetFullPath(pathA.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        string fullB = Path.GetFullPath(pathB.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return string.Equals(fullA, fullB, StringComparison.OrdinalIgnoreCase);
      }
      catch
      {
        return false;
      }
    }

    private static string GetPathSettingDisplayName(string pathSettingKey)
    {
      switch (pathSettingKey)
      {
        case nameof(SettingsPath): return "Каталог настроек";
        case nameof(LogsFolderPath): return "Каталог логов проекта";
        case nameof(BootDataFolderPath): return "Каталог загрузочных данных";
        case nameof(DataGomeostasFolderPath): return "Каталог данных гомеостаза";
        case nameof(DataActionsFolderPath): return "Каталог адаптивных действий";
        case nameof(SensorsFolderPath): return "Каталог вербальных сенсоров";
        case nameof(ReflexesFolderPath): return "Каталог безусл. и условн. рефлексов";
        case nameof(PsychicDataFolderPath): return "Каталог файлов психики";
        case nameof(ScenarioReportsFolderPath): return "Каталог отчётов сценариев (HTML)";
        default: return pathSettingKey;
      }
    }

    private void ApplyPathSetting(string key, string path)
    {
      switch (key)
      {
        case nameof(SettingsPath): SettingsPath = path; break;
        case nameof(LogsFolderPath): LogsFolderPath = path; break;
        case nameof(BootDataFolderPath): BootDataFolderPath = path; break;
        case nameof(DataGomeostasFolderPath): DataGomeostasFolderPath = path; break;
        case nameof(DataActionsFolderPath): DataActionsFolderPath = path; break;
        case nameof(SensorsFolderPath): SensorsFolderPath = path; break;
        case nameof(ReflexesFolderPath): ReflexesFolderPath = path; break;
        case nameof(PsychicDataFolderPath): PsychicDataFolderPath = path; break;
        case nameof(ScenarioReportsFolderPath): ScenarioReportsFolderPath = path; break;
      }
    }

    private void ApplyScalarSettingsFromAppSettings(XElement appSettings, List<string> errors)
    {
      TryReadInt(appSettings, nameof(DefaultStileId), v => DefaultStileId = v, "Стиль реагирования по умолчанию", errors);
      TryReadInt(appSettings, nameof(WaitingPeriodForActionsVal), v => WaitingPeriodForActionsVal = v, "Период ожидания ответа оператора (пульсов)", errors);

      TryReadIntValidated(
          appSettings,
          nameof(ThinkingCycleDecayAgeDivisor),
          v => v >= 1,
          v => ThinkingCycleDecayAgeDivisor = v,
          "Циклы: делитель возраста A",
          errors);
      TryReadIntValidated(
          appSettings,
          nameof(ThinkingCycleDecayBase),
          v => v >= 0,
          v => ThinkingCycleDecayBase = v,
          "Циклы: базовое снятие веса B",
          errors);
      TryReadIntValidated(
          appSettings,
          nameof(ThinkingCycleMainMaxAgePulses),
          v => v >= 1,
          v => ThinkingCycleMainMaxAgePulses = v,
          "Циклы: макс. возраст главного (пульсов)",
          errors);
      TryReadIntValidated(
          appSettings,
          nameof(NoOperatorStimulusSilencePulses),
          v => v >= 1,
          v => NoOperatorStimulusSilencePulses = v,
          "Событие «долго без оператора»: порог тишины",
          errors);

      TryReadBool(appSettings, nameof(HomeostasisPulseSpeedDriftEnabled), v => HomeostasisPulseSpeedDriftEnabled = v, "Изменение параметров по Speed на каждом пульсе", errors);
      TryReadInt(appSettings, nameof(DefaultAdaptiveActionId), v => DefaultAdaptiveActionId = v, "Адаптивное действие по умолчанию", errors);
      TryReadInt(appSettings, nameof(DefaultThemeTypeId), v => DefaultThemeTypeId = v, "Тема мышления по умолчанию", errors);

      TryReadIntWithValidator(
          appSettings,
          nameof(RecognitionThreshold),
          v => SettingsValidator.ValidateRecognitionThreshold(v),
          v => RecognitionThreshold = v,
          "Число повторов для записи сенсора",
          errors);
      TryReadIntWithValidator(
          appSettings,
          nameof(CompareLevel),
          v => SettingsValidator.ValidateCompareLevel(v),
          v => CompareLevel = v,
          "Интегральный порог состояния %",
          errors);
      TryReadFloatWithValidator(
          appSettings,
          nameof(DifSensorPar),
          v => SettingsValidator.ValidateDifSensorPar(v),
          v => DifSensorPar = v,
          "Мин. детектирование параметра",
          errors);

      TryReadIntWithValidator(
          appSettings,
          nameof(DynamicTime),
          v => SettingsValidator.ValidateDynamicTime(v),
          v => DynamicTime = v,
          "Время удержания состояний (пульсов)",
          errors);

      XElement reflexEl = appSettings.Element(nameof(ReflexActionDisplayDuration));
      if (reflexEl != null && !string.IsNullOrWhiteSpace(reflexEl.Value))
      {
        int reflexVal;
        if (!int.TryParse(reflexEl.Value.Trim(), out reflexVal))
          errors.Add("Время удержания действий (пульсов)");
        else if (reflexVal >= _dynamicTime)
          errors.Add("Время удержания действий (пульсов)");
        else
          ReflexActionDisplayDuration = reflexVal;
      }
      else
        errors.Add("Время удержания действий (пульсов)");

      TryReadBool(appSettings, nameof(LogEnabled), v => LogEnabled = v, "Включить логирование событий", errors);

      XElement logFormatEl = appSettings.Element("DefaultFormatLog");
      if (logFormatEl == null)
        logFormatEl = appSettings.Element("LogFormat");
      if (logFormatEl != null && !string.IsNullOrWhiteSpace(logFormatEl.Value))
      {
        string raw = logFormatEl.Value.Trim();
        ResearchLogger.LogFormat parsed;
        if (Enum.TryParse(raw, true, out parsed))
          DefaultFormatLog = (int)parsed;
        else
        {
          int intVal;
          if (int.TryParse(raw, out intVal) && Enum.IsDefined(typeof(ResearchLogger.LogFormat), intVal))
            DefaultFormatLog = intVal;
          else
            errors.Add("Формат логов");
        }
      }
      else
        errors.Add("Формат логов");

      OnPropertyChanged(nameof(DefaultStileId));
      OnPropertyChanged(nameof(WaitingPeriodForActionsVal));
      OnPropertyChanged(nameof(ThinkingCycleDecayAgeDivisor));
      OnPropertyChanged(nameof(ThinkingCycleDecayBase));
      OnPropertyChanged(nameof(ThinkingCycleMainMaxAgePulses));
      OnPropertyChanged(nameof(NoOperatorStimulusSilencePulses));
      OnPropertyChanged(nameof(HomeostasisPulseSpeedDriftEnabled));
      OnPropertyChanged(nameof(DefaultAdaptiveActionId));
      OnPropertyChanged(nameof(DefaultThemeTypeId));
      OnPropertyChanged(nameof(RecognitionThreshold));
      OnPropertyChanged(nameof(CompareLevel));
      OnPropertyChanged(nameof(DifSensorPar));
      OnPropertyChanged(nameof(DynamicTime));
      OnPropertyChanged(nameof(ReflexActionDisplayDuration));
      OnPropertyChanged(nameof(LogEnabled));
      OnPropertyChanged(nameof(DefaultFormatLog));
    }

    private static void TryReadIntValidated(
        XElement appSettings,
        string key,
        Func<int, bool> rangeOk,
        Action<int> apply,
        string displayName,
        List<string> errors)
    {
      XElement el = appSettings.Element(key);
      if (el == null || string.IsNullOrWhiteSpace(el.Value))
      {
        errors.Add(displayName);
        return;
      }

      int v;
      if (!int.TryParse(el.Value.Trim(), out v) || !rangeOk(v))
      {
        errors.Add(displayName);
        return;
      }

      apply(v);
    }

    private static void TryReadIntWithValidator(
        XElement appSettings,
        string key,
        Func<int, (bool isValid, string errorMessage)> validator,
        Action<int> apply,
        string displayName,
        List<string> errors)
    {
      XElement el = appSettings.Element(key);
      if (el == null || string.IsNullOrWhiteSpace(el.Value))
      {
        errors.Add(displayName);
        return;
      }

      int v;
      if (!int.TryParse(el.Value.Trim(), out v))
      {
        errors.Add(displayName);
        return;
      }

      var validation = validator(v);
      if (!validation.isValid)
      {
        errors.Add(displayName);
        return;
      }

      apply(v);
    }

    private static void TryReadFloatWithValidator(
        XElement appSettings,
        string key,
        Func<float, (bool isValid, string errorMessage)> validator,
        Action<float> apply,
        string displayName,
        List<string> errors)
    {
      XElement el = appSettings.Element(key);
      if (el == null || string.IsNullOrWhiteSpace(el.Value))
      {
        errors.Add(displayName);
        return;
      }

      float v;
      if (!float.TryParse(el.Value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v))
      {
        errors.Add(displayName);
        return;
      }

      var validation = validator(v);
      if (!validation.isValid)
      {
        errors.Add(displayName);
        return;
      }

      apply(v);
    }

    private static void TryReadInt(XElement appSettings, string key, Action<int> apply, string displayName, List<string> errors)
    {
      XElement el = appSettings.Element(key);
      if (el == null || string.IsNullOrWhiteSpace(el.Value))
      {
        errors.Add(displayName);
        return;
      }

      int v;
      if (!int.TryParse(el.Value.Trim(), out v))
      {
        errors.Add(displayName);
        return;
      }

      apply(v);
    }

    private static void TryReadBool(XElement appSettings, string key, Action<bool> apply, string displayName, List<string> errors)
    {
      XElement el = appSettings.Element(key);
      if (el == null || string.IsNullOrWhiteSpace(el.Value))
      {
        errors.Add(displayName);
        return;
      }

      bool v;
      if (!bool.TryParse(el.Value.Trim(), out v))
      {
        errors.Add(displayName);
        return;
      }

      apply(v);
    }

    private bool ValidateAllSettings()
    {
      var validations = new List<(bool isValid, string errorMessage)>
    {
        SettingsValidator.ValidateRecognitionThreshold(RecognitionThreshold),
        SettingsValidator.ValidateCompareLevel(CompareLevel),
        SettingsValidator.ValidateDifSensorPar(DifSensorPar),
        SettingsValidator.ValidateDynamicTime(DynamicTime)
    };

      var failedValidations = validations.Where(v => !v.isValid).ToList();
      if (failedValidations.Any())
      {
        string errorMessage = string.Join("\n\n", failedValidations.Select(v => v.errorMessage));
        MessageBox.Show($"Обнаружены ошибки в настройках:\n\n{errorMessage}", "Ошибка валидации");
        return false;
      }

      return true;
    }

    /// <summary>
    /// Записывает значения в хаб студии (<see cref="AppConfig.StudioHubSettingsFileName"/>), затем зеркалит их
    /// в профиль текущего проекта (<c>{корень}\Settings\</c> + <see cref="AppConfig.StudioSettingsFileName"/>).
    /// </summary>
    public void PushSettingsToAppConfig()
    {
      AppConfig.SetSetting(nameof(SettingsPath), SettingsPath);
      AppConfig.SetSetting(nameof(DataGomeostasFolderPath), DataGomeostasFolderPath);
      AppConfig.SetSetting(nameof(DataActionsFolderPath), DataActionsFolderPath);
      AppConfig.SetSetting(nameof(SensorsFolderPath), SensorsFolderPath);
      AppConfig.SetSetting(nameof(ReflexesFolderPath), ReflexesFolderPath);
      AppConfig.SetSetting(nameof(PsychicDataFolderPath), PsychicDataFolderPath);
      AppConfig.SetSetting(nameof(LogsFolderPath), LogsFolderPath);
      AppConfig.SetSetting(nameof(BootDataFolderPath), BootDataFolderPath);
      AppConfig.SetSetting(nameof(ScenarioReportsFolderPath), ScenarioReportsFolderPath);
      AppConfig.SetIntSetting(nameof(DefaultStileId), DefaultStileId);
      AppConfig.SetIntSetting(nameof(WaitingPeriodForActionsVal), WaitingPeriodForActionsVal);
      AppConfig.SetIntSetting(nameof(ThinkingCycleDecayAgeDivisor), ThinkingCycleDecayAgeDivisor);
      AppConfig.SetIntSetting(nameof(ThinkingCycleDecayBase), ThinkingCycleDecayBase);
      AppConfig.SetIntSetting(nameof(ThinkingCycleMainMaxAgePulses), ThinkingCycleMainMaxAgePulses);
      AppConfig.SetIntSetting(nameof(NoOperatorStimulusSilencePulses), NoOperatorStimulusSilencePulses);
      AppConfig.SetBoolSetting(nameof(HomeostasisPulseSpeedDriftEnabled), HomeostasisPulseSpeedDriftEnabled);
      AppConfig.SetIntSetting(nameof(DefaultAdaptiveActionId), DefaultAdaptiveActionId);
      AppConfig.SetIntSetting(nameof(DefaultThemeTypeId), DefaultThemeTypeId);
      AppConfig.SetIntSetting(nameof(RecognitionThreshold), RecognitionThreshold);
      AppConfig.SetIntSetting(nameof(CompareLevel), CompareLevel);
      AppConfig.SetFloatSetting(nameof(DifSensorPar), DifSensorPar);
      AppConfig.SetIntSetting(nameof(DynamicTime), DynamicTime);
      AppConfig.SetIntSetting(nameof(ReflexActionDisplayDuration), ReflexActionDisplayDuration);
      AppConfig.SetBoolSetting(nameof(LogEnabled), LogEnabled);
      AppConfig.SetLogFormatSetting(nameof(DefaultFormatLog), (ResearchLogger.LogFormat)DefaultFormatLog);

      AppConfig.MirrorHubToProjectProfile();
    }

    private void SaveSettingsWithParameter(object _)
    {
      if (!ValidateAllSettings())
        return;

      try
      {
        PushSettingsToAppConfig();

        var reloadQuestion = MessageBox.Show(
            "Настройки успешно сохранены.\n\nПерегрузить проект с изменёнными настройками?",
            "Успех",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (reloadQuestion == MessageBoxResult.Yes && _reloadRuntimeAfterProjectRootSwitch != null)
        {
          _reloadRuntimeAfterProjectRootSwitch(this);
          MessageBox.Show(
              "Проект успешно перезагружен; движок работает с сохранёнными настройками.",
              "Перезагрузка",
              MessageBoxButton.OK,
              MessageBoxImage.Information);
        }
      }
      catch (Exception ex)
      {
        var errorDialog = new TaskDialog
        {
          WindowTitle = "Ошибка",
          MainInstruction = "Ошибка при сохранении настроек",
          Content = ex.Message,
          MainIcon = TaskDialogIcon.Error,
          Buttons = { new TaskDialogButton(ButtonType.Ok) }
        };

        errorDialog.ShowDialog();
      }
    }
  }
}