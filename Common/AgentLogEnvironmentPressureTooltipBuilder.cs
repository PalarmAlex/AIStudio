using System.Linq;
using System.Text;
using ISIDA.Actions;
using ISIDA.Common;

namespace AIStudio.Common
{
  /// <summary>Сборка подсказки колонки «Среда» без привязки к LiveLogsViewModel (для ToolTip в popup).</summary>
  public static class AgentLogEnvironmentPressureTooltipBuilder
  {
    public static string Build(string cellRaw, string storedTooltip)
    {
      if (!string.IsNullOrWhiteSpace(cellRaw) && cellRaw.Trim() != "-")
      {
        string fromCell = BuildFromCell(cellRaw);
        if (!string.IsNullOrWhiteSpace(fromCell))
          return fromCell;
      }

      if (!string.IsNullOrWhiteSpace(storedTooltip))
        return TooltipMultilineText.FormatEnvironmentPressureTooltip(storedTooltip.Trim());

      return "На этом пульсе давление метрик среды не применялось";
    }

    private static string BuildFromCell(string cellRaw)
    {
      var segments = AgentLogEnvironmentPressureSegment.ParseCell(cellRaw);
      if (segments.Count == 0)
        return null;
      if (!InfluenceActionSystem.IsInitialized)
        return BuildFromCellWithoutCatalog(segments);

      var all = InfluenceActionSystem.Instance.GetAllInfluenceActions();
      var sb = new StringBuilder();
      foreach (var seg in segments)
      {
        var action = all.FirstOrDefault(a => a.Id == seg.ActionId);
        string block = TooltipMultilineText.FormatEnvironmentMetricBlock(
            seg.ActionId,
            seg.SignedMagnitudeText,
            action?.Name,
            action?.Description);
        if (sb.Length > 0)
          sb.AppendLine();
        sb.Append(block);
      }
      return sb.ToString().TrimEnd();
    }

    private static string BuildFromCellWithoutCatalog(
        System.Collections.Generic.List<AgentLogEnvironmentPressureSegment> segments)
    {
      var sb = new StringBuilder();
      foreach (var seg in segments)
      {
        string block = TooltipMultilineText.FormatEnvironmentMetricBlock(
            seg.ActionId,
            seg.SignedMagnitudeText,
            null,
            null);
        if (sb.Length > 0)
          sb.AppendLine();
        sb.Append(block);
      }
      return sb.ToString().TrimEnd();
    }
  }
}
