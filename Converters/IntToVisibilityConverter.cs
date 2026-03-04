using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class IntToVisibilityConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is int count)
      {
        bool showWhenEmpty = !string.Equals(parameter?.ToString(), "Invert", StringComparison.OrdinalIgnoreCase);
        return showWhenEmpty ? (count == 0 ? Visibility.Visible : Visibility.Collapsed)
                            : (count > 0 ? Visibility.Visible : Visibility.Collapsed);
      }
      return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
