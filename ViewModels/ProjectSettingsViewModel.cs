using isida.Reflexes;
using ISIDA.Actions;
using ISIDA.Gomeostas;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace AIStudio.ViewModels
{
  public class ProjectSettingsViewModel : INotifyPropertyChanged
  {
    private readonly GomeostasSystem _gomeostas;
    private readonly AdaptiveActionsSystem _actionsSystem;
    private readonly GeneticReflexesSystem _geneticReflexesSystem;

    private string _settingsPath;
    private string _dataGomeostasFolderPath;
    private string _dataGomeostasTemplateFolderPath;
    private string _dataActionsFolderPath;
    private string _dataActionsTemplateFolderPath;
    private string _sensorsFolderPath;
    private string _sensorsTemplateFolderPath;
    private string _reflexesFolderPath;
    private string _reflexesTemplateFolderPath;
    private int _defaultStileId;
    private int _defaultAdaptiveActionId;
    private int _defaultGeneticReflexId;
    private int _recognitionThreshold;
    private int _compareLevel;
    private int _difSensorPar;
    private int _dynamicTime;

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
    public string DataGomeostasTemplateFolderPath
    {
      get => _dataGomeostasTemplateFolderPath;
      set
      {
        _dataGomeostasTemplateFolderPath = value;
        OnPropertyChanged(nameof(DataGomeostasTemplateFolderPath));
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
    public string DataActionsTemplateFolderPath
    {
      get => _dataActionsTemplateFolderPath;
      set
      {
        _dataActionsTemplateFolderPath = value;
        OnPropertyChanged(nameof(DataActionsTemplateFolderPath));
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
    public string SensorsTemplateFolderPath
    {
      get => _sensorsTemplateFolderPath;
      set
      {
        _sensorsTemplateFolderPath = value;
        OnPropertyChanged(nameof(SensorsTemplateFolderPath));
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
    public string ReflexesTemplateFolderPath
    {
      get => _reflexesTemplateFolderPath;
      set
      {
        _reflexesTemplateFolderPath = value;
        OnPropertyChanged(nameof(ReflexesTemplateFolderPath));
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
    public int DefaultAdaptiveActionId
    {
      get => _defaultAdaptiveActionId;
      set
      {
        _defaultAdaptiveActionId = value;
        OnPropertyChanged(nameof(DefaultAdaptiveActionId));
      }
    }
    public int DefaultGeneticReflexId
    {
      get => _defaultGeneticReflexId;
      set
      {
        _defaultGeneticReflexId = value;
        OnPropertyChanged(nameof(DefaultGeneticReflexId));
      }
    }
    public int RecognitionThreshold
    {
      get => _recognitionThreshold;
      set
      {
        _recognitionThreshold = value;
        OnPropertyChanged(nameof(RecognitionThreshold));
      }
    }
    public int CompareLevel
    {
      get => _compareLevel;
      set
      {
        _compareLevel = value;
        OnPropertyChanged(nameof(CompareLevel));
      }
    }
    public int DifSensorPar
    {
      get => _difSensorPar;
      set
      {
        _difSensorPar = value;
        OnPropertyChanged(nameof(DifSensorPar));
      }
    }
    public int DynamicTime
    {
      get => _dynamicTime;
      set
      {
        _dynamicTime = value;
        OnPropertyChanged(nameof(DynamicTime));
      }
    }

    public ICommand BrowseFolderCommand { get; }
    public ICommand SaveSettingsCommand { get; }

    public ObservableCollection<SelectableItem> BehaviorStylesWithNone { get; } = new ObservableCollection<SelectableItem>();
    public ObservableCollection<SelectableItem> AdaptiveActionsWithNone { get; } = new ObservableCollection<SelectableItem>();
    public ObservableCollection<SelectableItem> GeneticReflexesWithNone { get; } = new ObservableCollection<SelectableItem>();

    public class SelectableItem
    {
      public int Id { get; set; }
      public string Name { get; set; }
    }

    public ProjectSettingsViewModel(GomeostasSystem gomeostas)
    {
      SettingsPath = AppConfig.SettingsPath;
      DataGomeostasFolderPath = AppConfig.DataGomeostasFolderPath;
      DataGomeostasTemplateFolderPath = AppConfig.DataGomeostasTemplateFolderPath;
      DataActionsFolderPath = AppConfig.DataActionsFolderPath;
      DataActionsTemplateFolderPath = AppConfig.DataActionsTemplateFolderPath;
      SensorsFolderPath = AppConfig.SensorsFolderPath;
      SensorsTemplateFolderPath = AppConfig.SensorsTemplateFolderPath;
      ReflexesFolderPath = AppConfig.ReflexesFolderPath;
      ReflexesTemplateFolderPath = AppConfig.ReflexesTemplateFolderPath;
      DefaultStileId = AppConfig.DefaultStileId;
      DefaultAdaptiveActionId = AppConfig.DefaultAdaptiveActionId;
      DefaultGeneticReflexId = AppConfig.DefaultGeneticReflexId;
      RecognitionThreshold = AppConfig.RecognitionThreshold;
      CompareLevel = AppConfig.CompareLevel;
      DifSensorPar = AppConfig.DifSensorPar;
      DynamicTime = AppConfig.DynamicTime;

      _gomeostas = gomeostas;

      try
      {
        if (!AdaptiveActionsSystem.IsInitialized)
        {
          AdaptiveActionsSystem.InitializeInstance(_gomeostas,
              DataActionsFolderPath,
              DataActionsTemplateFolderPath);
        }
        _actionsSystem = AdaptiveActionsSystem.Instance;

        if (!GeneticReflexesSystem.IsInitialized)
        {
          GeneticReflexesSystem.InitializeInstance(_gomeostas,
              ReflexesFolderPath,
              ReflexesTemplateFolderPath);
        }
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
      LoadGeneticReflexesWithNone();

      BrowseFolderCommand = new RelayCommand(BrowseFolderWithParameter);
      SaveSettingsCommand = new RelayCommand(SaveSettingsWithParameter);
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

    private void LoadGeneticReflexesWithNone()
    {
      GeneticReflexesWithNone.Clear();
      GeneticReflexesWithNone.Add(new SelectableItem { Id = 0, Name = "Нет" });

      if (_geneticReflexesSystem?.GetAllGeneticReflexes() != null)
      {
        foreach (var reflex in _geneticReflexesSystem.GetAllGeneticReflexes().OrderBy(r => r.Id))
        {
          GeneticReflexesWithNone.Add(new SelectableItem { Id = reflex.Id, Name = reflex.Name });
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
        case nameof(DataGomeostasTemplateFolderPath):
          initialPath = Directory.Exists(DataGomeostasTemplateFolderPath) ? DataGomeostasTemplateFolderPath : "";
          break;
        case nameof(DataActionsFolderPath):
          initialPath = Directory.Exists(DataActionsFolderPath) ? DataActionsFolderPath : "";
          break;
        case nameof(DataActionsTemplateFolderPath):
          initialPath = Directory.Exists(DataActionsTemplateFolderPath) ? DataActionsTemplateFolderPath : "";
          break;
        case nameof(SensorsFolderPath):
          initialPath = Directory.Exists(SensorsFolderPath) ? SensorsFolderPath : "";
          break;
        case nameof(SensorsTemplateFolderPath):
          initialPath = Directory.Exists(SensorsTemplateFolderPath) ? SensorsTemplateFolderPath : "";
          break;
        case nameof(ReflexesFolderPath):
          initialPath = Directory.Exists(ReflexesFolderPath) ? ReflexesFolderPath : "";
          break;
        case nameof(ReflexesTemplateFolderPath):
          initialPath = Directory.Exists(ReflexesTemplateFolderPath) ? ReflexesTemplateFolderPath : "";
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
          case nameof(DataGomeostasTemplateFolderPath):
            DataGomeostasTemplateFolderPath = dialog.SelectedPath;
            break;
          case nameof(DataActionsFolderPath):
            DataActionsFolderPath = dialog.SelectedPath;
            break;
          case nameof(DataActionsTemplateFolderPath):
            DataActionsTemplateFolderPath = dialog.SelectedPath;
            break;
          case nameof(SensorsFolderPath):
            SensorsFolderPath = dialog.SelectedPath;
            break;
          case nameof(SensorsTemplateFolderPath):
            SensorsTemplateFolderPath = dialog.SelectedPath;
            break;
          case nameof(ReflexesFolderPath):
            ReflexesFolderPath = dialog.SelectedPath;
            break;
          case nameof(ReflexesTemplateFolderPath):
            ReflexesTemplateFolderPath = dialog.SelectedPath;
            break;
        }
      }
    }

    private void SaveSettingsWithParameter(object _)
    {
      try
      {
        AppConfig.SetSetting(nameof(SettingsPath), SettingsPath);
        AppConfig.SetSetting(nameof(DataGomeostasFolderPath), DataGomeostasFolderPath);
        AppConfig.SetSetting(nameof(DataGomeostasTemplateFolderPath), DataGomeostasTemplateFolderPath);
        AppConfig.SetSetting(nameof(DataActionsFolderPath), DataActionsFolderPath);
        AppConfig.SetSetting(nameof(DataActionsTemplateFolderPath), DataActionsTemplateFolderPath);
        AppConfig.SetSetting(nameof(SensorsFolderPath), SensorsFolderPath);
        AppConfig.SetSetting(nameof(SensorsTemplateFolderPath), SensorsTemplateFolderPath);
        AppConfig.SetSetting(nameof(ReflexesFolderPath), ReflexesFolderPath);
        AppConfig.SetSetting(nameof(ReflexesTemplateFolderPath), ReflexesTemplateFolderPath);
        AppConfig.SetIntSetting(nameof(DefaultStileId), DefaultStileId);
        AppConfig.SetIntSetting(nameof(DefaultAdaptiveActionId), DefaultAdaptiveActionId);
        AppConfig.SetIntSetting(nameof(DefaultGeneticReflexId), DefaultGeneticReflexId);
        AppConfig.SetIntSetting(nameof(RecognitionThreshold), RecognitionThreshold);
        AppConfig.SetIntSetting(nameof(CompareLevel), CompareLevel);
        AppConfig.SetIntSetting(nameof(DifSensorPar), DifSensorPar);
        AppConfig.SetIntSetting(nameof(DynamicTime), DynamicTime);

        // Используем TaskDialog вместо MessageBox
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