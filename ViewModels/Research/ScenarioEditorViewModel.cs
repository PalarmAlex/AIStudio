using AIStudio;
using AIStudio.Common;
using AIStudio.ViewModels;

using ISIDA.Actions;
using ISIDA.Psychic.Automatism;
using ISIDA.Scenarios;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

using Ookii.Dialogs.Wpf;

namespace AIStudio.ViewModels.Research
{
  public sealed class ScenarioEditorViewModel : INotifyPropertyChanged
  {
    private readonly InfluenceActionSystem _influenceActions;

    private string _title = "";
    private string _description = "";
    private string _dateText = "";
    private int _preRunTargetStage = -1;
    private bool _preRunClearAgentData;
    private bool _preRunNormalHomeostasisState;
    private bool _scenarioObservationMode;
    private bool _scenarioAuthoritativeRecording;
    private string _massFillMode = "Unknown";
    private int _pulseStepIncrement = (int)ScenarioPulseStepIncrement.ActionHoldPlusOne;
    private int _runPulseTimingCoefficient = 1;

    public List<ScenarioExpectationChoiceItem> MassFillOptions { get; } = new List<ScenarioExpectationChoiceItem>
    {
      new ScenarioExpectationChoiceItem { Label = "Неизвестно", Code = "Unknown" },
      new ScenarioExpectationChoiceItem { Label = "Пусто (прочерк)", Code = "Dash" }
    };
    private ScenarioLineRow _selectedLine;
    private ScenarioLogExpectationRow _selectedExpectationRow;
    private readonly Func<ScenarioDocument, string, ScenarioEditorViewModel, bool> _tryStartScenario;
    private string _reportOutputFolder = "";
    private string _repeatBlockCountText = "1";

    public ScenarioEditorViewModel(
        InfluenceActionSystem influenceActions,
        ScenarioDocument doc,
        Func<ScenarioDocument, string, ScenarioEditorViewModel, bool> tryStartScenario = null)
    {
      _influenceActions = influenceActions ?? throw new ArgumentNullException(nameof(influenceActions));
      _tryStartScenario = tryStartScenario;
      _reportOutputFolder = AppConfig.ScenarioReportsFolderPath;
      Document = doc ?? throw new ArgumentNullException(nameof(doc));

      _title = doc.Header.Title ?? "";
      _description = doc.Header.Description ?? "";
      _dateText = string.IsNullOrWhiteSpace(doc.Header.DateText)
          ? DateTime.Now.ToString("yyyy-MM-dd")
          : doc.Header.DateText;
      _preRunTargetStage = doc.Header.PreRunTargetStage >= -1 && doc.Header.PreRunTargetStage <= 5
          ? doc.Header.PreRunTargetStage
          : -1;
      _preRunClearAgentData = doc.Header.PreRunClearAgentData;
      _preRunNormalHomeostasisState = doc.Header.PreRunNormalHomeostasisState;
      _scenarioObservationMode = doc.Header.ScenarioObservationMode;
      _scenarioAuthoritativeRecording = doc.Header.ScenarioAuthoritativeRecording;
      _pulseStepIncrement = NormalizePulseStepIncrementCode(doc.Header.PulseStepIncrement);
      _runPulseTimingCoefficient = NormalizeRunPulseTimingCoefficient(doc.Header.RunPulseTimingCoefficient);

      PulseTimingCoefficientChoices = new List<int> { 1, 5, 10, 20 };

      PulseStepIncrementChoices = new List<ScenarioPulseIncrementChoiceItem>
      {
        new ScenarioPulseIncrementChoiceItem { Code = (int)ScenarioPulseStepIncrement.Sequential, Label = "Следующий по порядку" },
        new ScenarioPulseIncrementChoiceItem { Code = (int)ScenarioPulseStepIncrement.ActionHold, Label = "Время удержания действий" },
        new ScenarioPulseIncrementChoiceItem { Code = (int)ScenarioPulseStepIncrement.ActionHoldPlusOne, Label = "Время удержания действий + 1" },
        new ScenarioPulseIncrementChoiceItem { Code = (int)ScenarioPulseStepIncrement.StateHoldPlusOne, Label = "Время удержания состояний + 1" }
      };

      ToneChoiceOptions = ActionsImagesSystem.GetToneList()
          .OrderBy(kv => kv.Key)
          .Select(kv => new ScenarioToneMoodChoiceItem { Id = kv.Key, Label = kv.Value })
          .ToList();
      MoodChoiceOptions = ActionsImagesSystem.GetMoodList()
          .OrderBy(kv => kv.Key)
          .Select(kv => new ScenarioToneMoodChoiceItem { Id = kv.Key, Label = kv.Value })
          .ToList();

      PreRunStageChoices = new ObservableCollection<EvolutionStageItem>();
      PreRunStageChoices.Add(new EvolutionStageItem { StageNumber = -1, Description = "Не менять стадию" });
      for (int i = 0; i <= 5; i++)
        PreRunStageChoices.Add(new EvolutionStageItem
        {
          StageNumber = i,
          Description = $"{i}: {EvolutionStageItem.GetDescription(i)}"
        });

      StateChoiceOptions = ScenarioExpectationChoiceLists.BuildStateChoices();
      OrUmChoiceOptions = ScenarioExpectationChoiceLists.BuildOrUmChoices();
      var combPath = Path.Combine(AppConfig.DataGomeostasFolderPath, "StyleCombinations.comb");
      StyleChoiceOptions = ScenarioExpectationChoiceLists.LoadStyleChoices(combPath);

      Lines = new BindingList<ScenarioLineRow>();
      Lines.AllowNew = false;
      Lines.AllowRemove = true;
      Lines.AllowEdit = true;
      Lines.RaiseListChangedEvents = false;
      foreach (var l in doc.Lines.OrderBy(x => x.StepIndex > 0 ? x.StepIndex : int.MaxValue).ThenBy(x => x.PulseWithinScenario))
      {
        var row = l.Clone();
        Lines.Add(row);
        row.RefreshActionNames(_influenceActions);
      }
      Lines.RaiseListChangedEvents = true;

      ScenarioPulseSchedule.EnsureSequentialStepIndices(Lines);
      foreach (var l in Lines)
        l.RefreshActionNames(_influenceActions);

      if (Document.LogExpectationColumnSkips == null)
        Document.LogExpectationColumnSkips = new ScenarioLogExpectationColumnSkips();
      ExpectationRows = new ObservableCollection<ScenarioLogExpectationRow>();
      LoadExpectationsFromDocument();

      Lines.ListChanged += OnLinesListChanged;

      SaveCommand = new RelayCommand(_ => Save(requestCloseAfterSuccess: false, showSuccessMessage: true));
      AddLineCommand = new RelayCommand(_ => AddLine());
      MassFillExpectationsCommand = new RelayCommand(_ => MassFillExpectations());
      BrowseReportFolderCommand = new RelayCommand(_ => BrowseReportFolder());

      HasUnsavedChanges = false;
    }

    public string ReportOutputFolder
    {
      get => _reportOutputFolder;
      set
      {
        if (_reportOutputFolder == value) return;
        _reportOutputFolder = value ?? "";
        OnPropertyChanged();
      }
    }

    public ICommand BrowseReportFolderCommand { get; }

    private void BrowseReportFolder()
    {
      var dialog = new VistaFolderBrowserDialog
      {
        Description = "Каталог для сохранения HTML-отчёта",
        UseDescriptionForTitle = true,
        SelectedPath = Directory.Exists(ReportOutputFolder) ? ReportOutputFolder : ""
      };
      if (dialog.ShowDialog() == true)
        ReportOutputFolder = dialog.SelectedPath;
    }

    public ScenarioDocument Document { get; }
    public BindingList<ScenarioLineRow> Lines { get; }

    public ObservableCollection<ScenarioLogExpectationRow> ExpectationRows { get; }

    public ScenarioLogExpectationRow SelectedExpectationRow
    {
      get => _selectedExpectationRow;
      set
      {
        if (_selectedExpectationRow == value) return;
        _selectedExpectationRow = value;
        OnPropertyChanged();
        CommandManager.InvalidateRequerySuggested();
      }
    }

    public ObservableCollection<EvolutionStageItem> PreRunStageChoices { get; }

    public List<ScenarioToneMoodChoiceItem> ToneChoiceOptions { get; }
    public List<ScenarioToneMoodChoiceItem> MoodChoiceOptions { get; }

    public List<ScenarioExpectationChoiceItem> StateChoiceOptions { get; }
    public List<ScenarioExpectationChoiceItem> StyleChoiceOptions { get; }
    public List<ScenarioExpectationChoiceItem> OrUmChoiceOptions { get; }

    public List<ScenarioPulseIncrementChoiceItem> PulseStepIncrementChoices { get; }

    /// <summary>Допустимые значения коэфф. пульсации для привязки ComboBox.</summary>
    public List<int> PulseTimingCoefficientChoices { get; }

    public int RunPulseTimingCoefficient
    {
      get => _runPulseTimingCoefficient;
      set
      {
        int v = NormalizeRunPulseTimingCoefficient(value);
        if (_runPulseTimingCoefficient == v)
          return;
        _runPulseTimingCoefficient = v;
        OnPropertyChanged();
        HasUnsavedChanges = true;
      }
    }

    private static int NormalizeRunPulseTimingCoefficient(int v) =>
        v == 1 || v == 5 || v == 10 || v == 20 ? v : 1;

    public int PulseStepIncrement
    {
      get => _pulseStepIncrement;
      set
      {
        int v = NormalizePulseStepIncrementCode(value);
        if (_pulseStepIncrement == v)
          return;
        _pulseStepIncrement = v;
        OnPropertyChanged();
        HasUnsavedChanges = true;
      }
    }

    private static int NormalizePulseStepIncrementCode(int code) =>
        code == (int)ScenarioPulseStepIncrement.Sequential
        || code == (int)ScenarioPulseStepIncrement.ActionHold
        || code == (int)ScenarioPulseStepIncrement.ActionHoldPlusOne
        || code == (int)ScenarioPulseStepIncrement.StateHoldPlusOne
            ? code
            : (int)ScenarioPulseStepIncrement.ActionHoldPlusOne;

    private int CurrentPulseStepDelta()
    {
      var mode = (ScenarioPulseStepIncrement)_pulseStepIncrement;
      return ScenarioPulseSchedule.PulseDeltaBetweenConsecutiveSteps(
          mode,
          Math.Max(0, AppConfig.ReflexActionDisplayDuration),
          Math.Max(0, AppConfig.DynamicTime));
    }

    public string MassFillMode
    {
      get => _massFillMode;
      set { if (_massFillMode == value) return; _massFillMode = value; OnPropertyChanged(); }
    }

    public ICommand MassFillExpectationsCommand { get; }

    private void MassFillExpectations()
    {
      bool unknown = string.Equals(_massFillMode, "Unknown", StringComparison.Ordinal);
      string target = unknown ? "" : "-";
      string verb = unknown ? "неизвестные (пустые) значения" : "прочерки «-»";
      if (MessageBox.Show(
              $"Заполнить во всех ячейках ожидаемого результата (кроме шага и пульса) {verb}?",
              "Массовая подстановка",
              MessageBoxButton.YesNo,
              MessageBoxImage.Question) != MessageBoxResult.Yes)
        return;
      SyncExpectationRowsWithLines();
      foreach (var r in ExpectationRows)
      {
        r.StateText = target;
        r.StyleText = target;
        r.ThemeText = target;
        r.TriggerText = target;
        r.OrUmText = target;
        r.GeneticReflexText = target;
        r.ConditionReflexText = target;
        r.AutomatizmText = target;
        r.ReflexChainText = target;
        r.AutomatizmChainText = target;
        r.MainCycleText = target;
      }
      HasUnsavedChanges = true;
    }

    private void ExpectationRowOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName == nameof(ScenarioLogExpectationRow.StepIndex)
          || e.PropertyName == nameof(ScenarioLogExpectationRow.PulseWithinScenario))
        return;
      HasUnsavedChanges = true;
    }

    private void AttachExpectationRow(ScenarioLogExpectationRow r)
    {
      r.PropertyChanged -= ExpectationRowOnPropertyChanged;
      r.PropertyChanged += ExpectationRowOnPropertyChanged;
    }

    private void OnLinesListChanged(object sender, ListChangedEventArgs e)
    {
      switch (e.ListChangedType)
      {
        case ListChangedType.ItemAdded:
          if (e.NewIndex >= 0 && e.NewIndex < Lines.Count)
            Lines[e.NewIndex].RefreshActionNames(_influenceActions);
          goto case ListChangedType.ItemDeleted;
        case ListChangedType.ItemDeleted:
          ScenarioPulseSchedule.EnsureSequentialStepIndices(Lines);
          foreach (var l in Lines)
            l.RefreshActionNames(_influenceActions);
          SyncExpectationRowsWithLines();
          HasUnsavedChanges = true;
          CommandManager.InvalidateRequerySuggested();
          break;
        case ListChangedType.Reset:
          break;
      }
    }

    private static void NormalizeExpectationRowFields(ScenarioLogExpectationRow r)
    {
      if (r == null) return;
      r.StateText = NormalizeCodeCell(r.StateText);
      r.StyleText = NormalizeCodeCell(r.StyleText);
      r.OrUmText = NormalizeCodeCell(r.OrUmText);
      r.ThemeText = NormalizeExpectedCell(r.ThemeText);
      r.TriggerText = NormalizeExpectedCell(r.TriggerText);
      r.GeneticReflexText = NormalizeExpectedCell(r.GeneticReflexText);
      r.ConditionReflexText = NormalizeExpectedCell(r.ConditionReflexText);
      r.AutomatizmText = NormalizeExpectedCell(r.AutomatizmText);
      r.ReflexChainText = NormalizeExpectedCell(r.ReflexChainText);
      r.AutomatizmChainText = NormalizeExpectedCell(r.AutomatizmChainText);
      r.MainCycleText = NormalizeExpectedCell(r.MainCycleText);
    }

    /// <summary>Состояние, стиль, ОР/УМ: «-», пусто (не проверять) или код.</summary>
    private static string NormalizeCodeCell(string s)
    {
      if (s == null) return "-";
      return s.Trim();
    }

    private static string NormalizeExpectedCell(string s) =>
        s == null ? "-" : s.Trim();

    private void LoadExpectationsFromDocument()
    {
      var byStep = (Document.LogExpectations ?? new List<ScenarioLogExpectationRow>())
          .GroupBy(x => x.StepIndex)
          .ToDictionary(g => g.Key, g => g.First());
      ExpectationRows.Clear();
      foreach (var line in Lines)
      {
        if (byStep.TryGetValue(line.StepIndex, out var src))
        {
          var c = src.Clone();
          c.StepIndex = line.StepIndex;
          c.PulseWithinScenario = line.PulseWithinScenario;
          NormalizeExpectationRowFields(c);
          ExpectationRows.Add(c);
          AttachExpectationRow(c);
        }
        else
        {
          var er = new ScenarioLogExpectationRow
          {
            StepIndex = line.StepIndex,
            PulseWithinScenario = line.PulseWithinScenario
          };
          NormalizeExpectationRowFields(er);
          ExpectationRows.Add(er);
          AttachExpectationRow(er);
        }
      }
      SyncExpectationRowsWithLines();
    }

    /// <summary>Синхронизирует шаг/пульс в таблице ожидаемого лога с гридом шагов (после правки пульса и т.п.).</summary>
    public void SyncExpectationRowsWithLinesFromEditor()
    {
      SyncExpectationRowsWithLines();
    }

    private void SyncExpectationRowsWithLines()
    {
      while (ExpectationRows.Count < Lines.Count)
      {
        var r = new ScenarioLogExpectationRow();
        NormalizeExpectationRowFields(r);
        AttachExpectationRow(r);
        ExpectationRows.Add(r);
      }
      while (ExpectationRows.Count > Lines.Count)
        ExpectationRows.RemoveAt(ExpectationRows.Count - 1);
      for (int i = 0; i < Lines.Count; i++)
      {
        var line = Lines[i];
        var exp = ExpectationRows[i];
        exp.StepIndex = line.StepIndex;
        exp.PulseWithinScenario = line.PulseWithinScenario;
      }
    }

    public InfluenceActionSystem InfluenceActions => _influenceActions;

    public string Title
    {
      get => _title;
      set
      {
        if (_title == value) return;
        _title = value;
        OnPropertyChanged();
        HasUnsavedChanges = true;
      }
    }

    public string Description
    {
      get => _description;
      set
      {
        if (_description == value) return;
        _description = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(DescriptionDisplay));
        HasUnsavedChanges = true;
      }
    }

    private const int DescriptionDisplayLimit = 100;

    public string DescriptionDisplay
    {
      get
      {
        if (string.IsNullOrEmpty(_description))
          return "";
        var flat = _description.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
        return flat.Length <= DescriptionDisplayLimit
            ? flat
            : flat.Substring(0, DescriptionDisplayLimit) + "…";
      }
    }

    public string DateText
    {
      get => _dateText;
      set
      {
        if (_dateText == value) return;
        _dateText = value;
        OnPropertyChanged();
        HasUnsavedChanges = true;
      }
    }

    public int PreRunTargetStage
    {
      get => _preRunTargetStage;
      set
      {
        if (_preRunTargetStage == value) return;
        _preRunTargetStage = value;
        OnPropertyChanged();
        HasUnsavedChanges = true;
      }
    }

    public bool PreRunClearAgentData
    {
      get => _preRunClearAgentData;
      set
      {
        if (_preRunClearAgentData == value) return;
        _preRunClearAgentData = value;
        OnPropertyChanged();
        HasUnsavedChanges = true;
      }
    }

    public bool PreRunNormalHomeostasisState
    {
      get => _preRunNormalHomeostasisState;
      set
      {
        if (_preRunNormalHomeostasisState == value) return;
        _preRunNormalHomeostasisState = value;
        OnPropertyChanged();
        HasUnsavedChanges = true;
      }
    }

    public bool ScenarioObservationMode
    {
      get => _scenarioObservationMode;
      set
      {
        if (_scenarioObservationMode == value) return;
        _scenarioObservationMode = value;
        OnPropertyChanged();
        HasUnsavedChanges = true;
      }
    }

    public bool ScenarioAuthoritativeRecording
    {
      get => _scenarioAuthoritativeRecording;
      set
      {
        if (_scenarioAuthoritativeRecording == value) return;
        _scenarioAuthoritativeRecording = value;
        OnPropertyChanged();
        HasUnsavedChanges = true;
      }
    }

    public ScenarioLineRow SelectedLine
    {
      get => _selectedLine;
      set
      {
        _selectedLine = value;
        OnPropertyChanged();
        CommandManager.InvalidateRequerySuggested();
      }
    }

    public ICommand SaveCommand { get; }
    public ICommand AddLineCommand { get; }

    /// <summary>Количество повторов выделенного блока строк (ввод в поле рядом с кнопкой «Повторить»).</summary>
    public string RepeatBlockCountText
    {
      get => _repeatBlockCountText;
      set
      {
        if (_repeatBlockCountText == value) return;
        _repeatBlockCountText = value ?? "";
        OnPropertyChanged();
      }
    }

    /// <summary>
    /// Вставляет подряд N копий непрерывно выделенного блока строк сразу после него (N — поле <see cref="RepeatBlockCountText"/>).
    /// Между концом предыдущего фрагмента и первой строкой копии пульс увеличивается как при «Добавить запись»
    /// (зазор из режима приращения пульса); внутри копии сохраняются относительные интервалы пульсов исходного блока.
    /// </summary>
    public void RepeatSelectedLinesBlock(IReadOnlyList<ScenarioLineRow> selectedItems)
    {
      if (!int.TryParse((RepeatBlockCountText ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int repeatCount) || repeatCount < 1)
      {
        MessageBox.Show(
            "Укажите количество повторов целым числом больше нуля.",
            "Повтор блока",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
      }

      if (selectedItems == null || selectedItems.Count == 0)
      {
        MessageBox.Show(
            "Не выбраны строки для повтора.",
            "Повтор блока",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        return;
      }

      var uniqueRows = selectedItems.Where(r => r != null).Distinct().ToList();
      if (uniqueRows.Count == 0)
      {
        MessageBox.Show(
            "Не выбраны строки для повтора.",
            "Повтор блока",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        return;
      }

      var indices = uniqueRows.Select(r => Lines.IndexOf(r)).Where(i => i >= 0).OrderBy(i => i).ToList();
      if (indices.Count != uniqueRows.Count)
      {
        MessageBox.Show(
            "Выделение содержит строки, которых нет в таблице шагов.",
            "Повтор блока",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
      }

      int minIx = indices[0];
      int maxIx = indices[indices.Count - 1];
      if (indices.Count != maxIx - minIx + 1)
      {
        MessageBox.Show(
            "Выберите только подряд идущие строки таблицы: непрерывный диапазон без пропусков.",
            "Повтор блока",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return;
      }

      if (ExpectationRows.Count != Lines.Count)
        SyncExpectationRowsWithLines();

      int blockStart = minIx;
      int blockEnd = maxIx;
      int blockLen = blockEnd - blockStart + 1;
      int pulseDelta = CurrentPulseStepDelta();

      Lines.ListChanged -= OnLinesListChanged;
      try
      {
        int insertAfter = blockEnd;
        for (int rep = 0; rep < repeatCount; rep++)
        {
          int anchorPulse = Lines[insertAfter].PulseWithinScenario;
          int basePulse = anchorPulse + pulseDelta;
          int blockFirstPulse = Lines[blockStart].PulseWithinScenario;
          for (int k = 0; k < blockLen; k++)
          {
            var srcLine = Lines[blockStart + k];
            var lineClone = srcLine.Clone();
            lineClone.PulseWithinScenario = basePulse + (srcLine.PulseWithinScenario - blockFirstPulse);
            var expClone = ExpectationRows[blockStart + k].Clone();
            Lines.Insert(insertAfter + 1 + k, lineClone);
            ExpectationRows.Insert(insertAfter + 1 + k, expClone);
            AttachExpectationRow(expClone);
            lineClone.RefreshActionNames(_influenceActions);
          }
          insertAfter += blockLen;
        }
      }
      finally
      {
        Lines.ListChanged += OnLinesListChanged;
      }

      ScenarioPulseSchedule.EnsureSequentialStepIndices(Lines);
      foreach (var l in Lines)
        l.RefreshActionNames(_influenceActions);
      SyncExpectationRowsWithLines();
      if (blockEnd + 1 < Lines.Count)
        SelectedLine = Lines[blockEnd + 1];
      HasUnsavedChanges = true;
      CommandManager.InvalidateRequerySuggested();
    }

    public event EventHandler<bool> RequestClose;

    public Action CloseAction { get; set; }

    public bool HasUnsavedChanges { get; private set; }

    public bool TryCancelWithPrompt()
    {
      if (!HasUnsavedChanges)
        return true;
      var r = MessageBox.Show("Сохранить изменения перед закрытием?", "Редактор сценария",
          MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
      if (r == MessageBoxResult.Cancel)
        return false;
      if (r == MessageBoxResult.Yes)
        return Save(requestCloseAfterSuccess: false);
      return true;
    }

    private ScenarioLineRow CreateNewLineRow()
    {
      int delta = CurrentPulseStepDelta();
      int maxPulse = Lines.Count == 0 ? 0 : Lines.Max(l => l.PulseWithinScenario);
      int nextPulse = maxPulse <= 0 ? 1 : maxPulse + delta;

      var row = new ScenarioLineRow
      {
        StepIndex = Lines.Count + 1,
        PulseWithinScenario = nextPulse,
        Kind = ScenarioLineKind.Pult,
        ToneId = 0,
        MoodId = 0
      };
      row.RefreshActionNames(_influenceActions);
      return row;
    }

    private void AddLine()
    {
      var row = CreateNewLineRow();
      Lines.Add(row);
      SelectedLine = row;
    }

    public void DeleteSelectedLines(IEnumerable<ScenarioLineRow> rows)
    {
      if (rows == null)
        return;
      var list = rows.Where(r => r != null && Lines.Contains(r)).Distinct().OrderByDescending(r => Lines.IndexOf(r)).ToList();
      if (list.Count == 0)
        return;
      foreach (var r in list)
        Lines.Remove(r);
      ScenarioPulseSchedule.EnsureSequentialStepIndices(Lines);
      foreach (var l in Lines)
        l.RefreshActionNames(_influenceActions);
      SelectedLine = Lines.FirstOrDefault();
      HasUnsavedChanges = true;
    }

    public void MarkDirty()
    {
      HasUnsavedChanges = true;
    }

    public bool Save(bool requestCloseAfterSuccess = true, bool showSuccessMessage = false)
    {
      var doc = BuildDocument();
      var err = OperatorScenarioValidator.ValidateDocument(doc, _influenceActions);
      if (err != null)
      {
        MessageBox.Show(err, "Проверка сценария", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
      }

      ScenarioStorage.EnsureFolder();
      if (doc.Header.Id == 0)
        doc.Header.Id = ScenarioStorage.NextScenarioId();

      doc.Header.Title = Title?.Trim() ?? "";
      doc.Header.Description = Description?.Trim() ?? "";
      doc.Header.DateText = DateText?.Trim() ?? "";
      doc.Header.PreRunTargetStage = PreRunTargetStage;
      doc.Header.PreRunClearAgentData = PreRunClearAgentData;
      doc.Header.PreRunNormalHomeostasisState = PreRunNormalHomeostasisState;
      doc.Header.ScenarioObservationMode = ScenarioObservationMode;
      doc.Header.ScenarioAuthoritativeRecording = ScenarioAuthoritativeRecording;
      doc.Header.PulseStepIncrement = PulseStepIncrement;
      doc.Header.RunPulseTimingCoefficient = RunPulseTimingCoefficient;

      var (okLines, errLines) = ScenarioStorage.SaveScenarioLines(doc);
      if (!okLines)
      {
        MessageBox.Show(errLines, "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
        return false;
      }

      var reg = ScenarioStorage.LoadRegistry();
      var existing = reg.FirstOrDefault(h => h.Id == doc.Header.Id);
      if (existing != null)
        reg.Remove(existing);
      reg.Add(new ScenarioHeader
      {
        Id = doc.Header.Id,
        Title = doc.Header.Title,
        Description = doc.Header.Description,
        DateText = doc.Header.DateText
      });

      var (okReg, errReg) = ScenarioStorage.SaveRegistry(reg);
      if (!okReg)
      {
        MessageBox.Show(errReg, "Ошибка сохранения реестра", MessageBoxButton.OK, MessageBoxImage.Error);
        return false;
      }

      Document.Header.Id = doc.Header.Id;

      HasUnsavedChanges = false;
      if (showSuccessMessage)
        MessageBox.Show("Сценарий успешно сохранен", "Сохранение",
            MessageBoxButton.OK, MessageBoxImage.Information);
      if (requestCloseAfterSuccess)
        RequestClose?.Invoke(this, true);
      return true;
    }

    public ScenarioDocument BuildDocument()
    {
      ScenarioPulseSchedule.EnsureSequentialStepIndices(Lines);
      SyncExpectationRowsWithLines();
      var doc = new ScenarioDocument
      {
        Header = new ScenarioHeader
        {
          Id = Document.Header.Id,
          Title = Title?.Trim() ?? "",
          Description = Description?.Trim() ?? "",
          DateText = DateText?.Trim() ?? "",
          InitialHomeostasisValues = Document.Header.InitialHomeostasisValues ?? "",
          PreRunTargetStage = PreRunTargetStage,
          PreRunClearAgentData = PreRunClearAgentData,
          PreRunNormalHomeostasisState = PreRunNormalHomeostasisState,
          ScenarioObservationMode = ScenarioObservationMode,
          ScenarioAuthoritativeRecording = ScenarioAuthoritativeRecording,
          PulseStepIncrement = PulseStepIncrement,
          RunPulseTimingCoefficient = RunPulseTimingCoefficient
        },
        Lines = Lines.Select(l => l.Clone()).ToList(),
        LogExpectationColumnSkips = Document.LogExpectationColumnSkips?.Clone() ?? new ScenarioLogExpectationColumnSkips(),
        LogExpectations = ExpectationRows.Select(r => r.Clone()).ToList()
      };
      return doc;
    }

    public void MarkSaved()
    {
      HasUnsavedChanges = false;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string name = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
  }
}
