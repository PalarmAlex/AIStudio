using System;
using System.Globalization;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class BeliefToTextWithTooltipConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is int belief)
      {
        switch (belief)
        {
          case 0:
            return "0";
          case 1:
            return "1";
          case 2:
            return "2";
          default:
            return $"{belief}";
        }
      }
      return "?";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
