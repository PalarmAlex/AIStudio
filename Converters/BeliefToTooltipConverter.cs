using System;
using System.Globalization;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class BeliefToTooltipConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is int belief)
      {
        switch (belief)
        {
          case 0:
            return "Предположение";
          case 1:
            return "Чужие сведения";
          case 2:
            return "Проверенное собственное знание";
          default:
            return $"Неизвестный код уверенности: {belief}";
        }
      }
      return "Неизвестно";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
