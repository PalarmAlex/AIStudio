using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class ListToStringConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is IEnumerable<int> ids)
      {
        return string.Join(", ", ids);
      }
      return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is string str)
      {
        try
        {
          return str.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
              .Select(s =>
              {
                if (int.TryParse(s.Trim(), out int num))
                  return num;
                throw new FormatException($"Некорректное число: '{s}'");
              })
              .ToList();
        }
        catch (FormatException)
        {
          // Возвращаем Binding.DoNothing, чтобы WPF не применял некорректное значение
          return DependencyProperty.UnsetValue;
        }
      }
      return new List<int>();
    }
  }
}
