using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using static ISIDA.Gomeostas.GomeostasSystem;

namespace AIStudio.Converters
{
  public class StyleColorConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      if (values.Length < 2 || !(values[0] is BehaviorStyle currentStyle))
        return new SolidColorBrush(Colors.Gray); // Неактивные - серые

      var activeStyles = values[1] as IList<BehaviorStyle>;
      if (activeStyles == null || !activeStyles.Any(s => s?.Id == currentStyle.Id))
        return new SolidColorBrush(Colors.Gray);

      // Синий цвет для рамки активных стилей
      return new SolidColorBrush(Colors.Blue);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}