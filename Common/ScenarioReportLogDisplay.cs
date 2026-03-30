using System;
using System.Collections.Generic;
using System.Linq;
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
