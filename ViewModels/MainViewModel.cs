using AIStudio.Common;
using AIStudio.Pages;
using AIStudio.Pages.Automatizm;
using AIStudio.Pages.Episodic;
using AIStudio.Pages.Reflexes;
using AIStudio.Pages.Research;
using AIStudio.Pages.Understanding;
using AIStudio.ViewModels;
using AIStudio.ViewModels.Episodic;
using AIStudio.ViewModels.Research;
using AIStudio.Windows;
using AIStudio.ViewModels.Understanding;
using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Gomeostas;
using ISIDA.Psychic;
using ISIDA.Psychic.Automatism;
using ISIDA.Reflexes;
using ISIDA.Scenarios;
using ISIDA.Sensors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using static ISIDA.Psychic.Automatism.AutomatizmChainsSystem;
using static ISIDA.Psychic.VerbalBrocaImagesSystem;

namespace AIStudio
{
  public class MainViewModel : INotifyPropertyChanged
  {
    #region Объявление констант и переменных

    private readonly IsidaContext _isidaContext;
    private readonly GomeostasSystem _gomeostas;
    private readonly SensorySystem _sensorySystem;
    private readonly AdaptiveActionsSystem _actionsSystem;
    private readonly InfluenceActionSystem _influenceActionSystem;
    private readonly GeneticReflexesSystem _geneticReflexesSystem;
    private readonly ConditionedReflexesSystem _conditionedReflexesSystem;
    private readonly PerceptionImagesSystem _perceptionImagesSystem;
    private readonly ActionsImagesSystem _actionsImagesSystem;
    private readonly AutomatizmSystem _automatizmSystem;
    private readonly AutomatizmTreeSystem _automatizmTreeSystem;
    private readonly PsychicSystem _psychicSystem;
    private readonly ReflexesActivator _reflexesActivator;
    private readonly ReflexTreeSystem _reflexTree;
    private readonly ReflexChainsSystem _reflexChains;
    private readonly ReflexExecutionService _reflexExecution;
    private readonly ResearchLogger _researchLogger;
    private readonly InfluenceActionsImagesSystem _influenceActionsImagesSystem;
    private readonly EmotionsImageSystem _emotionsImageSystem;
    private readonly VerbalBrocaImagesSystem _verbalBrocaImages;
    private readonly ConditionedReflexToAutomatizmConverter _conditionedReflexToAutomatizm;
    private readonly AutomatizmChainsSystem _automatizmChains;
    private readonly AutomatizmFileLoader _automatizmFileLoader;
    private readonly Stage2PrimitivesLoader _stage2PrimitivesLoader;
    private readonly OperatorScenarioRunner _scenarioRunner = new OperatorScenarioRunner();
    private bool _wasPulsatingForScenario;
    private ScenarioRunProgressWindow _scenarioRunProgressWindow;
    private string _pendingScenarioReportFolder;
    private ScenarioBatchRunState _scenarioBatchRun;
    private bool _scenarioPultModesSaved;
    private bool _savedObservationModeBeforeScenario;
    private bool _savedAuthoritativeRecordingBeforeScenario;

    public event PropertyChangedEventHandler PropertyChanged;
    private AgentViewModel _agentViewModel;

    private ICommand _openAgentCommand;
    public ICommand OpenAgentCommand => _openAgentCommand ?? (_openAgentCommand = new RelayCommand(_ => OpenAgent()));
    public ICommand VerticalMenuItemClickedCommand { get; }
    public ICommand ResetLifeTimeCommand { get; }

    private bool _isAgentExpanded = true;
    public bool IsAgentExpanded
    {
      get => _isAgentExpanded;
      set
      {
        if (_isAgentExpanded != value)
        {
          _isAgentExpanded = value;
          OnPropertyChanged(nameof(IsAgentExpanded));
        }
      }
    }

    private object _currentContent;
    public object CurrentContent
    {
      get => _currentContent;
      set
      {
        _currentContent = value;
        OnPropertyChanged(nameof(CurrentContent));
      }
    }

    private bool _isAgentDead;
    public bool IsAgentDead
    {
      get => _isAgentDead;
      set
      {
        if (_isAgentDead != value)
        {
          _isAgentDead = value;
          OnPropertyChanged(nameof(IsAgentDead));
          OnPropertyChanged(nameof(IsPulseButtonEnabled));
          OnPropertyChanged(nameof(PulseButtonText));
          OnPropertyChanged(nameof(PulseButtonColor));
          OnPropertyChanged(nameof(PulseStatus));

          if (_isAgentDead && IsPulsating)
            StopPulsationDueToDeath();
        }
      }
    }

    public bool IsPulseButtonEnabled => !IsAgentDead;

    #endregion

    public MainViewModel()
    {
      int _stepInzialized = 0;
      try
      {
        _stepInzialized = 1;

        var config = new IsidaConfig();

        config.BaseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "ISIDA");

        config.GomeostasFolder = AppConfig.DataGomeostasFolderPath;
        config.ActionsFolder = AppConfig.DataActionsFolderPath;
        config.SensorsFolder = AppConfig.SensorsFolderPath;
        config.ReflexesFolder = AppConfig.ReflexesFolderPath;
        config.PsychicDataFolder = AppConfig.PsychicDataFolderPath;
        config.LogsFolder = AppConfig.LogsFolderPath;
        config.BootDataFolder = AppConfig.BootDataFolderPath;
        config.LogFormat = AppConfig.LogFormat;
        config.LogEnabled = AppConfig.LogEnabled;
        config.DefaultStileId = AppConfig.DefaultStileId;
        config.CompareLevel = AppConfig.CompareLevel;
        config.DifSensorPar = AppConfig.DifSensorPar;
        config.DynamicTime = AppConfig.DynamicTime;
        config.ReflexActionDisplayDuration = AppConfig.ReflexActionDisplayDuration;
        config.DefaultAdaptiveActionId = AppConfig.DefaultAdaptiveActionId;
        config.DefaultThemeTypeId = AppConfig.DefaultThemeTypeId;
        config.RecognitionThreshold = AppConfig.RecognitionThreshold;
        config.WaitingPeriodForActionsVal = AppConfig.WaitingPeriodForActionsVal;
        config.ThinkingCycleDecayAgeDivisor = AppConfig.ThinkingCycleDecayAgeDivisor;
        config.ThinkingCycleDecayBase = AppConfig.ThinkingCycleDecayBase;
        config.ThinkingCycleMainMaxAgePulses = AppConfig.ThinkingCycleMainMaxAgePulses;
        config.NoOperatorStimulusSilencePulses = AppConfig.NoOperatorStimulusSilencePulses;
        config.MemoryLogWriter = MemoryLogManager.Instance;

        _stepInzialized = 2;

        _isidaContext = IsidaEngine.Create(config);

        _stepInzialized = 3;

        _gomeostas = _isidaContext.Gomeostas;
        _sensorySystem = _isidaContext.SensorySystem;
        _actionsSystem = _isidaContext.AdaptiveActions;
        _influenceActionSystem = _isidaContext.InfluenceActions;
        _geneticReflexesSystem = _isidaContext.GeneticReflexes;
        _conditionedReflexesSystem = _isidaContext.ConditionedReflexes;
        _perceptionImagesSystem = _isidaContext.PerceptionImages;
        _actionsImagesSystem = _isidaContext.ActionsImages;
        _automatizmSystem = _isidaContext.AutomatizmSystem;
        _automatizmTreeSystem = _isidaContext.AutomatizmTree;
        _psychicSystem = _isidaContext.PsychicSystem;
        _reflexesActivator = _isidaContext.ReflexesActivator;
        _reflexTree = _isidaContext.ReflexTree;
        _reflexChains = _isidaContext.ReflexChains;
        _reflexExecution = _isidaContext.ReflexExecution;
        _researchLogger = _isidaContext.ResearchLogger;
        _influenceActionsImagesSystem = _isidaContext.InfluenceActionsImages;
        _emotionsImageSystem = _isidaContext.EmotionsImageSystem;
        _verbalBrocaImages = _isidaContext.VerbalBrocaImagesSystem;
        _conditionedReflexToAutomatizm = _isidaContext.ConditionedReflexToAutomatizm;
        _automatizmChains = _isidaContext.AutomatizmChainsSystem;
        _automatizmFileLoader = _isidaContext.AutomatizmFileLoader;
        _stage2PrimitivesLoader = _isidaContext.Stage2PrimitivesLoader;

        _stepInzialized = 4;
      }
      catch (IsidaInitializationException ex)
      {
        Logger.Error(ex.Message);
        MessageBox.Show($"StepInzialized: {_stepInzialized}, Ошибка инициализации ISIDA на шаге {ex.InitializationStep}: {ex.Message}",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }
      catch (Exception ex)
      {
        Logger.Error(ex.Message);
        MessageBox.Show($"StepInzialized: {_stepInzialized}, Общая ошибка инициализации: {ex.Message}",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      PulseIndicatorColor = new SolidColorBrush(Color.FromRgb(80, 80, 80));

      InitializePulseCommands();
      SetupPulseHandlers();

      _scenarioRunner.Finished += OnScenarioRunFinished;
      _scenarioRunner.RunningStateChanged += OnScenarioRunningStateChanged;
      _scenarioRunner.StepProgress += OnScenarioStepProgress;
      _scenarioRunner.WaitingForActivation += OnScenarioWaitingForActivation;
      _wasPulsatingForScenario = GlobalTimer.IsPulsationRunning;
      GlobalTimer.PulsationStateChanged += OnPulsationStateChangedForScenario;

      ResetLifeTimeCommand = new RelayCommand(_ =>
      {
        if (IsAgentDead)
        {
          MessageBox.Show("Невозможно сбросить время жизни мертвого агента",
              "Агент мертв",
              MessageBoxButton.OK,
              MessageBoxImage.Error);
          return;
        }

        if (MessageBox.Show("Сбросить время жизни системы?", "Подтверждение",
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
          GlobalTimer.Reset();
          OnPropertyChanged(nameof(LifeTimeStatus));
        }
      }, _ => !IsAgentDead);
      VerticalMenuItemClickedCommand = new RelayCommand(ExecuteVerticalMenuItemClicked);

      IsAgentExpanded = true;
      OpenAgent();
      UpdateAgentState();
    }

    /// <summary>
    /// Завершение работы всех систем
    /// </summary>
    public void Shutdown()
    {
      try
      {
        Logger.Info("Начало завершения работы...");

        // 1. Останавливаем пульсацию если активна
        if (IsPulsating)
        {
          IsPulsating = false;
          Logger.Info("Пульсация остановлена");
        }

        // 2. Вызываем Dispose у всех систем через IsidaContext
        if (_isidaContext != null)
        {
          _isidaContext.Dispose();
          Logger.Info("IsidaContext освобожден");
        }

        Logger.Info("Завершение работы успешно завершено");
      }
      catch (Exception ex)
      {
        Logger.Error(ex.Message);
      }
    }

    private void UpdateAgentState()
    {
      try
      {
        var agentInfo = _gomeostas.GetAgentState();
        if (agentInfo != null)
        {
          bool wasDead = IsAgentDead;
          IsAgentDead = agentInfo.IsDead;
          // Если агент только что умер и пульсация активна
          if (!wasDead && IsAgentDead && IsPulsating)
          {
            Logger.Info("Агент умер, останавливаем пульсацию");
            StopPulsationDueToDeath();
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Error(ex.Message);
      }
    }

    private void StopPulsationDueToDeath()
    {
      SafeStopPulsation("агент мертв");
    }

    #region Левое меню проекта
    private void ExecuteVerticalMenuItemClicked(object parameter)
    {
      if (IsAgentDead)
      {
        MessageBox.Show("Невозможно выполнить операцию для мертвого агента",
            "Агент мертв",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        return;
      }

      if (parameter is string menuItem)
      {
        switch (menuItem)
        {
          case "1": // Жизненные параметры
            ShowSystemParameters();
            break;
          case "2": // Стили реагирования
            ShowBehaviorStyles();
            break;
          case "3": // Адаптивные действия
            ShowAdaptiveActions();
            break;
          case "4": // Безусловные рефлексы
            ShowGeneticReflexes();
            break;
          case "6": // Цепочки безусловных рефлексов
            OpenReflexChains();
            break;
          case "5": // Условные рефлексы
            ShowConditionedReflexes();
            break;
          case "8": // Таблица Автоматизмов
            ShowAutomatizms();
            break;
          case "10": // Цепочки Автоматизмов
            OpenAutomatizmChains();
            break;
          case "11": // Дерево эпизодической памяти
            ShowEpisodicMemoryTree();
            break;
          case "12": // Дерево ситуации
            ShowStub("Дерево ситуации");
            break;
          case "14": // Типы ситуаций
            ShowSituationTypes();
            break;
          case "15": // Темы мышления (типы тем)
            ShowThemeTypes();
            break;
          case "13": // Дерево проблем
            ShowProblemTree();
            break;
          case "16": // Циклы осмысления
            ShowThinkingCycles();
            break;
          case "17": // Значимость элементов восприятия
            ShowStub("Значимость элементов восприятия");
            break;
          case "18": // Информационная среда
            ShowStub("Информационная среда");
            break;
          case "19": // Экспортировать настройки
            ShowStub("Экспортировать настройки");
            break;
          case "20": // Сохранить текущее состояние
            ShowStub("Сохранить текущее состояние");
            break;
          case "21": // Сохранить память
            ShowStub("Сохранить память");
            break;
          case "22": // Архивировать всю память
            ShowStub("Архивировать всю память");
            break;
          case "23": // Восстановить память
            ShowStub("Восстановить память");
            break;
          case "24": // Сбросить память психики
            ShowStub("Сбросить память психики");
            break;
          case "25": // Настройки проекта
            ShowProjectSettings();
            break;
          case "26": // Справка: руководство пользователя
            OpenWebPage("https://scorcher.ru/isida/iadaptive_agents_guide.php");
            break;
          case "27": // Сенсорная система
            OpenSensors();
            break;
          case "28": // Внешнее воздействие
            ShowExtInfluence();
            break;
          case "29": // Справка: руководство разработчика
            OpenWebPage("https://scorcher.ru/isida/index.php");
            break;
          case "30": // Справка: теория МВАП
            OpenWebPage("https://p-mvap.ru");
            break;
          case "31": // Справка: теория проектирования
            OpenWebPage("https://scorcher.ru/theory_publisher/show_art.php?id=891");
            break;
          case "32": // Справка: исходник
            OpenWebPage("https://github.com/PalarmAlex/AIStudio.git");
            break;
          case "33": // Агент
            OpenAgent();
            break;
          case "39": // Свойства агента
            ShowAgentProperties();
            break;
          case "34":  // логи системы
            ShowLiveLogs();
            break;
          case "35":  // логи стилей
            ShowStileLogs();
            break;
          case "36":  // логи параметров
            ShowParametersLogs();
            break;
          case "37": // Справка: видеоканал
            OpenWebPage("https://rutube.ru/channel/74522900/videos");
            break;
          case "38":  // о программе
            ShowAbout();
            break;
          case "40": // Исследования: сценарии оператора
            ShowScenarioRegistry();
            break;
          case "41": // Исследования: группы сценариев
            ShowScenarioGroupRegistry();
            break;
          default:
            ShowStub($"Меню {menuItem}");
            break;
        }
      }
    }

    private void ShowSituationTypes()
    {
      var view = new SituationTypesView();
      var viewModel = new SituationTypesViewModel(
          _gomeostas,
          _isidaContext?.SituationTypeSystem);
      view.DataContext = viewModel;
      CurrentContent = view;
    }

    private void ShowThemeTypes()
    {
      var view = new ThemeTypesView();
      var viewModel = new ThemeTypesViewModel(_gomeostas);
      view.DataContext = viewModel;
      CurrentContent = view;
    }
 
    private void ShowEpisodicMemoryTree()
    {
      var view = new EpisodicMemoryTreeView();
      var viewModel = new EpisodicMemoryTreeViewModel(
          _isidaContext?.EpisodicMemory,
          _gomeostas,
          _emotionsImageSystem,
          _influenceActionSystem,
          _actionsSystem,
          _isidaContext?.ProblemTree,
          _influenceActionsImagesSystem,
          _actionsImagesSystem,
          _sensorySystem);
      view.DataContext = viewModel;
      CurrentContent = view;
    }

    private void ShowProblemTree()
    {
      var view = new ProblemTreeView();
      var automatizmsVm = new AutomatizmsViewModel(
          _gomeostas,
          _automatizmSystem,
          _automatizmTreeSystem,
          _actionsImagesSystem,
          _influenceActionsImagesSystem,
          _emotionsImageSystem,
          _sensorySystem,
          _influenceActionSystem,
          _actionsSystem,
          _verbalBrocaImages,
          _conditionedReflexToAutomatizm,
          _automatizmFileLoader,
          _stage2PrimitivesLoader);

      var viewModel = new ProblemTreeViewModel(
          _isidaContext?.ProblemTree,
          _automatizmTreeSystem,
          _isidaContext?.SituationTypeSystem,
          _isidaContext?.SituationImageSystem,
          getAutNodeDetails: automatizmsVm.GetFullAutNodeDetails);
      view.DataContext = viewModel;
      CurrentContent = view;
    }

    private void ShowThinkingCycles()
    {
      var view = new ThinkingCyclesView();
      var viewModel = new ThinkingCyclesViewModel(_psychicSystem);
      view.DataContext = viewModel;
      CurrentContent = view;
    }

    private void ShowScenarioRegistry()
    {
      void OpenEditorEmbedded(ScenarioEditorViewModel vm)
      {
        vm.CloseAction = ShowScenarioRegistry;
        vm.RequestClose += (_, __) => ShowScenarioRegistry();
        CurrentContent = new ScenarioEditorView { DataContext = vm };
      }

      var scenariosVm = new ScenarioRegistryViewModel(
          _influenceActionSystem,
          _scenarioRunner,
          openEditorEmbedded: OpenEditorEmbedded,
          tryStartScenario: TryStartScenario);
      CurrentContent = new ScenarioRegistryView { DataContext = scenariosVm };
    }

    private void ShowScenarioGroupRegistry()
    {
      void OpenGroupEditorEmbedded(ScenarioGroupEditorViewModel vm)
      {
        vm.CloseAction = ShowScenarioGroupRegistry;
        vm.RequestClose += (_, __) => ShowScenarioGroupRegistry();
        CurrentContent = new ScenarioGroupEditorView { DataContext = vm };
      }

      var groupRegistryVm = new ScenarioGroupRegistryViewModel(
          _scenarioRunner,
          TryStartScenarioGroup,
          OpenGroupEditorEmbedded,
          GetScenarioGroupLaunchPrecheckError);
      CurrentContent = new ScenarioGroupRegistryView { DataContext = groupRegistryVm };
    }

    /// <summary>Проверки среды до окна подтверждения группового запуска (пульсация, пульт и т.д.).</summary>
    private string GetScenarioGroupLaunchPrecheckError()
    {
      if (_scenarioBatchRun != null)
        return "Уже выполняется групповой прогон.";
      if (_scenarioRunner.IsRunning)
        return "Дождитесь завершения текущего сценария.";
      if (_agentViewModel?.AgentPultViewModel == null)
        return "Откройте раздел «Агент» (пульт), чтобы сценарий мог подавать воздействия.";
      if (!GlobalTimer.IsPulsationRunning)
        return "Включите пульсацию перед запуском сценария.";
      if (_gomeostas.GetAgentState()?.IsDead == true)
        return "Агент мёртв — запуск сценария невозможен.";
      return null;
    }

    private void OnPulsationStateChangedForScenario()
    {
      if (_wasPulsatingForScenario && !GlobalTimer.IsPulsationRunning)
        _scenarioRunner.OnPulsationStopped();
      _wasPulsatingForScenario = GlobalTimer.IsPulsationRunning;
    }

    private void OnScenarioRunningStateChanged()
    {
      Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
      {
        try
        {
          if (_scenarioRunner.IsRunning)
          {
            if (_scenarioRunProgressWindow == null)
            {
              _scenarioRunProgressWindow = new ScenarioRunProgressWindow
              {
                Owner = Application.Current?.MainWindow
              };
              _scenarioRunProgressWindow.SetStatus("Запуск сценария…");
              _scenarioRunProgressWindow.Show();
            }
          }
          else
          {
            if (_scenarioRunProgressWindow != null)
            {
              try { _scenarioRunProgressWindow.Close(); } catch { /* ignore */ }
              _scenarioRunProgressWindow = null;
            }
          }
        }
        catch (Exception ex)
        {
          Logger.Error(ex.Message);
        }
      }));
    }

    private void OnScenarioStepProgress(object sender, OperatorScenarioStepProgressEventArgs e)
    {
      Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
      {
        try
        {
          if (_scenarioRunProgressWindow != null)
            _scenarioRunProgressWindow.SetStatus($"Выполняется: шаг №{e.StepIndex}…");
        }
        catch (Exception ex)
        {
          Logger.Error(ex.Message);
        }
      }));
    }

    private void OnScenarioWaitingForActivation(int currentPulse, int targetPulse)
    {
      Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
      {
        try
        {
          if (_scenarioRunProgressWindow != null)
            _scenarioRunProgressWindow.SetStatus(
                $"Ожидание активации психики… (пульс {currentPulse}, старт на пульсе {targetPulse})");
        }
        catch (Exception ex)
        {
          Logger.Error(ex.Message);
        }
      }));
    }

    private static IEnumerable<ScenarioEditorViewModel> EnumerateOpenScenarioEditors(object currentContent)
    {
      if (currentContent is ScenarioEditorView embedded && embedded.DataContext is ScenarioEditorViewModel evm)
        yield return evm;
      var app = Application.Current;
      if (app?.Windows == null)
        yield break;
      foreach (Window w in app.Windows)
      {
        if (w is ScenarioEditorWindow && w.DataContext is ScenarioEditorViewModel wvm)
          yield return wvm;
      }
    }

    /// <summary>Тот же сценарий открыт в другом редакторе — запуск с диска даст рассинхрон с несохранённым.</summary>
    private bool IsSameScenarioOpenInAnotherEditor(ScenarioDocument doc, ScenarioEditorViewModel initiatingEditor)
    {
      if (doc?.Header == null)
        return false;
      int id = doc.Header.Id;
      foreach (var vm in EnumerateOpenScenarioEditors(CurrentContent))
      {
        if (vm == initiatingEditor)
          continue;
        var openDoc = vm.Document;
        if (openDoc?.Header == null)
          continue;
        if (id > 0 && openDoc.Header.Id == id)
          return true;
        if (id == 0 && ReferenceEquals(openDoc, doc))
          return true;
      }
      return false;
    }

    /// <summary>Запуск сценария из реестра или редактора: проверки, переход на стадию, старт раннера.</summary>
    /// <param name="initiatingEditor">Редактор, из которого нажали «Запустить»; null — запуск из реестра.</param>
    public bool TryStartScenario(ScenarioDocument doc, string reportOutputFolder, ScenarioEditorViewModel initiatingEditor = null)
    {
      if (doc == null)
        return false;

      if (IsSameScenarioOpenInAnotherEditor(doc, initiatingEditor))
      {
        MessageBox.Show(
            "Этот сценарий уже открыт в редакторе. Закройте вкладку редактора или окно сценария, затем запустите прогон снова.",
            "Запуск сценария",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        return false;
      }

      var folder = string.IsNullOrWhiteSpace(reportOutputFolder)
          ? AppConfig.ScenarioReportsFolderPath
          : reportOutputFolder.Trim();
      try
      {
        Directory.CreateDirectory(folder);
      }
      catch (Exception ex)
      {
        MessageBox.Show("Не удалось подготовить каталог отчёта: " + ex.Message, "Запуск сценария",
            MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
      }

      var err = OperatorScenarioValidator.ValidateForRun(
          doc, _influenceActionSystem, GlobalTimer.IsPulsationRunning, _gomeostas.GetAgentState()?.IsDead == true);
      if (err != null)
      {
        MessageBox.Show(err, "Запуск сценария", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
      }

      if (_agentViewModel?.AgentPultViewModel == null)
      {
        MessageBox.Show("Откройте раздел «Агент» (пульт), чтобы сценарий мог подавать воздействия.",
            "Запуск сценария", MessageBoxButton.OK, MessageBoxImage.Information);
        return false;
      }

      if (!TryApplyPreRunStageForScenario(doc, out bool homeostasisNormSettlePending))
        return false;

      if (homeostasisNormSettlePending)
      {
        BeginHomeostasisNormSettleWaitThenStartScenario(doc, folder);
        return true;
      }

      try
      {
        ApplyScenarioPultModesAndStartRunner(doc, folder);
      }
      catch (Exception ex)
      {
        GlobalTimer.ClearPulseWallClockAcceleration();
        RestorePultModesAfterScenarioIfNeeded();
        MessageBox.Show(ex.Message, "Запуск", MessageBoxButton.OK, MessageBoxImage.Error);
        return false;
      }
      return true;
    }

    /// <summary>Пакетный запуск группы сценариев с общим отчётом.</summary>
    public bool TryStartScenarioGroup(ScenarioGroupDocument groupDoc, string reportOutputFolder)
    {
      if (groupDoc == null)
        return false;
      if (_scenarioBatchRun != null)
      {
        MessageBox.Show("Уже выполняется групповой прогон.", "Группа сценариев",
            MessageBoxButton.OK, MessageBoxImage.Information);
        return false;
      }
      if (_scenarioRunner.IsRunning)
      {
        MessageBox.Show("Дождитесь завершения текущего сценария.", "Группа сценариев",
            MessageBoxButton.OK, MessageBoxImage.Information);
        return false;
      }

      var folder = string.IsNullOrWhiteSpace(reportOutputFolder)
          ? AppConfig.ScenarioReportsFolderPath
          : reportOutputFolder.Trim();
      try
      {
        Directory.CreateDirectory(folder);
      }
      catch (Exception ex)
      {
        MessageBox.Show("Не удалось подготовить каталог отчёта: " + ex.Message, "Группа сценариев",
            MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
      }

      var ordered = groupDoc.Members?
          .OrderBy(m => m.SortOrderInGroup).ThenBy(m => m.ScenarioId).ToList()
          ?? new List<ScenarioGroupMemberRow>();
      if (ordered.Count == 0)
      {
        MessageBox.Show("В группе нет сценариев для запуска.", "Группа сценариев",
            MessageBoxButton.OK, MessageBoxImage.Information);
        return false;
      }

      if (_agentViewModel?.AgentPultViewModel == null)
      {
        MessageBox.Show("Откройте раздел «Агент» (пульт), чтобы сценарий мог подавать воздействия.",
            "Группа сценариев", MessageBoxButton.OK, MessageBoxImage.Information);
        return false;
      }

      foreach (var m in ordered)
      {
        ScenarioDocument loaded;
        try
        {
          loaded = ScenarioStorage.LoadScenario(m.ScenarioId);
        }
        catch (Exception ex)
        {
          MessageBox.Show($"Не удалось загрузить сценарий ID {m.ScenarioId}: {ex.Message}",
              "Группа сценариев", MessageBoxButton.OK, MessageBoxImage.Warning);
          return false;
        }

        var probe = ScenarioGroupDocument.ApplyMemberToScenario(loaded, m, groupDoc.RunPulseTimingCoefficient);
        if (IsSameScenarioOpenInAnotherEditor(probe, null))
        {
          MessageBox.Show(
              $"Сценарий ID {m.ScenarioId} открыт в редакторе. Закройте редактор и повторите запуск группы.",
              "Группа сценариев", MessageBoxButton.OK, MessageBoxImage.Information);
          return false;
        }

        var err = OperatorScenarioValidator.ValidateForRun(
            probe, _influenceActionSystem, GlobalTimer.IsPulsationRunning, _gomeostas.GetAgentState()?.IsDead == true);
        if (err != null)
        {
          MessageBox.Show($"Сценарий ID {m.ScenarioId}: {err}", "Группа сценариев",
              MessageBoxButton.OK, MessageBoxImage.Warning);
          return false;
        }
      }

      _scenarioBatchRun = new ScenarioBatchRunState
      {
        GroupDefinition = groupDoc.Clone(),
        OrderedMembers = ordered,
        CurrentIndex = 0,
        ReportOutputFolder = folder
      };

      if (!TryStartNextBatchScenario())
      {
        _scenarioBatchRun = null;
        return false;
      }
      return true;
    }

    private bool TryStartNextBatchScenario()
    {
      var st = _scenarioBatchRun;
      if (st == null || st.CurrentIndex < 0 || st.CurrentIndex >= st.OrderedMembers.Count)
        return false;

      var m = st.OrderedMembers[st.CurrentIndex];
      ScenarioDocument loaded;
      try
      {
        loaded = ScenarioStorage.LoadScenario(m.ScenarioId);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Не удалось загрузить сценарий ID {m.ScenarioId}: {ex.Message}",
            "Группа сценариев", MessageBoxButton.OK, MessageBoxImage.Error);
        var failed = _scenarioBatchRun;
        _scenarioBatchRun = null;
        FinishScenarioBatchReport(failed, userAborted: true);
        return false;
      }

      var doc = ScenarioGroupDocument.ApplyMemberToScenario(loaded, m, st.GroupDefinition.RunPulseTimingCoefficient);
      if (!TryStartScenario(doc, st.ReportOutputFolder, null))
      {
        var failed = _scenarioBatchRun;
        _scenarioBatchRun = null;
        FinishScenarioBatchReport(failed, userAborted: true);
        return false;
      }
      return true;
    }

    private void FinishScenarioBatchReport(ScenarioBatchRunState state, bool userAborted)
    {
      if (state?.GroupDefinition == null)
        return;
      try
      {
        var folder = string.IsNullOrWhiteSpace(state.ReportOutputFolder)
            ? AppConfig.ScenarioReportsFolderPath
            : state.ReportOutputFolder.Trim();
        Directory.CreateDirectory(folder);
        var fname = $"scenario_group_{state.GroupDefinition.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
        var reportPath = Path.Combine(folder, fname);
        var html = ScenarioReportHtmlBuilder.BuildGroupBatchHtml(
            state.GroupDefinition,
            state.Completed,
            _influenceActionSystem,
            _perceptionImagesSystem);
        File.WriteAllText(reportPath, html, Encoding.UTF8);

        var msg = userAborted
            ? "Групповой прогон прерван или не завершён."
            : "Групповой прогон завершён.";
        msg += "\n\nОтчёт сохранён:\n" + reportPath + "\n\nОткрыть отчёт в браузере?";
        var result = MessageBox.Show(msg, "Группа сценариев",
            MessageBoxButton.YesNo, MessageBoxImage.Information);
        if (result == MessageBoxResult.Yes)
        {
          try
          {
            Process.Start(new ProcessStartInfo { FileName = reportPath, UseShellExecute = true });
          }
          catch (Exception ex)
          {
            MessageBox.Show(ex.Message, "Не удалось открыть отчёт", MessageBoxButton.OK, MessageBoxImage.Warning);
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Error(ex.Message);
        MessageBox.Show("Не удалось сохранить сводный отчёт группы: " + ex.Message,
            "Группа сценариев", MessageBoxButton.OK, MessageBoxImage.Warning);
      }
    }

    private void HandleBatchScenarioFinished(OperatorScenarioCompletedEventArgs e)
    {
      var st = _scenarioBatchRun;
      if (st == null)
        return;

      if (e.Document != null)
        st.Completed.Add(Tuple.Create(e.Document, e));

      if (!e.Success || e.AbortedByUser || e.AbortedByPulsationStop)
      {
        _scenarioBatchRun = null;
        FinishScenarioBatchReport(st, userAborted: true);
        return;
      }

      st.CurrentIndex++;
      if (st.CurrentIndex >= st.OrderedMembers.Count)
      {
        _scenarioBatchRun = null;
        FinishScenarioBatchReport(st, userAborted: false);
        return;
      }

      try
      {
        if (!TryStartNextBatchScenario())
        {
          /* состояние уже сброшено внутри TryStartNextBatchScenario */
        }
      }
      catch (Exception ex)
      {
        Logger.Error(ex.Message);
        _scenarioBatchRun = null;
        MessageBox.Show(ex.Message, "Группа сценариев", MessageBoxButton.OK, MessageBoxImage.Error);
        FinishScenarioBatchReport(st, userAborted: true);
      }
    }

    /// <summary>После сброса параметров в «норму»: ждать (время удержания состояний + 1) пульсов, затем старт раннера.</summary>
    private void BeginHomeostasisNormSettleWaitThenStartScenario(ScenarioDocument doc, string reportOutputFolder)
    {
      int coeff = doc?.Header?.RunPulseTimingCoefficient ?? 1;
      if (coeff != 1 && coeff != 5 && coeff != 10 && coeff != 20)
        coeff = 1;
      if (coeff > 1)
        GlobalTimer.SetPulseWallClockAcceleration(coeff, suppressAnimation: true);

      int totalPulses = Math.Max(1, AppConfig.DynamicTime + 1);
      int startPulse = GlobalTimer.GlobalPulsCount;
      int targetPulse = startPulse + totalPulses;

      Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
      {
        if (_scenarioRunProgressWindow == null)
        {
          _scenarioRunProgressWindow = new ScenarioRunProgressWindow
          {
            Owner = Application.Current?.MainWindow
          };
          _scenarioRunProgressWindow.Show();
        }
        _scenarioRunProgressWindow.SetStatus(
            $"Стабилизация после сброса в норму… пульс 0/{totalPulses}",
            compactFont: true);
      }));

      Action<int> handler = null;
      handler = pulseCount =>
      {
        if (!GlobalTimer.IsPulsationRunning)
        {
          GlobalTimer.OnPulseAfterGomeostasisBeforePsychic -= handler;
          GlobalTimer.ClearPulseWallClockAcceleration();
          Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
          {
            if (_scenarioRunProgressWindow != null)
            {
              try { _scenarioRunProgressWindow.Close(); } catch { /* ignore */ }
              _scenarioRunProgressWindow = null;
            }
            MessageBox.Show(
                "Пульсация остановлена во время паузы стабилизации гомеостаза.",
                "Запуск сценария",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
          }));
          return;
        }

        if (pulseCount < targetPulse)
        {
          int elapsed = pulseCount - startPulse;
          Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
          {
            if (_scenarioRunProgressWindow != null)
            {
              _scenarioRunProgressWindow.SetStatus(
                  $"Стабилизация после сброса в норму… пульс {elapsed}/{totalPulses}",
                  compactFont: true);
            }
          }));
          return;
        }

        GlobalTimer.OnPulseAfterGomeostasisBeforePsychic -= handler;
        Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
        {
          try
          {
            if (_scenarioRunProgressWindow != null)
              _scenarioRunProgressWindow.SetStatus("Запуск сценария…", compactFont: false);
            ApplyScenarioPultModesAndStartRunner(doc, reportOutputFolder);
          }
          catch (Exception ex)
          {
            GlobalTimer.ClearPulseWallClockAcceleration();
            if (_scenarioRunProgressWindow != null)
            {
              try { _scenarioRunProgressWindow.Close(); } catch { /* ignore */ }
              _scenarioRunProgressWindow = null;
            }
            RestorePultModesAfterScenarioIfNeeded();
            MessageBox.Show(ex.Message, "Запуск", MessageBoxButton.OK, MessageBoxImage.Error);
          }
        }));
      };

      GlobalTimer.OnPulseAfterGomeostasisBeforePsychic += handler;
    }

    private void ApplyScenarioPultModesAndStartRunner(ScenarioDocument doc, string reportOutputFolder)
    {
      int coeff = doc?.Header?.RunPulseTimingCoefficient ?? 1;
      if (coeff != 1 && coeff != 5 && coeff != 10 && coeff != 20)
        coeff = 1;
      GlobalTimer.SetPulseWallClockAcceleration(coeff, suppressAnimation: coeff > 1);

      _pendingScenarioReportFolder = reportOutputFolder;
      var pult = _agentViewModel.AgentPultViewModel;
      _savedObservationModeBeforeScenario = AppGlobalState.ObservationMode;
      _savedAuthoritativeRecordingBeforeScenario = pult.AuthoritativeMode;
      _scenarioPultModesSaved = true;
      pult.ObservationMode = doc.Header.ScenarioObservationMode;
      pult.AuthoritativeMode = doc.Header.ScenarioAuthoritativeRecording;

      _scenarioRunner.Start(doc, () => _agentViewModel?.AgentPultViewModel,
          () => _isidaContext.CancelWaitingPeriodAndResetMirror());
    }

    private void RestorePultModesAfterScenarioIfNeeded()
    {
      if (!_scenarioPultModesSaved || _agentViewModel?.AgentPultViewModel == null)
      {
        _scenarioPultModesSaved = false;
        return;
      }
      try
      {
        _agentViewModel.AgentPultViewModel.ObservationMode = _savedObservationModeBeforeScenario;
        _agentViewModel.AgentPultViewModel.AuthoritativeMode = _savedAuthoritativeRecordingBeforeScenario;
      }
      catch (Exception ex)
      {
        Logger.Error(ex.Message);
      }
      _scenarioPultModesSaved = false;
    }

    /// <param name="homeostasisNormSettlePending">Нужна пауза стабилизации после программного сброса параметров в «норму».</param>
    private bool TryApplyPreRunStageForScenario(ScenarioDocument doc, out bool homeostasisNormSettlePending)
    {
      homeostasisNormSettlePending = false;
      int target = doc.Header.PreRunTargetStage;
      bool wantStageChange = target >= 0 && target <= 5;
      if (!doc.Header.PreRunClearAgentData && !wantStageChange && !doc.Header.PreRunNormalHomeostasisState)
        return true;

      var agent = _gomeostas.GetAgentState();
      if (agent == null)
      {
        MessageBox.Show("Состояние агента недоступно.", "Запуск сценария", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
      }

      int current = agent.EvolutionStage;

      if (doc.Header.PreRunClearAgentData)
      {
        var clearRes = _gomeostas.ClearEvolutionStageDataForScenarioPreRun();
        if (!clearRes.Success)
        {
          MessageBox.Show(clearRes.Message ?? "Не удалось очистить данные стадии перед запуском сценария.",
              "Запуск сценария", MessageBoxButton.OK, MessageBoxImage.Warning);
          return false;
        }
      }

      bool saveAfterPreRun = false;
      if (wantStageChange && target != current)
      {
        bool force = target < current || target > current + 1;
        bool skipClear = !doc.Header.PreRunClearAgentData;
        var result = _gomeostas.SetEvolutionStage(target, force, skipClear);
        if (!result.Success)
        {
          MessageBox.Show(result.Message ?? "Не удалось перейти на выбранную стадию.",
              "Запуск сценария", MessageBoxButton.OK, MessageBoxImage.Warning);
          return false;
        }
        saveAfterPreRun = true;
      }

      if (doc.Header.PreRunNormalHomeostasisState)
      {
        try
        {
          _gomeostas.ApplySpeedOrientedNormalHomeostasisForScenarioPreRun();
        }
        catch (Exception ex)
        {
          MessageBox.Show(
              ex.Message,
              "Запуск сценария: начальное состояние Норма",
              MessageBoxButton.OK, MessageBoxImage.Warning);
          return false;
        }
        saveAfterPreRun = true;
        homeostasisNormSettlePending = true;
      }

      if (saveAfterPreRun)
        _gomeostas.SaveAgentProperties();

      return true;
    }

    private void OnScenarioRunFinished(object sender, OperatorScenarioCompletedEventArgs e)
    {
      GlobalTimer.ClearPulseWallClockAcceleration();
      RestorePultModesAfterScenarioIfNeeded();
      // SystemIdle: после FlushBufferedAgentRowToMemoryNow на том же пульсе записи ещё ставятся в MemoryLogManager
      // через BeginInvoke(Background); отчёт должен собраться позже очереди Background, иначе фантомное расхождение на последнем шаге.
      Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
      {
        try
        {
          if (_scenarioBatchRun != null)
          {
            HandleBatchScenarioFinished(e);
            return;
          }

          ScenarioLogComparisonSession.LastAnchorGlobalPulse = e.AnchorGlobalPulse;
          ScenarioLogComparisonSession.LastScenarioId = e.Document?.Header?.Id;

          if (e.Document == null)
            return;

          string reportPath = null;
          var folder = _pendingScenarioReportFolder;
          if (string.IsNullOrWhiteSpace(folder))
            folder = AppConfig.ScenarioReportsFolderPath;
          _pendingScenarioReportFolder = null;

          try
          {
            Directory.CreateDirectory(folder);
            var fname = $"scenario_{e.Document.Header?.Id ?? 0}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
            reportPath = Path.Combine(folder, fname);
            var html = ScenarioReportHtmlBuilder.BuildHtml(e.Document, e, _influenceActionSystem, _perceptionImagesSystem);
            File.WriteAllText(reportPath, html, Encoding.UTF8);
          }
          catch (Exception ex)
          {
            Logger.Error(ex.Message);
            MessageBox.Show("Отчёт не сохранён: " + ex.Message, "Сценарий", MessageBoxButton.OK, MessageBoxImage.Warning);
            reportPath = null;
          }

          var msg = e.Success
              ? "Сценарий завершён успешно."
              : (e.AbortedByUser ? "Сценарий остановлен пользователем."
                  : e.AbortedByPulsationStop ? "Сценарий прерван: остановлена пульсация."
                  : (!string.IsNullOrEmpty(e.ErrorMessage) ? "Ошибка: " + e.ErrorMessage : "Сценарий завершён."));

          if (e.ElapsedWallTime.TotalMilliseconds > 0 && e.ElapsedPulses > 0)
          {
            double pulsesPerSec = e.ElapsedPulses / e.ElapsedWallTime.TotalSeconds;
            msg += $"\n\nДиагностика: {e.ElapsedPulses} пульсов за {e.ElapsedWallTime.TotalSeconds:F1} сек" +
                   $" ({pulsesPerSec:F1} пульс/сек, фактическое ускорение ×{pulsesPerSec:F1}).";
          }

          var saved = reportPath != null && File.Exists(reportPath);
          if (saved)
            msg += "\n\nОтчёт сохранён:\n" + reportPath + "\n\nОткрыть отчёт в браузере?";
          else
            msg += "\n\nHTML-отчёт не был сохранён на диск.";

          var result = MessageBox.Show(msg, "Результат прогона сценария",
              saved ? MessageBoxButton.YesNo : MessageBoxButton.OK,
              MessageBoxImage.Information);

          if (result == MessageBoxResult.Yes && saved)
          {
            try
            {
              Process.Start(new ProcessStartInfo
              {
                FileName = reportPath,
                UseShellExecute = true
              });
            }
            catch (Exception ex)
            {
              MessageBox.Show(ex.Message, "Не удалось открыть отчёт", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
          }
        }
        catch (Exception ex)
        {
          Logger.Error(ex.Message);
        }
      }), DispatcherPriority.SystemIdle);
    }

    private void ShowAutomatizms()
    {
      var automatizmsView = new AutomatizmsView();
      var viewModel = new AutomatizmsViewModel(
          _gomeostas,
          _automatizmSystem,
          _automatizmTreeSystem,
          _actionsImagesSystem,
          _influenceActionsImagesSystem,
          _emotionsImageSystem,
          _sensorySystem,
          _influenceActionSystem,
          _actionsSystem,
          _verbalBrocaImages,
          _conditionedReflexToAutomatizm,
          _automatizmFileLoader,
          _stage2PrimitivesLoader);
      automatizmsView.DataContext = viewModel;
      CurrentContent = automatizmsView;
    }

    private void ShowAbout()
    {
      // Передаем метод OpenWebPage в конструктор AboutWindow
      var aboutWindow = new AboutWindow(OpenWebPage);
      aboutWindow.ShowDialog();
    }

    // Класс окна "О программе"
    private class AboutWindow : Window
    {
      private readonly Action<string> _openWebPageAction;

      public AboutWindow(Action<string> openWebPageAction)
      {
        _openWebPageAction = openWebPageAction ?? throw new ArgumentNullException(nameof(openWebPageAction));

        Title = "О программе";
        Width = 500;
        Height = 300;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.NoResize;
        Icon = GetDefaultIcon();

        // Закрытие по Esc
        PreviewKeyDown += (sender, e) =>
        {
          if (e.Key == Key.Escape)
          {
            Close();
            e.Handled = true;
          }
        };

        InitializeUI();
      }

      private void InitializeUI()
      {
        // Основной контейнер
        var mainGrid = new Grid();
        mainGrid.Margin = new Thickness(5);

        // Определяем строки
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }); // Заголовок
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }); // Версия
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }); // Разделитель
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Описание
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }); // Разделитель
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }); // Кнопки

        // Заголовок с переносом текста
        var titleTextBlock = new TextBlock
        {
          Text = IsidaEngine.ProjectName,
          FontSize = 18,
          FontWeight = FontWeights.Bold,
          TextWrapping = TextWrapping.Wrap,
          TextAlignment = TextAlignment.Center,
          HorizontalAlignment = HorizontalAlignment.Stretch,
          Margin = new Thickness(0, 0, 0, 5)
        };
        Grid.SetRow(titleTextBlock, 0);
        mainGrid.Children.Add(titleTextBlock);

        // Версия и дата сборки
        var versionTextBlock = new TextBlock
        {
          Text = $"Версия: {IsidaEngine.ProjectVersion} | Сборка: {IsidaEngine.BuildDate}",
          FontSize = 12,
          HorizontalAlignment = HorizontalAlignment.Center,
          Margin = new Thickness(0, 0, 0, 5)
        };
        Grid.SetRow(versionTextBlock, 1);
        mainGrid.Children.Add(versionTextBlock);

        // Первый разделитель
        var separator1 = new Separator
        {
          Margin = new Thickness(0, 5, 0, 5)
        };
        Grid.SetRow(separator1, 2);
        mainGrid.Children.Add(separator1);

        // Описание (ScrollViewer для длинного текста)
        var descriptionScrollViewer = new ScrollViewer
        {
          VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
          MaxHeight = 150
        };

        var descriptionText = IsidaEngine.ProjectDescription;
        var descriptionTextBlock = new TextBlock
        {
          Text = descriptionText,
          TextWrapping = TextWrapping.Wrap,
          Margin = new Thickness(5),
          TextAlignment = TextAlignment.Justify
        };

        descriptionScrollViewer.Content = descriptionTextBlock;
        Grid.SetRow(descriptionScrollViewer, 3);
        mainGrid.Children.Add(descriptionScrollViewer);

        // Второй разделитель
        var separator2 = new Separator
        {
          Margin = new Thickness(0, 5, 0, 5)
        };
        Grid.SetRow(separator2, 4);
        mainGrid.Children.Add(separator2);

        // Контейнер для кнопок
        var buttonStackPanel = new StackPanel
        {
          Orientation = Orientation.Horizontal,
          HorizontalAlignment = HorizontalAlignment.Center,
          Margin = new Thickness(0, 0, 0, 0)
        };

        // Кнопка "Подробнее"
        var detailsButton = new Button
        {
          Content = "Подробнее",
          Width = 100,
          Height = 30,
          Margin = new Thickness(5)
        };
        detailsButton.Click += (sender, e) => ShowDetailedInfo();
        buttonStackPanel.Children.Add(detailsButton);

        // Кнопка "Документация"
        var docsButton = new Button
        {
          Content = "Документация",
          Width = 110,
          Height = 30,
          Margin = new Thickness(5)
        };
        docsButton.Click += (sender, e) => _openWebPageAction?.Invoke(IsidaEngine.DocumentationUrl);
        buttonStackPanel.Children.Add(docsButton);

        // Кнопка "Закрыть"
        var closeButton = new Button
        {
          Content = "Закрыть",
          Width = 100,
          Height = 30,
          Margin = new Thickness(5),
          IsDefault = true
        };
        closeButton.Click += (sender, e) => Close();
        buttonStackPanel.Children.Add(closeButton);

        Grid.SetRow(buttonStackPanel, 5);
        mainGrid.Children.Add(buttonStackPanel);

        Content = mainGrid;
      }

      private void ShowDetailedInfo()
      {
        var detailedWindow = new Window
        {
          Title = "Подробная информация о проекте",
          Width = 650,
          Height = 430,
          WindowStartupLocation = WindowStartupLocation.CenterScreen,
          WindowStyle = WindowStyle.ToolWindow
        };

        detailedWindow.PreviewKeyDown += (sender, e) =>
        {
          if (e.Key == Key.Escape)
          {
            detailedWindow.Close();
            e.Handled = true;
          }
        };

        var scrollViewer = new ScrollViewer
        {
          VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
          HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var mainStack = new StackPanel
        {
          Margin = new Thickness(15)
        };

        // Название и версия
        mainStack.Children.Add(new TextBlock
        {
          Text = IsidaEngine.ProjectName,
          FontSize = 16,
          FontWeight = FontWeights.Bold,
          TextWrapping = TextWrapping.Wrap,
          Margin = new Thickness(0, 0, 0, 5)
        });

        mainStack.Children.Add(new TextBlock
        {
          Text = $"Версия: {IsidaEngine.ProjectVersion} | Сборка: {IsidaEngine.BuildDate}",
          FontSize = 12,
          Margin = new Thickness(0, 0, 0, 5)
        });

        mainStack.Children.Add(new Separator { Margin = new Thickness(0, 0, 0, 5) });

        // Полное описание
        var descriptionTextBlock = new TextBlock
        {
          Text = IsidaEngine.TheoreticalBasis,
          TextWrapping = TextWrapping.Wrap,
          TextAlignment = TextAlignment.Justify,
          Margin = new Thickness(0, 0, 0, 5)
        };
        mainStack.Children.Add(descriptionTextBlock);

        // Авторы
        if (IsidaEngine.ProjectAuthors != null && IsidaEngine.ProjectAuthors.Length > 0)
        {
          mainStack.Children.Add(new Separator { Margin = new Thickness(0, 5, 0, 5) });

          mainStack.Children.Add(new TextBlock
          {
            Text = "Авторы проекта:",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 5)
          });

          foreach (var author in IsidaEngine.ProjectAuthors)
          {
            mainStack.Children.Add(new TextBlock
            {
              Text = $"• {author}",
              TextWrapping = TextWrapping.Wrap,
              Margin = new Thickness(5, 0, 0, 5),
              FontSize = 11
            });
          }
        }

        // Документация
        mainStack.Children.Add(new Separator { Margin = new Thickness(0, 5, 0, 5) });

        mainStack.Children.Add(new TextBlock
        {
          Text = "Документация и ресурсы:",
          FontSize = 12,
          FontWeight = FontWeights.SemiBold,
          Margin = new Thickness(0, 0, 0, 5)
        });

        var hyperlink = new Hyperlink
        {
          NavigateUri = new Uri(IsidaEngine.DocumentationUrl)
        };
        hyperlink.Inlines.Add(IsidaEngine.DocumentationUrl);
        hyperlink.RequestNavigate += (sender, e) =>
        {
          try
          {
            _openWebPageAction?.Invoke(e.Uri.ToString());
            e.Handled = true;
          }
          catch (Exception ex)
          {
            MessageBox.Show(detailedWindow, $"Не удалось открыть ссылку:\n{ex.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
          }
        };

        var docsTextBlock = new TextBlock
        {
          TextWrapping = TextWrapping.Wrap,
          Margin = new Thickness(0, 0, 0, 5)
        };
        docsTextBlock.Inlines.Add(hyperlink);
        mainStack.Children.Add(docsTextBlock);

        // Кнопка закрытия
        var closeButton = new Button
        {
          Content = "Закрыть",
          Width = 80,
          HorizontalAlignment = HorizontalAlignment.Right,
          Margin = new Thickness(0, 5, 0, 0)
        };
        closeButton.Click += (sender, e) => detailedWindow.Close();
        mainStack.Children.Add(closeButton);

        scrollViewer.Content = mainStack;
        detailedWindow.Content = scrollViewer;
        detailedWindow.ShowDialog();
      }

      private ImageSource GetDefaultIcon()
      {
        try
        {
          var icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
          if (icon != null)
          {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                System.Windows.Int32Rect.Empty,
                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
          }
        }
        catch
        {
          // Игнорируем ошибки получения иконки
        }
        return null;
      }
    }

    /// <summary>
    /// Универсальный метод для открытия веб-страницы в браузере по умолчанию
    /// </summary>
    /// <param name="url">URL адрес веб-страницы</param>
    private void OpenWebPage(string url)
    {
      try
      {
        // Проверяем, что URL не пустой
        if (string.IsNullOrWhiteSpace(url))
        {
          MessageBox.Show("URL адрес не указан.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        // Убираем возможные пробелы
        url = url.Trim();

        // Добавляем протокол если отсутствует
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
          url = "https://" + url;
        }

        // Проверяем валидность URL
        if (Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult) &&
            (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
        {
          // Открываем URL в браузере по умолчанию
          Process.Start(new ProcessStartInfo
          {
            FileName = uriResult.ToString(),
            UseShellExecute = true
          });
        }
        else
        {
          MessageBox.Show("Некорректный URL адрес.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Не удалось открыть веб-страницу:\n{ex.Message}",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    /// <summary>
    /// Открывает страницу логов системы
    /// </summary>
    private void ShowLiveLogs()
    {
      var liveLogsView = new LiveLogsView();
      var viewModel = new LiveLogsViewModel(
          _gomeostas,
          _perceptionImagesSystem,
          _influenceActionSystem,
          _sensorySystem.VerbalChannel,
          _actionsSystem,
          _geneticReflexesSystem,
          _conditionedReflexesSystem,
          _automatizmSystem,
          _actionsImagesSystem);
      liveLogsView.DataContext = viewModel;
      CurrentContent = liveLogsView;
    }

    /// <summary>
    /// Открывает страницу логов стилей
    /// </summary>
    private void ShowStileLogs()
    {
      var stileLogsView = new StyleLogsView();
      var viewModel = new StyleLogsViewModel();
      stileLogsView.DataContext = viewModel;
      CurrentContent = stileLogsView;
    }

    /// <summary>
    /// Открывает страницу логов параметров
    /// </summary>
    private void ShowParametersLogs()
    {
      var parameterLogsView = new ParameterLogsView();
      var viewModel = new ParameterLogsViewModel();
      parameterLogsView.DataContext = viewModel;
      CurrentContent = parameterLogsView;
    }

    // Открыть страницу цепочек автоматизмов
    private void OpenAutomatizmChains()
    {
      var automatizmChainsView = new AutomatizmChainsView();
      var viewModel = new AutomatizmChainsViewModel(
        _automatizmSystem, 
        _automatizmChains, 
        _actionsImagesSystem,
        _actionsSystem,
        _sensorySystem);
      automatizmChainsView.DataContext = viewModel;
      CurrentContent = automatizmChainsView;
    }

    // Открыть страницу цепочек безусловных рефлексов
    private void OpenReflexChains()
    {
      var reflexChainsView = new ReflexChainsView();
      var viewModel = new ReflexChainsViewModel(
        _reflexChains,
        _geneticReflexesSystem,
        _actionsSystem);
      reflexChainsView.DataContext = viewModel;
      CurrentContent = reflexChainsView;
    }

    // Открыть страницу сенсоров
    private void OpenSensors()
    {
      var verbalTreesView = new VerbalTreesView();
      var verbalTreesViewModel = new VerbalTreesViewModel(_gomeostas, _sensorySystem.VerbalChannel);
      verbalTreesView.DataContext = verbalTreesViewModel;
      CurrentContent = verbalTreesView;
    }

    private void ShowExtInfluence()
    {
      var extInfluencesView = new ExterInalInfluencesView();
      var extInfluencesViewModel = new ExterInalInfluencesViewModel(_gomeostas, _influenceActionSystem);
      extInfluencesView.DataContext = extInfluencesViewModel;
      CurrentContent = extInfluencesView;
    }

    // Открыть страницу данных агента
    private void OpenAgent()
    {
      var agentView = new AgentView();
      _agentViewModel = new AgentViewModel(_gomeostas, () => _isidaContext.CancelWaitingPeriodAndResetMirror());
      agentView.DataContext = _agentViewModel;
      CurrentContent = agentView;
      UpdateAgentState();
    }

    private void ShowAgentProperties()
    {
      var view = new AgentPropertiesDialog(_gomeostas, () => OpenAgent());
      CurrentContent = view;
    }

    // Открыть страницу системных параметров
    private void ShowSystemParameters()
    {
      var systemParamsView = new SystemParametersView();
      var viewModel = new SystemParametersViewModel(_gomeostas);

      // Принудительно обновляем данные при открытии
      viewModel.RefreshParameters();

      systemParamsView.DataContext = viewModel;
      CurrentContent = systemParamsView;
    }

    // Открыть страницу стилей реагирования
    private void ShowBehaviorStyles()
    {
      var behaviorStylesView = new BehaviorStylesView();
      var viewModel = new BehaviorStylesViewModel(_gomeostas);
      behaviorStylesView.DataContext = viewModel;
      CurrentContent = behaviorStylesView;
    }

    // Открыть страницу адаптивных действий
    private void ShowAdaptiveActions()
    {
      var adaptiveActionsView = new AdaptiveActionsView();
      var viewModel = new AdaptiveActionsViewModel(_gomeostas, _actionsSystem);
      adaptiveActionsView.DataContext = viewModel;
      CurrentContent = adaptiveActionsView;
    }

    // Открыть страницу безусловных рефлексов
    private void ShowGeneticReflexes()
    {
      var geneticReflexesView = new GeneticReflexesView();
      string bootDataFolder = AppConfig.BootDataFolderPath ?? Path.Combine(
          Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
          "ISIDA", "BootData");
      var viewModel = new GeneticReflexesViewModel(
        _gomeostas,
        _geneticReflexesSystem,
        _actionsSystem,
        _influenceActionSystem,
        _reflexTree,
        _reflexChains,
        _isidaContext?.GeneticReflexFileLoader,
        bootDataFolder);
      geneticReflexesView.DataContext = viewModel;
      CurrentContent = geneticReflexesView;
    }

    // Открыть страницу условных рефлексов
    private void ShowConditionedReflexes()
    {
      var conditionedReflexesView = new ConditionedReflexesView();
      string bootDataFolder = AppConfig.BootDataFolderPath ?? Path.Combine(
          Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
          "ISIDA", "BootData");
      var viewModel = new ConditionedReflexesViewModel(
          _gomeostas,
          _conditionedReflexesSystem,
          _actionsSystem,
          _perceptionImagesSystem,
          _geneticReflexesSystem,
          _sensorySystem,
          bootDataFolder);
      conditionedReflexesView.DataContext = viewModel;
      CurrentContent = conditionedReflexesView;
    }

    // Метод-заглушка для отображения сообщения
    private void ShowStub(string menuTitle)
    {
      var stackPanel = new StackPanel
      {
        VerticalAlignment = VerticalAlignment.Center,
        HorizontalAlignment = HorizontalAlignment.Center
      };

      stackPanel.Children.Add(new TextBlock
      {
        Text = menuTitle,
        FontSize = 18,
        FontWeight = FontWeights.Bold,
        Margin = new Thickness(0, 0, 0, 5)
      });

      stackPanel.Children.Add(new TextBlock
      {
        Text = "Функционал в стадии разработки",
        FontSize = 14,
        Foreground = Brushes.Gray
      });

      CurrentContent = new ScrollViewer
      {
        Content = stackPanel,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto
      };
    }

    #endregion

    private bool _isExpanded = false;
    public bool IsExpanded
    {
      get => _isExpanded;
      set
      {
        if (_isExpanded != value)
        {
          _isExpanded = value;
          OnPropertyChanged(nameof(IsExpanded));
        }
      }
    }
    // Показать настройки проекта
    private void ShowProjectSettings()
    {
      var projectSettingsView = new ProjectSettingsView();
      var viewModel = new ProjectSettingsViewModel(_gomeostas);

      projectSettingsView.DataContext = viewModel;
      CurrentContent = projectSettingsView;
    }

    #region Пульсация

    private SolidColorBrush _pulseIndicatorColor = new SolidColorBrush(Colors.Gray);
    public SolidColorBrush PulseIndicatorColor
    {
      get => _pulseIndicatorColor;
      set
      {
        if (_pulseIndicatorColor != value)
        {
          _pulseIndicatorColor = value;
          OnPropertyChanged(nameof(PulseIndicatorColor));
        }
      }
    }

    private double _pulseBrightness;
    public double PulseBrightness
    {
      get => _pulseBrightness;
      set
      {
        if (Math.Abs(_pulseBrightness - value) > 0.01)
        {
          _pulseBrightness = value;
          OnPropertyChanged(nameof(PulseBrightness));
        }
      }
    }

    private bool _isPulseActive;
    public bool IsPulseActive
    {
      get => _isPulseActive;
      set
      {
        if (_isPulseActive != value)
        {
          _isPulseActive = value;
          OnPropertyChanged(nameof(IsPulseActive));
        }
      }
    }

    private bool _isPulsating;
    public bool IsPulsating
    {
      get => _isPulsating;
      set
      {
        if (_isPulsating != value)
        {
          _isPulsating = value;
          OnPropertyChanged(nameof(IsPulsating));
          OnPropertyChanged(nameof(PulseButtonText));
          OnPropertyChanged(nameof(PulseButtonColor));
        }
      }
    }

    public string PulseButtonText => IsAgentDead ? "АГЕНТ МЕРТВ" : (IsPulsating ? "СТОП" : "СТАРТ");
    public Brush PulseButtonColor => IsAgentDead ? Brushes.DarkRed : (IsPulsating ? Brushes.Red : Brushes.Green);
    public string PulseStatus => IsAgentDead ? "АГЕНТ МЕРТВ" : $"Пульсов: {GlobalTimer.GlobalPulsCount}";

    public string LifeTimeStatus
    {
      get
      {
        if (IsAgentDead)
          return "АГЕНТ МЕРТВ";

        var agentInfo = _gomeostas.GetAgentState();
        var ts = TimeSpan.FromSeconds(agentInfo.Lifetime);
        return $"Глобальное время жизни: {ts.Days / 365} лет, {(ts.Days % 365) / 30} месяцев, {ts:%d} дней, {ts:%h} часов, {ts:%m} минут, {ts:%s} секунд";
      }
    }

    public ICommand TogglePulseCommand { get; private set; }

    private void InitializePulseCommands()
    {
      TogglePulseCommand = new RelayCommand(_ =>
      {
        if (IsAgentDead)
        {
          MessageBox.Show("Невозможно управлять пульсацией мертвого агента",
            "Агент мертв",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

          return;
        }

        if (IsPulsating)
        {
          GlobalTimer.Stop();
          ResetWaitingPeriodDisplay();
        }
        else
        {
          try
          {
            GlobalTimer.Start();
          }
          catch (Exception ex)
          {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
          }
        }
        IsPulsating = !IsPulsating;
      }, _ => IsPulseButtonEnabled);
    }

    private void SetupPulseHandlers()
    {
      GlobalTimer.OnPulseBrightnessChanged += brightness =>
      {
        if (GlobalTimer.IsPulseAnimationSuppressed)
          return;
        Application.Current.Dispatcher.Invoke(() =>
        {
          try
          {
            byte minBrightness = 80;
            var color = Color.FromRgb(
                (byte)(minBrightness + (0xAD - minBrightness) * brightness),
                (byte)(minBrightness + (0xFF - minBrightness) * brightness),
                (byte)(minBrightness + (0x2E - minBrightness) * brightness));
            PulseIndicatorColor = new SolidColorBrush(color);
          }
          catch (Exception ex)
          {
            Logger.Error(ex.Message);
          }
        });
      };

      // Сценарий оператора: после дрейфа гомеостаза на пульсе, до ProcessPsychicPulse (см. GlobalTimer).
      GlobalTimer.OnPulseAfterGomeostasisBeforePsychic += pulseCount =>
      {
        Application.Current.Dispatcher.Invoke(() =>
        {
          try
          {
            _scenarioRunner.OnGlobalPulseBeforeProcessing(pulseCount);
          }
          catch (Exception ex)
          {
            Logger.Error(ex.Message);
          }
        });
      };

      GlobalTimer.OnPulseCompleted += pulseCount =>
      {
        Action action = () =>
        {
          try
          {
            OnPropertyChanged(nameof(PulseStatus));
            OnPropertyChanged(nameof(LifeTimeStatus));

            UpdateWaitingPeriodDisplay();

            if (!IsAgentDead)
              _researchLogger?.LogSystemState(pulseCount);

            _researchLogger?.FlushBufferedAgentRowToMemoryNow();
            _scenarioRunner.TryFinishAfterPulseCompleted();
          }
          catch (Exception ex)
          {
            Logger.Error(ex.Message);
          }
        };

        if (GlobalTimer.IsPulseAnimationSuppressed)
          Application.Current.Dispatcher.BeginInvoke(action);
        else
          Application.Current.Dispatcher.Invoke(action);
      };

      // ОБРАБОТЧИК ОШИБОК
      GlobalTimer.OnPulseError += errorMessage =>
      {
        Application.Current.Dispatcher.Invoke(() =>
        {
          // Безопасно останавливаем пульсацию
          SafeStopPulsation(errorMessage);
        });
      };
    }

    /// <summary>
    /// Обновить отображение периода ожидания оценки оператора
    /// </summary>
    private void UpdateWaitingPeriodDisplay()
    {
      try
      {
        bool isWaiting = AppGlobalState.WaitingForOperatorEvaluation;

        if (isWaiting)
        {
          // Обновляем обратный отсчет
          AppGlobalState.UpdateWaitingPeriodCountdown();

          var countdown = AppGlobalState.WaitingPeriodCountdown;

          if (countdown > 0)
          {
            // Обновляем MainViewModel
            ShowWaitingPeriod = true;
            WaitingPeriodText = $"Ожидание оценки: {countdown} пульсов";
            IsWaitingPeriodPulsating = true;

            // Также обновляем AgentViewModel если он существует
            if (_agentViewModel != null)
            {
              _agentViewModel.ShowWaitingPeriod = true;
              _agentViewModel.WaitingPeriodText = $"Период ожидания: {countdown}";
              _agentViewModel.IsWaitingPeriodPulsating = true;
            }
          }
          else
          {
            // Время истекло
            ResetWaitingPeriodDisplay();
          }
        }
        else if (ShowWaitingPeriod)
        {
          // Сбрасываем отображение если ожидание завершено
          ResetWaitingPeriodDisplay();
        }
      }
      catch (Exception ex)
      {
        Logger.Error(ex.Message);
        ResetWaitingPeriodDisplay();
      }
    }

    /// <summary>
    /// Сбросить отображение периода ожидания
    /// </summary>
    private void ResetWaitingPeriodDisplay()
    {
      ShowWaitingPeriod = false;
      IsWaitingPeriodPulsating = false;

      if (_agentViewModel != null)
      {
        _agentViewModel.ShowWaitingPeriod = false;
        _agentViewModel.IsWaitingPeriodPulsating = false;
      }
    }

    /// <summary>
    /// Принудительно отменить период ожидания оценки оператора
    /// </summary>
    private void CancelWaitingPeriod()
    {
      try
      {
        if (AppGlobalState.WaitingForOperatorEvaluation)
        {
          _isidaContext.CancelWaitingPeriodAndResetMirror();
          ShowWaitingPeriod = false;
          IsWaitingPeriodPulsating = false;

          Logger.Info("Период ожидания оценки оператора отменен пользователем");
        }
      }
      catch (Exception ex)
      {
        Logger.Error(ex.Message);
        MessageBox.Show($"Ошибка при отмене периода ожидания: {ex.Message}",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    /// <summary>
    /// Безопасная остановка пульсации
    /// </summary>
    private void SafeStopPulsation(string reason = "Неизвестная причина")
    {
      if (IsPulsating)
      {
        try
        {
          IsPulsating = false;

          OnPropertyChanged(nameof(IsPulsating));
          OnPropertyChanged(nameof(PulseButtonText));
          OnPropertyChanged(nameof(PulseButtonColor));
          OnPropertyChanged(nameof(PulseStatus));

          // Сбрасываем отображение периода ожидания
          ResetWaitingPeriodDisplay();

          // Обновляем состояние агента
          UpdateAgentState();

          if (IsAgentDead)
          {
            _agentViewModel.IsAgentDead = IsAgentDead;
            _agentViewModel.HeaderBackground = Brushes.Black;
            _agentViewModel.TextForeground = Brushes.Black;

            MessageBox.Show($"Пульсация остановлена - агент мертв\nПричина: {reason}",
              "Агент мертв",
              MessageBoxButton.OK,
              MessageBoxImage.Warning);
          }
          else if (reason.Contains("Критическая ошибка") || reason.Contains("Ошибка получения состояния"))
          {
            MessageBox.Show($"Пульсация остановлена:\n{reason}",
                "Критическая ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
          }
          Logger.Warning($"Пульсация остановлена: {reason}");
        }
        catch (Exception ex)
        {
          Logger.Error(ex.Message);

          // Принудительно устанавливаем состояние
          IsPulsating = false;
          OnPropertyChanged(nameof(IsPulsating));
          OnPropertyChanged(nameof(PulseButtonText));
        }
      }
    }

    #endregion

    #region Свойства периода ожидания оценки оператора

    private bool _showWaitingPeriod = false;
    public bool ShowWaitingPeriod
    {
      get => _showWaitingPeriod;
      set
      {
        if (_showWaitingPeriod != value)
        {
          _showWaitingPeriod = value;
          OnPropertyChanged(nameof(ShowWaitingPeriod));
        }
      }
    }

    private string _waitingPeriodText = "";
    public string WaitingPeriodText
    {
      get => _waitingPeriodText;
      set
      {
        if (_waitingPeriodText != value)
        {
          _waitingPeriodText = value;
          OnPropertyChanged(nameof(WaitingPeriodText));
        }
      }
    }

    private bool _isWaitingPeriodPulsating = false;
    public bool IsWaitingPeriodPulsating
    {
      get => _isWaitingPeriodPulsating;
      set
      {
        if (_isWaitingPeriodPulsating != value)
        {
          _isWaitingPeriodPulsating = value;
          OnPropertyChanged(nameof(IsWaitingPeriodPulsating));
        }
      }
    }

    private ICommand _cancelWaitingPeriodCommand;
    public ICommand CancelWaitingPeriodCommand =>
        _cancelWaitingPeriodCommand ?? (_cancelWaitingPeriodCommand = new RelayCommand(_ => CancelWaitingPeriod()));

    #endregion

    private sealed class ScenarioBatchRunState
    {
      public ScenarioGroupDocument GroupDefinition { get; set; }
      public List<ScenarioGroupMemberRow> OrderedMembers { get; set; }
      public int CurrentIndex { get; set; }
      public string ReportOutputFolder { get; set; }
      public List<Tuple<ScenarioDocument, OperatorScenarioCompletedEventArgs>> Completed { get; } =
          new List<Tuple<ScenarioDocument, OperatorScenarioCompletedEventArgs>>();
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}