using System;
using System.Globalization;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class NameToReadOnlyConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      // Разрешаем редактирование только для "Имя" и "Описание"
      return value?.ToString() != "Имя" && value?.ToString() != "Описание";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}