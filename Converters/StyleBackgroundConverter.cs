using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using static ISIDA.Gomeostas.GomeostasSystem;

namespace AIStudio.Converters
{
  public class StyleBackgroundConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      if (values.Length < 2 || !(values[0] is BehaviorStyle currentStyle))
        return new SolidColorBrush(Color.FromArgb(0x30, 0x80, 0x80, 0x80)); // Бледно-серый для неактивных

      var activeStyles = values[1] as IList<BehaviorStyle>;
      if (activeStyles == null || !activeStyles.Any(s => s?.Id == currentStyle.Id))
        return new SolidColorBrush(Color.FromArgb(0x30, 0x80, 0x80, 0x80)); // Бледно-серый для неактивных

      // Более насыщенный голубой цвет для фона активных стилей
      return new SolidColorBrush(Color.FromArgb(0x80, 0x00, 0xBF, 0xFF)); // DeepSkyBlue с прозрачностью 50%
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}