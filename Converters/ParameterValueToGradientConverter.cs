using System;
using System.Globalization;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class ParameterValueToGradientConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is int normaWell)
      {
        return normaWell / 100.0;
      }
      return 0.5;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
