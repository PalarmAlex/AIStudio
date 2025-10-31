﻿using AIStudio.Common;
using AIStudio.Pages;
using AIStudio.Pages.Reflexes;
using AIStudio.ViewModels;
using ISIDA.Actions;
using ISIDA.Common;
using ISIDA.Gomeostas;
using ISIDA.Reflexes;
using ISIDA.Sensors;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;

namespace AIStudio
{
  public class MainViewModel : INotifyPropertyChanged
  {
    #region Объявление констант и переменных

    private readonly GomeostasSystem _gomeostas;
    private readonly SensorySystem _sensorySystem;
    private readonly AdaptiveActionsSystem _actionsSystem;
    private readonly InfluenceActionSystem _influenceActionSystem;
    private readonly GeneticReflexesSystem _geneticReflexesSystem;
    private readonly ConditionedReflexesSystem _conditionedReflexesSystem;
    private readonly PerceptionImagesSystem _perceptionImagesSystem;
    private readonly ReflexesActivator _reflexesActivator;
    private readonly ReflexTreeSystem _reflexTree;
    private readonly ReflexExecutionService _reflexExecution;
    private readonly ResearchLogger _researchLogger;
    public event PropertyChangedEventHandler PropertyChanged;

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
      try
      {
        // Инициализация гомеостаза
        GomeostasSystem.InitializeInstance(AppConfig.DataGomeostasFolderPath, AppConfig.DataGomeostasTemplateFolderPath);
        _gomeostas = GomeostasSystem.Instance;

        // Инициализация первичных адаптивных действий
        AdaptiveActionsSystem.InitializeInstance(_gomeostas, AppConfig.DataActionsFolderPath, AppConfig.DataActionsTemplateFolderPath);
        _actionsSystem = AdaptiveActionsSystem.Instance;

        // Инициализация внешних действий
        InfluenceActionSystem.InitializeInstance(_gomeostas, AppConfig.DataActionsFolderPath, AppConfig.DataActionsTemplateFolderPath);
        _influenceActionSystem = InfluenceActionSystem.Instance;

        // Инициализация сенсорной системы
        SensorySystem.InitializeInstance(AppConfig.SensorsFolderPath, AppConfig.SensorsTemplateFolderPath);
        _sensorySystem = SensorySystem.Instance;

        // Инициализация безусловных рефлексов
        GeneticReflexesSystem.InitializeInstance(_gomeostas, AppConfig.ReflexesFolderPath, AppConfig.ReflexesTemplateFolderPath);
        _geneticReflexesSystem = GeneticReflexesSystem.Instance;

        // Инициализация образов рефлексов
        PerceptionImagesSystem.InitializeInstance(_gomeostas, _geneticReflexesSystem);
        _perceptionImagesSystem = PerceptionImagesSystem.Instance;
        _gomeostas.SetPerceptionImagesSystem(_perceptionImagesSystem);
        _influenceActionSystem.SetPerceptionImagesSystem(_perceptionImagesSystem);

        // Инициализация условных рефлексов
        ConditionedReflexesSystem.InitializeInstance(_gomeostas, _geneticReflexesSystem, _perceptionImagesSystem);
        _conditionedReflexesSystem = ConditionedReflexesSystem.Instance;

        // Инициализация дерева рефлексов
        ReflexTreeSystem.InitializeInstance(_geneticReflexesSystem, _perceptionImagesSystem);
        _reflexTree = ReflexTreeSystem.Instance;

        // Инициализация сервиса запуска рефлексов
        ReflexExecutionService.InitializeInstance(_actionsSystem, _influenceActionSystem);
        _reflexExecution = ReflexExecutionService.Instance;

        // Инициализация активатора рефлексов
        ReflexesActivator.InitializeInstance(_gomeostas, _geneticReflexesSystem, _conditionedReflexesSystem, _influenceActionSystem, _reflexTree, _reflexExecution, _actionsSystem);
        _reflexesActivator = ReflexesActivator.Instance;

        _researchLogger = new ResearchLogger(
            _gomeostas,
            _perceptionImagesSystem,
            _reflexesActivator,
            clearOnStart: AppConfig.LogEnabled,
            enabled: AppConfig.LogEnabled
        );
        ResearchLogger.SetMemoryLogWriter(MemoryLogManager.Instance);
        GlobalTimer.InitializeSystems(_gomeostas,_actionsSystem, _reflexesActivator);

        _gomeostas.DefaultStileId = AppConfig.DefaultStileId;
        _gomeostas.CompareLevel = AppConfig.CompareLevel;
        _gomeostas.DifSensorPar = AppConfig.DifSensorPar;
        _gomeostas.DynamicTime = AppConfig.DynamicTime;
        _gomeostas.DefaultBaseThreshold = AppConfig.DefaultBaseThreshold;
        _gomeostas.DefaultKCompetition = AppConfig.DefaultKCompetition;
        _actionsSystem.DefaultAdaptiveActionId = AppConfig.DefaultAdaptiveActionId;
        _sensorySystem.VerbalRecognitionThreshold = AppConfig.RecognitionThreshold;
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Ошибка инициализации систем: {ex.Message}");
        MessageBox.Show("Ошибка инициализации", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      PulseIndicatorColor = new SolidColorBrush(Color.FromRgb(80, 80, 80));

      InitializePulseCommands();
      SetupPulseHandlers();

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
            StopPulsationDueToDeath();
        }
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Ошибка получения состояния агента: {ex.Message}");
      }
    }

    private void StopPulsationDueToDeath()
    {
      if (IsPulsating)
      {
        GlobalTimer.Stop();
        IsPulsating = false;

        // Показываем сообщение о остановке пульсации
        MessageBox.Show("Пульсация автоматически остановлена - агент мертв",
            "Агент мертв",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

        Debug.WriteLine("Пульсация остановлена из-за смерти агента");
      }
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
          case "5": // Условные рефлексы
            ShowConditionedReflexes();
            break;
          case "6": // Дерево рефлексов
            ShowStub("Дерево рефлексов");
            break;
          case "8": // Таблица Автоматизмов
            ShowStub("Таблица Автоматизмов");
            break;
          case "9": // Дерево Автоматизмов
            ShowStub("Дерево Автоматизмов");
            break;
          case "10": // Цепочки Автоматизмов
            ShowStub("Цепочки Автоматизмов");
            break;
          case "11": // Дерево эпизодической памяти
            ShowStub("Дерево эпизодической памяти");
            break;
          case "12": // Дерево ситуации
            ShowStub("Дерево ситуации");
            break;
          case "13": // Дерево проблем
            ShowStub("Дерево проблем");
            break;
          case "14": // Моторные правила реагирования
            ShowStub("Моторные правила реагирования");
            break;
          case "15": // Ментальные правила реагирования
            ShowStub("Ментальные правила реагирования");
            break;
          case "16": // Циклы осмысления
            ShowStub("Циклы осмысления");
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
          case "33": // Агент
            OpenAgent();
            break;
          case "34":  // Живые логи системы
            ShowLiveLogs();
            break;
          default:
            ShowStub($"Меню {menuItem}");
            break;
        }
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
    /// Открывает страницу живых логов системы
    /// </summary>
    private void ShowLiveLogs()
    {
      var liveLogsView = new LiveLogsView();
      var viewModel = new LiveLogsViewModel(
          _gomeostas,
          _perceptionImagesSystem,
          _influenceActionSystem,
          _sensorySystem.VerbalChannel);
      liveLogsView.DataContext = viewModel;
      CurrentContent = liveLogsView;
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
      var agentViewModel = new AgentViewModel(_gomeostas);
      agentView.DataContext = agentViewModel;
      CurrentContent = agentView;
      UpdateAgentState();
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
      var viewModel = new GeneticReflexesViewModel(_gomeostas, _geneticReflexesSystem, _actionsSystem, _influenceActionSystem);
      geneticReflexesView.DataContext = viewModel;
      CurrentContent = geneticReflexesView;
    }

    // Открыть страницу условных рефлексов
    private void ShowConditionedReflexes()
    {
      var conditionedReflexesView = new ConditionedReflexesView();
      var viewModel = new ConditionedReflexesViewModel(
          _gomeostas,
          _conditionedReflexesSystem,
          _actionsSystem,
          _perceptionImagesSystem);
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
        Margin = new Thickness(0, 0, 0, 10)
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
        Application.Current.Dispatcher.Invoke(() =>
        {
          byte minBrightness = 80; // Минимальный уровень "серого" (из 255)

          var color = Color.FromRgb(
              (byte)(minBrightness + (0xAD - minBrightness) * brightness),
              (byte)(minBrightness + (0xFF - minBrightness) * brightness),
              (byte)(minBrightness + (0x2E - minBrightness) * brightness));

          PulseIndicatorColor = new SolidColorBrush(color);

          if (brightness >= 0.99)
          {
            _researchLogger?.LogSystemState(GlobalTimer.GlobalPulsCount);
            OnPropertyChanged(nameof(PulseStatus));
            OnPropertyChanged(nameof(LifeTimeStatus));
            UpdateAgentState(); // Обновляем состояние агента после каждого пульса

            if (IsAgentDead && IsPulsating)
              StopPulsationDueToDeath();
          }
        });
      };
    }

    #endregion

    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}