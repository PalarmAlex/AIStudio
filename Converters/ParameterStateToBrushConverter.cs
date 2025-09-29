using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using static ISIDA.Gomeostas.GomeostasSystem;

namespace AIStudio.Converters
{
  public class ParameterStateToBrushConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is ParameterState state)
      {
        switch (state)
        {
          case ParameterState.Normal: return Brushes.Yellow;
          case ParameterState.Bad: return Brushes.Red;
          case ParameterState.Well: return Brushes.Green;
          default: return Brushes.Gray;
        }
      }

      // Обработка случая, если value не является ParameterState (например, null)
      return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}

