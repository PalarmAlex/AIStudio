using System;
using System.Globalization;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class BoolToAngleConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return (value is bool && (bool)value) ? 180 : 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}
