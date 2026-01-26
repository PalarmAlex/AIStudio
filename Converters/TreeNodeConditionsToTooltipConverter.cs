using AIStudio.ViewModels;
using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class TreeNodeConditionsToTooltipConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is AutomatizmsViewModel.AutomatizmDisplayItem automatizm)
      {
        var sb = new StringBuilder();
        // Состояние
        sb.AppendLine($"Состояние: {automatizm.BaseConditionText}");

        // Эмоции (стили)
        sb.AppendLine($"Эмоции (стили): {automatizm.EmotionText}");

        // Воздействия с пульта
        sb.AppendLine($"Воздействия с пульта: {automatizm.InfluenceActionsText}");

        // Тон и настроение
        if (!string.IsNullOrEmpty(automatizm.ToneMoodText))
          sb.AppendLine($"Тон/Настроение: {automatizm.ToneMoodText}");
        else
          sb.AppendLine("Тон/Настроение: Нормальное");

        // Вербальный образ (фразы)
        if (!string.IsNullOrEmpty(automatizm.VerbalText))
          sb.AppendLine($"Вербальный образ: {automatizm.VerbalText}");
        else
          sb.AppendLine("Вербальный образ: Нет фраз");

        // Первый символ
        if (automatizm.SimbolID > 0)
          sb.AppendLine($"Первый символ: ID {automatizm.SimbolID}");
        else
          sb.AppendLine("Первый символ: Нет");

        return sb.ToString();
      }
      return "Нет данных об условиях запуска";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}