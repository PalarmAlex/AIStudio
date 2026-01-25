using AIStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class AutomatizmActionsToTextConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is AutomatizmsViewModel.ActionsImageDisplay actionsImage)
      {
        var parts = new List<string>();

        if (actionsImage.ActIdList != null && actionsImage.ActIdList.Any())
        {
          parts.Add($"Дей: [{string.Join(",", actionsImage.ActIdList.Take(3))}" +
                   (actionsImage.ActIdList.Count > 3 ? "...]" : "]"));
        }

        if (actionsImage.PhraseIdList != null && actionsImage.PhraseIdList.Any())
        {
          parts.Add($"Фраз: [{string.Join(",", actionsImage.PhraseIdList.Take(2))}" +
                   (actionsImage.PhraseIdList.Count > 2 ? "...]" : "]"));
        }

        if (actionsImage.ToneId != 0)
        {
          parts.Add($"Тон: {actionsImage.ToneId}");
        }

        if (actionsImage.MoodId != 0)
        {
          parts.Add($"Настр: {actionsImage.MoodId}");
        }

        return string.Join("; ", parts);
      }
      return "[нет данных]";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}