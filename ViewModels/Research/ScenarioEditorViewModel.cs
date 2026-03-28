using AIStudio;
using AIStudio.Common;
using AIStudio.ViewModels;

using ISIDA.Actions;
using ISIDA.Scenarios;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace AIStudio.ViewModels.Research
{
  public sealed class ScenarioEditorViewModel : INotifyPropertyChanged
  {
    private readonly InfluenceActionSystem _influenceActions;
    private readonly OperatorScenarioEngine _scenarioEngine;
    private readonly bool _isNew;

    private string _title = "";
    private string _description = "";
    private string _dateText = "";
    private ScenarioLineRow _selectedLine;

    /// <summary>Строка таблицы сравнения ожидаемого лога с фактическим.</summary>
    public sealed class ScenarioLogCompareRow : INotifyPropertyChanged
    {
      private int _stepIndex;
      private int _pulseWithinScenario;
      private bool _ok;
      private string _details = "";

      public int StepIndex
      {
        get => _stepIndex;
        set { if (_stepIndex == value) return; _stepIndex = value; OnPropertyChanged(); OnPropertyChanged(nameof(LabelText)); }
      }

      public int PulseWithinScenario
      {
        get => _pulseWithinScenario;
        set { if (_pulseWithinScenario == value) return; _pulseWithinScenario = value; OnPropertyChanged(); OnPropertyChanged(nameof(LabelText)); }
      }

      public bool Ok
      {
        get => _ok;
        set { if (_ok == value) return; _ok = value; OnPropertyChanged(); OnPropertyChanged(nameof(OkLabel)); }
      }

      public string Details
      {
        get => _details;
        set { if (_details == value) return; _details = value ?? ""; OnPropertyChanged(); }
      }

      public string OkLabel => Ok ? "OK" : "No";

      public string LabelText => $"Шаг {StepIndex}, пульс {PulseWithinScenario} — {OkLabel}";

      public event PropertyChangedEventHandler PropertyChanged;

      private void OnPropertyChanged([CallerMemberName] string name = null) =>
          PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public ScenarioEditorViewModel(
        InfluenceActionSystem influenceActions,
        OperatorScenarioEngine scenarioEngine,
        ScenarioDocument doc,
        bool isNew)
    {
      _influenceActions = influenceActions ?? throw new ArgumentNullException(nameof(influenceActions));
      _scenarioEngine = scenarioEngine ?? throw new ArgumentNullException(nameof(scenarioEngine));
      _isNew = isNew;
      Document = doc ?? throw new ArgumentNullException(nameof(doc));

      _title = doc.Header.Title ?? "";
      _description = doc.Header.Description ?? "";
      _dateText = string.IsNullOrWhiteSpace(doc.Header.DateText)
          ? DateTime.Now.ToString("yyyy-MM-dd")
          : doc.Header.DateText;

      Lines = new BindingList<ScenarioLineRow>();
      Lines.AllowNew = true;
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
      Lines.AddingNew += (_, e) => e.NewObject = CreateNewLineRow();

      ScenarioPulseSchedule.EnsureSequentialStepIndices(Lines);
      foreach (var l in Lines)
        l.RefreshActionNames(_influenceActions);

      if (Document.LogExpectationColumnSkips == null)
        Document.LogExpectationColumnSkips = new ScenarioLogExpectationColumnSkips();
      ExpectationRows = new ObservableCollection<ScenarioLogExpectationRow>();
      CompareResults = new ObservableCollection<ScenarioLogCompareRow>();
      LoadExpectationsFromDocument();

      Lines.ListChanged += OnLinesListChanged;

      SaveCommand = new RelayCommand(_ => Save(requestCloseAfterSuccess: true));
      CompareWithLogsCommand = new RelayCommand(_ => CompareWithLogs());

      HasUnsavedChanges = false;
    }

    public ScenarioDocument Document { get; }
    public BindingList<ScenarioLineRow> Lines { get; }

    /// <summary>Ожидаемые значения колонок лога (по строкам шагов).</summary>
    public ObservableCollection<ScenarioLogExpectationRow> ExpectationRows { get; }

    /// <summary>Результат сравнения с логами после «Сравнить с логами».</summary>
    public ObservableCollection<ScenarioLogCompareRow> CompareResults { get; }

    public ICommand CompareWithLogsCommand { get; }

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
          // После BindingList.ResetBindings() от Normalize — не вызывать Normalize снова (рекурсия / StackOverflow).
          break;
      }
    }

    private static void NormalizeExpectationRowFields(ScenarioLogExpectationRow r)
    {
      if (r == null) return;
      r.StateText = NormalizeExpectedCell(r.StateText);
      r.StyleText = NormalizeExpectedCell(r.StyleText);
      r.ThemeText = NormalizeExpectedCell(r.ThemeText);
      r.TriggerText = NormalizeExpectedCell(r.TriggerText);
      r.OrUmText = NormalizeExpectedCell(r.OrUmText);
      r.GeneticReflexText = NormalizeExpectedCell(r.GeneticReflexText);
      r.ConditionReflexText = NormalizeExpectedCell(r.ConditionReflexText);
      r.AutomatizmText = NormalizeExpectedCell(r.AutomatizmText);
      r.ReflexChainText = NormalizeExpectedCell(r.ReflexChainText);
      r.AutomatizmChainText = NormalizeExpectedCell(r.AutomatizmChainText);
      r.MainCycleText = NormalizeExpectedCell(r.MainCycleText);
    }

    private static string NormalizeExpectedCell(string s) =>
        string.IsNullOrWhiteSpace(s) ? "-" : s.Trim();

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

    private void CompareWithLogs()
    {
      if (!ScenarioLogComparisonSession.LastAnchorGlobalPulse.HasValue)
      {
        MessageBox.Show(
            "Нет данных о якоре пульса для последнего прогона. Запустите сценарий, дождитесь завершения, затем сравните.",
            "Сравнение с логами",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        return;
      }

      if (ScenarioLogComparisonSession.LastScenarioId.HasValue
          && Document.Header.Id != 0
          && ScenarioLogComparisonSession.LastScenarioId.Value != Document.Header.Id)
      {
        var r = MessageBox.Show(
            "Идентификатор сценария не совпадает с последним прогоном. Продолжить сравнение?",
            "Сравнение с логами",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (r != MessageBoxResult.Yes)
          return;
      }

      SyncExpectationRowsWithLines();
      var doc = BuildDocument();
      var anchor = ScenarioLogComparisonSession.LastAnchorGlobalPulse.Value;
      var agg = ScenarioLogComparer.AggregateByPulse(MemoryLogManager.Instance.LogEntries);
      var list = ScenarioLogComparer.Compare(doc, anchor, agg);
      CompareResults.Clear();
      foreach (var r in list)
      {
        CompareResults.Add(new ScenarioLogCompareRow
        {
          StepIndex = r.StepIndex,
          PulseWithinScenario = r.PulseWithinScenario,
          Ok = r.Ok,
          Details = r.Details ?? ""
        });
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
        HasUnsavedChanges = true;
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

    public event EventHandler<bool> RequestClose;

    /// <summary>Для встроенного редактора: вернуться к реестру (после «Закрыть» с подтверждением).</summary>
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
      var gap = _scenarioEngine.PulseGapBetweenSteps;
      int maxPulse = Lines.Count == 0 ? 0 : Lines.Max(l => l.PulseWithinScenario);
      int nextPulse = maxPulse <= 0 ? 1 : maxPulse + gap + 1;

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

    /// <summary>Удаление выделенных строк (клавиша Delete в таблице шагов).</summary>
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

    public bool Save(bool requestCloseAfterSuccess = true)
    {
      var doc = BuildDocument();
      var err = OperatorScenarioValidator.ValidateDocument(doc, _influenceActions);
      if (err != null)
      {
        MessageBox.Show(err, "Проверка сценария", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
      }

      ScenarioStorage.EnsureFolder();
      if (_isNew)
        doc.Header.Id = ScenarioStorage.NextScenarioId();

      doc.Header.Title = Title?.Trim() ?? "";
      doc.Header.Description = Description?.Trim() ?? "";
      doc.Header.DateText = DateText?.Trim() ?? "";

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

      HasUnsavedChanges = false;
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
          InitialHomeostasisValues = ""
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
