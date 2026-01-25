using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AIStudio.Converters
{
  public class UsefulnessToColorConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is int usefulness)
      {
        if (usefulness < 0) return Brushes.Red;
        if (usefulness == 0) return Brushes.Gray;
        if (usefulness > 0) return Brushes.Green;
      }
      return Brushes.Black;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
