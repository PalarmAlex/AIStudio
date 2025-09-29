using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class ParameterValueToIndicatorPositionConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      if (values.Length >= 1 && values[0] is float currentValue)
      {
        double offset = currentValue / 100.0;
        return new Thickness(offset * 180, 0, 0, 0); // 180 - ширина индикатора
      }
      return new Thickness(0);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
