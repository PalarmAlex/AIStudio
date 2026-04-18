using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace AIStudio.Common
{
  /// <summary>Формат HTML-отчёта после группового прогона.</summary>
  public enum ScenarioGroupReportFormat
  {
    /// <summary>Полные данные и сравнение по каждому сценарию.</summary>
    Detailed = 0,
    /// <summary>Состав группы с колонкой «Результат» и блоки расхождений только при несовпадениях.</summary>
    Compact = 1
  }

  /// <summary>Пункт выбора формата отчёта в комбобоксе редактора группы.</summary>
  public sealed class ScenarioGroupReportFormatItem
  {
    public ScenarioGroupReportFormat Format { get; set; }
    public string Display { get; set; } = "";
  }

  /// <summary>Запись реестра групп сценариев (как у сценария — только идентификация в списке).</summary>
  public sealed class ScenarioGroupHeader
  {
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";

    private const int DescriptionDisplayLimit = 100;

    public string DescriptionShort
    {
      get
      {
        if (string.IsNullOrEmpty(Description))
          return "";
        var flat = Description.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
        return flat.Length <= DescriptionDisplayLimit
            ? flat
            : flat.Substring(0, DescriptionDisplayLimit) + "…";
      }
    }

  }

  /// <summary>Один сценарий в составе группы: порядок, id и параметры предзапуска для группового прогона.</summary>
  public sealed class ScenarioGroupMemberRow : INotifyPropertyChanged
  {
    private int _sortOrderInGroup;
    private int _scenarioId;
    private int _preRunTargetStage = -1;
    private bool _preRunClearAgentData;
    private bool _preRunNormalHomeostasisState;
    private bool _scenarioObservationMode;
    private bool _scenarioAuthoritativeRecording;
    private string _scenarioCellDisplay = "(не выбран)";
    private string _scenarioSelectionToolTip = "";
    private string _stageSelectionToolTip = "";

    public int SortOrderInGroup
    {
      get => _sortOrderInGroup;
      set { if (_sortOrderInGroup == value) return; _sortOrderInGroup = value; OnPropertyChanged(); }
    }

    public int ScenarioId
    {
      get => _scenarioId;
      set { if (_scenarioId == value) return; _scenarioId = value; OnPropertyChanged(); }
    }

    public int PreRunTargetStage
    {
      get => _preRunTargetStage;
      set { if (_preRunTargetStage == value) return; _preRunTargetStage = value; OnPropertyChanged(); }
    }

    public bool PreRunClearAgentData
    {
      get => _preRunClearAgentData;
      set { if (_preRunClearAgentData == value) return; _preRunClearAgentData = value; OnPropertyChanged(); }
    }

    public bool PreRunNormalHomeostasisState
    {
      get => _preRunNormalHomeostasisState;
      set { if (_preRunNormalHomeostasisState == value) return; _preRunNormalHomeostasisState = value; OnPropertyChanged(); }
    }

    public bool ScenarioObservationMode
    {
      get => _scenarioObservationMode;
      set { if (_scenarioObservationMode == value) return; _scenarioObservationMode = value; OnPropertyChanged(); }
    }

    public bool ScenarioAuthoritativeRecording
    {
      get => _scenarioAuthoritativeRecording;
      set { if (_scenarioAuthoritativeRecording == value) return; _scenarioAuthoritativeRecording = value; OnPropertyChanged(); }
    }

    /// <summary>Краткая подпись сценария в ячейке таблицы (ID и название).</summary>
    public string ScenarioCellDisplay
    {
      get => _scenarioCellDisplay;
      private set
      {
        if (_scenarioCellDisplay == value) return;
        _scenarioCellDisplay = value;
        OnPropertyChanged();
      }
    }

    /// <summary>Полное название выбранного сценария для подсказки в таблице.</summary>
    public string ScenarioSelectionToolTip
    {
      get => _scenarioSelectionToolTip;
      private set
      {
        if (_scenarioSelectionToolTip == value) return;
        _scenarioSelectionToolTip = value;
        OnPropertyChanged();
      }
    }

    /// <summary>Полное описание выбранной стадии для подсказки в таблице.</summary>
    public string StageSelectionToolTip
    {
      get => _stageSelectionToolTip;
      private set
      {
        if (_stageSelectionToolTip == value) return;
        _stageSelectionToolTip = value;
        OnPropertyChanged();
      }
    }

    internal void UpdateMemberPresentation(string scenarioCellDisplay, string scenarioFullTitle, string stageDescription)
    {
      ScenarioCellDisplay = string.IsNullOrEmpty(scenarioCellDisplay) ? "(не выбран)" : scenarioCellDisplay;
      ScenarioSelectionToolTip = scenarioFullTitle ?? "";
      StageSelectionToolTip = stageDescription ?? "";
    }

    public ScenarioGroupMemberRow Clone()
    {
      return new ScenarioGroupMemberRow
      {
        SortOrderInGroup = SortOrderInGroup,
        ScenarioId = ScenarioId,
        PreRunTargetStage = PreRunTargetStage,
        PreRunClearAgentData = PreRunClearAgentData,
        PreRunNormalHomeostasisState = PreRunNormalHomeostasisState,
        ScenarioObservationMode = ScenarioObservationMode,
        ScenarioAuthoritativeRecording = ScenarioAuthoritativeRecording
      };
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
  }

  /// <summary>Полное описание группы: шапка (файл группы) и список сценариев.</summary>
  public sealed class ScenarioGroupDocument
  {
    public const int GroupLinesFileFormatVersion = 1;

    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    /// <summary>Коэфф. ускорения пульса для всех сценариев группы (как в одиночном сценарии).</summary>
    public int RunPulseTimingCoefficient { get; set; } = 1;

    /// <summary>Вид сводного отчёта после группового прогона.</summary>
    public ScenarioGroupReportFormat ReportFormat { get; set; } = ScenarioGroupReportFormat.Detailed;

    public List<ScenarioGroupMemberRow> Members { get; set; } = new List<ScenarioGroupMemberRow>();

    public ScenarioGroupDocument Clone()
    {
      return new ScenarioGroupDocument
      {
        Id = Id,
        Title = Title ?? "",
        Description = Description ?? "",
        RunPulseTimingCoefficient = RunPulseTimingCoefficient,
        ReportFormat = ReportFormat,
        Members = Members == null ? new List<ScenarioGroupMemberRow>() : Members.ConvertAll(m => m.Clone())
      };
    }

    /// <summary>Копия документа сценария с подставленными полями группового слота и общим коэффициентом пульсации.</summary>
    public static ISIDA.Scenarios.ScenarioDocument ApplyMemberToScenario(
        ISIDA.Scenarios.ScenarioDocument source,
        ScenarioGroupMemberRow member,
        int groupRunPulseTimingCoefficient)
    {
      var doc = source.Clone();
      doc.Header.PreRunTargetStage = member.PreRunTargetStage;
      doc.Header.PreRunClearAgentData = member.PreRunClearAgentData;
      doc.Header.PreRunNormalHomeostasisState = member.PreRunNormalHomeostasisState;
      doc.Header.ScenarioObservationMode = member.ScenarioObservationMode;
      doc.Header.ScenarioAuthoritativeRecording = member.ScenarioAuthoritativeRecording;
      int c = groupRunPulseTimingCoefficient;
      if (c == 1 || c == 5 || c == 10 || c == 20 || c == 50 || c == 100)
        doc.Header.RunPulseTimingCoefficient = c;
      return doc;
    }

    public static string FormatPreRunStageShort(int stage)
    {
      if (stage >= 0 && stage <= 5)
        return stage.ToString(CultureInfo.InvariantCulture);
      return "не менять";
    }
  }
}
