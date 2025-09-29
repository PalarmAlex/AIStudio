using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Collections.Generic;
using ISIDA.Actions; // Убедитесь, что это правильное пространство имен

namespace AIStudio.Converters
{
  /// <summary>
  /// Конвертер для суммирования положительных значений влияний действия.
  /// </summary>
  public class PositiveInfluenceSumConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is Dictionary<int, int> influences)
      {
        // Суммируем только положительные влияния
        return influences.Values.Where(v => v > 0).Sum();
      }
      return 0; // fallback
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}