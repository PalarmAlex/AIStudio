using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AIStudio.Converters
{
  public class ParameterGradientConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      if (values.Length >= 2 && values[0] is int normaWell && values[1] is int speed)
      {
        var gradient = new LinearGradientBrush
        {
          StartPoint = new System.Windows.Point(0, 0.5),
          EndPoint = new System.Windows.Point(1, 0.5)
        };

        // Для отрицательного Speed: Красный -> Желтый -> Зеленый
        if (speed < 0)
        {
          gradient.GradientStops.Add(new GradientStop(Colors.Red, 0));
          gradient.GradientStops.Add(new GradientStop(Colors.Yellow, normaWell / 100.0));
          gradient.GradientStops.Add(new GradientStop(Colors.Green, 1));
        }
        // Для положительного Speed: Зеленый -> Желтый -> Красный
        else
        {
          gradient.GradientStops.Add(new GradientStop(Colors.Green, 0));
          gradient.GradientStops.Add(new GradientStop(Colors.Yellow, (100 - normaWell) / 100.0));
          gradient.GradientStops.Add(new GradientStop(Colors.Red, 1));
        }

        return gradient;
      }
      return new LinearGradientBrush(Colors.Gray, Colors.Gray, 0);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
