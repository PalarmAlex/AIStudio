using System;
using System.Globalization;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class ParametersStylesColumnsConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is int cellCount && cellCount > 0)
      {
        // Для матрицы параметров-стилей всегда 8 столбцов:
        // 1 столбец для имен параметров + 7 столбцов для зон
        return 8;
      }
      return 8; // Значение по умолчанию
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
