using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class Level1ToTextConverter : IValueConverter
  {
    private static readonly Dictionary<int, string> Level1Texts = new Dictionary<int, string>
        {
            { -1, "Плохо" },
            { 0, "Норма" },
            { 1, "Хорошо" }
        };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is int level1)
      {
        if (level1 == -1)
          return "Плохо";
        if (level1 == 0)
          return "Норма";
        if (level1 == 1)
          return "Хорошо";
        return "Неизвестно";
      }
      return "Неизвестно";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
