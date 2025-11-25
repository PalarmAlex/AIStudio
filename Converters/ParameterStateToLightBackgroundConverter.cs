using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ISIDA.Gomeostas;

namespace AIStudio.Converters
{
  public class ParameterStateToLightBackgroundConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is GomeostasSystem.ParameterState state)
      {
        switch (state)
        {
          case GomeostasSystem.ParameterState.Bad:
            return new SolidColorBrush(Color.FromArgb(0xFF, 0xF8, 0xE0, 0xE0)); // Светло-красный
          case GomeostasSystem.ParameterState.Normal:
            return new SolidColorBrush(Color.FromArgb(0xFF, 0xF0, 0xF0, 0xF0)); // Светло-серый (стандартный)
          case GomeostasSystem.ParameterState.Well:
            return new SolidColorBrush(Color.FromArgb(0xFF, 0xE0, 0xF8, 0xE0)); // Светло-зеленый
          default:
            return new SolidColorBrush(Color.FromArgb(0xFF, 0xF0, 0xF0, 0xF0)); // По умолчанию
        }
      }
      return new SolidColorBrush(Color.FromArgb(0xFF, 0xF0, 0xF0, 0xF0));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
