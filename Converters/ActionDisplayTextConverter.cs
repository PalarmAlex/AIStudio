using System;
using System.Globalization;
using System.Windows.Data;
using static ISIDA.Actions.AdaptiveActionsSystem;

namespace AIStudio.Converters
{
  public class ActionDisplayTextConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is AdaptiveAction action)
      {
        int vigorPercentage = (int)((action.CurrentVigor / action.Vigor) * 100);

        // Компактный формат: "Имя [Э%]"
        return $"{action.Name} [{vigorPercentage}%]";
      }

      return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}