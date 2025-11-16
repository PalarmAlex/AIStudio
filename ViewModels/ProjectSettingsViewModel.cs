using ISIDA.Common;
using ISIDA.Reflexes;
using ISIDA.Actions;
using ISIDA.Gomeostas;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace AIStudio.ViewModels
{
  public class ProjectSettingsViewModel : INotifyPropertyChanged
  {
    private readonly GomeostasSystem _gomeostas;
    private readonly AdaptiveActionsSystem _actionsSystem;
    private readonly GeneticReflexesSystem _geneticReflexesSystem;

    private bool _isInitialized = false;
    private bool _logEnabled = false;
    private string _settingsPath;
    private string _dataGomeostasFolderPath;
    private string _dataActionsFolderPath;
    private string _sensorsFolderPath;
    private string _reflexesFolderPath;
    private string _logsFolderPath;
    private int _defaultStileId;
    private int _defaultAdaptiveActionId;
    private int _defaultFormatLog;

    private int _recognitionThreshold;
    private int _previousRecognitionThreshold;

    private int _compareLevel;
    private int _previousCompareLevel;

    private float _difSensorPar;
    private string _difSensorParText;

    private float _defaultBaseThreshold;
    private string _defaultBaseThresholdText;

    private float _defaultKCompetition;
    private string _defaultKCompetitionText;

    private int _dynamicTime;
    private int _previousDynamicTime;

    private int _reflexActionDisplayDuration;
    private int _previousReflexActionDisplayDuration;

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
    public string LogsFolderPath
    {
      get => _logsFolderPath;
      set
      {
        _logsFolderPath = value;
        OnPropertyChanged(nameof(LogsFolderPath));
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
    public string DefaultBaseThresholdText
    {
      get => _defaultBaseThresholdText ?? _defaultBaseThreshold.ToString(CultureInfo.InvariantCulture);
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
            _defaultBaseThresholdText = normalizedValue;
            _defaultBaseThreshold = result;
            OnPropertyChanged(nameof(DefaultBaseThreshold));
          }
          else
          {
            MessageBox.Show(validation.errorMessage, "Ошибка ввода");
            _defaultBaseThresholdText = _defaultBaseThreshold.ToString(CultureInfo.InvariantCulture);
            OnPropertyChanged(nameof(DefaultBaseThresholdText));
          }
        }
        else
          OnPropertyChanged(nameof(DefaultBaseThresholdText));
      }
    }
    public float DefaultBaseThreshold
    {
      get => _defaultBaseThreshold;
      set
      {
        var validation = SettingsValidator.ValidateDefaultBaseThreshold(value);
        if (validation.isValid)
        {
          _defaultBaseThreshold = value;
          _defaultBaseThresholdText = value.ToString(CultureInfo.InvariantCulture);
          OnPropertyChanged(nameof(DefaultBaseThreshold));
          OnPropertyChanged(nameof(DefaultBaseThresholdText));
        }
        else
          MessageBox.Show(validation.errorMessage, "Ошибка ввода");
      }
    }
    public string DefaultKCompetitionText
    {
      get => _defaultKCompetitionText ?? _defaultKCompetition.ToString(CultureInfo.InvariantCulture);
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
            _defaultKCompetitionText = normalizedValue;
            _defaultKCompetition = result;
            OnPropertyChanged(nameof(DefaultKCompetition));
          }
          else
          {
            MessageBox.Show(validation.errorMessage, "Ошибка ввода");
            _defaultKCompetitionText = _defaultKCompetition.ToString(CultureInfo.InvariantCulture);
            OnPropertyChanged(nameof(DefaultKCompetitionText));
          }
        }
        else
          OnPropertyChanged(nameof(DefaultKCompetitionText));
      }
    }
    public float DefaultKCompetition
    {
      get => _defaultKCompetition;
      set
      {
        var validation = SettingsValidator.ValidateDefaultKCompetition(value);
        if (validation.isValid)
        {
          _defaultKCompetition = value;
          _defaultKCompetitionText = value.ToString(CultureInfo.InvariantCulture);
          OnPropertyChanged(nameof(DefaultKCompetition));
          OnPropertyChanged(nameof(DefaultKCompetitionText));
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

    public ObservableCollection<SelectableItem> BehaviorStylesWithNone { get; } = new ObservableCollection<SelectableItem>();
    public ObservableCollection<SelectableItem> AdaptiveActionsWithNone { get; } = new ObservableCollection<SelectableItem>();
    public ObservableCollection<SelectableItem> GeneticReflexesWithNone { get; } = new ObservableCollection<SelectableItem>();
    public ObservableCollection<SelectableItem> FormatLog { get; } = new ObservableCollection<SelectableItem>();

    public class SelectableItem
    {
      public int Id { get; set; }
      public string Name { get; set; }
    }

    public ProjectSettingsViewModel(GomeostasSystem gomeostas)
    {
      SettingsPath = AppConfig.SettingsPath;
      DataGomeostasFolderPath = AppConfig.DataGomeostasFolderPath;
      DataActionsFolderPath = AppConfig.DataActionsFolderPath;
      SensorsFolderPath = AppConfig.SensorsFolderPath;
      ReflexesFolderPath = AppConfig.ReflexesFolderPath;
      LogsFolderPath = AppConfig.LogsFolderPath;
      DefaultStileId = AppConfig.DefaultStileId;
      DefaultAdaptiveActionId = AppConfig.DefaultAdaptiveActionId;
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

      _defaultBaseThreshold = AppConfig.DefaultBaseThreshold;
      _defaultKCompetition = AppConfig.DefaultKCompetition;

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
        _geneticReflexesSystem = GeneticReflexesSystem.Instance;
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
      LoadLogFormats();

      DefaultFormatLog = (int)AppConfig.LogFormat;
      BrowseFolderCommand = new RelayCommand(BrowseFolderWithParameter);
      SaveSettingsCommand = new RelayCommand(SaveSettingsWithParameter);

      _isInitialized = true;
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
        case nameof(LogsFolderPath):
          initialPath = Directory.Exists(LogsFolderPath) ? LogsFolderPath : "";
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
            break;
          case nameof(DataGomeostasFolderPath):
            DataGomeostasFolderPath = dialog.SelectedPath;
            break;
          case nameof(DataActionsFolderPath):
            DataActionsFolderPath = dialog.SelectedPath;
            break;
          case nameof(SensorsFolderPath):
            SensorsFolderPath = dialog.SelectedPath;
            break;
          case nameof(ReflexesFolderPath):
            ReflexesFolderPath = dialog.SelectedPath;
            break;
          case nameof(LogsFolderPath):
            LogsFolderPath = dialog.SelectedPath;
            break;
        }
      }
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

    private void SaveSettingsWithParameter(object _)
    {
      if (!ValidateAllSettings())
        return;

      try
      {
        AppConfig.SetSetting(nameof(SettingsPath), SettingsPath);
        AppConfig.SetSetting(nameof(DataGomeostasFolderPath), DataGomeostasFolderPath);
        AppConfig.SetSetting(nameof(DataActionsFolderPath), DataActionsFolderPath);
        AppConfig.SetSetting(nameof(SensorsFolderPath), SensorsFolderPath);
        AppConfig.SetSetting(nameof(ReflexesFolderPath), ReflexesFolderPath);
        AppConfig.SetSetting(nameof(LogsFolderPath), LogsFolderPath);
        AppConfig.SetIntSetting(nameof(DefaultStileId), DefaultStileId);
        AppConfig.SetIntSetting(nameof(DefaultAdaptiveActionId), DefaultAdaptiveActionId);
        AppConfig.SetIntSetting(nameof(RecognitionThreshold), RecognitionThreshold);
        AppConfig.SetIntSetting(nameof(CompareLevel), CompareLevel);
        AppConfig.SetFloatSetting(nameof(DifSensorPar), DifSensorPar);
        AppConfig.SetIntSetting(nameof(DynamicTime), DynamicTime);
        AppConfig.SetIntSetting(nameof(ReflexActionDisplayDuration), ReflexActionDisplayDuration);
        AppConfig.SetBoolSetting(nameof(LogEnabled), LogEnabled);
        AppConfig.SetLogFormatSetting(nameof(DefaultFormatLog), (ResearchLogger.LogFormat)DefaultFormatLog);

        var dialog = new TaskDialog
        {
          WindowTitle = "Успех",
          MainInstruction = "Настройки успешно сохранены!",
          Content = "Для применения изменений перезапустите приложение.",
          MainIcon = TaskDialogIcon.Information,
          Buttons = { new TaskDialogButton(ButtonType.Ok) }
        };

        dialog.ShowDialog();
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