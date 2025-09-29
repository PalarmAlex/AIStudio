using System;
using System.Globalization;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class SecondsToTimeFormatConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value == null || !int.TryParse(value.ToString(), out int totalSeconds))
        return "0:00:00:00";

      TimeSpan timeSpan = TimeSpan.FromSeconds(totalSeconds);
      return $"{timeSpan.Days}:{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
