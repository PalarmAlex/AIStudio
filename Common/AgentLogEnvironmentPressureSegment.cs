using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace AIStudio.Common
{
  /// <summary>Фрагмент колонки «Среда»: id метрики и величина давления со знаком.</summary>
  public sealed class AgentLogEnvironmentPressureSegment
  {
    private static readonly Brush PositiveBackground = CreateFrozen(64, 0, 255, 0);
    private static readonly Brush NegativeBackground = CreateFrozen(64, 255, 0, 0);

    private static SolidColorBrush CreateFrozen(byte a, byte r, byte g, byte b)
    {
      var br = new SolidColorBrush(Color.FromArgb(a, r, g, b));
      br.Freeze();
      return br;
    }

    public AgentLogEnvironmentPressureSegment(int actionId, int signedMagnitude, bool showLeadingComma)
    {
      ActionId = actionId;
      SignedMagnitude = signedMagnitude;
      ShowLeadingComma = showLeadingComma;
      SignedMagnitudeText = signedMagnitude > 0 ? "+" + signedMagnitude : signedMagnitude.ToString(CultureInfo.InvariantCulture);
      DisplayText = $"{actionId}:{SignedMagnitudeText}";
      CellBackground = signedMagnitude > 0 ? PositiveBackground : signedMagnitude < 0 ? NegativeBackground : null;
    }

    public int ActionId { get; }
    public int SignedMagnitude { get; }
    public string SignedMagnitudeText { get; }
    public string DisplayText { get; }
    public bool ShowLeadingComma { get; }
    public Brush CellBackground { get; }

    public static List<AgentLogEnvironmentPressureSegment> ParseCell(string cellRaw)
    {
      var list = new List<AgentLogEnvironmentPressureSegment>();
      if (string.IsNullOrWhiteSpace(cellRaw) || cellRaw.Trim() == "-")
        return list;
      var parts = cellRaw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
      bool first = true;
      foreach (var part in parts)
      {
        var t = part.Trim();
        var m = Regex.Match(t, @"^(\d+)\s*:\s*([+\-]?\d+)$");
        if (!m.Success)
          continue;
        if (!int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
          continue;
        if (!int.TryParse(m.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int mag))
          continue;
        list.Add(new AgentLogEnvironmentPressureSegment(id, mag, !first));
        first = false;
      }
      return list;
    }
  }
}
