using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using AIStudio.Common;
using ISIDA.Actions;
using ISIDA.Scenarios;
using Microsoft.Win32;

namespace AIStudio.ViewModels.Research
{
  public sealed class ScenarioRunLiveRow
  {
    public int StepIndex { get; set; }
    public int PulseWithinScenario { get; set; }
    public string State { get; set; } = "-";
    public string Style { get; set; } = "-";
    public string Theme { get; set; } = "-";
    public string Trigger { get; set; } = "-";
    public string OrUm { get; set; } = "-";
    public string GeneticReflex { get; set; } = "-";
    public string ConditionReflex { get; set; } = "-";
    public string Automatizm { get; set; } = "-";
    public string ReflexChain { get; set; } = "-";
    public string AutomatizmChain { get; set; } = "-";
    public string MainCycle { get; set; } = "-";
  }

  public sealed class ScenarioRunLiveViewModel
  {
    private readonly InfluenceActionSystem _influenceActions;

    public ScenarioRunLiveViewModel(
        ScenarioDocument scenarioDocument,
        OperatorScenarioCompletedEventArgs completion,
        InfluenceActionSystem influenceActions)
    {
      Document = scenarioDocument ?? throw new ArgumentNullException(nameof(scenarioDocument));
      Completion = completion ?? throw new ArgumentNullException(nameof(completion));
      _influenceActions = influenceActions ?? throw new ArgumentNullException(nameof(influenceActions));
      Rows = new ObservableCollection<ScenarioRunLiveRow>();
      SummaryText = BuildSummaryText(completion);
      PopulateRowsFromMemoryLogs(completion.AnchorGlobalPulse);
      SaveToFileCommand = new RelayCommand(_ => SaveToFile());
    }

    public ScenarioDocument Document { get; }
    public OperatorScenarioCompletedEventArgs Completion { get; }
    public ObservableCollection<ScenarioRunLiveRow> Rows { get; }
    public string SummaryText { get; }
    public ICommand SaveToFileCommand { get; }

    private static string BuildSummaryText(OperatorScenarioCompletedEventArgs e)
    {
      var sb = new StringBuilder();
      if (e.Success)
        sb.AppendLine("Сценарий завершён успешно.");
      else if (e.AbortedByUser)
        sb.AppendLine("Сценарий остановлен (кнопка «Стоп»).");
      else if (e.AbortedByPulsationStop)
        sb.AppendLine("Сценарий прерван: остановлена пульсация.");
      else if (!string.IsNullOrEmpty(e.ErrorMessage))
        sb.AppendLine("Ошибка: " + e.ErrorMessage);
      sb.Append("Последний выполненный пульс внутри сценария: ");
      sb.Append(e.LastExecutedPulseWithinScenario.ToString(CultureInfo.InvariantCulture));
      return sb.ToString().TrimEnd();
    }

    private void PopulateRowsFromMemoryLogs(int anchorPulse)
    {
      Rows.Clear();
      if (Document.Lines == null || Document.Lines.Count == 0)
        return;
      var agg = ScenarioLogComparer.AggregateByPulse(MemoryLogManager.Instance.LogEntries);
      foreach (var line in Document.Lines.OrderBy(l => l.StepIndex))
      {
        int globalPulse = anchorPulse + line.PulseWithinScenario;
        agg.TryGetValue(globalPulse, out var snap);
        snap = snap ?? new ScenarioLogComparer.AggregatedLogSnapshot();
        Rows.Add(new ScenarioRunLiveRow
        {
          StepIndex = line.StepIndex,
          PulseWithinScenario = line.PulseWithinScenario,
          State = snap.State,
          Style = snap.Style,
          Theme = snap.Theme,
          Trigger = snap.Trigger,
          OrUm = snap.OrUm,
          GeneticReflex = snap.GeneticReflex,
          ConditionReflex = snap.ConditionReflex,
          Automatizm = snap.Automatizm,
          ReflexChain = snap.ReflexChain,
          AutomatizmChain = snap.AutomatizmChain,
          MainCycle = snap.MainCycle
        });
      }
    }

    private void SaveToFile()
    {
      var dlg = new SaveFileDialog
      {
        Filter = "Текст (*.txt)|*.txt|Все файлы|*.*",
        FileName = "scenario_result.txt"
      };
      if (dlg.ShowDialog() != true)
        return;
      try
      {
        var outText = new StringBuilder();
        outText.AppendLine(SummaryText);
        outText.AppendLine();
        if (Document != null)
        {
          outText.AppendLine("--- Сценарий (шапка) ---");
          outText.AppendLine("ID: " + Document.Header.Id);
          outText.AppendLine("Название: " + Document.Header.Title);
          outText.AppendLine("Описание: " + Document.Header.Description);
          outText.AppendLine("Дата: " + Document.Header.DateText);
          outText.AppendLine("--- Строки ---");
          foreach (var line in Document.Lines.OrderBy(x => x.StepIndex))
          {
            line.RefreshActionNames(_influenceActions);
            var actionsText = string.IsNullOrEmpty(line.ActionNamesDisplay)
                ? line.ActionIdsText
                : line.ActionNamesDisplay;
            outText.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "Шаг {0}; пульс {1}; {2}; тон={3}; настроение={4}; действия={5}; фраза={6}; сброс ожидания={7}",
                line.StepIndex,
                line.PulseWithinScenario,
                line.Kind == ScenarioLineKind.WaitClick ? "W" : "P",
                line.ToneId,
                line.MoodId,
                actionsText,
                line.Phrase ?? "",
                line.ResetWaitingPeriod));
          }
          outText.AppendLine();
          outText.AppendLine("--- Факт по логам (по пульсам) ---");
          foreach (var r in Rows)
          {
            outText.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "Шаг {0}; пульс {1}; состояние={2}; стиль={3}; тема={4}; триггер={5}; ОР/УМ={6}; б/у={7}; усл={8}; авт={9}; цепРФ={10}; цепАВ={11}; циклМ={12}",
                r.StepIndex, r.PulseWithinScenario,
                r.State, r.Style, r.Theme, r.Trigger, r.OrUm,
                r.GeneticReflex, r.ConditionReflex, r.Automatizm,
                r.ReflexChain, r.AutomatizmChain, r.MainCycle));
          }
        }
        File.WriteAllText(dlg.FileName, outText.ToString(), Encoding.UTF8);
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message, "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }
  }
}
