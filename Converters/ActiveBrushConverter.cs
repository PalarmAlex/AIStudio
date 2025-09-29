using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace AIStudio.Converters
{
  public class ActiveBrushConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is bool isActive && isActive)
      {
        // Градиентный кисть для активных действий
        var gradient = new LinearGradientBrush();
        gradient.StartPoint = new System.Windows.Point(0, 0);
        gradient.EndPoint = new System.Windows.Point(1, 1);
        gradient.GradientStops.Add(new GradientStop(Colors.LightGreen, 0.0));
        gradient.GradientStops.Add(new GradientStop(Colors.Green, 1.0));
        return gradient;
      }
      return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
