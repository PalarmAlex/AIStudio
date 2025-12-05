using System;
using System.Globalization;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class StringStartsWithConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is string strValue && parameter is string prefix)
      {
        return strValue.StartsWith(prefix, StringComparison.Ordinal);
      }
      return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}