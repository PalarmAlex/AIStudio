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
      if (values.Length < 3 || !(values[1] is BehaviorStyle currentStyle))
        return new SolidColorBrush(Color.FromArgb(0x60, 0x80, 0x80, 0x80)); // Неактивные - серые

      var activeStyles = values[2] as IList<BehaviorStyle>;
      if (activeStyles == null || !activeStyles.Any(s => s?.Id == currentStyle.Id))
        return new SolidColorBrush(Color.FromArgb(0x60, 0x80, 0x80, 0x80));

      int currentWeight = (values[0] as int?) ?? 0;
      int maxWeight = activeStyles.Max(s => s?.Weight ?? 0);

      if (maxWeight == 0) return Brushes.Violet; // На случай если все веса 0

      double ratio = (double)currentWeight / maxWeight;

      // Цветовая шкала по спектру (от красного к фиолетовому)
      Color color = CalculateSpectrumColor(ratio);
      return new SolidColorBrush(color);
    }

    private Color CalculateSpectrumColor(double ratio)
    {
      // Нормализованный спектр (0.0-1.0 соответствует 620-380 нм)
      if (ratio < 0.25)
      {
        // Красный -> Оранжевый (620-590 нм)
        return Color.FromRgb(255, (byte)(ratio * 4 * 255), 0);
      }
      else if (ratio < 0.5)
      {
        // Оранжевый -> Желтый (590-570 нм)
        return Color.FromRgb(255, 255, 0);
      }
      else if (ratio < 0.75)
      {
        // Желтый -> Зеленый (570-495 нм)
        byte red = (byte)(255 * (1 - (ratio - 0.5) * 4));
        return Color.FromRgb(red, 255, 0);
      }
      else
      {
        // Зеленый -> Синий -> Фиолетовый (495-380 нм)
        byte green = (byte)(255 * (1 - (ratio - 0.75) * 4));
        byte blue = (byte)(255 * (ratio - 0.75) * 4);
        return Color.FromRgb(0, green, blue);
      }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
