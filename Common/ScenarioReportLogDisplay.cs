using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using ISIDA.Reflexes;

namespace AIStudio.Common
{
  /// <summary>Представление значений из лога в HTML-отчёте.</summary>
  public static class ScenarioReportLogDisplay
  {
    private static string NormalizeCell(string raw)
    {
      if (string.IsNullOrWhiteSpace(raw))
        return "-";
      var t = raw.Trim();
      return t.Length == 0 ? "-" : t;
    }

    /// <summary>Коды базового состояния из лога: -1 / 0 / 1 → Плохо / Норма / Хорошо.</summary>
    public static string FormatStateCell(string raw)
    {
      var a = NormalizeCell(raw ?? "");
      if (a == "-")
        return "-";
      switch (a)
      {
        case "-1":
          return "Плохо";
        case "0":
          return "Норма";
        case "1":
          return "Хорошо";
        default:
          return a;
      }
    }

    /// <summary>Колонка «Опасно» в отчёте: только «1» при опасности, иначе «-».</summary>
    public static string FormatDangerComparisonCell(string raw)
    {
      var a = NormalizeCell(raw ?? "");
      if (a == "1")
        return "1";
      return "-";
    }

    /// <summary>Колонка «Актуально» в отчёте: та же визуализация, что у «Опасно» («1» / «-»).</summary>
    public static string FormatVeryActualComparisonCell(string raw) => FormatDangerComparisonCell(raw);

    /// <summary>HTML ячейки «факт» для «ОР/УМ»: «УМ1»/«УМ2» зелёным при успехе, иначе красным; остальное без разметки.</summary>
    public static string FormatOrUmFactCellHtml(string raw, bool? thinkingLevelSuccess)
    {
      var a = NormalizeCell(raw ?? "");
      if (a != "УМ1" && a != "УМ2")
        return WebUtility.HtmlEncode(a);
      var cls = thinkingLevelSuccess == true ? "um-ok" : "um-bad";
      return "<span class=\"" + cls + "\">" + WebUtility.HtmlEncode(a) + "</span>";
    }

    /// <summary>HTML ячейки «Цикл М»: несколько циклов на пульсе — через запятую, цвет по статусу задачи.</summary>
    public static string FormatMainCycleFactCellHtml(ScenarioLogComparer.AggregatedLogSnapshot snap)
    {
      if (snap == null)
        return WebUtility.HtmlEncode("-");
      var segs = snap.MainCycleSegments;
      if (segs == null || segs.Count == 0)
        return WebUtility.HtmlEncode(NormalizeCell(snap.MainCycle ?? ""));
      if (segs.Count == 1)
        return MainCycleIdSpan(segs[0].TaskStatus, segs[0].Id);
      return string.Join(", ", segs.Select(s => MainCycleIdSpan(s.TaskStatus, s.Id)));
    }

    private static string MainCycleIdSpan(string taskStatus, int id)
    {
      string cls = "mc-await";
      if (string.Equals(taskStatus, "NoSolution", StringComparison.Ordinal))
        cls = "mc-ns";
      else if (string.Equals(taskStatus, "Awaiting", StringComparison.Ordinal))
        cls = "mc-await";
      else if (string.Equals(taskStatus, "Solved", StringComparison.Ordinal) ||
               string.Equals(taskStatus, "Completed", StringComparison.Ordinal))
        cls = "mc-solved";
      return "<span class=\"" + cls + "\">" +
             WebUtility.HtmlEncode(id.ToString(CultureInfo.InvariantCulture)) + "</span>";
    }

    /// <summary>
    /// В лог в поле стиля попадает Id образа стиля (<see cref="PerceptionImagesSystem.BehaviorStyleImage"/>).
    /// Для отчёта и сравнения с ожиданиями сценария нужны те же коды, что в StyleCombinations.comb: «1,2,3» (Id стилей по возрастанию).
    /// </summary>
    public static string StyleImageIdToCombinationCodes(string logStyleCell, PerceptionImagesSystem perception)
    {
      var a = NormalizeCell(logStyleCell ?? "");
      if (a == "-" || perception == null)
        return a;

      if (!int.TryParse(a, out int imageId) || imageId <= 0)
        return a;

      try
      {
        var styleImage = perception.GetAllBehaviorStyleImagesList().FirstOrDefault(img => img.Id == imageId);
        if (styleImage?.BehaviorStylesList == null || !styleImage.BehaviorStylesList.Any())
          return "-";

        return string.Join(",", styleImage.BehaviorStylesList.OrderBy(id => id));
      }
      catch
      {
        return "-";
      }
    }

    /// <summary>Переводит поле «Стиль» во всех снимках из Id образа в строку кодов комбинации.</summary>
    public static void RewriteAggregatedStylesToCombinationCodes(
        Dictionary<int, ScenarioLogComparer.AggregatedLogSnapshot> byPulse,
        PerceptionImagesSystem perception)
    {
      if (byPulse == null || perception == null)
        return;
      foreach (var snap in byPulse.Values)
        snap.Style = StyleImageIdToCombinationCodes(snap.Style, perception);
    }
  }
}
