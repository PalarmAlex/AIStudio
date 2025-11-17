using System;
using System.Globalization;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class MatrixColumnsConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is int count && count > 0)
      {
        // Для матрицы размером n x n общее количество ячеек = n²
        // Находим n как квадратный корень из общего количества
        int matrixSize = (int)Math.Sqrt(count);
        return matrixSize;
      }
      return 1;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}