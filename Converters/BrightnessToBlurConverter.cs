using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Effects;

namespace AIStudio.Converters
{
  public class BrightnessToBlurConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is double brightness)
      {
        // Преобразуем яркость (0.0-1.0) в радиус размытия (0-10)
        return brightness * 10;
      }
      return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
