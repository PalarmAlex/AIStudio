using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class BoolToStyleConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is bool isActive && parameter is string styles)
      {
        var styleNames = styles.Split(';');
        var activeStyle = Application.Current.TryFindResource(styleNames[0]);
        var inactiveStyle = Application.Current.TryFindResource(styleNames[1]);

        return isActive ? activeStyle : inactiveStyle;
      }
      return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}