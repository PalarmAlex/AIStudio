using AIStudio.Common;
using ISIDA.Common;
using ISIDA.Niche;
using ISIDA.Scenarios;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace AIStudio.ViewModels
{
  /// <summary>
  /// Модель представления панели триады Creature↔Niche (этап 6.1).
  /// </summary>
  public sealed class TriadViewModel : INotifyPropertyChanged, IDisposable
  {
    /// <inheritdoc />
    public event PropertyChangedEventHandler PropertyChanged;

    private readonly IsidaContext _context;
    private readonly string _logsFolder;
    private readonly string _environmentFolder;
    private readonly DispatcherTimer _refreshTimer;
    private bool _disposed;
    private TriadPhase _selectedPhase;
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Создаёт ViewModel панели триады.
    /// </summary>
    /// <param name="context">Контекст ISIDA.</param>
    /// <param name="logsFolder">Каталог логов проекта.</param>
    /// <param name="environmentFolder">Каталог Environment.</param>
    public TriadViewModel(IsidaContext context, string logsFolder, string environmentFolder)
    {
      _context = context;
      _logsFolder = logsFolder ?? string.Empty;
      _environmentFolder = environmentFolder ?? TriadProjectPaths.GetEnvironmentFolder();

      PhaseOptions = new ObservableCollection<TriadPhaseOption>
      {
        new TriadPhaseOption(TriadPhase.A, "Фаза A — Bootstrap", "Operator-centric AOE; прямое влияние на Creature."),
        new TriadPhaseOption(TriadPhase.B, "Фаза B — Ритуал + coupling", "Первичный AOE от Niche; Operator — вторичный канал."),
        new TriadPhaseOption(TriadPhase.C, "Фаза C — Только Niche", "На стадии 4+ прямой пульт на Creature заблокирован; Operator→Niche.")
      };
      FilteredPhaseOptions = new ObservableCollection<TriadPhaseOption>();

      NicheParameters = new ObservableCollection<NicheParamRow>();
      NicheParamDefRows = new ObservableCollection<NicheParamDefRow>();
      ActionCouplingRows = new ObservableCollection<CouplingRow>();
      NicheMappingRows = new ObservableCollection<NicheMappingRow>();
      OperatorCouplingRows = new ObservableCollection<OperatorCouplingRow>();
      ContourProbeRows = new ObservableCollection<ContourProbeRow>();
      RoleProfileOptions = new ObservableCollection<string> { "niche_minimal", "niche_reactive" };

      ApplyPhaseCommand = new RelayCommand(_ => ApplyPhase(), _ => CanEditConfig);
      ReloadConfigCommand = new RelayCommand(_ => ReloadConfig(), _ => CanEditConfig);
      SaveCouplingCommand = new RelayCommand(_ => SaveCoupling(), _ => CanEditConfig);
      OpenEnvironmentFolderCommand = new RelayCommand(_ => OpenEnvironmentFolder());
      RecordRunStartCommand = new RelayCommand(_ => RecordRunStart(), _ => Bridge != null && Bridge.IsActive);
      ResetNicheSoftCommand = new RelayCommand(_ => ResetDyad(DyadResetType.NicheSoft), _ => CanReset);
      ResetCreatureSoftCommand = new RelayCommand(_ => ResetDyad(DyadResetType.CreatureSoft), _ => CanReset);
      ResetDyadHardCommand = new RelayCommand(_ => ResetDyad(DyadResetType.DyadHard), _ => CanReset);
      ResetCalibrationCommand = new RelayCommand(_ => ResetDyad(DyadResetType.Calibration), _ => CanReset);
      OpenDyadLogCommand = new RelayCommand(_ => OpenDyadLogFile());
      OpenRunManifestCommand = new RelayCommand(_ => OpenRunManifestFile());
      InstallValidationScenariosCommand = new RelayCommand(_ => InstallValidationScenarios());
      CheckTriadMetricsCommand = new RelayCommand(_ => CheckLastTriadMetrics());

      _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
      _refreshTimer.Tick += (s, e) => RefreshLiveState();
      _refreshTimer.Start();

      ReloadConfig();
    }

    /// <summary>Варианты фазы эксперимента.</summary>
    public ObservableCollection<TriadPhaseOption> PhaseOptions { get; }

    /// <summary>Фазы, допустимые для текущей стадии симбионта (§4.1).</summary>
    public ObservableCollection<TriadPhaseOption> FilteredPhaseOptions { get; }

    /// <summary>Текущая стадия эволюции симбионта.</summary>
    public int AgentEvolutionStage => AppGlobalState.EvolutionStage;

    /// <summary>Текущие параметры Niche (live-снимок движка).</summary>
    public ObservableCollection<NicheParamRow> NicheParameters { get; }

    /// <summary>Параметры Niche из niche_params.dat (редактируемые).</summary>
    public ObservableCollection<NicheParamDefRow> NicheParamDefRows { get; }

    /// <summary>Coupling action→Niche.</summary>
    public ObservableCollection<CouplingRow> ActionCouplingRows { get; }

    /// <summary>Mapping Niche→Creature.</summary>
    public ObservableCollection<NicheMappingRow> NicheMappingRows { get; }

    /// <summary>Coupling Operator→Niche.</summary>
    public ObservableCollection<OperatorCouplingRow> OperatorCouplingRows { get; }

    /// <summary>Coupling contour probe→Niche.</summary>
    public ObservableCollection<ContourProbeRow> ContourProbeRows { get; }

    /// <summary>Доступные RoleProfile Niche.</summary>
    public ObservableCollection<string> RoleProfileOptions { get; }

    /// <summary>Последние строки лога диады.</summary>
    public string DyadLogTail { get; private set; } = "Лог диады пуст или файл не найден.";

    /// <summary>Выбранная фаза в UI.</summary>
    public TriadPhase SelectedPhase
    {
      get => _selectedPhase;
      set
      {
        if (_selectedPhase == value)
          return;
        _selectedPhase = value;
        OnPropertyChanged(nameof(SelectedPhase));
        OnPropertyChanged(nameof(SelectedPhaseDescription));
        OnPropertyChanged(nameof(PhaseRestrictionHint));
      }
    }

    /// <summary>Описание выбранной фазы.</summary>
    public string SelectedPhaseDescription
    {
      get
      {
        foreach (var opt in PhaseOptions)
        {
          if (opt.Phase == SelectedPhase)
            return opt.Description;
        }
        return string.Empty;
      }
    }

    /// <summary>Триада активна.</summary>
    public bool IsTriadActive => Bridge != null && Bridge.IsActive;

    /// <summary>Триада не настроена.</summary>
    public bool IsTriadInactive => !IsTriadActive;

    /// <summary>experiment_run_id.</summary>
    public string ExperimentRunId => Bridge?.ExperimentRunId ?? "—";

    /// <summary>Версия coupling mapping.</summary>
    public int CouplingMappingVersion => Bridge?.Config?.CouplingMappingVersion ?? 0;

    /// <summary>RoleProfile Niche (активный в движке).</summary>
    public string NicheRoleProfileId => Bridge?.NicheEngine?.RoleProfile?.ProfileId ?? "—";

    /// <summary>ContourId (активный в движке).</summary>
    public string ContourId => Bridge?.Config?.ContourId ?? ContourIdSetting ?? "—";

    /// <summary>Последний EnvironmentMetricProbeKey, применённый контуром.</summary>
    public string LastContourProbeKey => Bridge?.LastContourProbeKey ?? string.Empty;

    /// <summary>Dim последнего InputSnapshot контура.</summary>
    public int LastContourInputDim => Bridge?.LastContourInputDim ?? 0;

    /// <summary>Подсказка по контуру probe (§6.8).</summary>
    public string ContourProbeHint
    {
      get
      {
        if (Bridge == null || !Bridge.IsActive)
          return string.Empty;
        if (!UseProbeContour && !(Bridge.Config?.UseProbeContour ?? false))
          return "UseProbeContour выкл — EnvironmentMetricProbeKey не влияет на Niche. Включите на вкладке «Движок & AOE».";
        if (!string.IsNullOrWhiteSpace(LastContourProbeKey))
          return "Последний contour probe: «" + LastContourProbeKey + "» (dim=" + LastContourInputDim + ") → Niche.";
        return "Probe contour активен. Назначьте EnvironmentMetricProbeKey воздействию и строку в contour_probes.dat.";
      }
    }

    /// <summary>Путь к triad_run_manifest.json.</summary>
    public string RunManifestPath =>
        string.IsNullOrWhiteSpace(_logsFolder)
            ? string.Empty
            : Path.Combine(_logsFolder, TriadRunManifestLogger.ManifestFileName);

    /// <summary>Подсказка по документации прогона (§6.5).</summary>
    public string RunManifestHint
    {
      get
      {
        if (string.IsNullOrWhiteSpace(_logsFolder))
          return "Каталог логов не задан — манифест прогона недоступен.";
        if (TriadRunManifestLogger.TryReadCurrent(_logsFolder, out TriadRunManifest manifest))
          return "Манифест: run=" + manifest.ExperimentRunId + ", mapping v" + manifest.CouplingMappingVersion
              + " (" + TriadRunManifestLogger.ManifestFileName + ").";
        return "Манифест прогона: " + TriadRunManifestLogger.ManifestFileName
            + " (создаётся при Init Niche / старте движка).";
      }
    }

    private bool _incrementMappingVersionOnSave = true;
    private string _contourId = "static_mvp";
    private bool _spontaneousDriftEnabled;
    private int _editableCouplingMappingVersion = 1;
    private int _aoeBaselineN = 20;
    private float _aoeThreshold = 0.5f;
    private int _aoeHorizonK = 3;
    private int _aoeEvalWindow = 30;
    private bool _useFullNicheEngine;
    private string _nicheRoleProfileId = "niche_minimal";
    private bool _useProbeContour;

    /// <summary>ContourId (triad_config.dat).</summary>
    public string ContourIdSetting
    {
      get => _contourId;
      set { if (_contourId == value) return; _contourId = value ?? string.Empty; OnPropertyChanged(nameof(ContourIdSetting)); }
    }

    /// <summary>Спонтанный дрейф Niche.</summary>
    public bool SpontaneousDriftEnabled
    {
      get => _spontaneousDriftEnabled;
      set { if (_spontaneousDriftEnabled == value) return; _spontaneousDriftEnabled = value; OnPropertyChanged(nameof(SpontaneousDriftEnabled)); }
    }

    /// <summary>Редактируемая версия coupling mapping.</summary>
    public int EditableCouplingMappingVersion
    {
      get => _editableCouplingMappingVersion;
      set { if (_editableCouplingMappingVersion == value) return; _editableCouplingMappingVersion = value; OnPropertyChanged(nameof(EditableCouplingMappingVersion)); }
    }

    /// <summary>AOE BaselineN.</summary>
    public int AoeBaselineN
    {
      get => _aoeBaselineN;
      set { if (_aoeBaselineN == value) return; _aoeBaselineN = value; OnPropertyChanged(nameof(AoeBaselineN)); }
    }

    /// <summary>AOE ResponseThreshold.</summary>
    public float AoeThreshold
    {
      get => _aoeThreshold;
      set { if (Math.Abs(_aoeThreshold - value) < 0.0001f) return; _aoeThreshold = value; OnPropertyChanged(nameof(AoeThreshold)); }
    }

    /// <summary>AOE CorrelationHorizonK.</summary>
    public int AoeHorizonK
    {
      get => _aoeHorizonK;
      set { if (_aoeHorizonK == value) return; _aoeHorizonK = value; OnPropertyChanged(nameof(AoeHorizonK)); }
    }

    /// <summary>AOE EvalWindowPulses.</summary>
    public int AoeEvalWindow
    {
      get => _aoeEvalWindow;
      set { if (_aoeEvalWindow == value) return; _aoeEvalWindow = value; OnPropertyChanged(nameof(AoeEvalWindow)); }
    }

    /// <summary>UseFullNicheEngine.</summary>
    public bool UseFullNicheEngine
    {
      get => _useFullNicheEngine;
      set { if (_useFullNicheEngine == value) return; _useFullNicheEngine = value; OnPropertyChanged(nameof(UseFullNicheEngine)); }
    }

    /// <summary>RoleProfileId для Niche.</summary>
    public string NicheRoleProfileIdSetting
    {
      get => _nicheRoleProfileId;
      set { if (_nicheRoleProfileId == value) return; _nicheRoleProfileId = value ?? string.Empty; OnPropertyChanged(nameof(NicheRoleProfileIdSetting)); }
    }

    /// <summary>UseProbeContour.</summary>
    public bool UseProbeContour
    {
      get => _useProbeContour;
      set { if (_useProbeContour == value) return; _useProbeContour = value; OnPropertyChanged(nameof(UseProbeContour)); }
    }

    /// <summary>Увеличить coupling_mapping_version при сохранении.</summary>
    public bool IncrementMappingVersionOnSave
    {
      get => _incrementMappingVersionOnSave;
      set { if (_incrementMappingVersionOnSave == value) return; _incrementMappingVersionOnSave = value; OnPropertyChanged(nameof(IncrementMappingVersionOnSave)); }
    }

    /// <summary>Таблицы coupling только для чтения при работающей пульсации.</summary>
    public bool IsCouplingReadOnly => !CanEditConfig;

    /// <summary>Ожидание отклика Niche (AOE).</summary>
    public bool WaitingForNicheResponse =>
        _context?.TriadOrchestrator?.InstanceState?.WaitingForNicheResponse == true
        || AppGlobalState.WaitingForNicheResponse;

    /// <summary>Подсказка об ограничениях фазы по стадии и режиму C.</summary>
    public string PhaseRestrictionHint
    {
      get
      {
        var lines = new List<string>
        {
          "Допустимые фазы: " + TriadPhaseStagePolicy.FormatAllowedRange(AgentEvolutionStage) + " (задано движком)."
        };

        if (Bridge != null && Bridge.IsOperatorCreatureInfluenceBlocked)
          lines.Add("Фаза C, стадия 4+: прямое влияние оператора на Creature заблокировано — используйте Operator→Niche или probe.");
        else if (SelectedPhase == TriadPhase.C && AgentEvolutionStage < 4)
          lines.Add("Фаза C активна в конфиге; блок пульта на Creature включится при переходе на стадию 4+.");

        return string.Join(" ", lines);
      }
    }

    /// <summary>Каталог Environment.</summary>
    public string EnvironmentFolder => _environmentFolder;

    /// <summary>Статус последней операции.</summary>
    public string StatusMessage
    {
      get => _statusMessage;
      private set
      {
        _statusMessage = value ?? string.Empty;
        OnPropertyChanged(nameof(StatusMessage));
      }
    }

    /// <summary>Можно редактировать конфиг (пульс выключен).</summary>
    public bool CanEditConfig => !GlobalTimer.IsPulsationRunning;

    /// <summary>Можно выполнить сброс диады.</summary>
    public bool CanReset => Bridge != null && Bridge.IsActive && CanEditConfig;

    /// <summary>Применить выбранную фазу.</summary>
    public ICommand ApplyPhaseCommand { get; }

    /// <summary>Перезагрузить конфигурацию из Environment.</summary>
    public ICommand ReloadConfigCommand { get; }

    /// <summary>Сохранить coupling и настройки в Environment.</summary>
    public ICommand SaveCouplingCommand { get; }

    /// <summary>Открыть каталог Environment.</summary>
    public ICommand OpenEnvironmentFolderCommand { get; }

    /// <summary>Зафиксировать NicheInitSnapshot.</summary>
    public ICommand RecordRunStartCommand { get; }

    /// <summary>Сброс NicheSoft.</summary>
    public ICommand ResetNicheSoftCommand { get; }

    /// <summary>Сброс CreatureSoft.</summary>
    public ICommand ResetCreatureSoftCommand { get; }

    /// <summary>Сброс DyadHard.</summary>
    public ICommand ResetDyadHardCommand { get; }

    /// <summary>Калибровка Niche.</summary>
    public ICommand ResetCalibrationCommand { get; }

    /// <summary>Открыть AgentLogs_Dyad.jsonl.</summary>
    public ICommand OpenDyadLogCommand { get; }

    /// <summary>Открыть triad_run_manifest.json в каталоге логов.</summary>
    public ICommand OpenRunManifestCommand { get; }

    /// <summary>Установить сценарии валидации §6.14.</summary>
    public ICommand InstallValidationScenariosCommand { get; }

    /// <summary>Проверить метрики §13.3 по последнему прогону [Triad …].</summary>
    public ICommand CheckTriadMetricsCommand { get; }

    /// <summary>Подсказка по сценариям валидации.</summary>
    public string ValidationScenariosHint =>
        TriadValidationScenarioBootstrap.IsValidationPackInstalled()
            ? "Сценарии §6.14 установлены — «Исследования» → «Группы сценариев» → «[Triad] Валидация §6.14»."
            : "Установите шаблоны сценариев A/B/C для проверки триады (§6.14).";

    /// <summary>Итог последней проверки метрик §13.3.</summary>
    public string TriadMetricsHint
    {
      get
      {
        var r = ScenarioLogComparisonSession.LastTriadMetricsReport;
        if (r != null && r.Phase != TriadValidationPhase.Unknown)
          return r.SummaryText;
        return "Метрики §13.3 проверяются автоматически в HTML-отчёте сценария [Triad A/B/C].";
      }
    }

    /// <inheritdoc />
    public void Dispose()
    {
      if (_disposed)
        return;
      _disposed = true;
      _refreshTimer.Stop();
    }

    private CouplingBridge Bridge => _context?.CouplingBridge;

    private void ApplyPhase()
    {
      if (!CanEditConfig)
      {
        StatusMessage = "Остановите пульсацию перед сменой фазы.";
        return;
      }

      if (!TriadPhaseStagePolicy.IsPhaseAllowed(SelectedPhase, AgentEvolutionStage, out string policyError))
      {
        StatusMessage = policyError;
        return;
      }

      var config = BuildConfigFromUi();
      config.Phase = SelectedPhase;
      if (!CouplingMappingLoader.TrySaveExperimentConfig(_environmentFolder, config, out string error))
      {
        StatusMessage = "Ошибка сохранения фазы: " + error;
        return;
      }

      ReloadConfig();
      StatusMessage = "Фаза " + SelectedPhase + " сохранена в triad_config.dat.";
    }

    private void SaveCoupling()
    {
      if (!CanEditConfig)
      {
        StatusMessage = "Остановите пульсацию перед сохранением.";
        return;
      }

      var config = BuildConfigFromUi();
      if (IncrementMappingVersionOnSave)
        config.CouplingMappingVersion = EditableCouplingMappingVersion + 1;
      else
        config.CouplingMappingVersion = EditableCouplingMappingVersion;

      if (!CouplingMappingLoader.TrySaveExperimentConfig(_environmentFolder, config, out string error))
      {
        StatusMessage = "Ошибка сохранения: " + error;
        MessageBox.Show(error, "Сохранение Environment", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      ReloadConfig();
      StatusMessage = "Конфигурация триады сохранена в Environment (mapping v" + CouplingMappingVersion + ").";
    }

    private TriadExperimentConfig BuildConfigFromUi()
    {
      return new TriadExperimentConfig
      {
        Phase = SelectedPhase,
        ContourId = ContourIdSetting?.Trim() ?? string.Empty,
        SpontaneousDriftEnabled = SpontaneousDriftEnabled,
        CouplingMappingVersion = EditableCouplingMappingVersion,
        UseFullNicheEngine = UseFullNicheEngine,
        NicheRoleProfileId = NicheRoleProfileIdSetting?.Trim() ?? "niche_minimal",
        UseProbeContour = UseProbeContour,
        AoeSettings = new TriadAoeSettings
        {
          BaselineWindowN = AoeBaselineN,
          ResponseThreshold = AoeThreshold,
          CorrelationHorizonK = AoeHorizonK,
          EvalWindowPulses = AoeEvalWindow
        },
        ActionCoupling = ActionCouplingRows
            .Where(r => r.ActionId > 0 && r.NicheParamId > 0)
            .Select(r => new CouplingTarget
            {
              ActionId = r.ActionId,
              NicheParamId = r.NicheParamId,
              Delta = r.Delta,
              Scale = r.Scale
            }).ToList(),
        NicheToCreature = NicheMappingRows
            .Where(r => r.NicheParamId > 0 && r.CreatureParamId > 0)
            .Select(r => new NicheCreatureMapping
            {
              NicheParamId = r.NicheParamId,
              CreatureParamId = r.CreatureParamId,
              Scale = r.Scale,
              LagPulses = r.LagPulses
            }).ToList(),
        OperatorNicheCoupling = OperatorCouplingRows
            .Where(r => r.InfluenceActionId > 0 && r.NicheParamId > 0)
            .Select(r => new OperatorNicheCouplingTarget
            {
              InfluenceActionId = r.InfluenceActionId,
              NicheParamId = r.NicheParamId,
              Delta = r.Delta,
              Scale = r.Scale
            }).ToList(),
        NicheParameters = NicheParamDefRows
            .Where(r => r.ParamId > 0)
            .Select(r => new NicheParameterDef
            {
              ParamId = r.ParamId,
              InitialValue = r.InitialValue,
              SpeedPerPulse = r.SpeedPerPulse
            }).ToList(),
        ContourProbes = ContourProbeRows
            .Where(r => !string.IsNullOrWhiteSpace(r.ProbeKey) && r.NicheParamId > 0)
            .Select(r => new ContourProbeCoupling
            {
              ProbeKey = r.ProbeKey.Trim(),
              NicheParamId = r.NicheParamId,
              Delta = r.Delta
            }).ToList()
      };
    }

    private void ApplyConfigToUi(TriadExperimentConfig config)
    {
      SelectedPhase = config.Phase;
      ContourIdSetting = config.ContourId ?? string.Empty;
      SpontaneousDriftEnabled = config.SpontaneousDriftEnabled;
      EditableCouplingMappingVersion = config.CouplingMappingVersion;
      UseFullNicheEngine = config.UseFullNicheEngine;
      NicheRoleProfileIdSetting = string.IsNullOrWhiteSpace(config.NicheRoleProfileId)
          ? "niche_minimal"
          : config.NicheRoleProfileId;
      UseProbeContour = config.UseProbeContour;

      var aoe = config.AoeSettings ?? new TriadAoeSettings();
      AoeBaselineN = aoe.BaselineWindowN;
      AoeThreshold = aoe.ResponseThreshold;
      AoeHorizonK = aoe.CorrelationHorizonK;
      AoeEvalWindow = aoe.EvalWindowPulses;

      ActionCouplingRows.Clear();
      foreach (var row in config.ActionCoupling)
      {
        ActionCouplingRows.Add(new CouplingRow
        {
          ActionId = row.ActionId,
          NicheParamId = row.NicheParamId,
          Delta = row.Delta,
          Scale = row.Scale
        });
      }

      NicheMappingRows.Clear();
      foreach (var row in config.NicheToCreature)
      {
        NicheMappingRows.Add(new NicheMappingRow
        {
          NicheParamId = row.NicheParamId,
          CreatureParamId = row.CreatureParamId,
          Scale = row.Scale,
          LagPulses = row.LagPulses
        });
      }

      OperatorCouplingRows.Clear();
      foreach (var row in config.OperatorNicheCoupling)
      {
        OperatorCouplingRows.Add(new OperatorCouplingRow
        {
          InfluenceActionId = row.InfluenceActionId,
          NicheParamId = row.NicheParamId,
          Delta = row.Delta,
          Scale = row.Scale
        });
      }

      NicheParamDefRows.Clear();
      foreach (var row in config.NicheParameters)
      {
        NicheParamDefRows.Add(new NicheParamDefRow
        {
          ParamId = row.ParamId,
          InitialValue = row.InitialValue,
          SpeedPerPulse = row.SpeedPerPulse
        });
      }

      ContourProbeRows.Clear();
      foreach (var row in config.ContourProbes)
      {
        ContourProbeRows.Add(new ContourProbeRow
        {
          ProbeKey = row.ProbeKey,
          NicheParamId = row.NicheParamId,
          Delta = row.Delta
        });
      }
    }

    private void ReloadConfig()
    {
      TriadProjectPaths.EnsureTriadDataFoldersForRoot(null);
      Bridge?.ReloadConfig(_environmentFolder);

      var config = Bridge?.Config ?? CouplingMappingLoader.LoadFromFolder(_environmentFolder);
      ApplyConfigToUi(config);

      RefreshLiveState();
      OnPropertyChanged(nameof(IsTriadActive));
      OnPropertyChanged(nameof(IsTriadInactive));
      OnPropertyChanged(nameof(CanEditConfig));
      OnPropertyChanged(nameof(CanReset));
      StatusMessage = IsTriadActive
          ? "Триада активна. run=" + ExperimentRunId + ", mapping v" + CouplingMappingVersion
          : "Триада не активна — заполните файлы в каталоге Environment.";
    }

    private void RefreshLiveState()
    {
      _context?.SyncTriadPhaseWithEvolutionStage();
      RebuildFilteredPhaseOptions();

      NicheParameters.Clear();
      if (Bridge != null && Bridge.IsActive && Bridge.NicheEngine != null && Bridge.NicheEngine.IsInitialized)
      {
        foreach (var kvp in Bridge.NicheEngine.State.GetCurrentValues().OrderBy(p => p.Key))
        {
          NicheParameters.Add(new NicheParamRow { ParamId = kvp.Key, Value = kvp.Value });
        }
      }

      DyadLogTail = ReadDyadLogTail();
      OnPropertyChanged(nameof(DyadLogTail));
      OnPropertyChanged(nameof(ExperimentRunId));
      OnPropertyChanged(nameof(CouplingMappingVersion));
      OnPropertyChanged(nameof(NicheRoleProfileId));
      OnPropertyChanged(nameof(ContourId));
      OnPropertyChanged(nameof(LastContourProbeKey));
      OnPropertyChanged(nameof(LastContourInputDim));
      OnPropertyChanged(nameof(ContourProbeHint));
      OnPropertyChanged(nameof(RunManifestHint));
      OnPropertyChanged(nameof(RunManifestPath));
      OnPropertyChanged(nameof(WaitingForNicheResponse));
      OnPropertyChanged(nameof(PhaseRestrictionHint));
      OnPropertyChanged(nameof(AgentEvolutionStage));
      OnPropertyChanged(nameof(IsTriadActive));
      OnPropertyChanged(nameof(IsTriadInactive));
      OnPropertyChanged(nameof(CanEditConfig));
      OnPropertyChanged(nameof(CanReset));
      OnPropertyChanged(nameof(IsCouplingReadOnly));
    }

    private void RebuildFilteredPhaseOptions()
    {
      int stage = AgentEvolutionStage;
      TriadPhase min = TriadPhaseStagePolicy.GetMinPhaseForStage(stage);
      TriadPhase max = TriadPhaseStagePolicy.GetMaxPhaseForStage(stage);

      FilteredPhaseOptions.Clear();
      foreach (var opt in PhaseOptions)
      {
        if (opt.Phase >= min && opt.Phase <= max)
          FilteredPhaseOptions.Add(opt);
      }

      TriadPhase clamped = TriadPhaseStagePolicy.ClampPhase(SelectedPhase, stage);
      if (SelectedPhase != clamped)
        SelectedPhase = clamped;

      if (Bridge?.Config != null && Bridge.Config.Phase != SelectedPhase)
      {
        SelectedPhase = Bridge.Config.Phase;
      }

      OnPropertyChanged(nameof(FilteredPhaseOptions));
    }

    private string ReadDyadLogTail()
    {
      if (string.IsNullOrWhiteSpace(_logsFolder))
        return "Каталог логов не задан.";

      string path = Path.Combine(_logsFolder, "AgentLogs_Dyad.jsonl");
      if (!File.Exists(path))
        return "Файл AgentLogs_Dyad.jsonl не найден. Включите логирование в настройках проекта.";

      try
      {
        var lines = File.ReadAllLines(path);
        int take = Math.Min(40, lines.Length);
        if (take == 0)
          return "Лог диады пуст.";

        return string.Join(System.Environment.NewLine, lines.Skip(lines.Length - take));
      }
      catch (Exception ex)
      {
        return "Ошибка чтения лога: " + ex.Message;
      }
    }

    private void OpenEnvironmentFolder()
    {
      TriadProjectPaths.EnsureTriadDataFoldersForRoot(null);
      if (!Directory.Exists(_environmentFolder))
        Directory.CreateDirectory(_environmentFolder);

      Process.Start("explorer.exe", _environmentFolder);
    }

    private void RecordRunStart()
    {
      Bridge?.RecordRunStart();
      RefreshLiveState();
      StatusMessage = "NicheInitSnapshot + triad_run_manifest.json записаны (Environment + логи).";
    }

    private void ResetDyad(DyadResetType resetType)
    {
      if (!CanReset)
      {
        StatusMessage = "Сброс недоступен: остановите пульсацию или активируйте триаду.";
        return;
      }

      string prompt;
      switch (resetType)
      {
        case DyadResetType.NicheSoft:
          prompt = "Сбросить параметры Niche к начальному снимку?";
          break;
        case DyadResetType.CreatureSoft:
          prompt = "Сбросить гомеостаз Creature к норме и окна AOE?";
          break;
        case DyadResetType.DyadHard:
          prompt = "Жёсткий сброс диады (Niche + Creature + новый experiment_run_id)?";
          break;
        default:
          prompt = "Сбросить параметры Niche к значениям из niche_params.dat?";
          break;
      }

      if (MessageBox.Show(prompt, "Сброс диады", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        return;

      var result = _context.ResetDyad(resetType);
      RefreshLiveState();
      StatusMessage = result != null && result.Success
          ? result.Message
          : "Сброс не выполнен.";
    }

    private void OpenDyadLogFile()
    {
      if (string.IsNullOrWhiteSpace(_logsFolder))
        return;

      string path = Path.Combine(_logsFolder, "AgentLogs_Dyad.jsonl");
      if (!File.Exists(path))
      {
        MessageBox.Show("Файл лога диады не найден.", "Лог диады", MessageBoxButton.OK, MessageBoxImage.Information);
        return;
      }

      Process.Start(path);
    }

    private void OpenRunManifestFile()
    {
      if (string.IsNullOrWhiteSpace(_logsFolder))
        return;

      string path = RunManifestPath;
      if (!File.Exists(path))
      {
        MessageBox.Show(
            "Файл triad_run_manifest.json ещё не создан. Нажмите «Init Niche» или перезапустите движок.",
            "Манифест прогона",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        return;
      }

      Process.Start(path);
    }

    private void InstallValidationScenarios()
    {
      bool overwrite = TriadValidationScenarioBootstrap.IsValidationPackInstalled()
          && MessageBox.Show(
              "Сценарии валидации уже установлены. Перезаписать шаблоны [Triad A/B/C] и группу?",
              "Сценарии §6.14",
              MessageBoxButton.YesNo,
              MessageBoxImage.Question) == MessageBoxResult.Yes;

      if (!TriadValidationScenarioBootstrap.IsValidationPackInstalled()
          && MessageBox.Show(
              "Установить шаблоны сценариев валидации триады §6.14?\n\n"
              + "Будут созданы сценарии A/B/C, группа прогона и preset Environment (если файлы ещё шаблонные).",
              "Сценарии §6.14",
              MessageBoxButton.YesNo,
              MessageBoxImage.Question) != MessageBoxResult.Yes)
      {
        return;
      }

      TriadProjectPaths.EnsureTriadDataFoldersForRoot(null);
      if (!TriadValidationScenarioBootstrap.TryInstall(overwrite, _environmentFolder, out string message))
      {
        StatusMessage = message;
        MessageBox.Show(message, "Сценарии §6.14", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      ReloadConfig();
      OnPropertyChanged(nameof(ValidationScenariosHint));
      StatusMessage = "Сценарии валидации §6.14 установлены.";
      MessageBox.Show(message, "Сценарии §6.14", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void CheckLastTriadMetrics()
    {
      if (!ScenarioLogComparisonSession.LastScenarioId.HasValue
          || !ScenarioLogComparisonSession.LastAnchorGlobalPulse.HasValue)
      {
        MessageBox.Show(
            "Нет данных последнего прогона. Запустите сценарий [Triad A/B/C] из «Исследования».",
            "Метрики §13.3",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        return;
      }

      ScenarioDocument doc;
      try
      {
        doc = ScenarioStorage.LoadScenario(ScenarioLogComparisonSession.LastScenarioId.Value);
      }
      catch (Exception ex)
      {
        StatusMessage = "Не удалось загрузить сценарий: " + ex.Message;
        return;
      }

      if (!TriadValidationMetricsChecker.IsTriadValidationScenario(doc))
      {
        MessageBox.Show(
            "Последний прогон не был сценарием валидации триады ([Triad …]).",
            "Метрики §13.3",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        return;
      }

      var completion = new OperatorScenarioCompletedEventArgs
      {
        Document = doc,
        AnchorGlobalPulse = ScenarioLogComparisonSession.LastAnchorGlobalPulse.Value,
        LastExecutedPulseWithinScenario = doc.Lines?.Count > 0
            ? doc.Lines.Max(l => l.PulseWithinScenario)
            : 0,
        Success = true
      };

      var report = TriadValidationMetricsChecker.Evaluate(doc, completion, _logsFolder);
      ScenarioLogComparisonSession.LastTriadMetricsReport = report;
      OnPropertyChanged(nameof(TriadMetricsHint));

      var sb = new System.Text.StringBuilder();
      sb.AppendLine(report.SummaryText);
      sb.AppendLine();
      foreach (var c in report.Checks)
      {
        sb.Append(c.Passed ? "✓ " : "✗ ");
        sb.Append(c.Name);
        if (!string.IsNullOrWhiteSpace(c.Details))
          sb.Append(" — ").Append(c.Details);
        sb.AppendLine();
      }

      StatusMessage = report.SummaryText;
      MessageBox.Show(
          sb.ToString(),
          "Метрики §13.3",
          MessageBoxButton.OK,
          report.AllPassed ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void OnPropertyChanged(string name)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>Строка параметра Niche для live-снимка.</summary>
    public sealed class NicheParamRow
    {
      /// <summary>ID параметра.</summary>
      public int ParamId { get; set; }

      /// <summary>Текущее значение.</summary>
      public float Value { get; set; }
    }

    /// <summary>Строка niche_params.dat для редактора.</summary>
    public sealed class NicheParamDefRow
    {
      /// <summary>ID параметра.</summary>
      public int ParamId { get; set; }

      /// <summary>Начальное значение 0…100.</summary>
      public float InitialValue { get; set; }

      /// <summary>Дрейф за пульс.</summary>
      public float SpeedPerPulse { get; set; }
    }

    /// <summary>Строка coupling action→Niche.</summary>
    public sealed class CouplingRow
    {
      /// <summary>ID действия Creature.</summary>
      public int ActionId { get; set; }

      /// <summary>ID параметра Niche.</summary>
      public int NicheParamId { get; set; }

      /// <summary>Дельта.</summary>
      public float Delta { get; set; }

      /// <summary>Масштаб.</summary>
      public float Scale { get; set; }
    }

    /// <summary>Строка mapping Niche→Creature.</summary>
    public sealed class NicheMappingRow
    {
      /// <summary>ID параметра Niche.</summary>
      public int NicheParamId { get; set; }

      /// <summary>ID параметра Creature.</summary>
      public int CreatureParamId { get; set; }

      /// <summary>Масштаб.</summary>
      public float Scale { get; set; }

      /// <summary>Задержка в пульсах.</summary>
      public int LagPulses { get; set; }
    }

    /// <summary>Строка coupling Operator→Niche.</summary>
    public sealed class OperatorCouplingRow
    {
      /// <summary>ID воздействия с пульта.</summary>
      public int InfluenceActionId { get; set; }

      /// <summary>ID параметра Niche.</summary>
      public int NicheParamId { get; set; }

      /// <summary>Дельта.</summary>
      public float Delta { get; set; }

      /// <summary>Масштаб.</summary>
      public float Scale { get; set; }
    }

    /// <summary>Строка contour_probes.dat.</summary>
    public sealed class ContourProbeRow
    {
      /// <summary>Ключ пробы.</summary>
      public string ProbeKey { get; set; } = string.Empty;

      /// <summary>ID параметра Niche.</summary>
      public int NicheParamId { get; set; }

      /// <summary>Дельта.</summary>
      public float Delta { get; set; }
    }

    /// <summary>Описание фазы для ComboBox.</summary>
    public sealed class TriadPhaseOption
    {
      /// <summary>
      /// Создаёт описание фазы.
      /// </summary>
      /// <param name="phase">Фаза.</param>
      /// <param name="title">Заголовок.</param>
      /// <param name="description">Подробное описание.</param>
      public TriadPhaseOption(TriadPhase phase, string title, string description)
      {
        Phase = phase;
        Title = title;
        Description = description;
      }

      /// <summary>Фаза.</summary>
      public TriadPhase Phase { get; }

      /// <summary>Заголовок для списка.</summary>
      public string Title { get; }

      /// <summary>Описание ограничений.</summary>
      public string Description { get; }
    }
  }
}
