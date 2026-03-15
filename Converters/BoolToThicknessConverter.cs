using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AIStudio.Converters
{
  /// <summary>Когда value true — левый отступ (по умолчанию 12), иначе 0. Parameter — верхний отступ (по умолчанию 1).</summary>
  public class BoolToThicknessConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      bool useIndent = value is bool b && b;
      double top = 1;
      if (parameter is string s && double.TryParse(s, NumberStyles.Any, culture, out var p))
        top = p;
      return new Thickness(useIndent ? 12 : 0, top, 0, 0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
