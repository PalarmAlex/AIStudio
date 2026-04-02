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
        PerceptionImagesSystem perceptionImages = null)
    {
      if (doc == null)
        return "<html><body><p>Нет данных сценария.</p></body></html>";

      var sb = new StringBuilder();
      sb.AppendLine("<!DOCTYPE html>");
      sb.AppendLine("<html><head><meta charset=\"utf-8\"/>");
      sb.AppendLine("<style>");
      sb.AppendLine("body{font-family:Segoe UI,Tahoma,sans-serif;margin:16px;color:#222;}");
      sb.AppendLine("h1{font-size:18px;color:#1565C0;}");
      sb.AppendLine("h2{font-size:15px;color:#37474F;margin-top:20px;border-bottom:1px solid #B0BEC5;padding-bottom:4px;}");
      sb.AppendLine("table{border-collapse:collapse;width:100%;margin:8px 0;font-size:12px;}");
      sb.AppendLine("th,td{border:1px solid #CFD8DC;padding:6px 8px;text-align:left;vertical-align:top;}");
      sb.AppendLine("th{background:#ECEFF1;font-weight:600;}");
      sb.AppendLine("table.steps-zebra tr:nth-child(even){background:#FAFAFA;}");
      sb.AppendLine(".muted{color:#78909C;}");
      sb.AppendLine("table.meta-table{width:100%;table-layout:fixed;}");
      sb.AppendLine("table.meta-table th.meta-label{width:15%;min-width:100px;max-width:160px;font-size:11px;font-weight:600;padding:6px 8px;vertical-align:top;background:#ECEFF1;}");
      sb.AppendLine("table.meta-table td.meta-value{width:85%;font-size:13px;padding:8px 12px;line-height:1.45;word-wrap:break-word;}");
      sb.AppendLine("table.compare-table{font-size:11px;}");
      sb.AppendLine("table.compare-table th.col-exp-h{background:#E8F5E9;}");
      sb.AppendLine("table.compare-table td.col-exp{background:transparent;}");
      sb.AppendLine("table.compare-table th.col-fact-h,table.compare-table td.col-fact{background:#E3F2FD;}");
      sb.AppendLine("table.compare-table td.col-fact.fact-mismatch{background:#FFEBEE;color:#B71C1C;border-color:#FFCDD2;}");
      sb.AppendLine(".summary-box{margin-top:20px;padding:14px 16px;border-radius:6px;border:1px solid #CFD8DC;background:#FAFAFA;}");
      sb.AppendLine(".summary-box h3{margin:0 0 10px 0;font-size:14px;color:#37474F;}");
      sb.AppendLine(".summary-ok{color:#2E7D32;font-size:13px;font-weight:600;margin:0;}");
      sb.AppendLine(".summary-bad{color:#C62828;font-size:13px;font-weight:600;margin:0 0 8px 0;}");
      sb.AppendLine(".summary-box ul{margin:6px 0 0 0;padding-left:20px;}");
      sb.AppendLine(".summary-box li{margin:8px 0;font-size:12px;line-height:1.45;}");
      sb.AppendLine("</style></head><body>");

      sb.AppendLine("<h1>Отчёт по прогону сценария оператора</h1>");
      sb.AppendLine("<p>").Append(Escape(BuildSummaryParagraph(completion))).AppendLine("</p>");

      sb.AppendLine("<h2>Данные сценария</h2>");
      sb.AppendLine("<table class=\"meta-table\">");
      AppendMetaRow(sb, "ID", Escape(doc.Header?.Id.ToString(CultureInfo.InvariantCulture) ?? ""));
      AppendMetaRow(sb, "Дата", Escape(doc.Header?.DateText ?? ""));
      AppendMetaRow(sb, "Номер группы", Escape(doc.Header?.GroupNumber.ToString(CultureInfo.InvariantCulture) ?? ""));
      AppendMetaRow(sb, "Сортировка в группе", Escape(doc.Header?.SortOrderInGroup.ToString(CultureInfo.InvariantCulture) ?? ""));
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
      sb.AppendLine("</table>");

      sb.AppendLine("<h2>Шаги</h2>");
      sb.AppendLine("<table class=\"steps-zebra\"><tr><th>Шаг</th><th>№ пульса</th><th>Тип</th><th>Воздействия</th><th>Фраза</th><th>Тон</th><th>Настр.</th><th>Сброс ожид.</th></tr>");
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
            if (col.Label == "Состояние")
            {
              expDisp = ScenarioReportLogDisplay.FormatStateCell(expRaw);
              actDisp = ScenarioReportLogDisplay.FormatStateCell(actRaw);
            }
            sb.Append("<td class=\"col-exp\">").Append(Escape(expDisp)).Append("</td>");
            var factClass = "col-fact" + (IsFieldMismatch(expRaw, actRaw) ? " fact-mismatch" : "");
            sb.Append("<td class=\"").Append(factClass).Append("\">").Append(Escape(actDisp)).Append("</td>");
          }
          sb.AppendLine("</tr>");
        }
      }
      sb.AppendLine("</table>");

      AppendComparisonSummary(sb, compareList);

      sb.AppendLine("<p class=\"muted\" style=\"margin-top:24px;font-size:11px;\">Сформировано AIStudio.</p>");
      sb.AppendLine("</body></html>");
      return sb.ToString();
    }

    /// <summary>Та же логика, что <see cref="ScenarioLogComparer.Compare"/> (поле проверяется только если ожидание не пустое).</summary>
    private static bool IsFieldMismatch(string expectedRaw, string actualVal)
    {
      if (string.IsNullOrWhiteSpace(expectedRaw))
        return false;
      var e = expectedRaw.Trim();
      var a = ScenarioLogComparer.NormalizeDisplay(actualVal ?? "");
      return !string.Equals(e, a, StringComparison.Ordinal);
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
          sb.Append("<li><strong>Шаг ").Append(Escape(c.StepIndex.ToString(CultureInfo.InvariantCulture))).Append(", пульс внутри сценария ")
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
        return "Сценарий завершён успешно. Последний выполненный пульс внутри сценария: "
            + e.LastExecutedPulseWithinScenario.ToString(CultureInfo.InvariantCulture) + ".";
      if (e.AbortedByUser)
        return "Сценарий остановлен пользователем (кнопка «Стоп»).";
      if (e.AbortedByPulsationStop)
        return "Сценарий прерван: остановлена пульсация.";
      if (!string.IsNullOrEmpty(e.ErrorMessage))
        return "Ошибка: " + e.ErrorMessage;
      return "Прогон завершён с нестандартным исходом.";
    }

    private static string Escape(string s) => WebUtility.HtmlEncode(s ?? "");

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
  }
}
