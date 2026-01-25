using System;
using System.Globalization;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class BaseConditionToTextConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is int baseCondition)
      {
        switch (baseCondition)
        {
          case -1:
            return "Плохо";
          case 0:
            return "Норма";
          case 1:
            return "Хорошо";
          default:
            return $"Неизвестно ({baseCondition})";
        }
      }
      return "Неизвестно";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
