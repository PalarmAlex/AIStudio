using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using ISIDA.Actions;
using ISIDA.Psychic.Automatism;
using ISIDA.Reflexes;
using ISIDA.Scenarios;

namespace AIStudio.Common
{
  /// <summary>Формирование HTML-отчёта по прогону сценария: сведения из редактора и сравнение ожиданий с фактом по логам.</summary>
  public static class ScenarioReportHtmlBuilder
  {
    public static string BuildHtml(
        ScenarioDocument doc,
        OperatorScenarioCompletedEventArgs completion,
        InfluenceActionSystem influenceActions,
        PerceptionImagesSystem perceptionImages = null,
        AgentLogCellTooltipProvider cellTooltips = null)
    {
      if (doc == null)
        return "<html><body><p>Нет данных сценария.</p></body></html>";

      var sb = new StringBuilder();
      sb.AppendLine("<!DOCTYPE html>");
      sb.AppendLine("<html><head><meta charset=\"utf-8\"/>");
      AppendReportStyles(sb);
      sb.AppendLine("</head><body>");

      sb.AppendLine("<h1>Отчёт по прогону сценария оператора</h1>");
      sb.AppendLine("<p>").Append(Escape(BuildSummaryParagraph(completion))).AppendLine("</p>");
      AppendScenarioReportMainBody(sb, doc, completion, influenceActions, perceptionImages, cellTooltips);

      sb.AppendLine("<p class=\"muted\" style=\"margin-top:24px;font-size:11px;\">Сформировано AIStudio.</p>");
      sb.AppendLine("</body></html>");
      return sb.ToString();
    }

    /// <summary>Сводный HTML-отчёт по пакетному прогону группы сценариев.</summary>
    public static string BuildGroupBatchHtml(
        ScenarioGroupDocument groupDef,
        IReadOnlyList<Tuple<ScenarioDocument, OperatorScenarioCompletedEventArgs>> runs,
        InfluenceActionSystem influenceActions,
        PerceptionImagesSystem perceptionImages = null,
        AgentLogCellTooltipProvider cellTooltips = null)
    {
      if (groupDef == null || runs == null)
        return "<html><body><p>Нет данных группы.</p></body></html>";

      if (groupDef.ReportFormat == ScenarioGroupReportFormat.Compact)
        return BuildGroupBatchHtmlCompact(groupDef, runs, influenceActions, perceptionImages);

      var titleById = BuildScenarioTitleByIdMap(runs);

      var sb = new StringBuilder();
      sb.AppendLine("<!DOCTYPE html>");
      sb.AppendLine("<html><head><meta charset=\"utf-8\"/>");
      AppendReportStyles(sb);
      sb.AppendLine("</head><body>");

      AppendGroupBatchReportHeader(sb, groupDef, runs);

      AppendGroupCompositionTable(sb, groupDef, titleById, compact: false, compareByIndex: null);

      int n = 0;
      foreach (var tuple in runs)
      {
        n++;
        var doc = tuple.Item1;
        var completion = tuple.Item2;
        if (doc == null)
          continue;
        sb.AppendLine("<hr style=\"margin:28px 0;\"/>");
        sb.Append("<h2>Сценарий ").Append(Escape(n.ToString(CultureInfo.InvariantCulture)))
            .Append(": ID ").Append(Escape(doc.Header?.Id.ToString(CultureInfo.InvariantCulture) ?? ""))
            .Append(" — ").Append(Escape(doc.Header?.Title ?? "")).AppendLine("</h2>");
        sb.AppendLine("<p>").Append(Escape(BuildSummaryParagraph(completion))).AppendLine("</p>");
        AppendScenarioReportMainBody(sb, doc, completion, influenceActions, perceptionImages, cellTooltips);
      }

      sb.AppendLine("<p class=\"muted\" style=\"margin-top:24px;font-size:11px;\">Сформировано AIStudio.</p>");
      sb.AppendLine("</body></html>");
      return sb.ToString();
    }

    private static void AppendGroupBatchReportHeader(
        StringBuilder sb,
        ScenarioGroupDocument groupDef,
        IReadOnlyList<Tuple<ScenarioDocument, OperatorScenarioCompletedEventArgs>> runs)
    {
      sb.AppendLine("<h1>Отчёт по групповому прогону сценариев</h1>");
      sb.AppendLine("<h2>Группа</h2>");
      sb.AppendLine("<table class=\"meta-table\">");
      AppendMetaRow(sb, "ID группы", Escape(groupDef.Id.ToString(CultureInfo.InvariantCulture)));
      AppendMetaRow(sb, "Название", Escape(groupDef.Title ?? ""));
      var gdesc = groupDef.Description ?? "";
      var gdescHtml = WebUtility.HtmlEncode(gdesc).Replace("\r\n", "\n").Replace("\n", "<br/>");
      sb.AppendLine("<tr><th class=\"meta-label\">").Append(Escape("Описание")).Append("</th><td class=\"meta-value\">").Append(gdescHtml).AppendLine("</td></tr>");
      int gCoeff = NormalizeScenarioRunPulseCoeff(groupDef.RunPulseTimingCoefficient);
      AppendMetaRow(sb, "Коэфф. пульсации (группа)", Escape(gCoeff.ToString(CultureInfo.InvariantCulture)));
      AppendMetaRow(sb, "Фактическая скорость", Escape(FormatActualPulseSpeedTextAggregate(runs)));
      sb.AppendLine("</table>");
    }

    private static Dictionary<int, string> BuildScenarioTitleByIdMap(
        IReadOnlyList<Tuple<ScenarioDocument, OperatorScenarioCompletedEventArgs>> runs)
    {
      var d = new Dictionary<int, string>();
      foreach (var h in ScenarioStorage.LoadRegistry())
      {
        if (h.Id <= 0 || d.ContainsKey(h.Id))
          continue;
        d[h.Id] = h.Title ?? "";
      }
      if (runs != null)
      {
        foreach (var t in runs)
        {
          var id = t.Item1?.Header?.Id ?? 0;
          if (id <= 0)
            continue;
          var tit = t.Item1.Header.Title;
          if (!string.IsNullOrWhiteSpace(tit))
            d[id] = tit;
        }
      }
      return d;
    }

    private static string ScenarioTitleForMember(ScenarioGroupMemberRow m, Dictionary<int, string> titleById)
    {
      if (titleById.TryGetValue(m.ScenarioId, out var t) && !string.IsNullOrWhiteSpace(t))
        return t;
      return "Сценарий ID " + m.ScenarioId.ToString(CultureInfo.InvariantCulture);
    }

    private static void AppendGroupCompositionTable(
        StringBuilder sb,
        ScenarioGroupDocument groupDef,
        Dictionary<int, string> titleById,
        bool compact,
        List<List<ScenarioLogComparer.StepCompareResult>> compareByIndex)
    {
      sb.AppendLine("<h2>Состав группы (порядок прогона)</h2>");
      var tableClass = compact
          ? "steps-zebra group-compose group-compose-compact"
          : "steps-zebra group-compose";
      sb.Append("<table class=\"").Append(tableClass).AppendLine("\"><colgroup>");
      sb.AppendLine("<col style=\"width:4.5em\"/><col/>");
      sb.AppendLine("<col style=\"width:calc(7em/1.5)\"/><col style=\"width:6em\"/><col style=\"width:4em\"/><col style=\"width:4em\"/><col style=\"width:5.5em\"/>");
      if (compact)
        sb.AppendLine("<col style=\"width:24em\"/>");
      sb.AppendLine("</colgroup>");
      sb.Append("<tr><th>Сорт.</th><th>Сценарий</th><th>Стадия</th><th>Очистка</th><th>Норма</th><th>Набл.</th><th>Авт.зап.</th>");
      if (compact)
        sb.Append("<th>Результат</th>");
      sb.AppendLine("</tr>");

      var ordered = groupDef.Members.OrderBy(x => x.SortOrderInGroup).ThenBy(x => x.ScenarioId).ToList();
      for (int mi = 0; mi < ordered.Count; mi++)
      {
        var m = ordered[mi];
        sb.AppendLine("<tr>");
        sb.Append("<td>").Append(Escape(m.SortOrderInGroup.ToString(CultureInfo.InvariantCulture))).Append("</td>");
        sb.Append("<td>").Append(Escape(ScenarioTitleForMember(m, titleById))).Append("</td>");
        sb.Append("<td>").Append(Escape(ScenarioGroupDocument.FormatPreRunStageShort(m.PreRunTargetStage))).Append("</td>");
        sb.Append("<td>").Append(m.PreRunClearAgentData ? "да" : "нет").Append("</td>");
        sb.Append("<td>").Append(m.PreRunNormalHomeostasisState ? "да" : "нет").Append("</td>");
        sb.Append("<td>").Append(m.ScenarioObservationMode ? "да" : "нет").Append("</td>");
        sb.Append("<td>").Append(m.ScenarioAuthoritativeRecording ? "да" : "нет").Append("</td>");
        if (compact)
        {
          sb.Append("<td>");
          var cmp = compareByIndex[mi];
          if (cmp == null)
            sb.Append("<span class=\"muted\">Нет данных прогона</span>");
          else if (cmp.Any(c => !c.Ok))
            sb.Append("<span class=\"summary-bad\">Обнаружены расхождения</span>");
          else
            sb.Append("<span class=\"summary-ok\">Расхождений не обнаружено</span>");
          sb.Append("</td>");
        }
        sb.AppendLine("</tr>");
      }
      sb.AppendLine("</table>");
    }

    private static List<ScenarioLogComparer.StepCompareResult> BuildCompareResultsForReport(
        ScenarioDocument doc,
        OperatorScenarioCompletedEventArgs completion,
        PerceptionImagesSystem perceptionImages)
    {
      if (doc == null)
        return new List<ScenarioLogComparer.StepCompareResult>();

      int anchor = completion?.AnchorGlobalPulse ?? 0;
      var agg = ScenarioLogComparer.AggregateByPulse(MemoryLogManager.Instance.LogEntries);
      ScenarioReportLogDisplay.RewriteAggregatedStylesToCombinationCodes(agg, perceptionImages);
      return ScenarioLogComparer.Compare(doc, anchor, agg,
          new ScenarioLogComparer.CompareMessageFormatting
          {
            FormatStateFact = ScenarioReportLogDisplay.FormatStateCell
          });
    }

    private static string BuildGroupBatchHtmlCompact(
        ScenarioGroupDocument groupDef,
        IReadOnlyList<Tuple<ScenarioDocument, OperatorScenarioCompletedEventArgs>> runs,
        InfluenceActionSystem _influenceActions,
        PerceptionImagesSystem perceptionImages)
    {
      var orderedMembers = groupDef.Members.OrderBy(x => x.SortOrderInGroup).ThenBy(x => x.ScenarioId).ToList();
      var compareByIndex = new List<List<ScenarioLogComparer.StepCompareResult>>();
      for (int i = 0; i < orderedMembers.Count; i++)
      {
        if (i < runs.Count && runs[i].Item1 != null)
          compareByIndex.Add(BuildCompareResultsForReport(runs[i].Item1, runs[i].Item2, perceptionImages));
        else
          compareByIndex.Add(null);
      }

      var titleById = BuildScenarioTitleByIdMap(runs);

      var sb = new StringBuilder();
      sb.AppendLine("<!DOCTYPE html>");
      sb.AppendLine("<html><head><meta charset=\"utf-8\"/>");
      AppendReportStyles(sb);
      sb.AppendLine("</head><body>");

      AppendGroupBatchReportHeader(sb, groupDef, runs);

      AppendGroupCompositionTable(sb, groupDef, titleById, compact: true, compareByIndex);

      for (int i = 0; i < orderedMembers.Count; i++)
      {
        var cmp = compareByIndex[i];
        if (cmp == null || !cmp.Any(c => !c.Ok))
          continue;
        if (i >= runs.Count || runs[i].Item1 == null)
          continue;

        var doc = runs[i].Item1;
        var completion = runs[i].Item2;
        sb.AppendLine("<hr style=\"margin:28px 0;\"/>");
        sb.Append("<h2>Сценарий ").Append(Escape((i + 1).ToString(CultureInfo.InvariantCulture)))
            .Append(": ID ").Append(Escape(doc.Header?.Id.ToString(CultureInfo.InvariantCulture) ?? ""))
            .Append(" — ").Append(Escape(doc.Header?.Title ?? "")).AppendLine("</h2>");
        sb.AppendLine("<p>").Append(Escape(BuildSummaryParagraph(completion))).AppendLine("</p>");
        AppendComparisonSummary(sb, cmp);
      }

      sb.AppendLine("<p class=\"muted\" style=\"margin-top:24px;font-size:11px;\">Сформировано AIStudio.</p>");
      sb.AppendLine("</body></html>");
      return sb.ToString();
    }

    private static void AppendReportStyles(StringBuilder sb)
    {
      sb.AppendLine("<style>");
      sb.AppendLine("body{font-family:Segoe UI,Tahoma,sans-serif;margin:16px;color:#222;}");
      sb.AppendLine("h1{font-size:18px;color:#1565C0;}");
      sb.AppendLine("h2{font-size:15px;color:#37474F;margin-top:20px;border-bottom:1px solid #B0BEC5;padding-bottom:4px;}");
      sb.AppendLine("table{border-collapse:collapse;width:100%;margin:8px 0;font-size:12px;}");
      sb.AppendLine("th,td{border:1px solid #CFD8DC;padding:6px 8px;text-align:left;vertical-align:top;}");
      sb.AppendLine("th{background:#ECEFF1;font-weight:600;}");
      sb.AppendLine("table.steps-zebra tr:nth-child(even){background:#FAFAFA;}");
      sb.AppendLine("table.group-compose{table-layout:fixed;width:100%;}");
      sb.AppendLine("table.group-compose td:nth-child(1),table.group-compose th:nth-child(1){text-align:center;white-space:nowrap;}");
      sb.AppendLine("table.group-compose td:nth-child(2),table.group-compose th:nth-child(2){word-wrap:break-word;word-break:break-word;}");
      sb.AppendLine("table.group-compose:not(.group-compose-compact) td:nth-child(n+3),table.group-compose:not(.group-compose-compact) th:nth-child(n+3){text-align:center;white-space:nowrap;}");
      sb.AppendLine("table.group-compose.group-compose-compact td:nth-child(n+3):not(:last-child),table.group-compose.group-compose-compact th:nth-child(n+3):not(:last-child){text-align:center;white-space:nowrap;}");
      sb.AppendLine("table.group-compose.group-compose-compact td:last-child,table.group-compose.group-compose-compact th:last-child{white-space:nowrap;text-align:left;}");
      sb.AppendLine(".muted{color:#78909C;}");
      sb.AppendLine("table.meta-table{width:100%;table-layout:fixed;}");
      sb.AppendLine("table.meta-table th.meta-label{width:15%;min-width:100px;max-width:160px;font-size:11px;font-weight:600;padding:6px 8px;vertical-align:top;background:#ECEFF1;}");
      sb.AppendLine("table.meta-table td.meta-value{width:85%;font-size:13px;padding:8px 12px;line-height:1.45;word-wrap:break-word;}");
      sb.AppendLine("table.compare-table{font-size:11px;}");
      sb.AppendLine("table.compare-table th.col-exp-h{background:#E8F5E9;}");
      sb.AppendLine("table.compare-table td.col-exp{background:transparent;}");
      sb.AppendLine("table.compare-table th.col-fact-h,table.compare-table td.col-fact{background:#E3F2FD;}");
      sb.AppendLine("table.compare-table td.col-fact.fact-mismatch{background:#FFEBEE;color:#B71C1C;border-color:#FFCDD2;}");
      sb.AppendLine("table.compare-table td.col-fact span.um-ok{color:#2E7D32;font-weight:600;}");
      sb.AppendLine("table.compare-table td.col-fact span.um-bad{color:#C62828;font-weight:600;}");
      sb.AppendLine("table.compare-table td.col-fact.fact-mismatch span.um-ok{color:#2E7D32;}");
      sb.AppendLine("table.compare-table td.col-fact.fact-mismatch span.um-bad{color:#C62828;}");
      sb.AppendLine(".summary-box{margin-top:20px;padding:14px 16px;border-radius:6px;border:1px solid #CFD8DC;background:#FAFAFA;}");
      sb.AppendLine(".summary-box h3{margin:0 0 10px 0;font-size:14px;color:#37474F;}");
      sb.AppendLine(".summary-ok{color:#2E7D32;font-size:13px;font-weight:600;margin:0;}");
      sb.AppendLine(".summary-bad{color:#C62828;font-size:13px;font-weight:600;margin:0 0 8px 0;}");
      sb.AppendLine(".summary-box ul{margin:6px 0 0 0;padding-left:20px;}");
      sb.AppendLine(".summary-box li{margin:8px 0;font-size:12px;line-height:1.45;}");
      sb.AppendLine("</style>");
    }

    private static bool CellQualifiesForComparisonTooltip(string rawCell)
    {
      var a = ScenarioLogComparer.NormalizeDisplay(rawCell ?? "");
      return a != "-";
    }

    /// <summary>Текст для атрибута title в таблице сравнения (ожид. / факт).</summary>
    private static string GetComparisonCellTooltip(
        string columnLabel,
        bool isFact,
        string expRaw,
        string actRaw,
        ScenarioLogComparer.AggregatedLogSnapshot snap,
        AgentLogCellTooltipProvider tooltips)
    {
      if (tooltips == null)
        return null;
      var raw = isFact ? (actRaw ?? "") : (expRaw ?? "");
      if (!CellQualifiesForComparisonTooltip(raw))
        return null;

      string tip;
      switch (columnLabel)
      {
        case "Состояние":
          tip = AgentLogCellTooltipProvider.GetStateCodeTooltip(raw.Trim());
          break;
        case "Стиль":
          tip = tooltips.GetStyleCellTooltipForReport(raw);
          break;
        case "Тема":
          if (isFact && snap != null && !string.IsNullOrWhiteSpace(snap.ThemeTooltip))
            tip = snap.ThemeTooltip.Trim();
          else
            tip = tooltips.GetThinkingThemeTypeTooltip(raw);
          if (string.IsNullOrWhiteSpace(tip))
            return null;
          break;
        case "Триггер":
          tip = tooltips.GetTriggerTooltip(raw.Trim());
          break;
        case "ОР/УМ":
          tip = tooltips.GetOrUmTooltip(raw.Trim(), isFact ? snap?.OrUmThinkingSuccess : null);
          break;
        case "Опасно":
        case "Актуально":
          if (ScenarioLogComparer.NormalizeDisplay(raw) != "1")
            return null;
          tip = columnLabel == "Опасно" ? "Опасная ситуация" : "Актуальная ситуация";
          break;
        case "Б/у рефлекс":
          tip = tooltips.GetActionsForGeneticReflex(raw.Trim());
          break;
        case "Усл. рефлекс":
          tip = tooltips.GetActionsForConditionReflex(raw.Trim());
          break;
        case "Автоматизм":
          tip = tooltips.GetAutomatizmTooltip(raw.Trim());
          break;
        case "Цепочка РФ":
          tip = tooltips.GetReflexChainTooltip(raw.Trim());
          break;
        case "Цепочка АВ":
          tip = tooltips.GetAutomatizmChainTooltip(raw.Trim());
          break;
        case "Цикл М":
          if (!isFact || snap == null || string.IsNullOrWhiteSpace(snap.MainCycleTooltip))
            return null;
          tip = snap.MainCycleTooltip.Trim();
          break;
        default:
          return null;
      }

      if (string.IsNullOrWhiteSpace(tip))
        return null;
      return tip;
    }

    public static void AppendScenarioReportMainBody(
        StringBuilder sb,
        ScenarioDocument doc,
        OperatorScenarioCompletedEventArgs completion,
        InfluenceActionSystem influenceActions,
        PerceptionImagesSystem perceptionImages,
        AgentLogCellTooltipProvider cellTooltips = null)
    {
      sb.AppendLine("<h2>Данные сценария</h2>");
      sb.AppendLine("<table class=\"meta-table\">");
      AppendMetaRow(sb, "ID", Escape(doc.Header?.Id.ToString(CultureInfo.InvariantCulture) ?? ""));
      AppendMetaRow(sb, "Название", Escape(doc.Header?.Title ?? ""));
      var descRaw = doc.Header?.Description ?? "";
      var descHtml = WebUtility.HtmlEncode(descRaw).Replace("\r\n", "\n").Replace("\n", "<br/>");
      sb.AppendLine("<tr><th class=\"meta-label\">").Append(Escape("Описание")).Append("</th><td class=\"meta-value\">").Append(descHtml).AppendLine("</td></tr>");
      AppendMetaRow(sb, "Переход перед запуском (стадия)",
          Escape(doc.Header != null && doc.Header.PreRunTargetStage >= 0 && doc.Header.PreRunTargetStage <= 5
              ? doc.Header.PreRunTargetStage.ToString(CultureInfo.InvariantCulture)
              : "не менять"));
      AppendMetaRow(sb, "Очистка данных при переходе", Escape(doc.Header?.PreRunClearAgentData == true ? "да" : "нет"));
      AppendMetaRow(sb, "Сброс параметров в «норму» перед запуском", Escape(doc.Header?.PreRunNormalHomeostasisState == true ? "да" : "нет"));
      AppendMetaRow(sb, "Режим наблюдения при прогоне", Escape(doc.Header?.ScenarioObservationMode == true ? "да" : "нет"));
      AppendMetaRow(sb, "Авторитарная запись при прогоне", Escape(doc.Header?.ScenarioAuthoritativeRecording == true ? "да" : "нет"));
      int pulseCoeff = NormalizeScenarioRunPulseCoeff(doc.Header?.RunPulseTimingCoefficient ?? 1);
      AppendMetaRow(sb, "Коэфф. пульсации", Escape(pulseCoeff.ToString(CultureInfo.InvariantCulture)));
      AppendMetaRow(sb, "Фактическая скорость", Escape(FormatActualPulseSpeedText(completion)));
      sb.AppendLine("</table>");

      sb.AppendLine("<h2>Шаги</h2>");
      sb.AppendLine("<table class=\"steps-zebra\"><tr><th>Шаг</th><th>№ пульса</th><th>Тип</th><th>Воздействия</th><th>Фраза</th><th>Тон</th><th>Настр.</th><th>Цвет</th><th>Сброс ожид.</th></tr>");
      if (doc.Lines != null)
      {
        foreach (var line in doc.Lines.OrderBy(l => l.StepIndex))
        {
          line.RefreshActionNames(influenceActions);
          var actions = string.IsNullOrEmpty(line.ActionNamesDisplay) ? line.ActionIdsText : line.ActionNamesDisplay;
          sb.AppendLine("<tr>");
          sb.Append("<td>").Append(Escape(line.StepIndex.ToString(CultureInfo.InvariantCulture))).Append("</td>");
          sb.Append("<td>").Append(Escape(line.PulseWithinScenario.ToString(CultureInfo.InvariantCulture))).Append("</td>");
          sb.Append("<td>").Append(Escape(line.Kind == ScenarioLineKind.WaitClick ? "Ожидание" : "Пульт")).Append("</td>");
          sb.Append("<td>").Append(Escape(actions)).Append("</td>");
          sb.Append("<td>").Append(Escape(line.Phrase ?? "")).Append("</td>");
          sb.Append("<td>").Append(Escape(FormatToneCell(line.ToneId))).Append("</td>");
          sb.Append("<td>").Append(Escape(FormatMoodCell(line.MoodId))).Append("</td>");
          sb.Append("<td>").Append(Escape(FormatVisualColorCell(line.VisualColorId))).Append("</td>");
          sb.Append("<td>").Append(line.ResetWaitingPeriod ? "да" : "нет").Append("</td>");
          sb.AppendLine("</tr>");
        }
      }
      sb.AppendLine("</table>");

      int anchor = completion?.AnchorGlobalPulse ?? 0;
      var agg = ScenarioLogComparer.AggregateByPulse(MemoryLogManager.Instance.LogEntries);
      ScenarioReportLogDisplay.RewriteAggregatedStylesToCombinationCodes(agg, perceptionImages);
      var expectations = doc.LogExpectations ?? new List<ScenarioLogExpectationRow>();

      sb.AppendLine("<h2>Сравнение ожидаемых реакций и факта по логам</h2>");
      sb.AppendLine("<p class=\"muted\">Якорный глобальный пульс: ")
          .Append(Escape(anchor.ToString(CultureInfo.InvariantCulture)))
          .Append(". Глобальный пульс шага = якорь + № пульса внутри сценария. ");

      sb.AppendLine("<table class=\"compare-table\"><tr><th>Шаг</th><th>№ пульса</th>");
      foreach (var col in ComparisonColumnSpecs)
      {
        sb.Append("<th class=\"col-exp-h\">").Append(Escape(col.Label + " (ожид.)")).Append("</th>");
        sb.Append("<th class=\"col-fact-h\">").Append(Escape(col.Label + " (факт)")).Append("</th>");
      }
      sb.AppendLine("</tr>");

      var compareList = ScenarioLogComparer.Compare(doc, anchor, agg,
          new ScenarioLogComparer.CompareMessageFormatting
          {
            FormatStateFact = ScenarioReportLogDisplay.FormatStateCell
          });

      if (doc.Lines != null)
      {
        foreach (var line in doc.Lines.OrderBy(l => l.StepIndex))
        {
          var snap = ScenarioLogComparer.ResolveSnapshot(anchor + line.PulseWithinScenario, agg);
          var exp = expectations.FirstOrDefault(e => e.StepIndex == line.StepIndex && e.PulseWithinScenario == line.PulseWithinScenario)
              ?? expectations.FirstOrDefault(e => e.StepIndex == line.StepIndex)
              ?? new ScenarioLogExpectationRow();

          sb.AppendLine("<tr>");
          sb.Append("<td>").Append(Escape(line.StepIndex.ToString(CultureInfo.InvariantCulture))).Append("</td>");
          sb.Append("<td>").Append(Escape(line.PulseWithinScenario.ToString(CultureInfo.InvariantCulture))).Append("</td>");
          foreach (var col in ComparisonColumnSpecs)
          {
            var expRaw = col.GetExpectedMain(exp);
            var actRaw = col.GetActual(snap);
            var expDisp = expRaw;
            var actDisp = actRaw;
            var factCellHtmlRaw = false;
            string factCellHtml = null;
            if (col.Label == "Состояние")
            {
              expDisp = ScenarioReportLogDisplay.FormatStateCell(expRaw);
              actDisp = ScenarioReportLogDisplay.FormatStateCell(actRaw);
            }
            else if (col.Label == "Опасно")
            {
              expDisp = ScenarioReportLogDisplay.FormatDangerComparisonCell(expRaw);
              actDisp = ScenarioReportLogDisplay.FormatDangerComparisonCell(actRaw);
            }
            else if (col.Label == "Актуально")
            {
              expDisp = ScenarioReportLogDisplay.FormatVeryActualComparisonCell(expRaw);
              actDisp = ScenarioReportLogDisplay.FormatVeryActualComparisonCell(actRaw);
            }
            else if (col.Label == "ОР/УМ")
            {
              factCellHtmlRaw = true;
              factCellHtml = ScenarioReportLogDisplay.FormatOrUmFactCellHtml(actRaw, snap.OrUmThinkingSuccess);
            }
            var expTitle = GetComparisonCellTooltip(col.Label, isFact: false, expRaw, actRaw, snap, cellTooltips);
            sb.Append("<td class=\"col-exp\"");
            if (!string.IsNullOrEmpty(expTitle))
              sb.Append(" title=\"").Append(EscapeForHtmlTitleAttribute(expTitle)).Append("\"");
            sb.Append(">").Append(Escape(expDisp)).Append("</td>");
            var factClass = "col-fact" + (IsFieldMismatch(expRaw, actRaw, col.Label) ? " fact-mismatch" : "");
            var factTitle = GetComparisonCellTooltip(col.Label, isFact: true, expRaw, actRaw, snap, cellTooltips);
            sb.Append("<td class=\"").Append(factClass).Append("\"");
            if (!string.IsNullOrEmpty(factTitle))
              sb.Append(" title=\"").Append(EscapeForHtmlTitleAttribute(factTitle)).Append("\"");
            sb.Append(">");
            if (factCellHtmlRaw)
              sb.Append(factCellHtml);
            else
              sb.Append(Escape(actDisp));
            sb.Append("</td>");
          }
          sb.AppendLine("</tr>");
        }
      }
      sb.AppendLine("</table>");

      AppendComparisonSummary(sb, compareList);
    }

    /// <summary>Та же логика, что <see cref="ScenarioLogComparer.Compare"/> (поле проверяется только если ожидание не пустое).</summary>
    private static bool IsFieldMismatch(string expectedRaw, string actualVal, string columnLabel)
    {
      if (string.IsNullOrWhiteSpace(expectedRaw))
        return false;
      if (columnLabel == "Опасно")
        return !ScenarioLogComparer.DangerExpectationMatches(expectedRaw, actualVal);
      if (columnLabel == "Актуально")
        return !ScenarioLogComparer.VeryActualExpectationMatches(expectedRaw, actualVal);
      var a = ScenarioLogComparer.NormalizeDisplay(actualVal ?? "");
      return !ScenarioLogComparer.ExpectationCellMatches(expectedRaw, a);
    }

    private static void AppendComparisonSummary(StringBuilder sb, List<ScenarioLogComparer.StepCompareResult> compareList)
    {
      var failed = compareList.Where(c => !c.Ok).ToList();
      sb.AppendLine("<div class=\"summary-box\">");
      sb.AppendLine("<h3>Итог сравнения ожиданий с логами</h3>");
      if (failed.Count == 0)
      {
        sb.AppendLine("<p class=\"summary-ok\">Расхождений не обнаружено.</p>");
      }
      else
      {
        sb.AppendLine("<p class=\"summary-bad\">Обнаружены расхождения в строках:</p>");
        sb.AppendLine("<ul>");
        foreach (var c in failed)
        {
          sb.Append("<li><strong>Шаг ").Append(Escape(c.StepIndex.ToString(CultureInfo.InvariantCulture))).Append(", № пульса ")
              .Append(Escape(c.PulseWithinScenario.ToString(CultureInfo.InvariantCulture))).Append(".</strong> ")
              .Append(Escape(c.Details))
              .AppendLine("</li>");
        }
        sb.AppendLine("</ul>");
      }
      sb.AppendLine("</div>");
    }

    private sealed class ComparisonColumnSpec
    {
      public string Label { get; set; }
      public Func<ScenarioLogExpectationRow, string> GetExpectedMain { get; set; }
      public Func<ScenarioLogComparer.AggregatedLogSnapshot, string> GetActual { get; set; }
    }

    private static readonly ComparisonColumnSpec[] ComparisonColumnSpecs =
    {
      new ComparisonColumnSpec { Label = "Состояние", GetExpectedMain = e => e.StateText ?? "", GetActual = a => a.State },
      new ComparisonColumnSpec { Label = "Стиль", GetExpectedMain = e => e.StyleText ?? "", GetActual = a => a.Style },
      new ComparisonColumnSpec { Label = "Тема", GetExpectedMain = e => e.ThemeText ?? "", GetActual = a => a.Theme },
      new ComparisonColumnSpec { Label = "Триггер", GetExpectedMain = e => e.TriggerText ?? "", GetActual = a => a.Trigger },
      new ComparisonColumnSpec { Label = "ОР/УМ", GetExpectedMain = e => e.OrUmText ?? "", GetActual = a => a.OrUm },
      new ComparisonColumnSpec { Label = "Опасно", GetExpectedMain = e => e.DangerText ?? "", GetActual = a => a.Danger },
      new ComparisonColumnSpec { Label = "Актуально", GetExpectedMain = e => e.VeryActualText ?? "", GetActual = a => a.VeryActual },
      new ComparisonColumnSpec { Label = "Б/у рефлекс", GetExpectedMain = e => e.GeneticReflexText ?? "", GetActual = a => a.GeneticReflex },
      new ComparisonColumnSpec { Label = "Усл. рефлекс", GetExpectedMain = e => e.ConditionReflexText ?? "", GetActual = a => a.ConditionReflex },
      new ComparisonColumnSpec { Label = "Автоматизм", GetExpectedMain = e => e.AutomatizmText ?? "", GetActual = a => a.Automatizm },
      new ComparisonColumnSpec { Label = "Цепочка РФ", GetExpectedMain = e => e.ReflexChainText ?? "", GetActual = a => a.ReflexChain },
      new ComparisonColumnSpec { Label = "Цепочка АВ", GetExpectedMain = e => e.AutomatizmChainText ?? "", GetActual = a => a.AutomatizmChain },
      new ComparisonColumnSpec { Label = "Цикл М", GetExpectedMain = e => e.MainCycleText ?? "", GetActual = a => a.MainCycle }
    };

    private static void AppendMetaRow(StringBuilder sb, string name, string valueHtmlEscaped)
    {
      sb.AppendLine("<tr><th class=\"meta-label\">").Append(Escape(name)).Append("</th><td class=\"meta-value\">").Append(valueHtmlEscaped).AppendLine("</td></tr>");
    }

    private static string BuildSummaryParagraph(OperatorScenarioCompletedEventArgs e)
    {
      if (e == null)
        return "Нет данных о завершении.";
      if (e.Success)
        return "Сценарий завершён успешно. Последний выполненный № пульса: "
            + e.LastExecutedPulseWithinScenario.ToString(CultureInfo.InvariantCulture) + ".";
      if (e.AbortedByUser)
        return "Сценарий остановлен пользователем (кнопка «Стоп»).";
      if (e.AbortedByPulsationStop)
        return "Сценарий прерван: остановлена пульсация.";
      if (!string.IsNullOrEmpty(e.ErrorMessage))
        return "Ошибка: " + e.ErrorMessage;
      return "Прогон завершён с нестандартным исходом.";
    }

    /// <summary>Допустимые значения коэфф. пульсации в UI/прогоне (как в MainViewModel).</summary>
    private static int NormalizeScenarioRunPulseCoeff(int c)
    {
      if (c != 1 && c != 5 && c != 10 && c != 20)
        return 1;
      return c;
    }

    private static double? TryComputeActualPulsesPerSec(OperatorScenarioCompletedEventArgs e)
    {
      if (e == null || e.ElapsedWallTime.TotalMilliseconds <= 0 || e.ElapsedPulses <= 0)
        return null;
      return e.ElapsedPulses / e.ElapsedWallTime.TotalSeconds;
    }

    private static string FormatActualPulseSpeedText(OperatorScenarioCompletedEventArgs completion)
    {
      var a = TryComputeActualPulsesPerSec(completion);
      return a.HasValue
          ? $"{a.Value.ToString("F1", CultureInfo.InvariantCulture)} пульсов/сек"
          : "—";
    }

    private static string FormatActualPulseSpeedTextAggregate(
        IReadOnlyList<Tuple<ScenarioDocument, OperatorScenarioCompletedEventArgs>> runs)
    {
      if (runs == null || runs.Count == 0)
        return "—";
      double totalSec = 0;
      int totalPulses = 0;
      foreach (var t in runs)
      {
        var e = t.Item2;
        if (e == null)
          continue;
        if (e.ElapsedWallTime.TotalMilliseconds > 0 && e.ElapsedPulses > 0)
        {
          totalSec += e.ElapsedWallTime.TotalSeconds;
          totalPulses += e.ElapsedPulses;
        }
      }
      if (totalSec <= 0)
        return "—";
      return $"{(totalPulses / totalSec).ToString("F1", CultureInfo.InvariantCulture)} пульсов/сек";
    }

    private static string Escape(string s) => WebUtility.HtmlEncode(s ?? "");

    /// <summary>Кодирует текст для атрибута title: WebUtility.HtmlEncode и замена LF на числовую сущность перевода строки для многострочной подсказки в типичных браузерах.</summary>
    private static string EscapeForHtmlTitleAttribute(string s)
    {
      if (string.IsNullOrEmpty(s))
        return "";
      var normalized = s.Replace("\r\n", "\n").Replace('\r', '\n');
      var enc = WebUtility.HtmlEncode(normalized);
      return enc.Replace("\n", "&#10;");
    }

    private static string FormatToneCell(int toneId)
    {
      var t = ActionsImagesSystem.GetToneText(toneId);
      return string.IsNullOrEmpty(t)
          ? toneId.ToString(CultureInfo.InvariantCulture)
          : t + " (" + toneId.ToString(CultureInfo.InvariantCulture) + ")";
    }

    private static string FormatMoodCell(int moodId)
    {
      var t = ActionsImagesSystem.GetMoodText(moodId);
      return string.IsNullOrEmpty(t)
          ? moodId.ToString(CultureInfo.InvariantCulture)
          : t + " (" + moodId.ToString(CultureInfo.InvariantCulture) + ")";
    }

    private static string FormatVisualColorCell(int colorId)
    {
      int code = AgentVisualColor.IsValidCode(colorId) ? colorId : AgentVisualColor.White;
      return AgentVisualColor.GetDisplayName(code) + " (" + code.ToString(CultureInfo.InvariantCulture) + ")";
    }
  }
}
