using AIStudio.ViewModels;
using ISIDA.Psychic;
using ISIDA.Reflexes;
using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class TreeNodeConditionsToTooltipConverter : IValueConverter
  {
    /// <summary>Текст подсказки условий узла дерева (как в таблице автоматизмов).</summary>
    public static string FormatConditionsTooltip(AutomatizmsViewModel.AutomatizmDisplayItem automatizm)
    {
      if (automatizm == null)
        return "Нет данных об условиях запуска";

      var sb = new StringBuilder();
      sb.AppendLine($"Состояние: {automatizm.BaseConditionText}");
      sb.AppendLine($"Эмоции (стили): {automatizm.EmotionText}");
      sb.AppendLine($"Воздействия с пульта: {automatizm.InfluenceActionsText}");
      AppendToneAndMoodLines(sb, automatizm);

      if (!string.IsNullOrEmpty(automatizm.VerbalText))
        sb.AppendLine($"Вербальный образ: {automatizm.VerbalText}");
      else
        sb.AppendLine("Вербальный образ: Нет фраз");

      if (automatizm.SimbolID > 0)
        sb.AppendLine($"Первый символ: ID {automatizm.SimbolID}");
      else
        sb.AppendLine("Первый символ: Нет");

      int visualCode = AgentVisualColor.IsValidCode(automatizm.VisualID)
          ? automatizm.VisualID
          : AgentVisualColor.White;
      sb.AppendLine($"Цветовой фон: {AgentVisualColor.GetDisplayName(visualCode)}");

      return sb.ToString().TrimEnd();
    }

    private static void AppendToneAndMoodLines(StringBuilder sb, AutomatizmsViewModel.AutomatizmDisplayItem automatizm)
    {
      if (automatizm.ToneMoodID >= 100 && automatizm.ToneMoodID <= 307)
      {
        try
        {
          var (tone, mood) = PsychicSystem.GetToneMoodFromID(automatizm.ToneMoodID);
          string tStr = PsychicSystem.GetToneString(tone);
          string mStr = PsychicSystem.GetMoodString(mood);
          sb.AppendLine(string.IsNullOrEmpty(tStr) ? "Тон: —" : "Тон: " + tStr);
          sb.AppendLine(string.IsNullOrEmpty(mStr) ? "Настроение: —" : "Настроение: " + mStr);
        }
        catch
        {
          sb.AppendLine("Тон: —");
          sb.AppendLine("Настроение: —");
        }
      }
      else if (!string.IsNullOrEmpty(automatizm.ToneMoodText))
      {
        sb.AppendLine("Тон: " + automatizm.ToneMoodText);
        sb.AppendLine("Настроение: —");
      }
      else
      {
        sb.AppendLine("Тон: —");
        sb.AppendLine("Настроение: —");
      }
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is AutomatizmsViewModel.AutomatizmDisplayItem automatizm)
        return FormatConditionsTooltip(automatizm);
      return "Нет данных об условиях запуска";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}