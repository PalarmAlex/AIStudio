using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AIStudio.Converters
{
  /// <summary>
  /// Конвертер для преобразования строки в целое число с обработкой пустой строки
  /// Пустая строка преобразуется в 0
  /// </summary>
  public class StringToIntConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is int intValue)
      {
        // Если значение 0, возвращаем пустую строку
        return intValue == 0 ? string.Empty : intValue.ToString();
      }
      return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is string str)
      {
        // Если строка пустая или состоит только из пробелов, возвращаем 0
        if (string.IsNullOrWhiteSpace(str))
          return 0;

        // Пытаемся преобразовать строку в число
        if (int.TryParse(str.Trim(), out int result))
        {
          // Проверяем, что число не отрицательное (если это ID)
          return result >= 0 ? result : 0;
        }
        
        // Если преобразование не удалось, возвращаем UnsetValue
        return DependencyProperty.UnsetValue;
      }
      
      // Если значение не строка, возвращаем 0
      return 0;
    }
  }
}