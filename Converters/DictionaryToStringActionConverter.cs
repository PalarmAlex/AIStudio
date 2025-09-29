using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace AIStudio.Converters
{
  public class DictionaryToStringActionConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is Dictionary<int, int> dict && dict.Any())
      {
        return string.Join("; ", dict.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
      }
      return "нет влияния";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}