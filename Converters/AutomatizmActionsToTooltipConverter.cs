using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using AIStudio.ViewModels;

namespace AIStudio.Converters
{
  public class AutomatizmActionsToTooltipConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is AutomatizmsViewModel.ActionsImageDisplay actionsImage)
      {
        var sb = new StringBuilder();

        if (actionsImage.ActIdList != null && actionsImage.ActIdList.Any())
        {
          sb.AppendLine($"Действия ({actionsImage.ActIdList.Count}): {string.Join(", ", actionsImage.ActIdList)}");
        }
        else
        {
          sb.AppendLine("Действия: нет");
        }

        if (actionsImage.PhraseIdList != null && actionsImage.PhraseIdList.Any())
        {
          sb.AppendLine($"Фразы ({actionsImage.PhraseIdList.Count}): {string.Join(", ", actionsImage.PhraseIdList)}");
        }
        else
        {
          sb.AppendLine("Фразы: нет");
        }

        if (actionsImage.ToneId != 0)
        {
          sb.AppendLine($"Тон: {actionsImage.ToneId}");
        }
        else
        {
          sb.AppendLine("Тон: 0 (нормальный)");
        }

        if (actionsImage.MoodId != 0)
        {
          sb.AppendLine($"Настроение: {actionsImage.MoodId}");
        }
        else
        {
          sb.AppendLine("Настроение: 0 (нормальное)");
        }

        return sb.ToString();
      }
      return "Нет данных об образе действий";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
